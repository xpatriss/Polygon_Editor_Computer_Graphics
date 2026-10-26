using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Editing;

// Publiczne operacje edycji, zmiany modelu pozostają w Core, bez zależności od myszy i Avalonii
public static class PolygonEditor
{
    // Ustawia nową pozycję istniejącego wierzchołka; jego Id pozostaje bez zmian.
    public static void MoveVertex(Vertex vertex, Point2D position)
    {
        ArgumentNullException.ThrowIfNull(vertex);
        if (!double.IsFinite(position.X) || !double.IsFinite(position.Y))
            throw new ArgumentException("Position must be finite.", nameof(position));
        vertex.Position = position;
    }

    // Sprawdza argumenty i zleca wielokątowi podział krawędzi.
    // Zwraca: nowy wierzchołek.
    public static Vertex InsertMidpoint(Polygon polygon, Edge edge)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        ArgumentNullException.ThrowIfNull(edge);
        return polygon.SplitEdge(edge);
    }

    // Sprawdza argumenty i zleca wielokątowi usunięcie wierzchołka.
    // Zwraca: true po usunięciu, false dla trójkąta.
    public static bool RemoveVertex(Polygon polygon, Vertex vertex)
    {
        ArgumentNullException.ThrowIfNull(polygon);
        ArgumentNullException.ThrowIfNull(vertex);
        return polygon.RemoveVertex(vertex);
    }
}
