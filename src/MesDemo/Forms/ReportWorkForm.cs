using MesDemo.Models;
using MesDemo.Services;

namespace MesDemo.Forms;

public class ReportWorkForm : Form
{
    private readonly MesService _service = new();
    private readonly ComboBox _cboWorkOrder = new();
    private readonly ComboBox _cboWorkstation = new();
    private readonly TextBox _txtSn = new();
    private readonly TextBox _txtOperator = new();
    private readonly Label _lblNext = new();
    private readonly Label _lblMsg = new();
    private readonly DataGridView _grid = new();

    public ReportWorkForm()
    {
        BackColor = UiTheme.PageBack;

        var title = new Label
        {
            Text = "生产报工",
            Font = UiTheme.TitleFont,
            ForeColor = Color.FromArgb(35, 45, 55),
            Dock = DockStyle.Top,
            Height = 46
        };

        var top = new Panel { Dock = DockStyle.Top, Height = 132, BackColor = Color.White, Padding = new Padding(16) };

        top.Controls.Add(new Label { Text = "工单", Location = new Point(0, 15), Size = new Size(44, 24), TextAlign = ContentAlignment.MiddleLeft });
        _cboWorkOrder.Location = new Point(46, 11);
        _cboWorkOrder.Size = new Size(300, 28);
        _cboWorkOrder.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboWorkOrder.DisplayMember = "WorkOrderNo";
        top.Controls.Add(_cboWorkOrder);

        top.Controls.Add(new Label { Text = "操作人", Location = new Point(366, 15), Size = new Size(54, 24), TextAlign = ContentAlignment.MiddleLeft });
        _txtOperator.Location = new Point(424, 11);
        _txtOperator.Size = new Size(110, 28);
        _txtOperator.Text = "admin";
        top.Controls.Add(_txtOperator);

        top.Controls.Add(new Label { Text = "工位", Location = new Point(552, 15), Size = new Size(40, 24), TextAlign = ContentAlignment.MiddleLeft });
        _cboWorkstation.Location = new Point(594, 11);
        _cboWorkstation.Size = new Size(240, 28);
        _cboWorkstation.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboWorkstation.DisplayMember = "WorkstationName";
        top.Controls.Add(_cboWorkstation);

        top.Controls.Add(new Label { Text = "SN", Location = new Point(0, 62), Size = new Size(44, 30), TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold) });
        _txtSn.Location = new Point(46, 58);
        _txtSn.Size = new Size(420, 32);
        _txtSn.Font = new Font("Microsoft YaHei", 12F);
        _txtSn.PlaceholderText = "扫描或输入产品序列号，回车查询应过工序";
        top.Controls.Add(_txtSn);

        var btnQuery = UiTheme.SecondaryButton("查询应过工序");
        btnQuery.Location = new Point(478, 57);
        btnQuery.Size = new Size(112, 34);
        btnQuery.Click += (_, _) => QueryNext();
        top.Controls.Add(btnQuery);

        var btnOk = UiTheme.PrimaryButton("OK 过站");
        btnOk.Location = new Point(600, 57);
        btnOk.BackColor = UiTheme.Ok;
        btnOk.Click += (_, _) => Pass(true);
        top.Controls.Add(btnOk);

        var btnNg = UiTheme.PrimaryButton("记录 NG");
        btnNg.Location = new Point(704, 57);
        btnNg.BackColor = UiTheme.Ng;
        btnNg.Click += (_, _) => Pass(false);
        top.Controls.Add(btnNg);

        var btnGen = UiTheme.SecondaryButton("生成 SN");
        btnGen.Location = new Point(808, 57);
        btnGen.Size = new Size(96, 34);
        btnGen.Click += (_, _) => GenerateSn();
        top.Controls.Add(btnGen);

