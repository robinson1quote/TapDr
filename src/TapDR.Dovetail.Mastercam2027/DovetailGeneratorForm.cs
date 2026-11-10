using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using TapDR.Dovetail.Core;

namespace TapDR.Dovetail.Mastercam2027;

internal sealed class DovetailGeneratorForm : Form
{
    private readonly ComboBox _hardware = new();
    private readonly ComboBox _units = new();
    private readonly TextBox _length = Input("4.000");
    private readonly TextBox _width = Input("2.000");
    private readonly TextBox _height = Input("1.000");
    private readonly TextBox _depth = Input("0.080");
    private readonly TextBox _leftFlat = Input("0.250");
    private readonly TextBox _rightFlat = Input("0.250");
    private readonly TextBox _angle = Input("45.0");
    private readonly TextBox _edgeBreak = Input("0.010");
    private readonly TextBox _offsetX = Input("0");
    private readonly TextBox _offsetY = Input("0");
    private readonly TextBox _offsetZ = Input("0");
    private readonly CheckBox _centerWidthOnY = new() { Text = "Center stock width on Y0", AutoSize = true };
    private readonly CheckBox _centerLengthOnX = new() { Text = "Center stock length on X0", AutoSize = true };
    private readonly Label _unitLabel = new();
    private readonly Label _status = new();
    private readonly Label _dimensionSummary = new();
    private readonly Button _generate = new();
    private readonly CrossSectionPreviewPanel _preview = new();

    private LengthUnit _currentUnit = LengthUnit.Inch;
    private ViseDovetailCrossSection? _currentProfile;

    public DovetailGeneratorForm()
    {
        Text = "TapDR 5th Axis Stock Generator";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 650);
        Size = new Size(1180, 760);
        BackColor = Color.FromArgb(241, 244, 248);
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;

        BuildLayout();
        WireEvents();
        UpdatePreview();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(14),
            BackColor = BackColor
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        Controls.Add(root);

