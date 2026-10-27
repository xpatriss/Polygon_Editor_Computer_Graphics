using Avalonia.Controls;
using Avalonia.Input;
using PolygonEditor.App.Controls;
using PolygonEditor.App.Rendering;
using PolygonEditor.Core;

namespace PolygonEditor.App.Views;

public partial class MainWindow : Window
{
    private HelpWindow? helpWindow;

    public MainWindow()
    {
        // układ okna z MainWindow.axaml, logika geometrii w Core
        InitializeComponent();

        // kontrolka istnieje po wczytaniu XAML; odnajdujemy ją po x:Name.
        var canvas = this.FindControl<SceneCanvas>("EditorCanvas");
        if (canvas is null)
            throw new InvalidOperationException("EditorCanvas control was not found in MainWindow.");

        // Scenę tworzymy raz, aby odświeżanie zachowywało tożsamość jej elementów.
        var scene = DemoSceneFactory.Create();
        canvas.SetScene(scene);

        // Okno łączy przyciski z kontrolką; obliczenia edycji pozostają w Core.
        var status = this.FindControl<TextBlock>("SelectionStatus")
            ?? throw new InvalidOperationException("SelectionStatus was not found.");
        var insertButton = this.FindControl<Button>("InsertVertexButton")
            ?? throw new InvalidOperationException("InsertVertexButton was not found.");
        var deleteButton = this.FindControl<Button>("DeleteVertexButton")
            ?? throw new InvalidOperationException("DeleteVertexButton was not found.");
        // Po zmianie zaznaczenia okno aktualizuje tekst w panelu.
        canvas.SelectionChanged += (_, _) => status.Text = canvas.SelectionDescription;
        insertButton.Click += (_, _) => canvas.InsertVertex();
        deleteButton.Click += (_, _) => canvas.DeleteVertex();
        status.Text = canvas.SelectionDescription;

        var constraintsPanel = this.FindControl<ConstraintPanel>("ConstraintsPanel")
            ?? throw new InvalidOperationException("ConstraintsPanel was not found.");
        constraintsPanel.Connect(canvas, scene, this);

        var curvesPanel = this.FindControl<CurvePanel>("CurvesPanel")
            ?? throw new InvalidOperationException("CurvesPanel was not found.");
        curvesPanel.Connect(canvas);

        var libraryRadio = this.FindControl<RadioButton>("LibraryLineRadio")
            ?? throw new InvalidOperationException("LibraryLineRadio was not found.");
        var bresenhamRadio = this.FindControl<RadioButton>("BresenhamLineRadio")
            ?? throw new InvalidOperationException("BresenhamLineRadio was not found.");
        // Zdarzenie przychodzi także przy odznaczeniu; reagujemy tylko na wybór danej opcji.
        libraryRadio.IsCheckedChanged += (_, _) =>
        {
            if (libraryRadio.IsChecked == true)
                canvas.SetLineAlgorithm(LineDrawingAlgorithm.Library);
        };
        bresenhamRadio.IsCheckedChanged += (_, _) =>
        {
            if (bresenhamRadio.IsChecked == true)
                canvas.SetLineAlgorithm(LineDrawingAlgorithm.Bresenham);
        };
        var helpButton = this.FindControl<Button>("HelpButton")
            ?? throw new InvalidOperationException("HelpButton was not found.");
        helpButton.Click += (_, _) => ShowHelp();
        KeyDown += (_, args) =>
        {
            if (args.Key != Key.F1) return;
            ShowHelp();
            args.Handled = true;
        };
    }

    // Jedno okno pomocy: ponowne F1 przenosi do niego fokus.
    private void ShowHelp()
    {
        if (helpWindow is not null)
        {
            helpWindow.Activate();
            return;
        }
        helpWindow = new HelpWindow();
        helpWindow.Closed += (_, _) => helpWindow = null;
        helpWindow.Show(this);
    }
}
