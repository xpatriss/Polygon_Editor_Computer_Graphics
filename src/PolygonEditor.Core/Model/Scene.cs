namespace PolygonEditor.Core.Model
{
    // Scena przechowuje wielokąty niezależnie od interfejsu aplikacji.
    public sealed class Scene
    {
        private readonly List<Polygon> polygons;
        public IReadOnlyList<Polygon> Polygons { get; }

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
        }
    }
}
