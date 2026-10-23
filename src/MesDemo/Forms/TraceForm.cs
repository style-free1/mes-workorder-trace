using MesDemo.Models;
using MesDemo.Services;
using System.Data;

namespace MesDemo.Forms;

public class TraceForm : Form
{
    private readonly MesService _service = new();
    private readonly TextBox _txt = new();
    private readonly Label _lblInfo = new();
    private readonly DataGridView _gridPass = new();
    private readonly DataGridView _gridSn = new();
    private readonly TabControl _tabs = new();

    public TraceForm()
    {
        BackColor = UiTheme.PageBack;

        var title = new Label
        {
            Text = "追溯查询",
            Font = UiTheme.TitleFont,
            ForeColor = Color.FromArgb(35, 45, 55),
            Dock = DockStyle.Top,
            Height = 46
        };

        var top = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.White, Padding = new Padding(16) };

        top.Controls.Add(new Label
        {
            Text = "SN / 工单号",
            Location = new Point(0, 17),
            Size = new Size(90, 24),
            TextAlign = ContentAlignment.MiddleLeft
        });

        _txt.Location = new Point(94, 13);
        _txt.Size = new Size(380, 30);
        _txt.Font = new Font("Microsoft YaHei", 11F);
        _txt.PlaceholderText = "扫描 SN 或输入工单号";
        top.Controls.Add(_txt);

        var btnSn = UiTheme.PrimaryButton("按 SN 追溯");
        btnSn.Location = new Point(490, 12);
        btnSn.Size = new Size(110, 32);
        btnSn.Click += (_, _) => TraceSn();
        top.Controls.Add(btnSn);

        var btnWo = UiTheme.SecondaryButton("按工单查询");
        btnWo.Location = new Point(610, 12);
        btnWo.Size = new Size(110, 32);
        btnWo.Click += (_, _) => TraceWorkOrder();
        top.Controls.Add(btnWo);

        _lblInfo.Text = "输入 SN 可查看该产品的全部过站记录；输入工单号可查看该工单下的所有序列号。";
        _lblInfo.Dock = DockStyle.Top;
        _lblInfo.Height = 38;
        _lblInfo.Font = new Font("Microsoft YaHei", 9F);
        _lblInfo.ForeColor = Color.FromArgb(90, 100, 110);
        _lblInfo.TextAlign = ContentAlignment.MiddleLeft;
        _lblInfo.Padding = new Padding(4, 0, 0, 0);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = UiTheme.BaseFont;

        var tabPass = new TabPage("过站记录（按 SN）") { BackColor = Color.White };
        UiTheme.StyleGrid(_gridPass);
        _gridPass.AutoGenerateColumns = false;
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SN", DataPropertyName = "SN", FillWeight = 18 });
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工序", DataPropertyName = "OperationName", FillWeight = 14 });
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工序号", DataPropertyName = "StepSeq", FillWeight = 8 });
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工位", DataPropertyName = "WorkstationCode", FillWeight = 12 });
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "操作人", DataPropertyName = "OperatorName", FillWeight = 10 });
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "结果", DataPropertyName = "Result", FillWeight = 8 });
        _gridPass.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "时间", DataPropertyName = "PassedAt", FillWeight = 20, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm:ss" } });
        tabPass.Controls.Add(_gridPass);

        var tabSn = new TabPage("工单序列号（按工单号）") { BackColor = Color.White };
        UiTheme.StyleGrid(_gridSn);
        tabSn.Controls.Add(_gridSn);

        _tabs.TabPages.Add(tabPass);
        _tabs.TabPages.Add(tabSn);

        Controls.Add(_tabs);
        Controls.Add(_lblInfo);
        Controls.Add(top);
        Controls.Add(title);

        _txt.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                TraceSn();
            }
        };
    }

    private void TraceSn()
    {
        var sn = _txt.Text.Trim();
        if (sn.Length == 0) return;

        try
        {
            var info = _service.GetSn(sn);
            if (info == null)
            {
                _lblInfo.Text = "SN 不存在：" + sn;
                _lblInfo.ForeColor = UiTheme.Ng;
                _gridPass.DataSource = null;
                return;
            }

            var records = _service.TraceBySn(sn);
            _gridPass.DataSource = records;
            _tabs.SelectedIndex = 0;

            var okCount = records.Count(r => r.Result == "OK");
            _lblInfo.Text = $"{sn}　状态：{info.StatusText}　当前工序序号：{info.CurrentStepSeq}　有效过站：{okCount} 次　创建时间：{info.CreatedAt:yyyy-MM-dd HH:mm}";
            _lblInfo.ForeColor = info.Status == "Completed" ? UiTheme.Ok : UiTheme.Primary;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "追溯失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TraceWorkOrder()
    {
        var no = _txt.Text.Trim();
        if (no.Length == 0) return;

        try
        {
            var dt = _service.GetSnTable(no);
            _gridSn.DataSource = dt;
            _tabs.SelectedIndex = 1;

            if (dt.Rows.Count == 0)
            {
                _lblInfo.Text = $"工单 {no} 下没有序列号，请确认工单号或先在生产报工中生成 SN。";
                _lblInfo.ForeColor = Color.FromArgb(150, 90, 0);
            }
            else
            {
                _lblInfo.Text = $"工单 {no} 共 {dt.Rows.Count} 个序列号";
                _lblInfo.ForeColor = UiTheme.Primary;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "查询失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
