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

public class CreateRequestForm : Form
{
    private readonly HrService _hrService;

    private ComboBox _cmbEmployee = null!;
    private ComboBox _cmbType = null!;
    private DateTimePicker _dtpStart = null!;
    private DateTimePicker _dtpEnd = null!;
    private Label _lblCalculation = null!;
    private TextBox _txtReason = null!;

    // Панель Відпустки
    private Panel _pnlVacation = null!;
    private ComboBox _cmbVacationType = null!;
    private TextBox _txtSubstitute = null!;

    // Панель Лікарняного
    private Panel _pnlSickLeave = null!;
    private TextBox _txtMedCert = null!;
    private CheckBox _chkHospitalized = null!;

    // Панель Відрядження
    private Panel _pnlBusinessTrip = null!;
    private TextBox _txtCity = null!;
    private TextBox _txtPurpose = null!;
    private NumericUpDown _numAllowance = null!;

    public CreateRequestForm(HrService hrService)
    {
        _hrService = hrService;
        BuildUi();
        UpdateTypePanel();
        UpdatePeriodPreview();
    }

    private void BuildUi()
    {
        Text = "Подання нової кадрової заявки";
        Size = new Size(570, 575);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F);

        int y = 16;

        Controls.Add(new Label { Text = "Працівник *:", Left = 16, Top = y + 3, Width = 155 });
        _cmbEmployee = new ComboBox
        {
            Left = 178,
            Top = y,
            Width = 358,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (var emp in _hrService.GetActiveEmployees())
        {
            var dept = _hrService.GetDepartmentName(emp.DepartmentId);
            _cmbEmployee.Items.Add(new ComboItem<Employee>(
                $"{emp.FullName} ({dept} | залишок: {emp.AnnualVacationBalance} дн.)", emp));
        }
        if (_cmbEmployee.Items.Count > 0)
            _cmbEmployee.SelectedIndex = 0;
        _cmbEmployee.SelectedIndexChanged += (_, _) => UpdatePeriodPreview();
        Controls.Add(_cmbEmployee);
        y += 36;

        Controls.Add(new Label { Text = "Тип заявки *:", Left = 16, Top = y + 3, Width = 155 });
        _cmbType = new ComboBox
        {
            Left = 178,
            Top = y,
            Width = 358,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbType.Items.AddRange(new object[] { "Відпустка", "Лікарняний", "Відрядження" });
        _cmbType.SelectedIndex = 0;
        _cmbType.SelectedIndexChanged += (_, _) => UpdateTypePanel();
        Controls.Add(_cmbType);
        y += 36;

        Controls.Add(new Label { Text = "Період (з — по) *:", Left = 16, Top = y + 3, Width = 155 });
        _dtpStart = new DateTimePicker
        {
            Left = 178,
            Top = y,
            Width = 165,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(1)
        };
        _dtpStart.ValueChanged += (_, _) => UpdatePeriodPreview();

        Controls.Add(new Label { Text = "—", Left = 350, Top = y + 3, Width = 18, TextAlign = ContentAlignment.MiddleCenter });

        _dtpEnd = new DateTimePicker
        {
            Left = 371,
            Top = y,
            Width = 165,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddDays(5)
        };
        _dtpEnd.ValueChanged += (_, _) => UpdatePeriodPreview();
        Controls.AddRange(new Control[] { _dtpStart, _dtpEnd });
        y += 32;

        _lblCalculation = new Label
        {
            Left = 178,
            Top = y,
            Width = 358,
            Height = 22,
            ForeColor = Color.DarkSlateBlue
        };
        Controls.Add(_lblCalculation);
        y += 28;

        Controls.Add(new Label { Text = "Підстава / опис *:", Left = 16, Top = y + 3, Width = 155 });
        _txtReason = new TextBox
        {
            Left = 178,
            Top = y,
            Width = 358,
            Height = 72,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        Controls.Add(_txtReason);
        y += 84;

        var grpSpecific = new GroupBox
        {
            Text = "Параметри обраного типу заявки",
            Left = 16,
            Top = y,
            Width = 520,
            Height = 148
        };
        Controls.Add(grpSpecific);

        // 1. Панель Відпустки
        _pnlVacation = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        _pnlVacation.Controls.Add(new Label { Text = "Вид відпустки:", Left = 10, Top = 22, Width = 150 });
        _cmbVacationType = new ComboBox
        {
            Left = 165,
            Top = 19,
            Width = 330,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        foreach (VacationType vt in Enum.GetValues<VacationType>())
            _cmbVacationType.Items.Add(new ComboItem<VacationType>(UiDisplayHelper.FormatVacationType(vt), vt));
        _cmbVacationType.SelectedIndex = 0;
        _pnlVacation.Controls.Add(_cmbVacationType);

        _pnlVacation.Controls.Add(new Label { Text = "Хто заміщує:", Left = 10, Top = 62, Width = 150 });
        _txtSubstitute = new TextBox { Left = 165, Top = 59, Width = 330, PlaceholderText = "ПІБ колеги (необов'язково)" };
        _pnlVacation.Controls.Add(_txtSubstitute);
        grpSpecific.Controls.Add(_pnlVacation);

        // 2. Панель Лікарняного
        _pnlSickLeave = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), Visible = false };
        _pnlSickLeave.Controls.Add(new Label { Text = "№ медвисновку *:", Left = 10, Top = 22, Width = 150 });
        _txtMedCert = new TextBox { Left = 165, Top = 19, Width = 330, PlaceholderText = "Наприклад: EL-904215-2026" };
        _pnlSickLeave.Controls.Add(_txtMedCert);

        _chkHospitalized = new CheckBox
        {
            Text = "Стаціонарне лікування",
            Left = 165,
            Top = 62,
            Width = 280
        };
        _pnlSickLeave.Controls.Add(_chkHospitalized);
        grpSpecific.Controls.Add(_pnlSickLeave);

        // 3. Панель Відрядження
        _pnlBusinessTrip = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), Visible = false };
        _pnlBusinessTrip.Controls.Add(new Label { Text = "Місто призначення *:", Left = 10, Top = 18, Width = 150 });
        _txtCity = new TextBox { Left = 165, Top = 15, Width = 330 };
        _pnlBusinessTrip.Controls.Add(_txtCity);

