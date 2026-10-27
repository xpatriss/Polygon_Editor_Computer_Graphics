namespace PolygonEditor.Core.Constraints;

// Wymagana dodatnia długość odcinka, w jednostkach sceny
public sealed class LengthConstraint : GeometricConstraint
{
    public Guid EdgeId => EdgeIds[0];
    public double TargetLength { get; }

    public LengthConstraint(Guid edgeId, double targetLength) : base(edgeId)
    {
        if (!double.IsFinite(targetLength) || targetLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetLength));
        TargetLength = targetLength;
    }
}
