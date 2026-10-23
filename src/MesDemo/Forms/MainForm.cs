using MesDemo.Data;
using MesDemo.Services;

namespace MesDemo.Forms;

public class MainForm : Form
{
    private readonly Panel _content = new();
    private readonly MesService _service = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private Form? _current;

    public MainForm()
    {
        Text = "MES Demo - 生产执行系统";
        Size = new Size(1240, 800);
        MinimumSize = new Size(1040, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = UiTheme.BaseFont;
        BackColor = UiTheme.PageBack;
        DoubleBuffered = true;

        BuildNav();
        BuildContent();
        ShowPage("workorder");

        var status = new StatusStrip { BackColor = Color.White, SizingGrip = false };
        var lbl = new ToolStripStatusLabel { Text = "正在连接数据库…", Font = UiTheme.BaseFont };
        status.Items.Add(lbl);
        Controls.Add(status);

        Shown += (_, _) =>
        {
            var ok = Db.TestConnection(out var msg);
            lbl.Text = ok ? "数据库：" + Db.ConnectionString + "　|　连接正常" : msg;
            lbl.ForeColor = ok ? Color.FromArgb(60, 120, 70) : Color.Firebrick;
        };
    }

    private void BuildNav()
    {
        var nav = new Panel { Dock = DockStyle.Left, Width = 196, BackColor = UiTheme.NavBack };
        Controls.Add(nav);

        var brand = new Label
        {
            Text = "MES DEMO",
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei", 15F, FontStyle.Bold),
            Location = new Point(0, 22),
            Size = new Size(196, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };
        nav.Controls.Add(brand);

        var sub = new Label
        {
            Text = "生产执行系统 · 演示版",
            ForeColor = Color.FromArgb(140, 160, 180),
            Font = new Font("Microsoft YaHei", 8F),
            Location = new Point(0, 62),
            Size = new Size(196, 24),
            TextAlign = ContentAlignment.MiddleCenter
        };
        nav.Controls.Add(sub);

        var items = new (string Text, string Key)[]
        {
            ("工单管理", "workorder"),
            ("生产报工", "report"),
            ("追溯查询", "trace"),
            ("生产报表", "stat"),
            ("基础数据", "base")
        };

        for (int i = 0; i < items.Length; i++)
        {
            var (text, key) = items[i];
            var b = new Button
            {
                Text = text,
                Location = new Point(0, 110 + i * 50),
                Size = new Size(196, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = UiTheme.NavBack,
                ForeColor = Color.FromArgb(215, 225, 235),
                Font = new Font("Microsoft YaHei", 10F),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(28, 0, 0, 0),
                Cursor = Cursors.Hand,
                Tag = key
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = UiTheme.NavHover;
            b.Click += (_, _) => ShowPage(key);
            nav.Controls.Add(b);
            _navButtons[key] = b;
        }
    }

    private void BuildContent()
    {
        _content.Dock = DockStyle.Fill;
        _content.BackColor = UiTheme.PageBack;
        _content.Padding = new Padding(18);
        Controls.Add(_content);
        _content.BringToFront();
    }

    private void ShowPage(string key)
    {
        if (_current != null)
        {
            _content.Controls.Remove(_current);
            _current.Dispose();
            _current = null;
        }

        Form page = key switch
        {
            "workorder" => new WorkOrderForm(),
            "report" => new ReportWorkForm(),
            "trace" => new TraceForm(),
            "stat" => new StatForm(),
            "base" => new BaseDataForm(),
            _ => new WorkOrderForm()
        };

        page.TopLevel = false;
        page.FormBorderStyle = FormBorderStyle.None;
        page.Dock = DockStyle.Fill;
        _content.Controls.Add(page);
        page.Show();
        _current = page;

        foreach (var (k, b) in _navButtons)
        {
            b.BackColor = k == key ? UiTheme.NavActive : UiTheme.NavBack;
            b.ForeColor = k == key ? Color.White : Color.FromArgb(215, 225, 235);
        }
    }
}
