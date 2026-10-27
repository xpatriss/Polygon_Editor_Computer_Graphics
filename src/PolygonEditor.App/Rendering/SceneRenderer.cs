using Avalonia;
using Avalonia.Media;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Rasterization;
using System.Globalization;

namespace PolygonEditor.App.Rendering
{
    // Odczytuje model sceny i wyświetla go wybranym sposobem rysowania odcinków.
    public sealed class SceneRenderer
    {
        private const double VertexRadius = 4.0;
        private readonly Pen edgePen = new Pen(Brushes.Black, 2);
        private readonly Pen selectedPolygonPen = new Pen(Brushes.DarkOrange, 2);
        private readonly Pen selectedEdgePen = new Pen(Brushes.Crimson, 3);
        private readonly Pen secondaryEdgePen = new Pen(Brushes.Teal, 3);
        private readonly Pen controlPolygonPen = new Pen(Brushes.Gray, 1, DashStyle.Dash);

        // przekształca punkt w punkt Avalonii o tych samych współrzędnych
        private static Point ToAvaloniaPoint(Point2D position)
        {
            Point point = new Point(position.X, position.Y);
            return point;
        }

        // Rysuje najpierw wszystkie krawędzie, potem znaczniki wierzchołków.
        // Wyróżnia kolorem elementy wskazane przez selection; nie zmienia ich pozycji.
        public void Draw(DrawingContext context, Scene scene, HitResult? selection = null,
            LineDrawingAlgorithm algorithm = LineDrawingAlgorithm.Library, Edge? secondaryEdge = null)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(scene);

            foreach (var polygon in scene.Polygons)
            {
                foreach (var edge in polygon.Edges)
                {
                    var pen = ReferenceEquals(edge, selection?.Edge) ? selectedEdgePen
                        : ReferenceEquals(edge, secondaryEdge) ? secondaryEdgePen
                        : ReferenceEquals(polygon, selection?.Polygon) ? selectedPolygonPen : edgePen;
                    if (edge.Bezier is { } curve)
                    {
                        // Wielobok kontrolny jest pomocniczy: nie stanowi krawędzi obwodu.
                        context.DrawLine(controlPolygonPen, ToAvaloniaPoint(edge.Start.Position), ToAvaloniaPoint(curve.FirstControl.Position));
                        context.DrawLine(controlPolygonPen, ToAvaloniaPoint(curve.FirstControl.Position), ToAvaloniaPoint(curve.SecondControl.Position));
                        context.DrawLine(controlPolygonPen, ToAvaloniaPoint(curve.SecondControl.Position), ToAvaloniaPoint(edge.End.Position));
                    }
                    foreach (var (start, end) in SceneGeometry.EdgeSegments(edge))
                        DrawLine(context, pen, start, end, algorithm);
                }
            }

            DrawConstraintLabels(context, scene);
            // Kwadratowe uchwyty rysujemy pod znacznikami wierzchołków, zgodnie z kolejnością trafiania.
            foreach (var edge in scene.Polygons.SelectMany(polygon => polygon.Edges))
                if (edge.Bezier is { } curve)
                    foreach (var control in new[] { curve.FirstControl, curve.SecondControl })
                    {
                        var brush = ReferenceEquals(control, selection?.ControlPoint) ? Brushes.Crimson : Brushes.DarkViolet;
                        context.DrawRectangle(brush, null, new Rect(control.Position.X - 4, control.Position.Y - 4, 8, 8));
                    }
            foreach (var polygon in scene.Polygons)
            {
                foreach (var vertex in polygon.Vertices)
                {
                    var brush = ReferenceEquals(vertex, selection?.Vertex) ? Brushes.Crimson : Brushes.DodgerBlue;
                    context.DrawEllipse(brush, null, ToAvaloniaPoint(vertex.Position), VertexRadius, VertexRadius);
                    if (polygon.Edges.Any(edge => (edge.Start == vertex || edge.End == vertex) && edge.Kind != EdgeKind.Line))
                    {
                        var label = new FormattedText(vertex.Continuity.ToString(), CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight, Typeface.Default, 11, Brushes.DarkSlateGray);
                        context.DrawText(label, new Point(vertex.Position.X + 7, vertex.Position.Y - 17));
                    }
                }
            }
        }

        // Kilka relacji jednej krawędzi układamy w osobnych wierszach obok jej środka.
        private static void DrawConstraintLabels(DrawingContext context, Scene scene)
        {
            var rows = new Dictionary<Guid, int>();
            for (int i = 0; i < scene.Constraints.Count; i++)
            {
                var constraint = scene.Constraints[i];
                foreach (var id in constraint.EdgeIds)
                {
                    if (scene.FindEdge(id) is not { } edge)
                        continue;
                    rows.TryGetValue(id, out int row);
                    rows[id] = row + 1;
                    var middle = new Point((edge.Start.Position.X + edge.End.Position.X) / 2 + 8,
                        (edge.Start.Position.Y + edge.End.Position.Y) / 2 + 8 + row * 17);
                    var text = new FormattedText(ConstraintLabels.Marker(constraint, i), CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.DarkSlateBlue);
                    context.DrawRectangle(Brushes.White, null, new Rect(middle, new Size(text.Width, text.Height)));
                    context.DrawText(text, middle);
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
