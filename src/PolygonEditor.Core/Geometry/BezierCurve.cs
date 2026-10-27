namespace PolygonEditor.Core.Geometry;
public static class BezierCurve
{
    // Wzór Bernsteina do odczytu pojedynczego punktu i niezależnego sprawdzania próbkowania.
    public static Point2D Evaluate(Point2D p0, Point2D p1, Point2D p2, Point2D p3, double t)
    {
        if (!double.IsFinite(t) || t < 0 || t > 1)
            throw new ArgumentOutOfRangeException(nameof(t));
        double s = 1 - t;
        double b0 = s * s * s, b1 = 3 * t * s * s, b2 = 3 * t * t * s, b3 = t * t * t;
        return new Point2D(b0 * p0.X + b1 * p1.X + b2 * p2.X + b3 * p3.X,
            b0 * p0.Y + b1 * p1.Y + b2 * p2.Y + b3 * p3.Y);
    }

    // Kolejne punkty liczymy w pętli wyłącznie przez dodawanie różnic, osobno dla X i Y.
    public static IEnumerable<Point2D> Sample(Point2D p0, Point2D p1, Point2D p2, Point2D p3, int steps)
    {
        if (steps < 1)
            throw new ArgumentOutOfRangeException(nameof(steps));
        var x = InitialDifferences(p0.X, p1.X, p2.X, p3.X, steps);
        var y = InitialDifferences(p0.Y, p1.Y, p2.Y, p3.Y, steps);
        for (int step = 0; step <= steps; step++)
        {
            // Dokładny koniec zapobiega widocznej szczelinie od błędu numerycznego.
            yield return step == steps ? p3 : new Point2D(x.Value, y.Value);
            if (step == steps)
                yield break;
            x.Value += x.First;
            x.First += x.Second;
            x.Second += x.Third;
            y.Value += y.First;
            y.First += y.Second;
            y.Second += y.Third;
        }
    }

    private static (double Value, double First, double Second, double Third) InitialDifferences(
        double p0, double p1, double p2, double p3, int steps)
    {
        // Współczynniki a*t³ + b*t² + c*t + d po przejściu z bazy Bernsteina.
        double a = -p0 + 3 * p1 - 3 * p2 + p3;
        double b = 3 * p0 - 6 * p1 + 3 * p2;
        double c = 3 * (p1 - p0);
        double d = p0;
        double h = 1.0 / steps, h2 = h * h, h3 = h2 * h;
        return (d, a * h3 + b * h2 + c * h, 6 * a * h3 + 2 * b * h2, 6 * a * h3);
    }

    // Podział de Casteljau w t=0.5; przed uzgodnieniem ciągłości zachowuje dokładny kształt krzywej.
    public static (Point2D LeftFirst, Point2D LeftSecond, Point2D Middle, Point2D RightFirst, Point2D RightSecond)
        SplitHalf(Point2D p0, Point2D p1, Point2D p2, Point2D p3)
    {
        var a = Lerp(p0, p1, 0.5);
        var b = Lerp(p1, p2, 0.5);
        var c = Lerp(p2, p3, 0.5);
        var d = Lerp(a, b, 0.5);
        var e = Lerp(b, c, 0.5);
        return (a, d, Lerp(d, e, 0.5), e, c);
    }

    public static Point2D Lerp(Point2D a, Point2D b, double t)
        => new(a.X + t * (b.X - a.X), a.Y + t * (b.Y - a.Y));
}
