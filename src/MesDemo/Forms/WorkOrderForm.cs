using MesDemo.Models;
using MesDemo.Services;
using System.Data;

namespace MesDemo.Forms;

public class WorkOrderForm : Form
{
    private readonly MesService _service = new();
    private readonly DataGridView _grid = new();

    public WorkOrderForm()
    {
        BackColor = UiTheme.PageBack;

        var title = new Label
        {
            Text = "工单管理",
            Font = UiTheme.TitleFont,
            ForeColor = Color.FromArgb(35, 45, 55),
            Dock = DockStyle.Top,
            Height = 46
        };
        Controls.Add(title);

        var bar = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = UiTheme.PageBack };
        Controls.Add(bar);

        var btnNew = UiTheme.PrimaryButton("新建工单");
        btnNew.Location = new Point(0, 6);
        btnNew.Click += (_, _) => NewWorkOrder();

        var btnRelease = UiTheme.SecondaryButton("下达");
        btnRelease.Location = new Point(104, 6);
        btnRelease.Click += (_, _) => ChangeStatus("release");

        var btnStart = UiTheme.SecondaryButton("开工");
        btnStart.Location = new Point(208, 6);
        btnStart.Click += (_, _) => ChangeStatus("start");

        var btnRefresh = UiTheme.SecondaryButton("刷新");
        btnRefresh.Location = new Point(312, 6);
        btnRefresh.Click += (_, _) => LoadData();

        bar.Controls.AddRange(new Control[] { btnNew, btnRelease, btnStart, btnRefresh });

        var hint = new Label
        {
            Text = "说明：新建 → 下达 → 开工；只有“已下达”状态才能创建序列号和报工。",
            ForeColor = Color.FromArgb(110, 120, 130),
            Dock = DockStyle.Top,
            Height = 28,
            Font = new Font("Microsoft YaHei", 8.5F)
        };
        Controls.Add(hint);

        var wrap = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(1) };
        Controls.Add(wrap);
        wrap.BringToFront();

        UiTheme.StyleGrid(_grid);
        _grid.AutoGenerateColumns = false;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "工单号", DataPropertyName = "WorkOrderNo", FillWeight = 18 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "物料", DataPropertyName = "MaterialName", FillWeight = 14 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "计划数", DataPropertyName = "PlanQty", FillWeight = 9 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "状态", DataPropertyName = "StatusText", FillWeight = 10 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "创建时间", DataPropertyName = "CreatedAt", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "下达时间", DataPropertyName = "ReleasedAt", FillWeight = 18, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd HH:mm" } });
        wrap.Controls.Add(_grid);

        Load += (_, _) => LoadData();
    }

    private void LoadData()
    {
        try
        {
            _grid.DataSource = _service.GetWorkOrders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "加载工单失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private WorkOrder? Selected()
    {
        if (_grid.CurrentRow?.DataBoundItem is WorkOrder wo) return wo;
        MessageBox.Show(this, "请先选择一条工单", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    private void ChangeStatus(string action)
    {
        var wo = Selected();
        if (wo == null) return;
        try
        {
            if (action == "release") _service.ReleaseWorkOrder(wo.WorkOrderId);
            else _service.StartWorkOrder(wo.WorkOrderId);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void NewWorkOrder()
    {
        using var dlg = new NewWorkOrderForm(_service);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            LoadData();
    }
}

public class NewWorkOrderForm : Form
{
    private readonly MesService _service;
    private readonly ComboBox _cboMaterial = new();
    private readonly NumericUpDown _numQty = new();
    private readonly TextBox _txtNo = new();

    public NewWorkOrderForm(MesService service)
    {
        _service = service;
        Text = "新建工单";
        Size = new Size(440, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = UiTheme.BaseFont;
        BackColor = Color.White;

        Controls.Add(Label("工单号", 24, 26));
        _txtNo.Location = new Point(104, 22);
        _txtNo.Size = new Size(290, 28);
        _txtNo.Text = "WO-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Controls.Add(_txtNo);

        Controls.Add(Label("物料", 24, 76));
        _cboMaterial.Location = new Point(104, 72);
        _cboMaterial.Size = new Size(290, 28);
        _cboMaterial.DropDownStyle = ComboBoxStyle.DropDownList;
        _cboMaterial.DisplayMember = "MaterialName";
        _cboMaterial.ValueMember = "MaterialId";
        _cboMaterial.DataSource = _service.GetMaterials();
        Controls.Add(_cboMaterial);

        Controls.Add(Label("计划数量", 24, 126));
        _numQty.Location = new Point(104, 122);
        _numQty.Size = new Size(120, 28);
        _numQty.Minimum = 1;
        _numQty.Maximum = 10000;
        _numQty.Value = 5;
        Controls.Add(_numQty);

        var btnOk = UiTheme.PrimaryButton("创建");
        btnOk.Location = new Point(196, 200);
        btnOk.DialogResult = DialogResult.None;
        btnOk.Click += (_, _) => Create();

        var btnCancel = UiTheme.SecondaryButton("取消");
        btnCancel.Location = new Point(300, 200);
        btnCancel.DialogResult = DialogResult.Cancel;

        Controls.Add(btnOk);
        Controls.Add(btnCancel);
        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private static Label Label(string text, int x, int y) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(76, 24),
        TextAlign = ContentAlignment.MiddleLeft,
        ForeColor = Color.FromArgb(60, 60, 60)
    };

    private void Create()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_txtNo.Text))
            {
                MessageBox.Show(this, "请输入工单号", "提示");
                return;
            }
            if (_cboMaterial.SelectedValue is not int materialId)
            {
                MessageBox.Show(this, "请选择物料", "提示");
                return;
            }

            _service.CreateWorkOrder(_txtNo.Text.Trim(), materialId, (int)_numQty.Value);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "创建失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
