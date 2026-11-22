using System.Diagnostics;
using System.Numerics;
using T = BezierSurfaceRenderer.App.Theme;

namespace BezierSurfaceRenderer.App;

/// <summary>
/// Layout (built in code, no designer file):
///   MenuStrip   File (Load texture, Load normal map, Exit) · View (Reset orientation) ·
///               Colors (Light color, Object color) · Help (Documentation)
///   Left        ViewportControl (fills)
///   Right       control panel, grouped:
///     GEOMETRY  slider "Triangulation" · checkboxes Bézier control net / Triangle mesh / Filled triangles
///     MATERIAL  sliders kd, ks, m · surface picker · radios Solid color / Texture (per surface)
///     SURFACE   checkbox Normal map
///     LIGHT     slider Light z · checkboxes Animate / Edit light path
/// Owns the render loop timer (≈60 Hz while the light animates; on-demand redraw otherwise).
/// Keyboard shortcuts are handled in ProcessCmdKey so they work regardless of focused control.
/// </summary>
public sealed class MainForm : Form
{
    private const int PanelWidth = 300;
    private const int ContentWidth = PanelWidth - 2 * 16 - SystemScrollAllowance;
    private const int SystemScrollAllowance = 18;

    private const float LightPathRadius = 2.0f;

    private static readonly string AssetsDir = Path.Combine(AppContext.BaseDirectory, "Assets");

