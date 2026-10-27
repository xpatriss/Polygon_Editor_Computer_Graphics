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
            var left = new Edge(edge.Start, vertex);
            var right = new Edge(vertex, edge.End);
            if (edge.Bezier is { } curve)
            {
                // Dzielimy faktyczną krzywą, nie jej cięciwę.
                var split = Geometry.BezierCurve.SplitHalf(start, curve.FirstControl.Position, curve.SecondControl.Position, end);
                vertex.Position = split.Middle;
                left.Bezier = new BezierSegment(split.LeftFirst, split.LeftSecond);
                right.Bezier = new BezierSegment(split.RightFirst, split.RightSecond);
                vertex.Continuity = ContinuityClass.G1;
            }
            else if (edge.Arc is { } arc)
            {
                vertex.Position = Geometry.CircularArcGeometry.PointAt(edge, 0.5);
                left.Arc = new ArcSegment(arc.Center.Position) { Direction = arc.Direction };
                right.Arc = new ArcSegment(arc.Center.Position) { Direction = arc.Direction };
                // Przy G0/G0 nowe G1 w środku zachowuje okrąg i daje po jednym G1 na każdym dziecku.
                // Jeśli oryginał miał już G1, nowe złącze ma G0, aby nie nadać dziecku dwóch G1.
                vertex.Continuity = edge.Start.Continuity == ContinuityClass.G0 && edge.End.Continuity == ContinuityClass.G0
                    ? ContinuityClass.G1 : ContinuityClass.G0;
            }
            vertices.Insert(index + 1, vertex);
            edges[index] = left;
            edges.Insert(index + 1, right);
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
            var incoming = edges[previous];
            var outgoing = edges[index];
            var merged = new Edge(incoming.Start, outgoing.End);
            if (incoming.Kind != EdgeKind.Line || outgoing.Kind != EdgeKind.Line)
            {
                // Nowa krzywa zachowuje zewnętrzne styczne obu zastępowanych segmentów.
                var firstTangent = Geometry.EndpointTangent.For(incoming, true).Vector;
                var secondTangent = Geometry.EndpointTangent.For(outgoing, false).Vector;
                var first = new Geometry.Point2D(incoming.Start.Position.X + firstTangent.X / 3,
                    incoming.Start.Position.Y + firstTangent.Y / 3);
                var second = new Geometry.Point2D(outgoing.End.Position.X - secondTangent.X / 3,
                    outgoing.End.Position.Y - secondTangent.Y / 3);
                merged.Bezier = new BezierSegment(first, second);
            }
            edges[previous] = merged;
            edges.RemoveAt(index);
            vertices.RemoveAt(index);
            return true;
        }

        // Kopia obwodu do cofnięcia nieudanej edycji. Zachowujemy oryginalne obiekty i Id.
        internal (Vertex[] Vertices, Edge[] Edges) CaptureTopology() => (vertices.ToArray(), edges.ToArray());

        internal void RestoreTopology(Vertex[] savedVertices, Edge[] savedEdges)
        {
            vertices.Clear();
            vertices.AddRange(savedVertices);
            edges.Clear();
            edges.AddRange(savedEdges);
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
