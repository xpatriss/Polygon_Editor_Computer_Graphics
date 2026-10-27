namespace PolygonEditor.Core.Model
{
    // Krawędź - ma stały identyfikator i łączy dwa istniejące wierzchołki.
    // Sąsiednie krawędzie współdzielą obiekty wierzchołków.
    public sealed class Edge
    {
        public Guid Id { get; }
        public Vertex Start { get; }
        public Vertex End { get; }
        public BezierSegment? Bezier { get; internal set; }
        public ArcSegment? Arc { get; internal set; }
        public EdgeKind Kind => Arc is not null ? EdgeKind.CircularArc : Bezier is not null ? EdgeKind.CubicBezier : EdgeKind.Line;

        public Edge(Vertex start, Vertex end)
        {
            ArgumentNullException.ThrowIfNull(start);
            ArgumentNullException.ThrowIfNull(end);
            if (ReferenceEquals(start, end))
                throw new ArgumentException("Edge endpoints must be different vertex objects.", nameof(end));

            this.Start = start;
            this.End = end;
            this.Id = Guid.NewGuid();
        }
    }
}
