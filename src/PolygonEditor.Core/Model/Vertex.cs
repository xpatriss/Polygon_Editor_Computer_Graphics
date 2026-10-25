using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Model
{
    // Wierzchołek - zachowuje unikalny identyfikator przy zmianie położenia
    public sealed class Vertex
    {
        public Guid Id { get; }
        public Point2D Position { get; internal set; }

        public Vertex(Point2D position)
        {
            this.Position = position;
            Id = Guid.NewGuid();
        }
    }
}
