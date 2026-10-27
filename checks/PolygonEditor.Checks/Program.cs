using PolygonEditor.Core.Geometry;
using PolygonEditor.Core.Model;

var failures = 0;
var checks = 0;

void Check(string name, Action action)
{
    checks++;
    try
    {
        action();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception error)
    {
        failures++;
        Console.WriteLine($"FAIL: {name}: {error.GetType().Name}: {error.Message}");
    }
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

static void Reject<TException>(Action action, string parameter) where TException : ArgumentException
{
    try
    {
        action();
    }
    catch (TException error)
    {
        Require(error.ParamName == parameter, $"Expected parameter '{parameter}', got '{error.ParamName}'.");
        return;
    }

    throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
}

static Vertex V(double x, double y) => new(new Point2D(x, y));

static List<Vertex> Triangle() => new() { V(0, 0), V(20, 0), V(0, 20) };

static IEnumerable<T> Once<T>(IEnumerable<T> items)
{
    var traversals = 0;
    return Traverse();

    IEnumerable<T> Traverse()
    {
        if (++traversals > 1)
            throw new InvalidOperationException("The input sequence was enumerated more than once.");

        foreach (var item in items)
            yield return item;
    }
}

Check("Edge keeps the given endpoint objects and stable identity", () =>
{
    var points = Triangle();
    var edge = new Edge(points[0], points[1]);
    var id = edge.Id;
    Require(ReferenceEquals(edge.Start, points[0]) && ReferenceEquals(edge.End, points[1]), "Endpoints were copied or swapped.");
    Require(id != Guid.Empty && edge.Id == id, "Edge identity is empty or changes on access.");
});

Check("Edge rejects a null start", () => Reject<ArgumentNullException>(() => new Edge(null!, V(0, 0)), "start"));
Check("Edge rejects a null end", () => Reject<ArgumentNullException>(() => new Edge(V(0, 0), null!), "end"));
Check("Edge rejects the same object at both ends", () =>
{
    var vertex = V(0, 0);
    Reject<ArgumentException>(() => new Edge(vertex, vertex), "end");
});

Check("Edge accepts distinct objects at equal coordinates", () =>
{
    var edge = new Edge(V(1, 2), V(1, 2));
    Require(!ReferenceEquals(edge.Start, edge.End), "Distinct vertices were merged.");
});

Check("Polygon creates a closed triangle with shared endpoints", () =>
{
    var points = Triangle();
    var polygon = new Polygon(points);
    Require(polygon.Id != Guid.Empty, "Polygon identity is empty.");
    Require(polygon.Vertices.Count == 3 && polygon.Edges.Count == 3, "Incorrect number of vertices or edges.");
    Require(polygon.Edges.Select(edge => edge.Id).Distinct().Count() == 3, "Edge identities are not distinct.");
    for (var i = 0; i < 3; i++)
    {
        var next = (i + 1) % 3;
        Require(ReferenceEquals(polygon.Vertices[i], points[i]), "Vertex order or identity changed.");
        Require(ReferenceEquals(polygon.Edges[i].Start, points[i]), "Incorrect edge start.");
        Require(ReferenceEquals(polygon.Edges[i].End, points[next]), "Incorrect edge end.");
        Require(ReferenceEquals(polygon.Edges[i].End, polygon.Edges[next].Start), "Neighboring edges do not share a vertex.");
        Require(ReferenceEquals(polygon.Edges[i], polygon.Edges[i]), "Edges are recreated on access.");
    }
});

Check("Polygon accepts self-intersections and preserves input order", () =>
{
    var points = new[] { V(0, 0), V(10, 10), V(0, 10), V(10, 0) };
    var polygon = new Polygon(points);
    Require(polygon.Edges.Count == 4, "Bow-tie polygon did not retain four edges.");
    for (var i = 0; i < points.Length; i++)
        Require(ReferenceEquals(polygon.Vertices[i], points[i]), "Input order changed.");
});

Check("Polygon copies the collection without cloning its vertices", () =>
{
    var points = Triangle();
    var first = points[0];
    var polygon = new Polygon(points);
    points.Clear();
    Require(polygon.Vertices.Count == 3 && ReferenceEquals(polygon.Vertices[0], first), "Input collection leaked or vertices were cloned.");
});

Check("Polygon accepts a sequence that can be traversed only once", () =>
{
    var polygon = new Polygon(Once(Triangle()));
    Require(polygon.Edges.Count == 3, "Single-traversal input did not create a triangle.");
});

Check("Polygon rejects a null collection", () => Reject<ArgumentNullException>(() => new Polygon(null!), "vertices"));
Check("Polygon rejects fewer than three vertices", () => Reject<ArgumentException>(() => new Polygon(new[] { V(0, 0), V(1, 1) }), "vertices"));
Check("Polygon rejects a null element", () => Reject<ArgumentException>(() => new Polygon(new[] { V(0, 0), V(1, 1), null! }), "vertices"));
Check("Polygon rejects a repeated vertex object", () =>
{
    var vertex = V(0, 0);
    Reject<ArgumentException>(() => new Polygon(new[] { vertex, V(1, 1), vertex }), "vertices");
});

Check("Polygon accepts distinct vertex objects at equal coordinates", () =>
{
    var polygon = new Polygon(new[] { V(0, 0), V(0, 0), V(1, 1) });
    Require(polygon.Vertices.Count == 3, "Equal coordinates were treated as identical objects.");
});

Check("Polygon collection views reject structural changes", () =>
{
    var polygon = new Polygon(Triangle());
    Require(((ICollection<Vertex>)polygon.Vertices).IsReadOnly, "Vertex view is writable.");
    Require(((ICollection<Edge>)polygon.Edges).IsReadOnly, "Edge view is writable.");
    try
    {
        ((ICollection<Vertex>)polygon.Vertices).Clear();
    }
    catch (NotSupportedException)
    {
        Require(polygon.Vertices.Count == 3, "Collection changed despite rejecting the operation.");
        return;
    }
    throw new InvalidOperationException("The public vertex view allowed a structural change.");
});

Check("Scene accepts an empty collection", () => Require(new Scene(Array.Empty<Polygon>()).Polygons.Count == 0, "Scene is not empty."));
Check("Scene accepts three polygons and copies the collection", () =>
{
    var polygons = new List<Polygon> { new(Triangle()), new(Triangle()), new(Triangle()) };
    var first = polygons[0];
    var scene = new Scene(polygons);
    polygons.Clear();
    Require(scene.Polygons.Count == 3 && ReferenceEquals(scene.Polygons[0], first), "Scene collection or object identity is incorrect.");
    Require(((ICollection<Polygon>)scene.Polygons).IsReadOnly, "Scene view is writable.");
});

Check("Scene accepts a sequence that can be traversed only once", () => Require(new Scene(Once(new[] { new Polygon(Triangle()) })).Polygons.Count == 1, "Polygon was lost."));
Check("Scene rejects a null collection", () => Reject<ArgumentNullException>(() => new Scene(null!), "polygons"));
Check("Scene rejects a null element", () => Reject<ArgumentException>(() => new Scene(new Polygon[] { null! }), "polygons"));

static Polygon Square() => new(new[] { V(0, 0), V(100, 0), V(100, 100), V(0, 100) });
static void Closed(Polygon polygon)
{
    Require(polygon.Vertices.Count == polygon.Edges.Count, "Vertex/edge counts differ.");
    for (int i = 0; i < polygon.Vertices.Count; i++)
    {
        Require(ReferenceEquals(polygon.Edges[i].Start, polygon.Vertices[i]), "Broken start.");
        Require(ReferenceEquals(polygon.Edges[i].End, polygon.Vertices[(i + 1) % polygon.Vertices.Count]), "Broken end.");
    }
}
for (int index = 0; index < 4; index++)
{
    int selectedIndex = index;
    Check($"Split edge {index}: midpoint, closure and unaffected identities", () =>
    {
        var polygon = Square();
        var old = polygon.Edges.ToArray();
        var edge = old[selectedIndex];
        var view = polygon.Vertices;
        var vertex = PolygonEditor.Core.Editing.PolygonEditor.InsertMidpoint(new Scene(new[] { polygon }), polygon, edge);
        Require(vertex.Position.X == (edge.Start.Position.X + edge.End.Position.X) / 2 && vertex.Position.Y == (edge.Start.Position.Y + edge.End.Position.Y) / 2, "Wrong midpoint.");
        Require(view.Count == 5 && !polygon.Edges.Contains(edge), "View not updated or old edge retained.");
        foreach (var untouched in old.Where(e => !ReferenceEquals(e, edge)))
            Require(polygon.Edges.Contains(untouched), "Unrelated edge was replaced.");
        Closed(polygon);
    });
    Check($"Remove vertex {index}: closure and unaffected identities", () =>
    {
        var polygon = Square();
        var vertex = polygon.Vertices[selectedIndex];
        var untouched = polygon.Edges.Where(e => !ReferenceEquals(e.Start, vertex) && !ReferenceEquals(e.End, vertex)).ToArray();
        var scene = new Scene(new[] { polygon });
        Require(PolygonEditor.Core.Editing.PolygonEditor.RemoveVertex(scene, polygon, vertex), "Removal failed.");
        Require(polygon.Vertices.Count == 3 && !polygon.Vertices.Contains(vertex), "Vertex retained.");
        foreach (var edge in untouched) Require(polygon.Edges.Contains(edge), "Unrelated edge replaced.");
        Closed(polygon);
        Require(!PolygonEditor.Core.Editing.PolygonEditor.RemoveVertex(scene, polygon, polygon.Vertices[0]), "Triangle shrank.");
        Closed(polygon);
    });
}
Check("Topology rejects foreign objects without changing polygon", () =>
{
    var polygon = Square();
    var scene = new Scene(new[] { polygon });
    Reject<ArgumentException>(() => PolygonEditor.Core.Editing.PolygonEditor.InsertMidpoint(scene, polygon, Square().Edges[0]), "edge");
    Reject<ArgumentException>(() => PolygonEditor.Core.Editing.PolygonEditor.RemoveVertex(scene, polygon, V(0, 0)), "vertex");
    Require(polygon.Vertices.Count == 4, "Invalid operation changed model.");
    Closed(polygon);
});
Check("Hit testing: vertex, edge, interior, empty and zero-length segment", () =>
{
    var polygon = Square(); var scene = new Scene(new[] { polygon });
    Require(ReferenceEquals(PolygonEditor.Core.Geometry.HitTesting.Pick(scene, new(0, 0))?.Vertex, polygon.Vertices[0]), "Wrong vertex.");
    Require(ReferenceEquals(PolygonEditor.Core.Geometry.HitTesting.Pick(scene, new(50, 0))?.Edge, polygon.Edges[0]), "Wrong edge.");
    var hit = PolygonEditor.Core.Geometry.HitTesting.Pick(scene, new(50, 50));
    Require(hit?.Polygon == polygon && hit.Vertex is null && hit.Edge is null, "Wrong interior hit.");
    Require(PolygonEditor.Core.Geometry.HitTesting.Pick(scene, new(200, 200)) is null, "Empty area hit.");
    Require(PolygonEditor.Core.Geometry.HitTesting.DistanceToSegmentSquared(new(3, 4), new(0, 0), new(0, 0)) == 25, "Degenerate edge distance.");
});
Check("Vertex drag follows cursor and stops after release", () =>
{
    var polygon = Square(); var scene = new Scene(new[] { polygon });
    var interaction = new PolygonEditor.App.Interaction.EditorInteraction();
    interaction.BeginDrag(scene, new(3, 3));
    interaction.Move(new(30, 40));
    Require(polygon.Vertices[0].Position.X == 30 && polygon.Vertices[0].Position.Y == 40, "Vertex missed cursor.");
    Require(polygon.Vertices[1].Position.X == 100, "Other vertex moved.");
    interaction.EndDrag(); interaction.Move(new(90, 90));
    Require(polygon.Vertices[0].Position.X == 30, "Drag continued after release.");
});
Check("Click near a vertex selects without moving, including release and mouse jitter", () =>
{
    var polygon = Square(); var scene = new Scene(new[] { polygon });
    var interaction = new PolygonEditor.App.Interaction.EditorInteraction();
    interaction.BeginDrag(scene, new(6, 0));
    Require(ReferenceEquals(interaction.Selection?.Vertex, polygon.Vertices[0]), "Nearby vertex was not selected.");
    Require(polygon.Vertices[0].Position.X == 0 && polygon.Vertices[0].Position.Y == 0, "Press moved vertex.");
    interaction.Move(new(6, 0)); // Tę samą pozycję przekazuje również obsługa puszczenia przycisku.
    interaction.Move(new(7, 1));
    interaction.EndDrag();
    Require(polygon.Vertices[0].Position.X == 0 && polygon.Vertices[0].Position.Y == 0, "Click or jitter moved vertex.");
});
Check("Dragging starts at threshold and continues under cursor even back at press position", () =>
{
    var polygon = Square(); var scene = new Scene(new[] { polygon });
    var interaction = new PolygonEditor.App.Interaction.EditorInteraction();
    interaction.BeginDrag(scene, new(6, 0));
    interaction.Move(new(8, 0));
    Require(polygon.Vertices[0].Position.X == 0, "Drag started below threshold.");
    interaction.Move(new(9, 0));
    Require(polygon.Vertices[0].Position.X == 9, "Threshold did not start drag.");
    interaction.Move(new(6, 0));
    Require(polygon.Vertices[0].Position.X == 6, "Active drag stopped near press position.");
    interaction.EndDrag();
    interaction.BeginDrag(scene, new(8, 0));
    interaction.Move(new(8, 0)); interaction.EndDrag();
    Require(polygon.Vertices[0].Position.X == 6, "Next click inherited active drag state.");
});
Check("Click inside polygon with jitter does not translate it", () =>
{
    var polygon = Square(); var interaction = new PolygonEditor.App.Interaction.EditorInteraction();
    interaction.BeginDrag(new Scene(new[] { polygon }), new(50, 50));
    interaction.Move(new(51, 51)); interaction.EndDrag();
    Require(polygon.Vertices[0].Position.X == 0 && polygon.Vertices[0].Position.Y == 0, "Click translated polygon.");
});
Check("Polygon drag uses a fixed snapshot without cumulative drift", () =>
{
    var polygon = Square(); var scene = new Scene(new[] { polygon });
    var interaction = new PolygonEditor.App.Interaction.EditorInteraction();
    var ids = polygon.Edges.Select(e => e.Id).ToArray();
    interaction.BeginDrag(scene, new(50, 50));
    interaction.Move(new(60, 70)); interaction.Move(new(80, 90));
    Require(polygon.Vertices[0].Position.X == 30 && polygon.Vertices[0].Position.Y == 40, "Cumulative movement.");
    interaction.Move(new(50, 50));
    Require(polygon.Vertices[0].Position.X == 0 && polygon.Vertices[0].Position.Y == 0, "Return to origin failed.");
    Require(ids.SequenceEqual(polygon.Edges.Select(e => e.Id)), "Movement changed edge identities.");
});
Check("Selection supports insert, delete and clear", () =>
{
    var polygon = Square(); var scene = new Scene(new[] { polygon });
    var interaction = new PolygonEditor.App.Interaction.EditorInteraction();
    interaction.BeginDrag(scene, new(50, 0)); interaction.EndDrag();
    Require(interaction.InsertVertex() && interaction.Selection?.Vertex is not null, "Insert selection failed.");
    Require(interaction.DeleteVertex() && polygon.Vertices.Count == 4, "Delete selection failed.");
    interaction.Clear(); Require(interaction.Selection is null && !interaction.DeleteVertex(), "Clear failed.");
    Closed(polygon);
});
Check("Self-intersection interior and topmost polygon are selectable", () =>
{
    var bow = new Polygon(new[] { V(0,0), V(100,100), V(0,100), V(100,0) });
    Require(PolygonEditor.Core.Geometry.HitTesting.Contains(bow, new(50, 10)), "Bow-tie lobe was rejected.");
    Require(!PolygonEditor.Core.Geometry.HitTesting.Contains(bow, new(5, 50)), "Outside bow-tie was accepted.");
    var bottom = Square(); var top = Square(); var scene = new Scene(new[] { bottom, top });
    Require(PolygonEditor.Core.Geometry.HitTesting.Pick(scene, new(50, 50))?.Polygon == top, "Wrong polygon layer.");
});
BresenhamChecks.Run(Check);
ConstraintChecks.Run(Check);
Console.WriteLine($"RESULT: {checks - failures}/{checks} checks passed.");
return failures == 0 ? 0 : 1;
