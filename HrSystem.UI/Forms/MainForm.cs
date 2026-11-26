using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Models;
using HrSystem.Core.Services;

namespace HrSystem.UI.Forms;

public class MainForm : Form
{
    private readonly HrService _hrService;
    private bool _isInitializing;

    private Label _lblStats = null!;

    // Вкладка 1: Кадрові заявки
    private ComboBox _cmbFilterStatus = null!;
    private ComboBox _cmbFilterType = null!;
    private ComboBox _cmbFilterDept = null!;
    private TextBox _txtSearch = null!;
    private DataGridView _gridRequests = null!;

    // Вкладка 2: Працівники
    private DataGridView _gridEmployees = null!;

    // Вкладка 3: Підрозділи та графіки
    private DataGridView _gridDepartments = null!;
    private TextBox _txtDeptName = null!;
    private TextBox _txtDeptManager = null!;
    private TextBox _txtDeptDesc = null!;

    private DataGridView _gridSchedules = null!;
    private TextBox _txtSchedName = null!;
    private DateTimePicker _dtpSchedStart = null!;
    private DateTimePicker _dtpSchedEnd = null!;
    private NumericUpDown _numSchedHours = null!;
    private CheckBox _chkShift2x2 = null!;
    private CheckedListBox _clbWorkingDays = null!;

    public MainForm(HrService hrService)
    {
        _hrService = hrService;
        _isInitializing = true;
        BuildUi();
        PopulateDepartmentFilter();
        _isInitializing = false;
        RefreshAll();
    }

