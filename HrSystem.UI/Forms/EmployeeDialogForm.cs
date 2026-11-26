using System;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Models;
using HrSystem.Core.Services;

namespace HrSystem.UI.Forms;

public class EmployeeDialogForm : Form
{
    private readonly HrService _hrService;
    private readonly Employee? _editingEmployee;

    private TextBox _txtFullName = null!;
    private TextBox _txtPosition = null!;
    private ComboBox _cmbDepartment = null!;
    private ComboBox _cmbSchedule = null!;
    private DateTimePicker _dtpHireDate = null!;
    private NumericUpDown _numVacationBalance = null!;

    public EmployeeDialogForm(HrService hrService, Employee? editingEmployee = null)
    {
        _hrService = hrService;
        _editingEmployee = editingEmployee;
        BuildUi();
        LoadInitialValues();
    }

    private void BuildUi()
    {
        bool isEdit = _editingEmployee is not null;
        Text = isEdit ? "Зміна посади, підрозділу та графіка" : "Додавання нового працівника";
        Size = new Size(490, 340);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F);

        int y = 18;

        Controls.Add(new Label { Text = "ПІБ працівника *:", Left = 16, Top = y + 3, Width = 155 });
        _txtFullName = new TextBox { Left = 178, Top = y, Width = 278, ReadOnly = isEdit };
        Controls.Add(_txtFullName);
        y += 36;

        Controls.Add(new Label { Text = "Посада *:", Left = 16, Top = y + 3, Width = 155 });
        _txtPosition = new TextBox { Left = 178, Top = y, Width = 278 };
        Controls.Add(_txtPosition);
        y += 36;

        Controls.Add(new Label { Text = "Підрозділ *:", Left = 16, Top = y + 3, Width = 155 });
        _cmbDepartment = new ComboBox { Left = 178, Top = y, Width = 278, DropDownStyle = ComboBoxStyle.DropDownList };
        Controls.Add(_cmbDepartment);
        y += 36;

        Controls.Add(new Label { Text = "Робочий графік *:", Left = 16, Top = y + 3, Width = 155 });
        _cmbSchedule = new ComboBox { Left = 178, Top = y, Width = 278, DropDownStyle = ComboBoxStyle.DropDownList };
        Controls.Add(_cmbSchedule);
        y += 36;

        Controls.Add(new Label { Text = "Дата прийняття:", Left = 16, Top = y + 3, Width = 155 });
        _dtpHireDate = new DateTimePicker
        {
            Left = 178,
            Top = y,
            Width = 140,
            Format = DateTimePickerFormat.Short,
            Enabled = !isEdit
        };
        Controls.Add(_dtpHireDate);
        y += 36;

        Controls.Add(new Label { Text = "Баланс відпустки (дн.):", Left = 16, Top = y + 3, Width = 155 });
        _numVacationBalance = new NumericUpDown
        {
            Left = 178,
            Top = y,
            Width = 100,
            Minimum = 0,
            Maximum = 60,
            Value = 24,
            Enabled = !isEdit
        };
        Controls.Add(_numVacationBalance);
        y += 48;

        var btnSave = new Button
        {
            Text = isEdit ? "Зберегти зміни" : "Додати працівника",
            Left = 200,
            Top = y,
            Width = 150,
            Height = 32
        };
        btnSave.Click += BtnSave_Click;
        Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text = "Скасувати",
            Left = 360,
            Top = y,
            Width = 96,
            Height = 32,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnCancel);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void LoadInitialValues()
    {
        foreach (var dept in _hrService.GetAllDepartments())
            _cmbDepartment.Items.Add(new ComboItem<Department>(dept.Name, dept));

        foreach (var sched in _hrService.GetAllWorkSchedules())
            _cmbSchedule.Items.Add(new ComboItem<WorkSchedule>(sched.ToString(), sched));

        if (_editingEmployee is not null)
        {
            var departmentId = _editingEmployee.DepartmentId;
            var workScheduleId = _editingEmployee.WorkScheduleId;

            _txtFullName.Text = _editingEmployee.FullName;
            _txtPosition.Text = _editingEmployee.Position;
            _dtpHireDate.Value = _editingEmployee.HireDate.ToDateTime(TimeOnly.MinValue);
            _numVacationBalance.Value = Math.Clamp(_editingEmployee.AnnualVacationBalance, 0, 60);

            SelectById<Department>(_cmbDepartment, d => d.Id == departmentId);
            SelectById<WorkSchedule>(_cmbSchedule, s => s.Id == workScheduleId);
        }
        else
        {
            if (_cmbDepartment.Items.Count > 0) _cmbDepartment.SelectedIndex = 0;
            if (_cmbSchedule.Items.Count > 0) _cmbSchedule.SelectedIndex = 0;
        }
    }

    private static void SelectById<T>(ComboBox box, Func<T, bool> predicate)
    {
        for (int i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is ComboItem<T> item && predicate(item.Value))
            {
                box.SelectedIndex = i;
                return;
            }
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_cmbDepartment.SelectedItem is not ComboItem<Department> deptItem)
                throw new HrDomainException("Спочатку створіть хоча б один підрозділ.");

            if (_cmbSchedule.SelectedItem is not ComboItem<WorkSchedule> schedItem)
                throw new HrDomainException("Спочатку створіть хоча б один робочий графік.");

            if (_editingEmployee is not null)
            {
                _hrService.UpdateEmployeeDetails(_editingEmployee.Id, _txtPosition.Text, deptItem.Value.Id, schedItem.Value.Id);
            }
            else
            {
                _hrService.AddEmployee(
                    _txtFullName.Text,
                    _txtPosition.Text,
                    deptItem.Value.Id,
                    schedItem.Value.Id,
                    DateOnly.FromDateTime(_dtpHireDate.Value),
                    (int)_numVacationBalance.Value);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is ArgumentException or HrDomainException)
        {
            MessageBox.Show(ex.Message, "Перевірка даних", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}