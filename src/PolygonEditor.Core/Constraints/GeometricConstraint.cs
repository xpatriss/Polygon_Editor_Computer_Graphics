namespace PolygonEditor.Core.Constraints;

// Wspólna tożsamość relacji; krawędzie wskazujemy przez stałe Id
public abstract class GeometricConstraint
{
    public Guid Id { get; } = Guid.NewGuid();
    public IReadOnlyList<Guid> EdgeIds { get; }

    protected GeometricConstraint(params Guid[] edgeIds)
    {
        if (edgeIds.Any(id => id == Guid.Empty) || edgeIds.Distinct().Count() != edgeIds.Length)
            throw new ArgumentException("Constraint edge identities must be nonempty and distinct.", nameof(edgeIds));
        EdgeIds = Array.AsReadOnly(edgeIds);
    }
}