        _pnlBusinessTrip.Controls.Add(new Label { Text = "Мета поїздки *:", Left = 10, Top = 54, Width = 150 });
        _txtPurpose = new TextBox { Left = 165, Top = 51, Width = 330 };
        _pnlBusinessTrip.Controls.Add(_txtPurpose);

        _pnlBusinessTrip.Controls.Add(new Label { Text = "Добові (грн/день):", Left = 10, Top = 90, Width = 150 });
        _numAllowance = new NumericUpDown
        {
            Left = 165,
            Top = 87,
            Width = 140,
            Minimum = 0,
            Maximum = 10000,
            Increment = 50,
            Value = 650
        };
        _pnlBusinessTrip.Controls.Add(_numAllowance);
        grpSpecific.Controls.Add(_pnlBusinessTrip);

        y += 166;

        var btnSubmit = new Button
        {
            Text = "Подати заявку",
            Left = 290,
            Top = y,
            Width = 140,
            Height = 34
        };
        btnSubmit.Click += BtnSubmit_Click;
        Controls.Add(btnSubmit);

        var btnCancel = new Button
        {
            Text = "Скасувати",
            Left = 440,
            Top = y,
            Width = 96,
            Height = 34,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnCancel);

        AcceptButton = btnSubmit;
        CancelButton = btnCancel;
    }

    private void UpdateTypePanel()
    {
        _pnlVacation.Visible = _cmbType.SelectedIndex == 0;
        _pnlSickLeave.Visible = _cmbType.SelectedIndex == 1;
        _pnlBusinessTrip.Visible = _cmbType.SelectedIndex == 2;
    }

    private void UpdatePeriodPreview()
    {
        var start = DateOnly.FromDateTime(_dtpStart.Value);
        var end = DateOnly.FromDateTime(_dtpEnd.Value);

        if (end < start)
        {
            _lblCalculation.Text = "Увага: дата завершення раніше за дату початку.";
            _lblCalculation.ForeColor = Color.Crimson;
            return;
        }

        var calendarDays = end.DayNumber - start.DayNumber + 1;
        _lblCalculation.ForeColor = Color.DarkSlateBlue;

        if (_cmbEmployee.SelectedItem is ComboItem<Employee> empItem)
        {
            var schedule = _hrService.GetWorkScheduleById(empItem.Value.WorkScheduleId);
            var workDays = schedule.CountWorkingDays(start, end);
            _lblCalculation.Text = $"Календарних днів: {calendarDays} | Робочих днів за графіком: {workDays}";
        }
        else
        {
            _lblCalculation.Text = $"Календарних днів: {calendarDays}";
        }
    }

    private void BtnSubmit_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_cmbEmployee.SelectedItem is not ComboItem<Employee> empItem)
                throw new HrDomainException("Немає активних працівників для подання заявки.");

            var employee = empItem.Value;
            var start = DateOnly.FromDateTime(_dtpStart.Value);
            var end = DateOnly.FromDateTime(_dtpEnd.Value);

            HrRequest request = _cmbType.SelectedIndex switch
            {
                0 => new VacationRequest(
                    employee.Id,
                    start,
                    end,
                    _txtReason.Text,
                    employee.FullName,
                    ((ComboItem<VacationType>)_cmbVacationType.SelectedItem!).Value,
                    _txtSubstitute.Text),

                1 => new SickLeaveRequest(
                    employee.Id,
                    start,
                    end,
                    _txtReason.Text,
                    employee.FullName,
                    _txtMedCert.Text,
                    _chkHospitalized.Checked),

                2 => new BusinessTripRequest(
                    employee.Id,
                    start,
                    end,
                    _txtReason.Text,
                    employee.FullName,
                    _txtCity.Text,
                    _txtPurpose.Text,
                    _numAllowance.Value),

                _ => throw new HrDomainException("Оберіть коректний тип заявки.")
            };

            _hrService.SubmitRequest(request);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is ArgumentException or HrDomainException)
        {
            MessageBox.Show(ex.Message, "Перевірка даних", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}