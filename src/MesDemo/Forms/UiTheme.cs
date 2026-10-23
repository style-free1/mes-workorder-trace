using System.Drawing;

namespace MesDemo.Forms;

public static class UiTheme
{
    public static readonly Color Primary = Color.FromArgb(11, 79, 138);
    public static readonly Color NavBack = Color.FromArgb(31, 45, 61);
    public static readonly Color NavHover = Color.FromArgb(45, 62, 80);
    public static readonly Color NavActive = Color.FromArgb(11, 79, 138);
    public static readonly Color PageBack = Color.FromArgb(244, 247, 250);
    public static readonly Color Ok = Color.FromArgb(22, 140, 74);
    public static readonly Color Ng = Color.FromArgb(190, 45, 45);

    public static readonly Font BaseFont = new("Microsoft YaHei", 9F);
    public static readonly Font TitleFont = new("Microsoft YaHei", 16F, FontStyle.Bold);
    public static readonly Font SectionFont = new("Microsoft YaHei", 11F, FontStyle.Bold);

    public static Button PrimaryButton(string text)
    {
        var b = new Button
        {
            Text = text,
            Height = 34,
            Width = 96,
            BackColor = Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = BaseFont,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    public static Button SecondaryButton(string text)
    {
        var b = new Button
        {
            Text = text,
            Height = 34,
            Width = 96,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(60, 60, 60),
            FlatStyle = FlatStyle.Flat,
            Font = BaseFont,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
        return b;
    }

    public static void StyleGrid(DataGridView g)
    {
        g.Dock = DockStyle.Fill;
        g.BackgroundColor = Color.White;
        g.BorderStyle = BorderStyle.None;
        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.RowHeadersVisible = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.MultiSelect = false;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.ColumnHeadersHeight = 34;
        g.RowTemplate.Height = 30;
        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(238, 244, 250);
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(40, 40, 40);
        g.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
        g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        g.DefaultCellStyle.Font = BaseFont;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 228, 245);
        g.DefaultCellStyle.SelectionForeColor = Color.Black;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 251, 253);
    }
}
