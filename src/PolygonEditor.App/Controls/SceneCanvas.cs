using Avalonia.Controls;
using Avalonia.Media;
using PolygonEditor.App.Rendering;
using PolygonEditor.Core.Model;

namespace PolygonEditor.App.Controls;

// Łączy model sceny z odświeżaniem kontrolki; rysowanie wykonuje SceneRenderer.
public sealed class SceneCanvas : Control
{
    private Scene? scene;
    private readonly SceneRenderer renderer = new SceneRenderer();

    public void SetScene(Scene scene)
    {
        ArgumentNullException.ThrowIfNull(scene);
        this.scene = scene;
        InvalidateVisual(); // zgłasza potrzebę odświeżenia
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (scene == null)
            return;
        renderer.Draw(context, scene);
    }

}
