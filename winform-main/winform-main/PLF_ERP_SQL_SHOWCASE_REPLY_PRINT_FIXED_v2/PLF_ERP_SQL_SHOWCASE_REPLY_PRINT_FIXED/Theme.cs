using System.Drawing;
using System.Windows.Forms;

namespace PremiumLivingFurnitureWinForms;

public static class Theme
{
    // Softer, more colourful ERP theme
    public static Color AppBg => Color.FromArgb(239, 246, 255);          // light blue background
    public static Color Side => Color.FromArgb(30, 41, 85);             // deep indigo sidebar
    public static Color Side2 => Color.FromArgb(49, 46, 129);           // sidebar hover
    public static Color Primary => Color.FromArgb(37, 99, 235);         // modern blue
    public static Color Primary2 => Color.FromArgb(124, 58, 237);       // purple accent
    public static Color PrimarySoft => Color.FromArgb(219, 234, 254);   // soft blue
    public static Color Accent => Color.FromArgb(14, 165, 233);         // cyan accent
    public static Color Success => Color.FromArgb(22, 163, 74);         // green
    public static Color Warning => Color.FromArgb(245, 158, 11);        // amber
    public static Color Danger => Color.FromArgb(220, 38, 38);          // red
    public static Color Text => Color.FromArgb(15, 23, 42);
    public static Color Muted => Color.FromArgb(100, 116, 139);
    public static Color Card => Color.FromArgb(255, 255, 255);
    public static Font DefaultFont => new Font("Segoe UI", 10);

    public static Button PrimaryButton(string text, int width = 130) => Btn(text, width, Primary, Color.White, false, Primary2, Color.White);
    public static Button SecondaryButton(string text, int width = 120) => Btn(text, width, Color.White, Primary, true, PrimarySoft, Primary);

    static Button Btn(string text, int width, Color back, Color fore, bool border, Color hoverBack, Color hoverFore)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = 40,
            BackColor = back,
            ForeColor = fore,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 5, 10, 5)
        };
        b.FlatAppearance.BorderSize = border ? 1 : 0;
        b.FlatAppearance.BorderColor = Color.FromArgb(147, 197, 253);
        b.FlatAppearance.MouseDownBackColor = Primary2;
        b.FlatAppearance.MouseOverBackColor = hoverBack;
        b.MouseEnter += (_, _) => { b.BackColor = hoverBack; b.ForeColor = hoverFore; };
        b.MouseLeave += (_, _) => { b.BackColor = back; b.ForeColor = fore; };
        return b;
    }

    public static Label Chip(string text) => new Label
    {
        Text = "  " + text + "  ",
        AutoSize = false,
        Width = 165,
        Height = 30,
        TextAlign = ContentAlignment.MiddleCenter,
        BackColor = Color.FromArgb(224, 231, 255),
        ForeColor = Color.FromArgb(67, 56, 202),
        Font = new Font("Segoe UI", 9, FontStyle.Bold)
    };

    public static void Grid(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.RowHeadersVisible = false;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.BackgroundColor = Card;
        g.BorderStyle = BorderStyle.None;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.GridColor = Color.FromArgb(219, 234, 254);
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersHeight = 42;
        g.RowTemplate.Height = 36;

        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(37, 99, 235);
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 4, 6, 4);

        g.DefaultCellStyle.Font = new Font("Segoe UI", 9);
        g.DefaultCellStyle.Padding = new Padding(6, 3, 6, 3);
        g.DefaultCellStyle.BackColor = Color.White;
        g.DefaultCellStyle.ForeColor = Text;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(221, 214, 254);
        g.DefaultCellStyle.SelectionForeColor = Text;

        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 255);
    }
}