        var nextPanel = new Panel { Dock = DockStyle.Top, Height = 62, BackColor = Color.FromArgb(238, 244, 250), Padding = new Padding(18, 10, 18, 10) };
        _lblNext.Text = "请扫描产品 SN";
        _lblNext.Dock = DockStyle.Fill;
        _lblNext.Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold);
        _lblNext.ForeColor = UiTheme.Primary;
        _lblNext.TextAlign = ContentAlignment.MiddleLeft;
        nextPanel.Controls.Add(_lblNext);

        _lblMsg.Text = "就绪";
        _lblMsg.Dock = DockStyle.Bottom;
        _lblMsg.Height = 40;
        _lblMsg.Font = new Font("Microsoft YaHei", 10F);
        _lblMsg.ForeColor = Color.FromArgb(90, 100, 110);
        _lblMsg.TextAlign = ContentAlignment.MiddleLeft;
        _lblMsg.Padding = new Padding(4, 0, 0, 0);

        UiTheme.StyleGrid(_grid);
        _grid.CellDoubleClick += (_, _) => { };

        Controls.Add(_grid);
        Controls.Add(_lblMsg);
        Controls.Add(nextPanel);
        Controls.Add(top);
        Controls.Add(title);

        _txtSn.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                QueryNext();
            }
        };

        Load += (_, _) =>
        {
            LoadCombos();
            LoadRecent();
        };
    }

    private void LoadCombos()
    {
        try
        {
            var orders = _service.GetWorkOrders().Where(w => w.Status is "Released" or "InProgress").ToList();
            _cboWorkOrder.DataSource = orders;
            _cboWorkOrder.DisplayMember = "WorkOrderNo";

            _cboWorkstation.DataSource = _service.GetWorkstations();
            _cboWorkstation.DisplayMember = "WorkstationName";
            _cboWorkstation.SelectedIndex = -1;

            if (orders.Count == 0)
                _lblMsg.Text = "当前没有已下达的工单，请先到「工单管理」下达工单";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "加载基础数据失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadRecent()
    {
        try
        {
            _grid.DataSource = _service.GetRecentPassRecords(null, 10);
        }
        catch
        {
            // 最近记录加载失败不阻塞报工
        }
    }

    private void QueryNext()
    {
        var sn = _txtSn.Text.Trim();
        if (sn.Length == 0) return;

        try
        {
            var info = _service.GetSnNextStep(sn);
            if (info == null)
            {
                _lblNext.Text = "SN 不存在：" + sn;
                _lblNext.ForeColor = UiTheme.Ng;
                return;
            }

            if (info.NextStepSeq == 0)
            {
                _lblNext.Text = $"{info.SN}　{info.MaterialName}　已完成全部工序";
                _lblNext.ForeColor = UiTheme.Ok;
                return;
            }

            _lblNext.Text = $"{info.SN}　{info.MaterialName}　|　当前应过：{info.NextOperationName}（{info.NextStepSeq}）";
            _lblNext.ForeColor = UiTheme.Primary;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "查询失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Pass(bool isOk)
    {
        var sn = _txtSn.Text.Trim();
        if (sn.Length == 0)
        {
            MessageBox.Show(this, "请先扫描或输入 SN", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_cboWorkstation.SelectedItem is not Workstation ws || ws.OperationId == null)
        {
            MessageBox.Show(this, "请选择工位（工位需绑定工序）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var result = _service.PassStation(sn, ws.OperationId.Value, ws.WorkstationCode, _txtOperator.Text.Trim(), isOk);
            _lblMsg.Text = result.Message;
            _lblMsg.ForeColor = result.Success ? UiTheme.Ok : UiTheme.Ng;

            if (result.Success)
            {
                _txtSn.Clear();
                _lblNext.Text = result.NextOperation.Length > 0 ? result.NextOperation : "请扫描下一个 SN";
                _lblNext.ForeColor = UiTheme.Primary;
            }

            LoadRecent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "报工失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GenerateSn()
    {
        if (_cboWorkOrder.SelectedItem is not WorkOrder wo)
        {
            MessageBox.Show(this, "请先选择工单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new SnBatchForm(wo.WorkOrderNo);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var n = _service.CreateSnBatch(wo.WorkOrderId, dlg.Count);
            _lblMsg.Text = $"已为 {wo.WorkOrderNo} 生成 {n} 个 SN，规则：工单号-四位序号（可从追溯查询查看）";
            _lblMsg.ForeColor = UiTheme.Ok;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "生成失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

public class SnBatchForm : Form
{
    private readonly NumericUpDown _num = new();
    public int Count { get; private set; }

    public SnBatchForm(string workOrderNo)
    {
        Text = "生成序列号";
        Size = new Size(400, 220);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = UiTheme.BaseFont;
        BackColor = Color.White;

        Controls.Add(new Label
        {
            Text = $"工单：{workOrderNo}",
            Location = new Point(24, 22),
            Size = new Size(340, 24),
            ForeColor = Color.FromArgb(60, 60, 60)
        });

        Controls.Add(new Label
        {
            Text = "生成数量",
            Location = new Point(24, 70),
            Size = new Size(80, 24),
            TextAlign = ContentAlignment.MiddleLeft
        });

        _num.Location = new Point(110, 66);
        _num.Size = new Size(120, 28);
        _num.Minimum = 1;
        _num.Maximum = 100;
        _num.Value = 5;
        Controls.Add(_num);

        var ok = UiTheme.PrimaryButton("生成");
        ok.Location = new Point(150, 130);
        ok.Click += (_, _) =>
        {
            Count = (int)_num.Value;
            DialogResult = DialogResult.OK;
            Close();
        };

        var cancel = UiTheme.SecondaryButton("取消");
        cancel.Location = new Point(254, 130);
        cancel.DialogResult = DialogResult.Cancel;

        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
