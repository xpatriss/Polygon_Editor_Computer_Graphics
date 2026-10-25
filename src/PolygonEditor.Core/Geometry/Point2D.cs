namespace PolygonEditor.Core.Geometry
{
    // typ przechowujący współrzędne punktu w 2D - tylko do odczytu, niemodyfikowalny po utworzeniu
    public readonly struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
    }
}