    private readonly Scene _scene;
    private readonly LightSource _light;
    private readonly ViewportControl _viewport = new() { Dock = DockStyle.Fill };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 15 };
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private TimeSpan _lastTick;

    private Texture? _normalMap;
    private Color _lightColor = Color.White;

    // Controls
    private LabeledSlider _divisions = null!, _kd = null!, _ks = null!, _m = null!, _lightZ = null!;
    private CheckBox _showNet = null!, _showMesh = null!, _showFill = null!;
    private ComboBox _surfacePicker = null!;
    private RadioButton _solid = null!, _textured = null!;
    private CheckBox _normalMapOn = null!, _animate = null!, _editPath = null!;
    private Panel _lightSwatch = null!, _objectSwatch = null!;
    private Label _textureLabel = null!, _normalMapLabel = null!;

    public MainForm(Scene scene)
    {
        _scene = scene;
        _light = LightSource.CreateDefault(LightPathRadius, 2.5f);

        SuspendLayout();
        AutoScaleDimensions = new SizeF(96f, 96f); // layout is authored at 100 %; scale for higher DPI
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Bézier Surface Renderer";
        MinimumSize = new Size(900, 640);
        Size = new Size(1360, 880);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        T.Apply(this);

        LoadDefaultAssets();

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
        _kd.Value, _ks.Value, (int)_m.Value, ToVector(_lightColor), _light.Position,
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
        _objectSwatch.BackColor = ToColor(surface.SolidColor);
        _textureLabel.Text = surface.Texture?.Name ?? "(none)";
    }

    private void SetUseTexture(bool useTexture)
    {
        SelectedSurface.UseTexture = useTexture;
        _viewport.Invalidate();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = _clock.Elapsed;
        var dt = now - _lastTick;
        _lastTick = now;
        if (!_light.IsRunning) return;
        _light.Advance(dt);
        _viewport.Invalidate();
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
            _normalMapLabel.Text = texture.Name;
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
        if (light) { _lightColor = dialog.Color; _lightSwatch.BackColor = dialog.Color; }
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
            case Keys.OemOpenBrackets: _divisions.Value -= 1; return true;
            case Keys.OemCloseBrackets: _divisions.Value += 1; return true;
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
        var menu = new MenuStrip { Dock = DockStyle.Top, Padding = new Padding(8, 4, 0, 4) };

        var file = new ToolStripMenuItem("&File");
        file.DropDownItems.Add(Item("Load &texture…", "Ctrl+T", () => LoadImage(false)));
        file.DropDownItems.Add(Item("Load &normal map…", "Ctrl+N", () => LoadImage(true)));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("E&xit", "Alt+F4", Close));

        var view = new ToolStripMenuItem("&View");
        view.DropDownItems.Add(Item("&Reset orientation", "R", ResetView));

        var colors = new ToolStripMenuItem("&Colors");
        colors.DropDownItems.Add(Item("&Light color (IL)…", "Ctrl+L", () => PickColor(true)));
        colors.DropDownItems.Add(Item("&Object color (IO) of selected surface…", "Ctrl+K", () => PickColor(false)));

        var help = new ToolStripMenuItem("&Help");
        help.DropDownItems.Add(Item("&Documentation", "F1", ShowHelp));

        menu.Items.AddRange([file, view, colors, help]);
        T.Apply(menu);
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
        var host = new Panel { Dock = DockStyle.Right, Width = PanelWidth, BackColor = T.Panel };
        host.Paint += (_, e) =>
        {
            using var pen = new Pen(T.Border);
            e.Graphics.DrawLine(pen, 0, 0, 0, host.Height);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16, 12, 16, 16),
            BackColor = T.Panel,
        };
        host.Controls.Add(flow);

        flow.Controls.Add(new Label
        {
            Text = "BÉZIER / RENDER", Font = T.Title, ForeColor = T.Text, AutoSize = true,
            Margin = new Padding(0, 0, 0, 2),
        });
        flow.Controls.Add(new Label
        {
            Text = "drag to rotate · F1 for help", Font = T.MonoSmall, ForeColor = T.TextMuted, AutoSize = true,
            Margin = new Padding(1, 0, 0, 6),
        });

        // GEOMETRY
        flow.Controls.Add(Section("GEOMETRY"));
        _divisions = Slider("Triangulation (cells per side)", 1, 64, 1, "0", 16);
        _divisions.ValueChanged += (_, _) => { _scene.SetDivisions((int)_divisions.Value); _viewport.Invalidate(); };
        _scene.SetDivisions((int)_divisions.Value);
        flow.Controls.Add(_divisions);
        _showNet = Check("Bézier control net", false, "1");
        _showMesh = Check("Triangle mesh", true, "2");
        _showFill = Check("Filled triangles", true, "3");
        flow.Controls.AddRange([_showNet, _showMesh, _showFill]);

        // MATERIAL
        flow.Controls.Add(Section("MATERIAL"));
        _kd = Slider("kd · diffuse", 0, 1, 0.01f, "0.00", 0.5f);
        _ks = Slider("ks · specular", 0, 1, 0.01f, "0.00", 0.5f);
        _m = Slider("m · shininess", 1, 100, 1, "0", 20);
        flow.Controls.AddRange([_kd, _ks, _m]);
        foreach (var s in new[] { _kd, _ks, _m }) s.ValueChanged += (_, _) => _viewport.Invalidate();

        flow.Controls.Add(Caption("Object color IO of surface   [S]"));
        _surfacePicker = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList, Width = ContentWidth - 6, FlatStyle = FlatStyle.Flat,
            BackColor = T.PanelRaised, ForeColor = T.Text, Margin = new Padding(3, 2, 0, 4),
        };
        for (int k = 0; k < _scene.Surfaces.Count; k++) _surfacePicker.Items.Add($"Surface {k + 1}");
        _surfacePicker.SelectedIndex = 0;
        _surfacePicker.SelectedIndexChanged += (_, _) => ShowSelectedMaterial();
        flow.Controls.Add(_surfacePicker);

        _solid = Radio("Solid color", true);
        _objectSwatch = Swatch(Color.White, () => PickColor(false));
        flow.Controls.Add(Row(_solid, _objectSwatch));
        _textured = Radio("Texture", false);
        _textureLabel = FileLabel("(none)");
        flow.Controls.Add(Row(_textured, MiniButton("Load…", () => LoadImage(false))));
        flow.Controls.Add(_textureLabel);
        // The two radios live in different rows (containers), so WinForms would not group them.
        _solid.CheckedChanged += (_, _) => { if (_solid.Checked) { _textured.Checked = false; SetUseTexture(false); } };
        _textured.CheckedChanged += (_, _) => { if (_textured.Checked) { _solid.Checked = false; SetUseTexture(true); } };
        ShowSelectedMaterial();

        // SURFACE
        flow.Controls.Add(Section("SURFACE DETAIL"));
        _normalMapOn = Check("Normal map", false, "N");
        flow.Controls.Add(Row(_normalMapOn, MiniButton("Load…", () => LoadImage(true))));
        _normalMapLabel = FileLabel(_normalMap?.Name ?? "(none)");
        flow.Controls.Add(_normalMapLabel);

        // LIGHT
        flow.Controls.Add(Section("LIGHT"));
        _lightZ = Slider("Light z (plane of the path)", -2, 6, 0.05f, "0.00", _light.Z);
        _lightZ.ValueChanged += (_, _) => { _light.Z = _lightZ.Value; _viewport.Invalidate(); };
        flow.Controls.Add(_lightZ);
        flow.Controls.Add(Caption("Light color IL"));
        _lightSwatch = Swatch(_lightColor, () => PickColor(true));
        flow.Controls.Add(Row(MiniButton("Choose…", () => PickColor(true)), _lightSwatch));
        _animate = Check("Animate light", true, "Space");
        _editPath = Check("Edit light path", false, "E");
        flow.Controls.AddRange([_animate, _editPath]);

        return host;
    }

    private static Label Section(string text) => new()
    {
        Text = text,
        Font = T.Section,
        ForeColor = T.Accent,
        AutoSize = false,
        Width = ContentWidth,
        Height = 26,
        TextAlign = ContentAlignment.BottomLeft,
        Margin = new Padding(0, 14, 0, 4),
    };

    private static Label Caption(string text) => new()
    {
        Text = text, ForeColor = T.TextMuted, AutoSize = true, Margin = new Padding(3, 6, 0, 2),
    };

    private static Label FileLabel(string text) => new()
    {
        Text = text, Font = T.MonoSmall, ForeColor = T.TextMuted, AutoSize = false, Width = ContentWidth,
        Height = 16, AutoEllipsis = true, Margin = new Padding(22, 0, 0, 2),
    };

    private static LabeledSlider Slider(string caption, float min, float max, float step, string format, float value) => new()
    {
        Caption = caption, Minimum = min, Maximum = max, Step = step, Format = format, Value = value,
        Width = ContentWidth, Margin = new Padding(0, 2, 0, 2),
    };

    private CheckBox Check(string text, bool value, string shortcut)
    {
        var box = new CheckBox
        {
            Text = $"{text}   [{shortcut}]", Checked = value, AutoSize = true, ForeColor = T.Text,
            FlatStyle = FlatStyle.Flat, Margin = new Padding(3, 3, 0, 3), Cursor = Cursors.Hand,
        };
        box.FlatAppearance.BorderColor = T.TextMuted;
        box.FlatAppearance.CheckedBackColor = T.Border;
        box.CheckedChanged += (_, _) => SyncViewFlags();
        return box;
    }

    private RadioButton Radio(string text, bool value)
    {
        var radio = new RadioButton
        {
            Text = text, Checked = value, AutoSize = true, ForeColor = T.Text, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(3, 3, 0, 3), Cursor = Cursors.Hand,
        };
        radio.FlatAppearance.BorderColor = T.TextMuted;
        radio.FlatAppearance.CheckedBackColor = T.Border;
        radio.CheckedChanged += (_, _) => _viewport.Invalidate();
        return radio;
    }

    private static Button MiniButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = T.PanelRaised,
            ForeColor = T.Text, Cursor = Cursors.Hand, Margin = new Padding(6, 0, 0, 0),
        };
        button.FlatAppearance.BorderColor = T.Border;
        button.FlatAppearance.MouseOverBackColor = T.Border;
        button.Click += (_, _) => action();
        return button;
    }

    private static Panel Swatch(Color color, Action onClick)
    {
        var swatch = new Panel
        {
            BackColor = color, Size = new Size(36, 20), Cursor = Cursors.Hand,
            BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(8, 4, 0, 0),
        };
        swatch.Click += (_, _) => onClick();
        return swatch;
    }

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true,
            Margin = new Padding(0), Padding = new Padding(0), BackColor = T.Panel,
        };
        row.Controls.AddRange(controls);
        return row;
    }
}
