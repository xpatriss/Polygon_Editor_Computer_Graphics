using PolygonEditor.Core.Constraints;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Solving;

// solver: kolejno zmniejsza błędy relacji, aż wszystkie mieszczą się w tolerancjach lub osiągnie limit iteracji
public static partial class ConstraintSolver
{
    // dopuszczalna różnica między aktualną a wymaganą długością, w jednostkach sceny
    public const double LengthTolerance = 0.0001;
    // dopuszczalny błąd równoległości, w radianach
    public const double AngleTolerance = 0.000001;
    // maksymalna liczba przejść przez całą listę relacji
    private const int MaximumIterations = 400;
    // bardzo krótki odcinek traktujemy jako pozbawiony użytecznego kierunku
    private const double MinimumLength = 0.00000001;

    // Punkty przypięte, np. pod kursorem, pozostają nieruchome. Nieudana próba cofa wszystkie korekty.
    // Można podać próbny zestaw relacji, zanim zostanie zapisany w scenie.
    public static bool TrySolve(Scene scene, IReadOnlySet<ScenePoint>? pinnedVertices = null,
        IReadOnlyList<GeometricConstraint>? constraints = null)
    { 
        // jeśli nie przekazano próbnego zestawu, korzystamy z relacji zapisanych w scenie
        constraints ??= scene.Constraints;
        // słownik Id -> obiekt krawędzi 
        var edges = scene.Polygons.SelectMany(polygon => polygon.Edges).ToDictionary(edge => edge.Id);
        // kopia pozycji z wejścia do solvera, już po ustawieniu żądanego ruchu przez SceneEditor
        var before = SceneGeometry.CapturePositions(scene);
        if (!ValidateArcRules(scene, out _)) return false;
        // w jednej iteracji próbujemy poprawić kolejno każdą relację
        for (int iteration = 0; iteration < MaximumIterations; iteration++)
        {
            foreach (var constraint in constraints)
            {
                // liczymy aktualny błąd relacji i wpływ jej punktów na ten błąd
                var equation = Evaluate(constraint, edges);
                // null oznacza brak poprawnego równania
                // false z Correct oznacza nieudaną korektę
                if (equation is null || !Correct(equation, constraint is ParallelConstraint, pinnedVertices))
                {
                    // wycofujemy wszystkie poprawki tej próby, także dotyczące wcześniejszych relacji
                    SceneGeometry.RestorePositions(before);
                    return false;
                }
            }
            if (!UpdateSemicircleCenters(scene, pinnedVertices))
            {
                SceneGeometry.RestorePositions(before);
                return false;
            }
            foreach (var equation in ArcEquations(scene))
            {
                if (equation is null || !Correct(equation, false, pinnedVertices))
                {
                    SceneGeometry.RestorePositions(before);
                    return false;
                }
            }
            // Ciągłość jest częścią tego samego uzgadniania co długość i równoległość.
            foreach (var joint in ContinuityJoints(scene))
            {
                foreach (var equation in EvaluateContinuity(joint.Vertex, joint.Incoming, joint.Outgoing))
                {
                    if (equation is null || !Correct(equation, joint.Vertex.Continuity == ContinuityClass.G1, pinnedVertices))
                    {
                        SceneGeometry.RestorePositions(before);
                        return false;
                    }
                }
            }
            // before.Keys to wierzchołki i uchwyty: sprawdzamy ich aktualne pozycje, nie zapisaną kopię
            // NaN lub nieskończoność oznacza niepoprawny wynik obliczeń - kończymy próby
            if (before.Keys.Any(vertex => !double.IsFinite(vertex.Position.X) || !double.IsFinite(vertex.Position.Y)))
                break;
            // sprawdzamy wszystkie relacje ponownie: ostatnia korekta mogła naruszyć wcześniejsze.
            if (constraints.All(constraint => IsSatisfied(Evaluate(constraint, edges))) && ContinuitiesSatisfied(scene)
                && ArcShapesSatisfied(scene))
                return true;
        }
        // minął limit albo pojawiły się niepoprawne liczby - przywracamy stan z wejścia do solvera
        SceneGeometry.RestorePositions(before);
        return false;
    }

