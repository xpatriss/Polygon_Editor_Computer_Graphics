using PolygonEditor.Core.Geometry;

namespace PolygonEditor.Core.Model;

// Środek jest parametrem geometrii łuku, nie uchwytem obsługiwanym myszą.
public sealed class ArcCenter : ScenePoint
{
    public ArcCenter(Point2D position) : base(position) { }
}

public sealed class ArcSegment
{
    public ArcCenter Center { get; }
    // +1 oznacza rosnący kąt atan2 (na ekranie Y rośnie w dół); -1 przeciwny zwrot.
    public int Direction { get; internal set; } = 1;

    public ArcSegment(Point2D center) => Center = new ArcCenter(center);
}
