namespace PolygonEditor.Core.Constraints;

// Równoległość dopuszcza ten sam lub przeciwny zwrot; nie narzuca długości krawędzi.
public sealed class ParallelConstraint : GeometricConstraint
{
    public Guid FirstEdgeId => EdgeIds[0];
    public Guid SecondEdgeId => EdgeIds[1];

    public ParallelConstraint(Guid firstEdgeId, Guid secondEdgeId) : base(firstEdgeId, secondEdgeId) { }
}
