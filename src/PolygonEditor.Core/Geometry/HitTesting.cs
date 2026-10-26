using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Geometry;

// Wynik kliknięcia: wskazuje wielokąt oraz ewentualnie jego wierzchołek lub krawędź.
// Vertex i Edge równe null wskazują cały wielokąt, np. po kliknięciu jego wnętrza.
// Przechowuje istniejące obiekty sceny, bez kopiowania ich geometrii.
public sealed record HitResult(Polygon Polygon, Vertex? Vertex = null, Edge? Edge = null);

// Sprawdza, który element sceny znajduje się pod kursorem lub blisko niego.
public static class HitTesting
{
    // Szuka kolejno wierzchołka, krawędzi i wnętrza; później rysowane obiekty mają pierwszeństwo.
    // Zwraca: opis trafienia albo null, gdy kliknięto puste miejsce.
    public static HitResult? Pick(Scene scene, Point2D point, double tolerance = 8)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (!double.IsFinite(tolerance) || tolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(tolerance));

        // Znaczniki są nad wszystkimi krawędziami; kolejność trafiania odpowiada rysowaniu.
        for (int i = scene.Polygons.Count - 1; i >= 0; i--)
        {
            var polygon = scene.Polygons[i];
            foreach (var vertex in polygon.Vertices.Reverse())
                if (DistanceSquared(point, vertex.Position) <= tolerance * tolerance)
                    return new HitResult(polygon, Vertex: vertex);
        }
        for (int i = scene.Polygons.Count - 1; i >= 0; i--)
        {
            var polygon = scene.Polygons[i];
            foreach (var edge in polygon.Edges.Reverse())
                if (DistanceToSegmentSquared(point, edge.Start.Position, edge.End.Position) <= tolerance * tolerance)
                    return new HitResult(polygon, Edge: edge);
        }
        for (int i = scene.Polygons.Count - 1; i >= 0; i--)
            if (Contains(scene.Polygons[i], point))
                return new HitResult(scene.Polygons[i]);
        return null;
    }

    // Zwraca kwadrat odległości dwóch punktów, do porównania z tolerancją nie potrzeba pierwiastka.
    public static double DistanceSquared(Point2D a, Point2D b)
        => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

    // Znajduje najbliższe miejsce na odcinku, uwzględniając także jego końce.
    // Zwraca: kwadrat odległości punktu od odcinka prostopadle do niego,
    // jeśli rzut wypada poza odcinkiem - odległość od najbliższego końca odcinka
    public static double DistanceToSegmentSquared(Point2D point, Point2D start, Point2D end)
    {
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        double lengthSquared = dx * dx + dy * dy;
        // odcinek o zerowej długości traktujemy jak pojedynczy punkt.
        if (lengthSquared == 0)
            return DistanceSquared(point, start);

        // iloczyn skalarny wektora od początku odcinka do kursora i wektora odcinka
        double dotProduct = (point.X - start.X) * dx + (point.Y - start.Y) * dy;
        // rzut na prostą ograniczamy do odcinka: parametr t należy do [0, 1]
        double t = Math.Clamp(dotProduct / lengthSquared, 0, 1);
        return DistanceSquared(point, new Point2D(start.X + t * dx, start.Y + t * dy));
    }

    // Sprawdza wnętrze regułą parzystości: liczymy przecięcia półprostej w prawo z obwodem.
    // Zwraca: true dla nieparzystej liczby przecięć. Granicę wcześniej sprawdza Pick.
    public static bool Contains(Polygon polygon, Point2D point)
    {
        bool inside = false;
        foreach (var edge in polygon.Edges)
        {
            var a = edge.Start.Position;
            var b = edge.End.Position;
            // Reguła parzystości: półprosta w prawo. Półotwarty przedział Y
            // zapobiega dwukrotnemu liczeniu wspólnego wierzchołka, pomija odcinki poziome
            if ((a.Y > point.Y) != (b.Y > point.Y))
            {
                // fraction - jaką część drogi od A do B trzeba przejść, żeby osiągnąć Y kursora
                // crossingX - przechodzimy tę samą część drogi we współrzędnej X
                double fraction = (point.Y - a.Y) / (b.Y - a.Y);
                double crossingX = a.X + fraction * (b.X - a.X);
                // jeśli przecięcie leży po prawej stronie kursora, liczymy je, odwracając stan
                if (point.X < crossingX)
                    inside = !inside;
            }
        }
        return inside;
    }
}