    private void BuildUi()
    {
        Text = "HR-Система — Керування персоналом, графіками та кадровими заявками";
        Size = new Size(1240, 740);
        MinimumSize = new Size(1020, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5F);

        // Верхня панель статистики
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 44,
            BackColor = Color.FromArgb(238, 244, 250),
            Padding = new Padding(14, 10, 14, 8)
        };
        _lblStats = new Label
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(26, 54, 93)
        };
        pnlTop.Controls.Add(_lblStats);

        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Padding = new Point(14, 6)
        };

        tabs.TabPages.Add(BuildRequestsTab());
        tabs.TabPages.Add(BuildEmployeesTab());
        tabs.TabPages.Add(BuildOrgAndSchedulesTab());

        // Порядок для DockStyle: спочатку Fill, потім Top
        Controls.Add(tabs);
        Controls.Add(pnlTop);
    }

    // --- Вкладка 1: Кадрові заявки ---
    private TabPage BuildRequestsTab()
    {
        var tab = new TabPage("Кадрові заявки та погодження");

        var pnlActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(8, 7, 8, 5),
            WrapContents = false
        };

        var btnNew = new Button { Text = "+ Подати заявку", Width = 145, Height = 32 };
        btnNew.Click += BtnNewRequest_Click;

        var btnDetails = new Button { Text = "Деталі / Погодження", Width = 175, Height = 32 };
        btnDetails.Click += (_, _) => OpenSelectedRequest();

        var btnDelete = new Button { Text = "Видалити заявку", Width = 140, Height = 32 };
        btnDelete.Click += BtnDeleteRequest_Click;

        var btnRefresh = new Button { Text = "Оновити", Width = 100, Height = 32 };
        btnRefresh.Click += (_, _) => RefreshAll();

        pnlActions.Controls.AddRange(new Control[] { btnNew, btnDetails, btnDelete, btnRefresh });

        var grpFilters = new GroupBox
        {
            Text = "Фільтрація та пошук заявок",
            Dock = DockStyle.Top,
            Height = 90,
            Padding = new Padding(8)
        };

        grpFilters.Controls.Add(new Label { Text = "Статус:", Left = 14, Top = 25, Width = 54 });
        _cmbFilterStatus = new ComboBox { Left = 70, Top = 22, Width = 195, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbFilterStatus.Items.Add(new ComboItem<RequestStatus?>("Усі статуси", null));
        foreach (RequestStatus st in Enum.GetValues<RequestStatus>())
            _cmbFilterStatus.Items.Add(new ComboItem<RequestStatus?>(UiDisplayHelper.FormatStatus(st), st));
        _cmbFilterStatus.SelectedIndex = 0;
        _cmbFilterStatus.SelectedIndexChanged += (_, _) => RefreshRequestsGrid();
        grpFilters.Controls.Add(_cmbFilterStatus);

        grpFilters.Controls.Add(new Label { Text = "Тип:", Left = 280, Top = 25, Width = 38 });
        _cmbFilterType = new ComboBox { Left = 320, Top = 22, Width = 155, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbFilterType.Items.AddRange(new object[] { "Усі типи", "Відпустка", "Лікарняний", "Відрядження" });
        _cmbFilterType.SelectedIndex = 0;
        _cmbFilterType.SelectedIndexChanged += (_, _) => RefreshRequestsGrid();
        grpFilters.Controls.Add(_cmbFilterType);

        grpFilters.Controls.Add(new Label { Text = "Підрозділ:", Left = 490, Top = 25, Width = 75 });
        _cmbFilterDept = new ComboBox { Left = 568, Top = 22, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbFilterDept.SelectedIndexChanged += (_, _) => RefreshRequestsGrid();
        grpFilters.Controls.Add(_cmbFilterDept);

        grpFilters.Controls.Add(new Label { Text = "Пошук:", Left = 14, Top = 56, Width = 54 });
        _txtSearch = new TextBox
        {
            Left = 70,
            Top = 53,
            Width = 405,
            PlaceholderText = "Працівник, підрозділ, підстава, місто, номер медвисновку..."
        };
        _txtSearch.TextChanged += (_, _) => RefreshRequestsGrid();
        grpFilters.Controls.Add(_txtSearch);

        var btnReset = new Button { Text = "Скинути фільтри", Left = 490, Top = 51, Width = 145, Height = 27 };
        btnReset.Click += BtnResetFilters_Click;
        grpFilters.Controls.Add(btnReset);

        _gridRequests = CreateReadOnlyGrid();
        _gridRequests.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) OpenSelectedRequest(); };
        _gridRequests.CellFormatting += GridRequests_CellFormatting;

        tab.Controls.Add(_gridRequests);
        tab.Controls.Add(grpFilters);
        tab.Controls.Add(pnlActions);
        return tab;
    }

    // --- Вкладка 2: Працівники ---
    private TabPage BuildEmployeesTab()
    {
        var tab = new TabPage("Працівники");

        var pnlActions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(8, 7, 8, 5),
            WrapContents = false
        };

        var btnAddEmp = new Button { Text = "+ Додати працівника", Width = 165, Height = 32 };
        btnAddEmp.Click += BtnAddEmployee_Click;

        var btnEditAssign = new Button { Text = "Змінити посаду / підрозділ / графік", Width = 250, Height = 32 };
        btnEditAssign.Click += BtnEditAssignment_Click;

        var btnToggleActive = new Button { Text = "Активний / Неактивний", Width = 175, Height = 32 };
        btnToggleActive.Click += BtnToggleEmployeeActive_Click;

        var btnDismiss = new Button { Text = "Звільнити працівника", Width = 170, Height = 32 };
        btnDismiss.Click += BtnDismissEmployee_Click;

        pnlActions.Controls.AddRange(new Control[] { btnAddEmp, btnEditAssign, btnToggleActive, btnDismiss });

        _gridEmployees = CreateReadOnlyGrid();
        _gridEmployees.CellFormatting += GridEmployees_CellFormatting;

        tab.Controls.Add(_gridEmployees);
        tab.Controls.Add(pnlActions);
        return tab;
    }

    // --- Вкладка 3: Підрозділи та робочі графіки ---
    private TabPage BuildOrgAndSchedulesTab()
    {
        var tab = new TabPage("Підрозділи та робочі графіки");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(6)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // Ліва колонка: Підрозділи
        var grpDepts = new GroupBox { Text = "Підрозділи організації", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _gridDepartments = CreateReadOnlyGrid();

        var pnlAddDept = new Panel { Dock = DockStyle.Bottom, Height = 152, Padding = new Padding(4) };
        pnlAddDept.Controls.Add(new Label { Text = "Додати підрозділ:", Left = 4, Top = 6, Width = 200, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) });

        pnlAddDept.Controls.Add(new Label { Text = "Назва *:", Left = 4, Top = 34, Width = 90 });
        _txtDeptName = new TextBox { Left = 100, Top = 31, Width = 280 };
        pnlAddDept.Controls.Add(_txtDeptName);

        pnlAddDept.Controls.Add(new Label { Text = "Керівник *:", Left = 4, Top = 64, Width = 90 });
        _txtDeptManager = new TextBox { Left = 100, Top = 61, Width = 280 };
        pnlAddDept.Controls.Add(_txtDeptManager);

        pnlAddDept.Controls.Add(new Label { Text = "Опис:", Left = 4, Top = 94, Width = 90 });
        _txtDeptDesc = new TextBox { Left = 100, Top = 91, Width = 280 };
        pnlAddDept.Controls.Add(_txtDeptDesc);

        var btnAddDept = new Button { Text = "+ Додати підрозділ", Left = 100, Top = 120, Width = 170, Height = 28 };
        btnAddDept.Click += BtnAddDepartment_Click;
        pnlAddDept.Controls.Add(btnAddDept);

        grpDepts.Controls.Add(_gridDepartments);
        grpDepts.Controls.Add(pnlAddDept);

        // Права колонка: Робочі графіки
        var grpSchedules = new GroupBox { Text = "Робочі графіки", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _gridSchedules = CreateReadOnlyGrid();

        var pnlAddSched = new Panel { Dock = DockStyle.Bottom, Height = 205, Padding = new Padding(4) };
        pnlAddSched.Controls.Add(new Label { Text = "Додати робочий графік:", Left = 4, Top = 6, Width = 220, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) });

        pnlAddSched.Controls.Add(new Label { Text = "Назва *:", Left = 4, Top = 34, Width = 95 });
        _txtSchedName = new TextBox { Left = 105, Top = 31, Width = 285 };
        pnlAddSched.Controls.Add(_txtSchedName);

        pnlAddSched.Controls.Add(new Label { Text = "Години:", Left = 4, Top = 64, Width = 95 });
        _dtpSchedStart = new DateTimePicker
        {
            Left = 105,
            Top = 61,
            Width = 75,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(9)
        };
        pnlAddSched.Controls.Add(new Label { Text = "—", Left = 184, Top = 64, Width = 16 });
        _dtpSchedEnd = new DateTimePicker
        {
            Left = 202,
            Top = 61,
            Width = 75,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm",
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(18)
        };
        pnlAddSched.Controls.Add(new Label { Text = "Год/тиж:", Left = 286, Top = 64, Width = 58 });
        _numSchedHours = new NumericUpDown { Left = 345, Top = 61, Width = 55, Minimum = 8, Maximum = 60, Value = 40 };
        pnlAddSched.Controls.AddRange(new Control[] { _dtpSchedStart, _dtpSchedEnd, _numSchedHours });

        _chkShift2x2 = new CheckBox { Text = "Змінний графік 2/2 (без прив'язки до днів тижня)", Left = 105, Top = 92, Width = 340 };
        _chkShift2x2.CheckedChanged += (_, _) => _clbWorkingDays.Enabled = !_chkShift2x2.Checked;
        pnlAddSched.Controls.Add(_chkShift2x2);

        pnlAddSched.Controls.Add(new Label { Text = "Робочі дні:", Left = 4, Top = 120, Width = 95 });
        _clbWorkingDays = new CheckedListBox
        {
            Left = 105,
            Top = 118,
            Width = 295,
            Height = 46,
            MultiColumn = true,
            ColumnWidth = 55,
            CheckOnClick = true
        };
        var daysOrder = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        };
        foreach (var d in daysOrder)
        {
            bool isWeekday = d is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
            _clbWorkingDays.Items.Add(new ComboItem<DayOfWeek>(UiDisplayHelper.FormatDayShort(d), d), isWeekday);
        }
        pnlAddSched.Controls.Add(_clbWorkingDays);

        var btnAddSched = new Button { Text = "+ Додати графік", Left = 105, Top = 170, Width = 165, Height = 28 };
        btnAddSched.Click += BtnAddSchedule_Click;
        pnlAddSched.Controls.Add(btnAddSched);

        grpSchedules.Controls.Add(_gridSchedules);
        grpSchedules.Controls.Add(pnlAddSched);

        layout.Controls.Add(grpDepts, 0, 0);
        layout.Controls.Add(grpSchedules, 1, 0);

        tab.Controls.Add(layout);
        return tab;
    }

    private static DataGridView CreateReadOnlyGrid() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false,
        BackgroundColor = Color.White
    };

    // --- Оновлення даних ---
    private void PopulateDepartmentFilter()
    {
        var prevInit = _isInitializing;
        _isInitializing = true;

        _cmbFilterDept.Items.Clear();
        _cmbFilterDept.Items.Add(new ComboItem<Guid?>("Усі підрозділи", null));
        foreach (var dept in _hrService.GetAllDepartments())
            _cmbFilterDept.Items.Add(new ComboItem<Guid?>(dept.Name, dept.Id));
        _cmbFilterDept.SelectedIndex = 0;

        _isInitializing = prevInit;
    }

    private void RefreshAll()
    {
        RefreshRequestsGrid();
        RefreshEmployeesGrid();
        RefreshDepartmentsAndSchedulesGrids();
        UpdateStatisticsBar();
    }

    private void RefreshRequestsGrid()
    {
        if (_isInitializing)
            return;

        var filter = new RequestFilter
        {
            Status = (_cmbFilterStatus.SelectedItem as ComboItem<RequestStatus?>)?.Value,
            RequestTypeName = _cmbFilterType.SelectedIndex > 0 ? _cmbFilterType.SelectedItem?.ToString() : null,
            DepartmentId = (_cmbFilterDept.SelectedItem as ComboItem<Guid?>)?.Value,
            SearchText = _txtSearch.Text
        };

        var rows = _hrService.FilterRequests(filter).Select(r =>
        {
            var emp = _hrService.GetEmployeeById(r.EmployeeId);
            var sched = _hrService.GetWorkScheduleById(emp.WorkScheduleId);
            return new RequestRowView
            {
                Id = r.Id,
                Працівник = emp.FullName,
                Підрозділ = _hrService.GetDepartmentName(emp.DepartmentId),
                Тип = r.RequestTypeName,
                Період = $"{r.StartDate:dd.MM.yyyy} — {r.EndDate:dd.MM.yyyy}",
                Днів = $"{r.TotalCalendarDays} ({sched.CountWorkingDays(r.StartDate, r.EndDate)} роб.)",
                Статус = UiDisplayHelper.FormatStatus(r.Status),
                Деталі = r.GetSpecificDetails(sched),
                RawStatus = r.Status
            };
        }).ToList();

        _gridRequests.DataSource = rows;
        HideColumn(_gridRequests, "Id");
        HideColumn(_gridRequests, "RawStatus");

        if (_gridRequests.Columns["Деталі"] is not null)
            _gridRequests.Columns["Деталі"]!.FillWeight = 180;
        if (_gridRequests.Columns["Статус"] is not null)
            _gridRequests.Columns["Статус"]!.FillWeight = 120;

        UpdateStatisticsBar();
    }

    private void RefreshEmployeesGrid()
    {
        var rows = _hrService.GetAllEmployees().Select(e => new EmployeeRowView
        {
            Id = e.Id,
            ПІБ = e.FullName,
            Посада = e.Position,
            Підрозділ = _hrService.GetDepartmentName(e.DepartmentId),
            Графік = _hrService.GetScheduleName(e.WorkScheduleId),
            Прийнято = e.HireDate.ToString("dd.MM.yyyy"),
            Відпустка = $"{e.AnnualVacationBalance} дн.",
            Статус = e.IsDismissed ? $"Звільнений ({e.DismissalDate:dd.MM.yyyy})" : (e.IsActive ? "Активний" : "Неактивний"),
            Примітка = e.IsDismissed ? (e.DismissalReason ?? string.Empty) : string.Empty,
            IsActiveFlag = e.IsActive,
            IsDismissedFlag = e.IsDismissed
        }).ToList();

        _gridEmployees.DataSource = rows;
        HideColumn(_gridEmployees, "Id");
        HideColumn(_gridEmployees, "IsActiveFlag");
        HideColumn(_gridEmployees, "IsDismissedFlag");
    }

    private void RefreshDepartmentsAndSchedulesGrids()
    {
        var allEmps = _hrService.GetAllEmployees();

        _gridDepartments.DataSource = _hrService.GetAllDepartments().Select(d => new
        {
            Назва = d.Name,
            Керівник = d.ManagerName,
            Працівників = allEmps.Count(e => e.DepartmentId == d.Id),
            Опис = d.Description
        }).ToList();

        _gridSchedules.DataSource = _hrService.GetAllWorkSchedules().Select(s => new
        {
            Назва = s.Name,
            Час = s.GetTimeRangeText(),
            Дні = s.IsShiftTwoByTwo
                ? "Змінний 2/2"
                : string.Join(", ", s.WorkingDays.Select(UiDisplayHelper.FormatDayShort)),
            Працівників = allEmps.Count(e => e.WorkScheduleId == s.Id)
        }).ToList();
    }

    private void UpdateStatisticsBar()
    {
        var s = _hrService.GetStatistics();
        _lblStats.Text =
            $"Працівники: {s.ActiveEmployees}/{s.TotalEmployees} акт.   |   Підрозділи: {s.TotalDepartments}   |   " +
            $"Заявки: {s.TotalRequests}  (Подані: {s.SubmittedRequests}, Погоджені кер.: {s.ManagerApprovedRequests}, " +
            $"Затверджені HR: {s.HrApprovedRequests}, Відхилені: {s.RejectedRequests})";
    }

    // --- Підсвітка рядків ---
    private void GridRequests_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _gridRequests.Rows[e.RowIndex].DataBoundItem is not RequestRowView row)
            return;

        _gridRequests.Rows[e.RowIndex].DefaultCellStyle.BackColor = row.RawStatus switch
        {
            RequestStatus.ManagerApproved => Color.LemonChiffon,
            RequestStatus.HrApproved => Color.Honeydew,
            RequestStatus.Rejected => Color.MistyRose,
            RequestStatus.Cancelled => Color.Gainsboro,
            _ => Color.White
        };
    }

    private void GridEmployees_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _gridEmployees.Rows[e.RowIndex].DataBoundItem is not EmployeeRowView row)
            return;

        if (row.IsDismissedFlag)
            _gridEmployees.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
        else if (!row.IsActiveFlag)
            _gridEmployees.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Gainsboro;
    }

    // --- Обробники подій ---
    private void BtnNewRequest_Click(object? sender, EventArgs e)
    {
        using var form = new CreateRequestForm(_hrService);
        if (form.ShowDialog(this) == DialogResult.OK)
            RefreshAll();
    }

    private void OpenSelectedRequest()
    {
        if (_gridRequests.SelectedRows.Count == 0 ||
            _gridRequests.SelectedRows[0].DataBoundItem is not RequestRowView row)
        {
            MessageBox.Show("Оберіть заявку в таблиці.", "Інформація", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new RequestDetailsForm(_hrService, row.Id);
        form.ShowDialog(this);
        if (form.HasChanges)
            RefreshAll();
    }

    private void BtnDeleteRequest_Click(object? sender, EventArgs e)
    {
        if (_gridRequests.SelectedRows.Count == 0 ||
            _gridRequests.SelectedRows[0].DataBoundItem is not RequestRowView row)
            return;

        var confirm = MessageBox.Show(
            $"Видалити заявку «{row.Тип}» працівника {row.Працівник} ({row.Період})?",
            "Підтвердження видалення",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
            return;

        try
        {
            _hrService.DeleteRequest(row.Id);
            RefreshAll();
        }
        catch (HrDomainException ex)
        {
            MessageBox.Show(ex.Message, "Видалення заборонено", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnResetFilters_Click(object? sender, EventArgs e)
    {
        _isInitializing = true;
        _cmbFilterStatus.SelectedIndex = 0;
        _cmbFilterType.SelectedIndex = 0;
        _cmbFilterDept.SelectedIndex = 0;
        _txtSearch.Clear();
        _isInitializing = false;
        RefreshRequestsGrid();
    }

    private void BtnAddEmployee_Click(object? sender, EventArgs e)
    {
        using var form = new EmployeeDialogForm(_hrService);
        if (form.ShowDialog(this) == DialogResult.OK)
            RefreshAll();
    }

    private void BtnEditAssignment_Click(object? sender, EventArgs e)
    {
        if (_gridEmployees.SelectedRows.Count == 0 ||
            _gridEmployees.SelectedRows[0].DataBoundItem is not EmployeeRowView row)
            return;

        var emp = _hrService.GetEmployeeById(row.Id);
        if (emp.IsDismissed)
        {
            MessageBox.Show("Неможливо редагувати дані звільненого працівника.", "Заборонено",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new EmployeeDialogForm(_hrService, emp);
        if (form.ShowDialog(this) == DialogResult.OK)
            RefreshAll();
    }

    private void BtnToggleEmployeeActive_Click(object? sender, EventArgs e)
    {
        if (_gridEmployees.SelectedRows.Count == 0 ||
            _gridEmployees.SelectedRows[0].DataBoundItem is not EmployeeRowView row)
            return;

        try
        {
            _hrService.SetEmployeeActiveStatus(row.Id, !row.IsActiveFlag);
            RefreshAll();
        }
        catch (HrDomainException ex)
        {
            MessageBox.Show(ex.Message, "Операція заборонена", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnDismissEmployee_Click(object? sender, EventArgs e)
    {
        if (_gridEmployees.SelectedRows.Count == 0 ||
            _gridEmployees.SelectedRows[0].DataBoundItem is not EmployeeRowView row)
            return;

        var emp = _hrService.GetEmployeeById(row.Id);
        if (emp.IsDismissed)
        {
            MessageBox.Show("Цей працівник вже звільнений.", "Інформація",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var form = new DismissEmployeeForm(_hrService, emp);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            RefreshAll();
            if (form.CancelledRequestsCount > 0)
            {
                MessageBox.Show(
                    $"Працівника звільнено. Автоматично скасовано незавершених заявок: {form.CancelledRequestsCount}.",
                    "Звільнення оформлено",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }

    private void BtnAddDepartment_Click(object? sender, EventArgs e)
    {
        try
        {
            _hrService.AddDepartment(_txtDeptName.Text, _txtDeptManager.Text, _txtDeptDesc.Text);
            _txtDeptName.Clear();
            _txtDeptManager.Clear();
            _txtDeptDesc.Clear();
            PopulateDepartmentFilter();
            RefreshAll();
        }
        catch (Exception ex) when (ex is ArgumentException or HrDomainException)
        {
            MessageBox.Show(ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void BtnAddSchedule_Click(object? sender, EventArgs e)
    {
        try
        {
            var selectedDays = _clbWorkingDays.CheckedItems
                .OfType<ComboItem<DayOfWeek>>()
                .Select(i => i.Value)
                .ToList();

            _hrService.AddWorkSchedule(
                _txtSchedName.Text,
                TimeOnly.FromDateTime(_dtpSchedStart.Value),
                TimeOnly.FromDateTime(_dtpSchedEnd.Value),
                (int)_numSchedHours.Value,
                selectedDays,
                _chkShift2x2.Checked);

            _txtSchedName.Clear();
            _chkShift2x2.Checked = false;
            RefreshAll();
        }
        catch (Exception ex) when (ex is ArgumentException or HrDomainException)
        {
            MessageBox.Show(ex.Message, "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void HideColumn(DataGridView grid, string name)
    {
        if (grid.Columns[name] is not null)
            grid.Columns[name]!.Visible = false;
    }

    private sealed class RequestRowView
    {
        public Guid Id { get; set; }
        public string Працівник { get; set; } = string.Empty;
        public string Підрозділ { get; set; } = string.Empty;
        public string Тип { get; set; } = string.Empty;
        public string Період { get; set; } = string.Empty;
        public string Днів { get; set; } = string.Empty;
        public string Статус { get; set; } = string.Empty;
        public string Деталі { get; set; } = string.Empty;
        public RequestStatus RawStatus { get; set; }
    }

    private sealed class EmployeeRowView
    {
        public Guid Id { get; set; }
        public string ПІБ { get; set; } = string.Empty;
        public string Посада { get; set; } = string.Empty;
        public string Підрозділ { get; set; } = string.Empty;
        public string Графік { get; set; } = string.Empty;
        public string Прийнято { get; set; } = string.Empty;
        public string Відпустка { get; set; } = string.Empty;
        public string Статус { get; set; } = string.Empty;
        public string Примітка { get; set; } = string.Empty;
        public bool IsActiveFlag { get; set; }
        public bool IsDismissedFlag { get; set; }
    }
}