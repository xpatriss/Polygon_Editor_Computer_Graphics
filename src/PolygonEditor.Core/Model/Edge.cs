namespace PolygonEditor.Core.Model
{
    // Krawędź - ma stały identyfikator i łączy dwa istniejące wierzchołki.
    // Sąsiednie krawędzie współdzielą obiekty wierzchołków.
    public sealed class Edge
    {
        public Guid Id { get; }
        public Vertex Start { get; }
        public Vertex End { get; }

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
