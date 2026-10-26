using PolygonEditor.Core.Rasterization;

// Sprawdza wynik algorytmu bez okna i bez bibliotek testowych.
internal static class BresenhamChecks
{
    public static void Run(Action<string, Action> check)
    {
        check("Bresenham matches a manually calculated shallow line", () =>
        {
            RasterPoint[] expected = [new(0, 0), new(1, 0), new(2, 1), new(3, 1), new(4, 1), new(5, 2), new(6, 2)];
            Require(BresenhamLineRasterizer.Rasterize(new(0, 0), new(6, 2)).SequenceEqual(expected),
                "The raster differs from the manually calculated sequence.");
        });

        check("Bresenham returns one pixel for a zero-length segment", () =>
        {
            var point = new RasterPoint(-3, 7);
            Require(BresenhamLineRasterizer.Rasterize(point, point).SequenceEqual(new[] { point }),
                "A degenerate segment must contain exactly its endpoint.");
        });

        check("Bresenham: 6561 segments cover all directions and stay near the ideal line", () =>
        {
            for (int startX = -4; startX <= 4; startX++)
            for (int startY = -4; startY <= 4; startY++)
            for (int endX = -4; endX <= 4; endX++)
            for (int endY = -4; endY <= 4; endY++)
            {
                var start = new RasterPoint(startX, startY);
                var end = new RasterPoint(endX, endY);
                var pixels = BresenhamLineRasterizer.Rasterize(start, end).ToArray();
                int dx = endX - startX;
                int dy = endY - startY;
                int majorLength = Math.Max(Math.Abs(dx), Math.Abs(dy));
                Require(pixels.Length == majorLength + 1, "Wrong pixel count.");
                Require(pixels[0] == start && pixels[^1] == end, "An endpoint is missing or swapped.");
                for (int i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    Require(pixel.X >= Math.Min(startX, endX) && pixel.X <= Math.Max(startX, endX)
                        && pixel.Y >= Math.Min(startY, endY) && pixel.Y <= Math.Max(startY, endY),
                        "A pixel lies outside the segment bounds.");
                    // Równanie prostej: błąd drugiej współrzędnej nie przekracza połowy komórki.
                    int lineError = Math.Abs(dy * (pixel.X - startX) - dx * (pixel.Y - startY));
                    Require(2 * lineError <= majorLength, "A pixel is too far from the ideal line.");
                    if (i > 0)
                    {
                        int stepX = pixel.X - pixels[i - 1].X;
                        int stepY = pixel.Y - pixels[i - 1].Y;
                        Require(Math.Max(Math.Abs(stepX), Math.Abs(stepY)) == 1, "Gap or repeated pixel.");
                        Require(stepX * Math.Sign(dx) >= 0 && stepY * Math.Sign(dy) >= 0,
                            "The raster reverses direction.");
                    }
                }
            }
        });

        check("Bresenham handles coordinates near int limits and can be read lazily", () =>
        {
            RasterPoint[] expected = [new(int.MaxValue - 2, int.MinValue),
                new(int.MaxValue - 1, int.MinValue + 1), new(int.MaxValue, int.MinValue + 2)];
            Require(BresenhamLineRasterizer.Rasterize(expected[0], expected[^1]).SequenceEqual(expected),
                "Coordinates near int limits overflowed.");
            var prefix = BresenhamLineRasterizer.Rasterize(new(int.MinValue, 7), new(int.MaxValue, 7)).Take(3);
            RasterPoint[] expectedPrefix = [new(int.MinValue, 7), new(int.MinValue + 1, 7), new(int.MinValue + 2, 7)];
            Require(prefix.SequenceEqual(expectedPrefix), "Long coordinate difference overflowed.");
        });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
