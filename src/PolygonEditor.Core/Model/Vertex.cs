using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Model
{
    // Wierzchołek - zachowuje unikalny identyfikator przy zmianie położenia
    public sealed class Vertex : ScenePoint
    {
        public ContinuityClass Continuity { get; internal set; } = ContinuityClass.G0;
        // Jawnego wyboru nie nadpisujemy domyślnym G1 przy późniejszej zmianie typu krawędzi.
        public bool IsContinuityExplicit { get; internal set; }

        public Vertex(Point2D position) : base(position) { }
    }
}
