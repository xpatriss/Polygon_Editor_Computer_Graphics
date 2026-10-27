using PolygonEditor.Core.Model;

namespace PolygonEditor.Core.Editing;

// Operacje zmieniające obwód; porządkują też relacje usuniętych krawędzi.
public static class PolygonEditor
{
    // Dzieli krawędź i usuwa relacje zastąpionego odcinka.
    // Zwraca: nowy wierzchołek.
    public static Vertex InsertMidpoint(Scene scene, Polygon polygon, Edge edge)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(polygon);
        ArgumentNullException.ThrowIfNull(edge);
        if (!scene.Polygons.Contains(polygon))
            throw new ArgumentException("Polygon must belong to the scene.", nameof(polygon));
        var vertex = polygon.SplitEdge(edge);
        scene.RemoveConstraintsForEdges(new[] { edge.Id });
        return vertex;
    }

    // Usuwa wierzchołek i relacje obu zastąpionych krawędzi.
    // Zwraca: true po usunięciu, false dla trójkąta.
    public static bool RemoveVertex(Scene scene, Polygon polygon, Vertex vertex)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(polygon);
        ArgumentNullException.ThrowIfNull(vertex);
        if (!scene.Polygons.Contains(polygon))
            throw new ArgumentException("Polygon must belong to the scene.", nameof(polygon));
        var removedEdges = polygon.Edges.Where(edge => edge.Start == vertex || edge.End == vertex).Select(edge => edge.Id).ToArray();
        if (!polygon.RemoveVertex(vertex))
            return false;
        scene.RemoveConstraintsForEdges(removedEdges);
        return true;
    }
}
