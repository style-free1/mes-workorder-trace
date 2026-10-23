using MesDemo.Models;
using MesDemo.Services;

namespace MesDemo.Forms;

public class StatForm : Form
{
    private readonly MesService _service = new();
    private readonly DataGridView _gridWo = new();
    private readonly DataGridView _gridStep = new();
    private readonly Label _lblInfo = new();

    public StatForm()
    {
        BackColor = UiTheme.PageBack;

        var title = new Label
        {
            Text = "生产报表",
            Font = UiTheme.TitleFont,
            ForeColor = Color.FromArgb(35, 45, 55),
            Dock = DockStyle.Top,
            Height = 46
        };

        var bar = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = UiTheme.PageBack };
        var btn = UiTheme.SecondaryButton("刷新");
        btn.Location = new Point(0, 4);
        btn.Click += (_, _) => LoadData();
        bar.Controls.Add(btn);

        _lblInfo.Dock = DockStyle.Top;
        _lblInfo.Height = 30;
        _lblInfo.Font = new Font("Microsoft YaHei", 8.5F);
        _lblInfo.ForeColor = Color.FromArgb(100, 110, 120);
        _lblInfo.Text = "选中上方工单，可查看该工单各工序的完成进度。";
        _lblInfo.Padding = new Padding(4, 0, 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 300,
            BackColor = UiTheme.PageBack
        };

        UiTheme.StyleGrid(_gridWo);
        _gridWo.SelectionChanged += (_, _) => LoadSteps();

        UiTheme.StyleGrid(_gridStep);
        _gridStep.AutoGenerateColumns = false;
        _gridStep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工序号", DataPropertyName = "StepSeq", FillWeight = 12 });
        _gridStep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工序", DataPropertyName = "OperationName", FillWeight = 22 });
        _gridStep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "计划数", DataPropertyName = "PlanQty", FillWeight = 14 });
        _gridStep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "完成数", DataPropertyName = "DoneQty", FillWeight = 14 });
        _gridStep.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = "Status", FillWeight = 14 });

        split.Panel1.Controls.Add(_gridWo);
        split.Panel2.Controls.Add(_gridStep);

        Controls.Add(split);
        Controls.Add(_lblInfo);
        Controls.Add(bar);
        Controls.Add(title);

        Load += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            _gridWo.DataSource = _service.GetWorkOrderProgress();
            if (_gridWo.Rows.Count > 0)
                _gridWo.Rows[0].Selected = true;
            LoadSteps();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "加载报表失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadSteps()
    {
        if (_gridWo.CurrentRow == null) return;
        var no = _gridWo.CurrentRow.Cells[0].Value?.ToString();
        if (string.IsNullOrEmpty(no)) return;

        try
        {
            var wo = _service.GetWorkOrders().FirstOrDefault(w => w.WorkOrderNo == no);
            if (wo == null) return;
            _gridStep.DataSource = _service.GetWorkOrderSteps(wo.WorkOrderId);
        }
        catch
        {
            // 明细加载失败不影响主表
        }
    }
}
