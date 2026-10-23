using MesDemo.Services;
using System.Data;

namespace MesDemo.Forms;

public class BaseDataForm : Form
{
    private readonly MesService _service = new();
    private readonly TabControl _tabs = new();

    public BaseDataForm()
    {
        BackColor = UiTheme.PageBack;

        var title = new Label
        {
            Text = "基础数据",
            Font = UiTheme.TitleFont,
            ForeColor = Color.FromArgb(35, 45, 55),
            Dock = DockStyle.Top,
            Height = 46
        };

        var hint = new Label
        {
            Text = "基础数据是 MES 的源头：物料决定做什么，工艺路线决定怎么做，工位决定在哪做。",
            Dock = DockStyle.Top,
            Height = 30,
            Font = new Font("Microsoft YaHei", 8.5F),
            ForeColor = Color.FromArgb(100, 110, 120),
            Padding = new Padding(4, 0, 0, 0)
        };

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = UiTheme.BaseFont;

        MakeTab("物料", () => _service.GetMaterialTable());
        MakeTab("工序", () => _service.GetOperationTable());
        MakeTab("工艺路线", () => _service.GetRoutingTable());
        MakeTab("工位", () => _service.GetWorkstationTable());

        Controls.Add(_tabs);
        Controls.Add(hint);
        Controls.Add(title);

        Load += (_, _) => LoadAll();
    }

    private readonly List<(TabPage Page, Func<DataTable> Loader)> _loaders = new();

    private TabPage MakeTab(string name, Func<DataTable> loader)
    {
        var page = new TabPage(name) { BackColor = Color.White };
        var grid = new DataGridView { Name = "grid" };
        UiTheme.StyleGrid(grid);
        page.Controls.Add(grid);
        _tabs.TabPages.Add(page);
        _loaders.Add((page, loader));
        return page;
    }

    private void LoadAll()
    {
        try
        {
            foreach (var (page, loader) in _loaders)
            {
                if (page.Controls["grid"] is DataGridView g)
                    g.DataSource = loader();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "加载基础数据失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
