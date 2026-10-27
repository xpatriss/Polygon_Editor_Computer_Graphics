using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;
using PolygonEditor.Core.Editing;

namespace PolygonEditor.Core
{
    // Definiuje scenę początkową; kolejne wielokąty dodajemy w metodzie Create
    public static class DemoSceneFactory
    {
        // przygotowuje listę wielokątów i zwraca nową scenę
        public static Scene Create()
        {
            List<Polygon> polygons = new List<Polygon>();
            // zwykły prostokąt
            polygons.Add(CreatePolygon(
                new Point2D(80, 80),
                new Point2D(260, 80),
                new Point2D(260, 240),
                new Point2D(80, 240)));

            // samoprzecięcie
            polygons.Add(CreatePolygon(
                new Point2D(360, 100),
                new Point2D(580, 280),
                new Point2D(360, 280),
                new Point2D(580, 100)));

            Scene scene = new Scene(polygons);
            // Długość górnej krawędzi prostokąta i równoległość z poziomą krawędzią drugiego wielokąta.
            if (!ConstraintEditor.TrySetLength(scene, polygons[0].Edges[0], 180, out var message)
                || !ConstraintEditor.TryAddParallel(scene, polygons[0].Edges[0], polygons[1].Edges[1], out message))
                throw new InvalidOperationException(message);
            return scene;
        }

        // tworzy wielokąt na podstawie współrzędnych wierzchołków
        private static Polygon CreatePolygon(params Point2D[] points)
        {
            List<Vertex> vertices = new List<Vertex>();

            foreach (var point in points)
            {
                vertices.Add(new Vertex(point));
            }
            Polygon polygon = new Polygon(vertices);
            return polygon;
        }
    }
}
