using System.Numerics;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// Main window (built in code, no designer file): menu on top, ViewportControl on the left and a
/// panel of standard WinForms controls on the right (one top-down list). Owns the animation timer.
/// Keyboard shortcuts are handled in ProcessCmdKey so they work regardless of the focused control.
/// </summary>
public sealed class MainForm : Form
{
    private const int PanelWidth = 260;
    private const int ContentWidth = PanelWidth - 40; // leaves room for the panel's vertical scroll bar

    private const float LightPathRadius = 2.0f;

    /// <summary>Start (and Reset) view: tilted, so the surfaces are seen in 3D and not straight from above as a square.</summary>
    private static readonly Mat3 StartView = Mat3.RotationX(-0.9f) * Mat3.RotationZ(0.6f);

    private static readonly string AssetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");

    private readonly Scene _scene;
    private readonly LightSource _light;
    private readonly ViewportControl _viewport = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };

    private Texture? _normalMap;
    private Color _lightColor = Color.White;

    // Controls
    private TrackBar _divisions = null!, _kd = null!, _ks = null!, _m = null!, _lightZ = null!;
    private CheckBox _showNet = null!, _showMesh = null!, _showFill = null!;
    private ComboBox _surfacePicker = null!;
    private RadioButton _solid = null!, _textured = null!;
    private CheckBox _normalMapOn = null!, _animate = null!, _editPath = null!;
    private Button _objectColor = null!, _lightColorButton = null!;

    public MainForm(Scene scene)
    {
        _scene = scene;
        _light = LightSource.CreateDefault(LightPathRadius, 2.5f);

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96f, 96f); // layout is authored at 100 %; scale for higher DPI
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 9f);
        Text = "Bézier Surface Renderer";
        MinimumSize = new Size(800, 600);
        Size = new Size(1200, 800);
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Maximized; // at 150 % scaling the default size would not fit the screen
        KeyPreview = true;

        LoadDefaultAssets();
        _scene.Rotate(StartView);

        // Dock order: the Fill control must be added first so it receives the remaining space.
        Controls.Add(_viewport);
        Controls.Add(BuildPanel());
        Controls.Add(BuildMenu());

        _viewport.Scene = _scene;
        _viewport.Light = _light;
        _viewport.SettingsSource = CurrentSettings;
        SyncViewFlags();

        _timer.Tick += OnTick;
        _timer.Start();
        ResumeLayout(performLayout: true);
    }

    // ---------------------------------------------------------------- settings

    private RenderSettings CurrentSettings() => new(
        _kd.Value / 100f, _ks.Value / 100f, _m.Value, ToVector(_lightColor), _light.Position,
        _normalMapOn.Checked ? _normalMap : null);

    private static Vector3 ToVector(Color c) => new Vector3(c.R, c.G, c.B) / 255f;
    private static Color ToColor(Vector3 v) => Color.FromArgb((int)(v.X * 255), (int)(v.Y * 255), (int)(v.Z * 255));

    /// <summary>Surface whose object colour IO is edited by the panel (IO is chosen per surface).</summary>
    private BezierSurface SelectedSurface => _scene.Surfaces[Math.Max(0, _surfacePicker.SelectedIndex)];

    /// <summary>Shows the selected surface's IO in the panel.</summary>
    private void ShowSelectedMaterial()
    {
        var surface = SelectedSurface;
        _solid.Checked = !surface.UseTexture;
        _textured.Checked = surface.UseTexture;
        _textured.Text = $"Texture ({surface.Texture?.Name ?? "none"})";
        _objectColor.BackColor = ToColor(surface.SolidColor);
    }

    /// <summary>Only requests a redraw; the viewport moves the light by the real time elapsed since its last frame.</summary>
    private void OnTick(object? sender, EventArgs e)
    {
        if (_light.IsRunning) _viewport.Invalidate();
    }

    private void SyncViewFlags()
    {
        _viewport.ShowControlNet = _showNet.Checked;
        _viewport.ShowWireframe = _showMesh.Checked;
        _viewport.ShowFilled = _showFill.Checked;
        _viewport.EditLightPath = _editPath.Checked;
        _light.IsRunning = _animate.Checked;
        _viewport.Invalidate();
    }

    // ---------------------------------------------------------------- assets

    /// <summary>Default texture on every surface (the second one starts textured), bricks normal map.</summary>
    private void LoadDefaultAssets()
    {
        var texture = TryLoad(Path.Combine(AssetsDir, "textures", "default.png"), quiet: true);
        foreach (var surface in _scene.Surfaces) surface.Texture = texture;
        _scene.Surfaces[1].UseTexture = texture != null;
        _normalMap = TryLoad(Path.Combine(AssetsDir, "normalmaps", "bricks.png"), quiet: true);
    }

    private Texture? TryLoad(string path, bool quiet)
    {
        try
        {
            return BitmapInterop.LoadTexture(path);
        }
        catch (Exception ex) when (ex is IOException or ArgumentException or UnauthorizedAccessException or OutOfMemoryException)
        {
            // GDI+ reports unsupported/corrupt images as ArgumentException or OutOfMemoryException.
            if (!quiet)
                MessageBox.Show(this, $"Could not load image:\n{path}\n\n{ex.Message}", "Load image",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }
    }

    private void LoadImage(bool isNormalMap)
    {
        using var dialog = new OpenFileDialog
        {
            Title = isNormalMap ? "Load normal map" : "Load texture",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|All files|*.*",
            InitialDirectory = Path.Combine(AssetsDir, isNormalMap ? "normalmaps" : "textures"),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (TryLoad(dialog.FileName, quiet: false) is not { } texture) return;

        if (isNormalMap)
        {
            _normalMap = texture;
            _normalMapOn.Text = $"Normal map ({texture.Name})";
            _normalMapOn.Checked = true;
        }
        else
        {
            SelectedSurface.Texture = texture;
            SelectedSurface.UseTexture = true;
            ShowSelectedMaterial();
        }
        _viewport.Invalidate();
    }

    private void PickColor(bool light)
    {
        using var dialog = new ColorDialog { FullOpen = true, Color = light ? _lightColor : ToColor(SelectedSurface.SolidColor) };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (light) { _lightColor = dialog.Color; _lightColorButton.BackColor = dialog.Color; }
        else
        {
            SelectedSurface.SolidColor = ToVector(dialog.Color);
            SelectedSurface.UseTexture = false;
            ShowSelectedMaterial();
        }
        _viewport.Invalidate();
    }

    private void ShowHelp()
    {
        using var help = new HelpForm();
        help.ShowDialog(this);
    }

    private void ResetView()
    {
        _scene.ResetOrientation();
        _scene.Rotate(StartView);
        _viewport.Invalidate();
    }

    // ---------------------------------------------------------------- keyboard

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F1: ShowHelp(); return true;
            case Keys.Space: _animate.Checked = !_animate.Checked; return true;
            case Keys.E: _editPath.Checked = !_editPath.Checked; return true;
            case Keys.R: ResetView(); return true;
            case Keys.D1: _showNet.Checked = !_showNet.Checked; return true;
            case Keys.D2: _showMesh.Checked = !_showMesh.Checked; return true;
            case Keys.D3: _showFill.Checked = !_showFill.Checked; return true;
            case Keys.T: (_textured.Checked ? _solid : _textured).Checked = true; return true;
            case Keys.S: _surfacePicker.SelectedIndex = (_surfacePicker.SelectedIndex + 1) % _surfacePicker.Items.Count; return true;
            case Keys.N: _normalMapOn.Checked = !_normalMapOn.Checked; return true;
            case Keys.OemOpenBrackets: _divisions.Value = Math.Max(_divisions.Minimum, _divisions.Value - 1); return true;
            case Keys.OemCloseBrackets: _divisions.Value = Math.Min(_divisions.Maximum, _divisions.Value + 1); return true;
            case Keys.Control | Keys.T: LoadImage(isNormalMap: false); return true;
            case Keys.Control | Keys.N: LoadImage(isNormalMap: true); return true;
            case Keys.Control | Keys.L: PickColor(light: true); return true;
            case Keys.Control | Keys.K: PickColor(light: false); return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ---------------------------------------------------------------- layout

    private MenuStrip BuildMenu()
    {
        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("Load &texture…", "Ctrl+T", () => LoadImage(false)));
        file.DropDownItems.Add(Item("Load &normal map…", "Ctrl+N", () => LoadImage(true)));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("E&xit", "Alt+F4", Close));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(Item("&Reset orientation", "R", ResetView));

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("&Documentation", "F1", ShowHelp));

        var menu = new MenuStrip();
        menu.Items.AddRange([file, view, help]);
        MainMenuStrip = menu;
        return menu;

        // Shortcuts are only displayed here; ProcessCmdKey handles them so they work everywhere.
        static ToolStripMenuItem Item(string text, string shortcut, Action action)
        {
            var item = new ToolStripMenuItem(text) { ShortcutKeyDisplayString = shortcut };
            item.Click += (_, _) => action();
            return item;
        }
    }

    private Control BuildPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right, Width = PanelWidth, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, AutoScroll = true, Padding = new Padding(10),
        };
        void Add(params Control[] controls) => panel.Controls.AddRange(controls);

        // Geometry
        Add(Header("Geometry"));
        _divisions = Slider(panel, "Triangulation", 1, 64, 16, v => v.ToString());
        _divisions.ValueChanged += (_, _) => { _scene.SetDivisions(_divisions.Value); _viewport.Invalidate(); };
        _scene.SetDivisions(_divisions.Value);
        _showNet = Check("Bézier control net", false);
        _showMesh = Check("Triangle mesh", true);
        _showFill = Check("Filled triangles", true);
        Add(_showNet, _showMesh, _showFill);

        // Lighting coefficients
        Add(Header("Material"));
        _kd = Slider(panel, "kd", 0, 100, 50, v => (v / 100f).ToString("0.00"));
        _ks = Slider(panel, "ks", 0, 100, 50, v => (v / 100f).ToString("0.00"));
        _m = Slider(panel, "m", 1, 100, 20, v => v.ToString());
        foreach (var s in new[] { _kd, _ks, _m }) s.ValueChanged += (_, _) => _viewport.Invalidate();

        // Object colour IO, chosen per surface. Both radios sit directly in the panel, so WinForms groups them.
        Add(Header("Object color"));
        _surfacePicker = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = ContentWidth };
        for (int k = 0; k < _scene.Surfaces.Count; k++) _surfacePicker.Items.Add($"Surface {k + 1}");
        _surfacePicker.SelectedIndex = 0;
        _surfacePicker.SelectedIndexChanged += (_, _) => ShowSelectedMaterial();
        _solid = new RadioButton { Text = "Solid color", AutoSize = true };
        _textured = new RadioButton { Text = "Texture", AutoSize = true };
        _textured.CheckedChanged += (_, _) => { SelectedSurface.UseTexture = _textured.Checked; _viewport.Invalidate(); };
        _objectColor = PanelButton("Color…", () => PickColor(false));
        Add(new Label { Text = "Surface to edit (S):", AutoSize = true }, _surfacePicker, _solid, _textured, _objectColor, PanelButton("Load texture…", () => LoadImage(false)));
        ShowSelectedMaterial();

        Add(Header("Normal map"));
        _normalMapOn = Check($"Normal map ({_normalMap?.Name ?? "none"})", false);
        Add(_normalMapOn, PanelButton("Load normal map…", () => LoadImage(true)));

        Add(Header("Light"));
        _lightZ = Slider(panel, "Light z", -40, 120, (int)MathF.Round(_light.Z * 20), v => (v / 20f).ToString("0.00"));
        _lightZ.ValueChanged += (_, _) => { _light.Z = _lightZ.Value / 20f; _viewport.Invalidate(); };
        _lightColorButton = PanelButton("Light color…", () => PickColor(true));
        _lightColorButton.BackColor = _lightColor;
        _animate = Check("Animate light", true);
        _editPath = Check("Edit light path", false);
        Add(_lightColorButton, _animate, _editPath);
        // FlowLayoutPanel ignores its bottom padding when scrolling; an empty spacer lets the last row scroll fully into view.
        Add(new Label { Height = 30 });

        return panel;
    }

    private static Label Header(string text) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold), Margin = new Padding(0, 10, 0, 2),
    };

    /// <summary>Adds "caption: value" label + TrackBar; the label follows the value.</summary>
    private static TrackBar Slider(Control parent, string caption, int min, int max, int value, Func<int, string> format)
    {
        var label = new Label { AutoSize = true, Text = $"{caption}: {format(value)}" };
        var bar = new TrackBar
        {
            Minimum = min, Maximum = max, Value = value, Width = ContentWidth,
            TickStyle = TickStyle.None, AutoSize = false, Height = 30,
        };
        bar.ValueChanged += (_, _) => label.Text = $"{caption}: {format(bar.Value)}";
        parent.Controls.Add(label);
        parent.Controls.Add(bar);
        return bar;
    }

    private CheckBox Check(string text, bool value)
    {
        var box = new CheckBox { Text = text, Checked = value, AutoSize = true };
        box.CheckedChanged += (_, _) => SyncViewFlags();
        return box;
    }

    private static Button PanelButton(string text, Action action)
    {
        var button = new Button { Text = text, Width = ContentWidth, AutoSize = true };
        button.Click += (_, _) => action();
        return button;
    }
}