    // Tylko sprawdza wszystkie relacje, bez przesuwania punktów
    public static bool AreSatisfied(Scene scene)
    {
        var edges = scene.Polygons.SelectMany(polygon => polygon.Edges).ToDictionary(edge => edge.Id);
        // All zwraca true, jeśli każda relacja spełnia swoją tolerancję; dla pustej listy też true
        return ValidateArcRules(scene, out _) && scene.Constraints.All(constraint => IsSatisfied(Evaluate(constraint, edges)))
            && ContinuitiesSatisfied(scene) && ArcShapesSatisfied(scene);
    }

    // Równanie musi istnieć, błąd być skończony, a jego wartość bezwzględna mieścić się w tolerancji.
    private static bool IsSatisfied(Equation? equation)
        => equation is not null && double.IsFinite(equation.Error) && Math.Abs(equation.Error) <= equation.Tolerance;

    private static Equation? Evaluate(GeometricConstraint constraint, IReadOnlyDictionary<Guid, Edge> edges)
    {
        // sprawdzamy, czy każda krawędź wymieniona w relacji nadal istnieje w scenie
        if (constraint.EdgeIds.Any(id => !edges.ContainsKey(id) || edges[id].Kind != EdgeKind.Line))
            return null;
        // wybieramy sposób obliczenia błędu według typu relacji; nie zmieniamy geometrii
        return constraint switch
        {
            LengthConstraint length => EvaluateLength(edges[length.EdgeId], length.TargetLength),
            ParallelConstraint parallel => EvaluateParallel(edges[parallel.FirstEdgeId], edges[parallel.SecondEdgeId]),
            _ => null
        };
    }

    // Błąd długości to długość aktualna minus wymagana. Gradient wskazuje kierunek zmiany błędu.
    private static Equation EvaluateLength(Edge edge, double target)
    {
        // dx i dy to składowe wektora od początku do końca krawędzi
        double dx = edge.End.Position.X - edge.Start.Position.X;
        double dy = edge.End.Position.Y - edge.Start.Position.Y;
        // aktualna długość odcinka z twierdzenia Pitagorasa; target to długość wymagana
        double length = Math.Sqrt(dx * dx + dy * dy);
        // ux i uy to kierunek odcinka przeskalowany do długości 1
        // złożony do punktu odcinek rozwijamy w osi X, bo nie ma własnego kierunku
        double ux = length > MinimumLength ? dx / length : 1;
        double uy = length > MinimumLength ? dy / length : 0;
        // słownik wierzchołek -> wpływ zmiany jego X i Y na błąd
        var gradients = new Dictionary<ScenePoint, (double X, double Y)>();
        // ruch początku wzdłuż odcinka skraca go, a ruch końca w tym kierunku go wydłuża
        AddGradient(gradients, edge.Start, -ux, -uy);
        AddGradient(gradients, edge.End, ux, uy);
        // dodatni błąd: za długi odcinek
        // ujemny: za krótki
        // zero: wymagana długość
        return new Equation(length - target, LengthTolerance, gradients);
    }

    // Błąd to kąt między kierunkami, sprowadzony do [-pi/2, pi/2]; przeciwny zwrot też jest równoległy.
    private static Equation? EvaluateParallel(Edge first, Edge second)
    {
        // u = (ux, uy) to wektor pierwszej krawędzi, v = (vx, vy) drugiej
        double ux = first.End.Position.X - first.Start.Position.X;
        double uy = first.End.Position.Y - first.Start.Position.Y;
        double vx = second.End.Position.X - second.Start.Position.X;
        double vy = second.End.Position.Y - second.Start.Position.Y;
        // kwadraty długości obu wektorów
        double firstSquared = ux * ux + uy * uy;
        double secondSquared = vx * vx + vy * vy;
        // przy praktycznie zerowej długości nie umiemy określić kierunku ani równoległości - return null
        if (firstSquared < MinimumLength * MinimumLength || secondSquared < MinimumLength * MinimumLength)
            return null;
        // (wyznacznik ux*vy - uy*vx, iloczyn skalarny ux*vx + uy*vy)
        // angle to kąt ze znakiem od u do v, w radianach; pi radianów to 180 stopni
        double angle = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
        // wybieramy bliższą równoległość: ten sam lub przeciwny zwrot wektorów
        // Np. 170 stopni zmieniamy na -10, a 180 na 0; nie wymagamy zgodnego zwrotu
        if (angle > Math.PI / 2)
            angle -= Math.PI;
        else if (angle < -Math.PI / 2)
            angle += Math.PI;

        var gradients = new Dictionary<ScenePoint, (double X, double Y)>();
        // wpisy mówią, jak mały ruch każdego końca w X/Y zmieni błąd kąta
        // to pochodne różnicy kątów; początek ma przeciwny znak niż koniec, bo wektor = koniec - początek.
        AddGradient(gradients, first.Start, -uy / firstSquared, ux / firstSquared);
        AddGradient(gradients, first.End, uy / firstSquared, -ux / firstSquared);
        AddGradient(gradients, second.Start, vy / secondSquared, -vx / secondSquared);
        AddGradient(gradients, second.End, -vy / secondSquared, vx / secondSquared);

        return new Equation(angle, AngleTolerance, gradients);
    }

