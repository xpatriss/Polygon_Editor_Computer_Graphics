using Avalonia;
using Avalonia.Media;
using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

namespace PolygonEditor.App.Rendering
{
    // Odczytuje model sceny i rysuje go przy użyciu bibliotecznych operacji Avalonii
    public sealed class SceneRenderer
    {
        private const double VertexRadius = 4.0;
        private readonly Pen edgePen = new Pen(Brushes.Black, 2);

        // przekształca punkt w punkt Avalonii o tych samych współrzędnych
        private static Point ToAvaloniaPoint(Point2D position)
        {
            Point point = new Point(position.X, position.Y);
            return point;
        }

        // rysuje krawędzie wszystkich wielokątów, a następnie znaczniki wierzchołków
        public void Draw(DrawingContext context, Scene scene)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(scene);

            foreach (var polygon in scene.Polygons)
            {
                foreach (var edge in polygon.Edges)
                {
                    context.DrawLine(edgePen, ToAvaloniaPoint(edge.Start.Position), ToAvaloniaPoint(edge.End.Position));
                }
            }

            foreach (var polygon in scene.Polygons)
            {
                foreach (var vertex in polygon.Vertices)
                {
                    context.DrawEllipse(Brushes.DodgerBlue, null, ToAvaloniaPoint(vertex.Position), VertexRadius, VertexRadius);
                }
            }
        }
    }
}
