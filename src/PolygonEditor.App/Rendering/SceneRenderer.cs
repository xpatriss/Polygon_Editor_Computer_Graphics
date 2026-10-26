using Avalonia;
using Avalonia.Media;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Rasterization;

namespace PolygonEditor.App.Rendering
{
    // Odczytuje model sceny i wyświetla go wybranym sposobem rysowania odcinków.
    public sealed class SceneRenderer
    {
        private const double VertexRadius = 4.0;
        private readonly Pen edgePen = new Pen(Brushes.Black, 2);
        private readonly Pen selectedPolygonPen = new Pen(Brushes.DarkOrange, 2);
        private readonly Pen selectedEdgePen = new Pen(Brushes.Crimson, 3);

        // przekształca punkt w punkt Avalonii o tych samych współrzędnych
        private static Point ToAvaloniaPoint(Point2D position)
        {
            Point point = new Point(position.X, position.Y);
            return point;
        }

        // Rysuje najpierw wszystkie krawędzie, potem znaczniki wierzchołków.
        // Wyróżnia kolorem elementy wskazane przez selection; nie zmienia ich pozycji.
        public void Draw(DrawingContext context, Scene scene, HitResult? selection = null,
            LineDrawingAlgorithm algorithm = LineDrawingAlgorithm.Library)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(scene);

            foreach (var polygon in scene.Polygons)
            {
                foreach (var edge in polygon.Edges)
                {
                    var pen = ReferenceEquals(edge, selection?.Edge) ? selectedEdgePen
                        : ReferenceEquals(polygon, selection?.Polygon) ? selectedPolygonPen : edgePen;
                    DrawLine(context, pen, edge.Start.Position, edge.End.Position, algorithm);
                }
            }

            foreach (var polygon in scene.Polygons)
            {
                foreach (var vertex in polygon.Vertices)
                {
                    var brush = ReferenceEquals(vertex, selection?.Vertex) ? Brushes.Crimson : Brushes.DodgerBlue;
                    context.DrawEllipse(brush, null, ToAvaloniaPoint(vertex.Position), VertexRadius, VertexRadius);
                }
            }
        }

        // Biblioteka rysuje linię; w trybie Bresenhama wyświetlamy wyznaczone przez Core komórki.
        private static void DrawLine(DrawingContext context, Pen pen, Point2D start, Point2D end,
            LineDrawingAlgorithm algorithm)
        {
            if (algorithm == LineDrawingAlgorithm.Library)
            {
                context.DrawLine(pen, ToAvaloniaPoint(start), ToAvaloniaPoint(end));
                return;
            }

            // Zaokrąglamy tylko dane do rysowania, bez przesuwania wierzchołków modelu.
            var rasterStart = ToRasterPoint(start);
            var rasterEnd = ToRasterPoint(end);
            // Wyłączamy wygładzanie komórek, aby było widać dyskretną siatkę Bresenhama.
            using (context.PushRenderOptions(new RenderOptions { EdgeMode = EdgeMode.Aliased }))
            {
                foreach (var pixel in BresenhamLineRasterizer.Rasterize(rasterStart, rasterEnd))
                    context.DrawRectangle(pen.Brush, null, new Rect(pixel.X, pixel.Y, 1, 1));
            }
        }

        private static RasterPoint ToRasterPoint(Point2D point)
            => new((int)Math.Round(point.X, MidpointRounding.AwayFromZero),
                (int)Math.Round(point.Y, MidpointRounding.AwayFromZero));
    }
}
