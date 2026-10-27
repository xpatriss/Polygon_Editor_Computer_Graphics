using PolygonEditor.Core.Constraints;

namespace PolygonEditor.Core.Model
{
    // Scena przechowuje wielokąty niezależnie od interfejsu aplikacji.
    public sealed class Scene
    {
        private readonly List<Polygon> polygons;
        private readonly List<GeometricConstraint> constraints = new();
        public IReadOnlyList<Polygon> Polygons { get; }
        public IReadOnlyList<GeometricConstraint> Constraints { get; }

        public Scene(IEnumerable<Polygon> polygons)
        {
            ArgumentNullException.ThrowIfNull(polygons);
            this.polygons = polygons.ToList();
            foreach (var polygon in this.polygons)
            {
                if (polygon is null)
                    throw new ArgumentException("Polygons cannot contain null.", nameof(polygons));
            }
            Polygons = this.polygons.AsReadOnly();
            Constraints = constraints.AsReadOnly();
        }

        public Edge? FindEdge(Guid id)
            => polygons.SelectMany(polygon => polygon.Edges).FirstOrDefault(edge => edge.Id == id);

        internal void AddConstraint(GeometricConstraint constraint) => constraints.Add(constraint);
        internal bool RemoveConstraint(GeometricConstraint constraint) => constraints.Remove(constraint);

        // Usunięcie krawędzi usuwa każdą relację, która się do niej odwoływała.
        internal void RemoveConstraintsForEdges(IEnumerable<Guid> edgeIds)
        {
            var removed = edgeIds.ToHashSet();
            constraints.RemoveAll(constraint => constraint.EdgeIds.Any(removed.Contains));
        }
    }
}
