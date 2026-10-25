namespace PolygonEditor.Core.Model
{
    // Wielokąt - ma stały identyfikator i przechowuje zamknięty obwód.
    // Kolejność wierzchołków jest zachowana; samoprzecięcia są dozwolone.
    public sealed class Polygon
    {
        private readonly List<Vertex> vertices;
        private readonly List<Edge> edges;

        public Guid Id { get; }
        public IReadOnlyList<Vertex> Vertices { get; }
        public IReadOnlyList<Edge> Edges { get; }

        public Polygon(IEnumerable<Vertex> vertices)
        {
            ArgumentNullException.ThrowIfNull(vertices);

            this.vertices = vertices.ToList();

            if (this.vertices.Count < 3)
                throw new ArgumentException("Number of vertices cannot be less than 3.", nameof(vertices));

            HashSet<Vertex> uniqueVertices = new HashSet<Vertex>();
            foreach (var vertex in this.vertices)
            {
                if (vertex is null)
                    throw new ArgumentException("Vertices cannot contain null.", nameof(vertices));
                if (uniqueVertices.Add(vertex) == false)
                    throw new ArgumentException("Vertex objects cannot repeat.", nameof(vertices));
            }

            this.edges = new List<Edge>();
            for (int i = 0; i < this.vertices.Count; i++)
            {
                int nextIndex = (i + 1) % this.vertices.Count;
                Edge edge = new Edge(this.vertices[i], this.vertices[nextIndex]);
                this.edges.Add(edge);
            }

            Vertices = this.vertices.AsReadOnly();
            Edges = this.edges.AsReadOnly();
            Id = Guid.NewGuid();
        }
    }
}
