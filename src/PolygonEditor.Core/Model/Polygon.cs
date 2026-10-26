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

        // Dodaje wierzchołek w środku krawędzi i zastępuje ją dwiema nowymi.
        // Pozostałe krawędzie pozostają bez zmian.
        // Zwraca: dodany wierzchołek.
        internal Vertex SplitEdge(Edge edge)
        {
            int index = edges.IndexOf(edge);
            if (index < 0)
                throw new ArgumentException("Edge does not belong to this polygon.", nameof(edge));

            var start = edge.Start.Position;
            var end = edge.End.Position;
            var vertex = new Vertex(new Geometry.Point2D(start.X / 2 + end.X / 2, start.Y / 2 + end.Y / 2));
            vertices.Insert(index + 1, vertex);
            edges[index] = new Edge(edge.Start, vertex);
            edges.Insert(index + 1, new Edge(vertex, edge.End));
            return vertex;
        }

        // Usuwa wierzchołek i zastępuje dwie jego krawędzie jedną łączącą sąsiadów.
        // Zwraca: true po usunięciu, false gdy wielokąt ma już tylko 3 wierzchołki.
        internal bool RemoveVertex(Vertex vertex)
        {
            int index = vertices.IndexOf(vertex);
            if (index < 0)
                throw new ArgumentException("Vertex does not belong to this polygon.", nameof(vertex));
            if (vertices.Count == 3)
                return false;

            int previous = (index + edges.Count - 1) % edges.Count;
            // Dla pierwszego wierzchołka poprzednią krawędzią jest ostatnia na liście.
            edges[previous] = new Edge(edges[previous].Start, edges[index].End);
            edges.RemoveAt(index);
            vertices.RemoveAt(index);
            return true;
        }

        // Tworzy wielokąt z wierzchołków podanych w kolejności obwodu.
        // Łączy też ostatni wierzchołek z pierwszym.
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