        var left = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = BackColor };
        var leftStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(0, 0, 8, 8)
        };
        left.Controls.Add(leftStack);
        root.Controls.Add(left, 0, 0);

        var title = new Label
        {
            Text = "TapDR  |  5TH AXIS STOCK GENERATOR",
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 39, 63),
            AutoSize = false,
            Width = 430,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft
        };
        leftStack.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "Create native, editable vise stock-prep geometry in the active Mastercam file.",
            ForeColor = Color.FromArgb(83, 99, 119),
            AutoSize = false,
            Width = 430,
            Height = 38
        };
        leftStack.Controls.Add(subtitle);

        var hardwareGroup = MakeGroup("HARDWARE", 2, 430);
        ConfigureCombo(_hardware, ["V75100X — R96-V75100 adapter", "V562 — original / 96 mm mount", "V6105 — 96 mm mount"]);
        ConfigureCombo(_units, ["Inch", "Millimeter"]);
        AddRow(hardwareGroup, 0, "Vise model", _hardware);
        AddRow(hardwareGroup, 1, "Input units", _units);
        leftStack.Controls.Add(hardwareGroup);

        var stockGroup = MakeGroup("STOCK SIZE", 3, 430);
        AddRow(stockGroup, 0, "Length", _length);
        AddRow(stockGroup, 1, "Width", _width);
        AddRow(stockGroup, 2, "Height", _height);
        leftStack.Controls.Add(stockGroup);

        var profileGroup = MakeGroup("DSP-V DOVETAIL PROFILE", 5, 430);
        AddRow(profileGroup, 0, "Dovetail depth", _depth);
        AddRow(profileGroup, 1, "Left locating flat", _leftFlat);
        AddRow(profileGroup, 2, "Right locating flat", _rightFlat);
        AddRow(profileGroup, 3, "Flank angle (degrees)", _angle);
        AddRow(profileGroup, 4, "Toe edge break (max 0.010)", _edgeBreak);
        leftStack.Controls.Add(profileGroup);

        var placementGroup = MakeGroup("PLACEMENT IN MASTERCAM", 4, 430);
        AddRow(placementGroup, 0, "Origin offset X", _offsetX);
        AddRow(placementGroup, 1, "Origin offset Y", _offsetY);
        AddRow(placementGroup, 2, "Origin offset Z", _offsetZ);
        var checkPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Width = 410,
            Padding = new Padding(6, 2, 6, 4)
        };
        checkPanel.Controls.Add(_centerWidthOnY);
        checkPanel.Controls.Add(_centerLengthOnX);
        var placementTable = (TableLayoutPanel)placementGroup.Tag!;
        placementTable.Controls.Add(checkPanel, 0, 3);
        placementTable.SetColumnSpan(checkPanel, 2);
        placementTable.RowStyles[3].SizeType = SizeType.AutoSize;
        leftStack.Controls.Add(placementGroup);

        _unitLabel.Text = "Dimensions in inches";
        _unitLabel.ForeColor = Color.FromArgb(83, 99, 119);
        _unitLabel.AutoSize = true;
        leftStack.Controls.Add(_unitLabel);

        _status.Text = "Enter stock dimensions to preview.";
        _status.ForeColor = Color.FromArgb(83, 99, 119);
        _status.AutoSize = false;
        _status.Width = 430;
        _status.Height = 44;
        _status.Padding = new Padding(4, 8, 4, 4);
        leftStack.Controls.Add(_status);

        _generate.Text = "GENERATE NATIVE SOLID";
        _generate.Width = 430;
        _generate.Height = 46;
        _generate.FlatStyle = FlatStyle.Flat;
        _generate.FlatAppearance.BorderSize = 0;
        _generate.BackColor = Color.FromArgb(24, 103, 173);
        _generate.ForeColor = Color.White;
        _generate.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
        _generate.Cursor = Cursors.Hand;
        leftStack.Controls.Add(_generate);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.White,
            Padding = new Padding(14),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.Controls.Add(right, 1, 0);

        var previewTitle = new Label
        {
            Text = "LIVE STOCK PREVIEW",
            Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(22, 39, 63),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        right.Controls.Add(previewTitle, 0, 0);

        _preview.Dock = DockStyle.Fill;
        _preview.BackColor = Color.FromArgb(250, 251, 253);
        _preview.BorderStyle = BorderStyle.FixedSingle;
        right.Controls.Add(_preview, 0, 1);

        _dimensionSummary.Text = "4.000 × 2.000 × 1.000 in";
        _dimensionSummary.ForeColor = Color.FromArgb(63, 79, 99);
        _dimensionSummary.Dock = DockStyle.Fill;
        _dimensionSummary.TextAlign = ContentAlignment.MiddleLeft;
        _dimensionSummary.Padding = new Padding(4, 0, 4, 0);
        right.Controls.Add(_dimensionSummary, 0, 2);
    }

    private void WireEvents()
    {
        foreach (var textBox in new[]
        {
            _length, _width, _height, _depth, _leftFlat, _rightFlat,
            _angle, _edgeBreak, _offsetX, _offsetY, _offsetZ
        })
        {
            textBox.TextChanged += (_, _) => UpdatePreview();
        }

        _hardware.SelectedIndexChanged += (_, _) => UpdatePreview();
        _units.SelectedIndexChanged += (_, _) => ChangeUnits();
        _centerWidthOnY.CheckedChanged += (_, _) => UpdatePreview();
        _centerLengthOnX.CheckedChanged += (_, _) => UpdatePreview();
        _generate.Click += (_, _) => GenerateSolid();
    }

    private void ChangeUnits()
    {
        var next = _units.SelectedIndex == 1 ? LengthUnit.Millimeter : LengthUnit.Inch;
        if (next == _currentUnit)
        {
            UpdatePreview();
            return;
        }

        foreach (var textBox in new[]
        {
            _length, _width, _height, _depth, _leftFlat, _rightFlat, _edgeBreak,
            _offsetX, _offsetY, _offsetZ
        })
        {
            if (TryParse(textBox.Text, out var value))
            {
                var converted = LengthUnits.Convert(value, _currentUnit, next);
                textBox.Text = converted.ToString("0.######", CultureInfo.InvariantCulture);
            }
        }

        _currentUnit = next;
        _unitLabel.Text = next == LengthUnit.Inch ? "Dimensions in inches" : "Dimensions in millimeters";
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        try
        {
            var parameters = ReadParameters();
            var validation = ViseStockPrepValidator.Validate(parameters);
            if (!validation.CanGenerate)
                throw new ArgumentException(string.Join(" ", validation.Issues
                    .Where(issue => issue.Severity == ValidationSeverity.Error)
                    .Select(issue => issue.Message)));

            _currentProfile = ViseDovetailProfileGenerator.Generate(parameters);
            _preview.Profile = _currentProfile;
            _preview.StockLength = parameters.StockLength;
            _preview.InputUnit = parameters.Unit;
            _generate.Enabled = true;
            _status.ForeColor = validation.HasWarnings
                ? Color.FromArgb(161, 102, 22)
                : Color.FromArgb(52, 111, 83);
            _status.Text = validation.HasWarnings
                ? string.Join(" ", validation.Issues.Where(i => i.Severity == ValidationSeverity.Warning).Select(i => i.Message))
                : "Preview checks passed. Verify the generated stock before machining; production validation is still pending.";
            _dimensionSummary.Text =
                $"{Format(parameters.StockLength)} × {Format(parameters.StockWidth)} × {Format(parameters.StockHeight)} {UnitSuffix(parameters.Unit)}";
            _preview.Invalidate();
        }
        catch (Exception ex)
        {
            _currentProfile = null;
            _preview.Profile = null;
            _generate.Enabled = false;
            _status.ForeColor = Color.FromArgb(170, 62, 58);
            _status.Text = ex.Message;
            _dimensionSummary.Text = "Correct the dimensions to preview the stock.";
            _preview.Invalidate();
        }
    }

    private void GenerateSolid()
    {
        try
        {
            var parameters = ReadParameters();
            var profile = ViseDovetailProfileGenerator.Generate(parameters);
            var model = _hardware.SelectedIndex switch
            {
                0 => "V75100X / R96-V75100",
                1 => "V562",
                _ => "V6105"
            };

            var result = MastercamNativeSolidBuilder.CreateViseBlank(
                profile,
                parameters.StockLength,
                parameters.Unit,
                model,
                ReadValue(_offsetX, "Offset X"),
                ReadValue(_offsetY, "Offset Y"),
                ReadValue(_offsetZ, "Offset Z"),
                _centerWidthOnY.Checked,
                _centerLengthOnX.Checked);

            _status.ForeColor = Color.FromArgb(35, 119, 79);
            _status.Text =
                $"Created {result.Descriptor}. Hidden profile sketch is on Mastercam level {result.ConstructionLevel}.";
        }
        catch (Exception ex)
        {
            _status.ForeColor = Color.FromArgb(170, 62, 58);
            _status.Text = ex.GetBaseException().Message;
        }
    }

    private ViseStockPrepParameters ReadParameters() =>
        new()
        {
            StockLength = ReadValue(_length, "Stock length"),
            StockWidth = ReadValue(_width, "Stock width"),
            StockHeight = ReadValue(_height, "Stock height"),
            Unit = _currentUnit,
            DovetailDepth = ReadValue(_depth, "Dovetail depth"),
            LeftLocatingFlat = ReadValue(_leftFlat, "Left locating flat"),
            RightLocatingFlat = ReadValue(_rightFlat, "Right locating flat"),
            FlankAngleDegrees = ReadValue(_angle, "Flank angle"),
            MaximumEdgeBreak = ReadValue(_edgeBreak, "Toe edge break")
        };

    private static double ReadValue(TextBox input, string label)
    {
        if (!TryParse(input.Text, out var value))
            throw new ArgumentException($"{label} must be a valid number using a decimal point.");
        return value;
    }

    private static bool TryParse(string text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        && double.IsFinite(value);

    private static string Format(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string UnitSuffix(LengthUnit unit) =>
        unit == LengthUnit.Inch ? "in" : "mm";

    private static TextBox Input(string value) => new()
    {
        Text = value,
        Dock = DockStyle.Fill,
        TextAlign = HorizontalAlignment.Right,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(4, 3, 4, 3)
    };

    private static void ConfigureCombo(ComboBox combo, string[] options)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Items.AddRange(options);
        combo.Dock = DockStyle.Fill;
        combo.SelectedIndex = 0;
        combo.Margin = new Padding(4, 3, 4, 3);
    }

    private static GroupBox MakeGroup(string title, int rows, int width)
    {
        var group = new GroupBox
        {
            Text = title,
            Width = width,
            Height = rows * 38 + 32,
            ForeColor = Color.FromArgb(42, 61, 82),
            Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
            Padding = new Padding(8, 12, 8, 6),
            Margin = new Padding(0, 7, 0, 3)
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rows,
            Padding = new Padding(4, 0, 4, 0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        for (var i = 0; i < rows; i++)
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        group.Controls.Add(table);
        group.Tag = table;
        return group;
    }

    private static void AddRow(GroupBox group, int row, string label, Control input)
    {
        var table = (TableLayoutPanel)group.Tag!;
        var caption = new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(74, 88, 105),
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleLeft
        };
        table.Controls.Add(caption, 0, row);
        table.Controls.Add(input, 1, row);
    }
}

internal sealed class CrossSectionPreviewPanel : Panel
{
    public ViseDovetailCrossSection? Profile { get; set; }
    public double StockLength { get; set; }
    public LengthUnit InputUnit { get; set; } = LengthUnit.Inch;

    public CrossSectionPreviewPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        if (Profile is null || !double.IsFinite(StockLength) || StockLength <= 0)
        {
            TextRenderer.DrawText(
                g,
                "Enter valid stock dimensions to see the live preview.",
                Font,
                ClientRectangle,
                Color.FromArgb(118, 130, 145),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            return;
        }

        var lengthInches = LengthUnits.Convert(StockLength, InputUnit, LengthUnit.Inch);
        var scaleToInches = LengthUnits.Convert(1, InputUnit, LengthUnit.Inch);

        var section = Profile.Boundary
            .Select(p => new PointF((float)(p.X * scaleToInches), (float)(p.Y * scaleToInches)))
            .ToArray();

        const float cos30 = 0.8660254F;
        const float sin30 = 0.5F;
        PointF Project(PointF p, float z) =>
            new((p.X - z) * cos30, p.Y - (p.X + z) * sin30);

        var projected = section.SelectMany(p => new[] { Project(p, 0), Project(p, (float)lengthInches) }).ToArray();
        var minX = projected.Min(p => p.X);
        var maxX = projected.Max(p => p.X);
        var minY = projected.Min(p => p.Y);
        var maxY = projected.Max(p => p.Y);
        var rangeX = Math.Max(maxX - minX, 0.001F);
        var rangeY = Math.Max(maxY - minY, 0.001F);
        var availableWidth = Math.Max(ClientSize.Width - 72, 80);
        var availableHeight = Math.Max(ClientSize.Height - 86, 80);
        var scale = Math.Min(availableWidth / rangeX, availableHeight / rangeY);
        scale = Math.Max(0.01F, scale);

        var centerX = ClientSize.Width / 2F;
        var centerY = ClientSize.Height / 2F + 3;
        PointF Map(PointF p)
        {
            var x = centerX + (p.X - (minX + maxX) / 2F) * scale;
            var y = centerY - (p.Y - (minY + maxY) / 2F) * scale;
            return new PointF(x, y);
        }

        var front = section.Select(p => Map(Project(p, 0))).ToArray();
        var back = section.Select(p => Map(Project(p, (float)lengthInches))).ToArray();

        using var sideBrush = new SolidBrush(Color.FromArgb(208, 218, 230));
        using var endBrush = new SolidBrush(Color.FromArgb(229, 235, 242));
        using var edgePen = new Pen(Color.FromArgb(47, 69, 94), 1.8F);

        for (var i = 0; i < front.Length; i++)
        {
            var next = (i + 1) % front.Length;
            var face = new[] { front[i], front[next], back[next], back[i] };
            g.FillPolygon(sideBrush, face);
            g.DrawPolygon(edgePen, face);
        }

        g.FillPolygon(endBrush, back);
        g.DrawPolygon(edgePen, back);
        g.DrawPolygon(edgePen, front);

        for (var i = 0; i < front.Length; i++)
            g.DrawLine(edgePen, front[i], back[i]);

        using var titleFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
        using var metaFont = new Font("Segoe UI", 8.5F);
        TextRenderer.DrawText(g, "Live stock preview", titleFont,
            new Point(12, 12), Color.FromArgb(36, 55, 77));
        var note = $"Length {StockLength:0.###} {UnitSuffix(InputUnit)}  |  Width {Profile.StockWidth:0.###} {UnitSuffix(InputUnit)}  |  Height {Profile.StockHeight:0.###} {UnitSuffix(InputUnit)}";
        TextRenderer.DrawText(g, note, metaFont,
            new Rectangle(12, ClientSize.Height - 28, ClientSize.Width - 24, 20),
            Color.FromArgb(94, 109, 127),
            TextFormatFlags.EndEllipsis);
    }

    private static string UnitSuffix(LengthUnit unit) =>
        unit == LengthUnit.Inch ? "in" : "mm";
}
