namespace PolygonEditor.Core.Rasterization;

// Algorytm Bresenhama 
public static class BresenhamLineRasterizer
{
    // Wyznacza kolejne piksele odcinka, włącznie z oboma końcami.
    // Nie rysuje: zwraca punkty, które warstwa App może wyświetlić.
    public static IEnumerable<RasterPoint> Rasterize(RasterPoint start, RasterPoint end)
    {
        int x = start.X;
        int y = start.Y;
        // long chroni różnice i zmienną decyzyjną przed przepełnieniem typu int
        long dx = Math.Abs((long)end.X - start.X);
        long dy = Math.Abs((long)end.Y - start.Y);
        // zmienne określające kierunek rysowania
        int stepX = start.X < end.X ? 1 : -1;
        int stepY = start.Y < end.Y ? 1 : -1;

        // określenie osi i stromości
        bool steep = dy > dx;
        long majorLength = steep ? dy : dx;
        long minorLength = steep ? dx : dy;
        // decyzja (błąd odchylenia od idealnej prostej)
        long decision = 2 * minorLength - majorLength;
        long incrementStraight = 2 * minorLength;
        long incrementDiagonal = 2 * (minorLength - majorLength);

        for (long step = 0; step <= majorLength; step++)
        {
            // Generuje i oddaje pojedynczy punkt do warstwy aplikacji, po czym zatrzymuje się
            // i czeka, aż aplikacja poprosi o kolejny piksel
            yield return new RasterPoint(x, y);
            if (step == majorLength)
                yield break;

            if (decision >= 0)
            {
                // Krok ukośny: zmieniamy także współrzędną drugiej osi.
                if (steep)
                    x += stepX;
                else
                    y += stepY;
                decision += incrementDiagonal;
            }
            else
                decision += incrementStraight;

            // Krok po osi głównej jest wspólny dla obu decyzji.
            if (steep)
                y += stepY;
            else
                x += stepX;
        }
    }
}