    // Wspólny wierzchołek może wystąpić w obu krawędziach: jego pochodne trzeba zsumować
    private static void AddGradient(Dictionary<ScenePoint, (double X, double Y)> gradients,
        ScenePoint vertex, double x, double y)
    {
        // x i y to dodawane składowe gradientu
        // odczytujemy wcześniejszy wkład tego punktu w tej relacji; brak wpisu daje (0, 0)
        gradients.TryGetValue(vertex, out var previous);
        // sumujemy wkłady, bo wspólny punkt należy do obu wektorów tej samej relacji
        gradients[vertex] = (previous.X + x, previous.Y + y);
    }

    // Rozdziela korektę między wolne punkty proporcjonalnie do wpływu na błąd relacji.
    private static bool Correct(Equation equation, bool angular, IReadOnlySet<ScenePoint>? pinned)
    {
        // jeśli relacja już pasuje, nie trzeba niczego przesuwać
        if (IsSatisfied(equation))
            return true;
        // nie da się wyznaczyć poprawnej korekty z NaN lub nieskończoności
        if (!double.IsFinite(equation.Error))
            return false;
        // denominator to suma kwadratów składowych gradientów tylko wolnych punktów
        double denominator = 0;
        foreach (var (vertex, gradient) in equation.Gradients)
            // punkt liczymy jako wolny, gdy nie należy do pinned; null oznacza brak przypięć
            if (pinned?.Contains(vertex) != true)
                denominator += gradient.X * gradient.X + gradient.Y * gradient.Y;
        // za mały mianownik: np. wszystkie punkty są przypięte albo gradienty się wyzerowały
        // nie dzielimy przez zero lub niepoprawną liczbę; ta próba korekty się nie udaje
        if (denominator < 1e-24 || !double.IsFinite(denominator))
            return false;

        // angular mówi, czy poprawiamy kąt
        // error to błąd użyty w tym kroku korekty
        // małe kroki kąta ograniczają gwałtowną zmianę geometrii przy dużym obrocie
        double error = angular ? Math.Clamp(equation.Error, -0.25, 0.25) : equation.Error;
        // factor skaluje gradient, znak -error kieruje błąd ku zeru: dodatni zmniejszamy, ujemny zwiększamy
        // to lokalne przybliżenie korekty, dlatego po przesunięciu ponownie sprawdzamy relacje
        double factor = -error / denominator;
        foreach (var (vertex, gradient) in equation.Gradients)
        {
            // pomijamy punkty przypięte, np. aktywny wierzchołek pod kursorem
            if (pinned?.Contains(vertex) == true)
                continue;
            // do aktualnej pozycji dodajemy korektę X i Y: factor razy odpowiednia składowa gradientu
            var position = vertex.Position;
            vertex.Position = new Point2D(position.X + factor * gradient.X, position.Y + factor * gradient.Y);
        }
        // true oznacza wykonaną korektę (albo brak potrzeby ruchu), nie sukces całego solvera
        return true;
    }

    // Jeden warunek skalarny: jego błąd, tolerancja oraz wpływ współrzędnych poszczególnych punktów.
    // Error: długość minus cel albo kąt
    // Tolerance: dopuszczalny błąd tego typu relacji
    // Gradients: dla każdego wierzchołka para pochodnych błędu po jego X i Y
    private sealed record Equation(double Error, double Tolerance,
        Dictionary<ScenePoint, (double X, double Y)> Gradients);
}
