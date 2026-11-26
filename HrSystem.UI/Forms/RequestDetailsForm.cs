using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Services;
using HrSystem.Core.Workflow;

namespace HrSystem.UI.Forms;

public class RequestDetailsForm : Form
{
    private readonly HrService _hrService;
    private readonly Guid _requestId;

    private Label _lblHeader = null!;
    private Label _lblEmployee = null!;
    private Label _lblStatus = null!;
    private Label _lblDepartment = null!;
    private Label _lblSchedule = null!;
    private Label _lblPeriod = null!;
    private Label _lblBalance = null!;
    private Label _lblDetails = null!;
    private TextBox _txtReason = null!;

    private TextBox _txtActor = null!;
    private ComboBox _cmbNextStatus = null!;
    private TextBox _txtComment = null!;
    private Button _btnApply = null!;

    private ListBox _lstHistory = null!;

    public bool HasChanges { get; private set; }

    public RequestDetailsForm(HrService hrService, Guid requestId)
    {
        _hrService = hrService;
        _requestId = requestId;
        BuildUi();
        LoadRequestData();
    }

    private void BuildUi()
    {
        Text = "Картка кадрової заявки та погодження";
        Size = new Size(780, 650);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F);

        var grpInfo = new GroupBox
        {
            Text = "Інформація про заявку",
            Left = 14,
            Top = 10,
            Width = 736,
            Height = 235
        };
        Controls.Add(grpInfo);

        _lblHeader = new Label { Left = 14, Top = 22, Width = 705, Height = 25, Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
        _lblEmployee = new Label { Left = 14, Top = 52, Width = 350 };
        _lblStatus = new Label { Left = 375, Top = 52, Width = 345, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
        _lblDepartment = new Label { Left = 14, Top = 76, Width = 350 };
        _lblBalance = new Label { Left = 375, Top = 76, Width = 345 };
        _lblSchedule = new Label { Left = 14, Top = 100, Width = 350 };
        _lblPeriod = new Label { Left = 375, Top = 100, Width = 345 };
        _lblDetails = new Label { Left = 14, Top = 126, Width = 705, ForeColor = Color.DarkSlateBlue };

        _txtReason = new TextBox
        {
            Left = 14,
            Top = 154,
            Width = 705,
            Height = 68,
            Multiline = true,
            ReadOnly = true,
            TabStop = false,
            BackColor = SystemColors.Window,
            ScrollBars = ScrollBars.Vertical
        };

        grpInfo.Controls.AddRange(new Control[]
        {
            _lblHeader, _lblEmployee, _lblStatus, _lblDepartment,
            _lblBalance, _lblSchedule, _lblPeriod, _lblDetails, _txtReason
        });

        var grpWorkflow = new GroupBox
        {
            Text = "Рішення щодо погодження",
            Left = 14,
            Top = 252,
            Width = 736,
            Height = 132
        };
        Controls.Add(grpWorkflow);

        grpWorkflow.Controls.Add(new Label { Text = "Хто приймає рішення:", Left = 14, Top = 28, Width = 150 });
        _txtActor = new TextBox { Left = 170, Top = 25, Width = 330 };
        grpWorkflow.Controls.Add(_txtActor);

        grpWorkflow.Controls.Add(new Label { Text = "Новий статус:", Left = 14, Top = 64, Width = 150 });
        _cmbNextStatus = new ComboBox
        {
            Left = 170,
            Top = 61,
            Width = 215,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        grpWorkflow.Controls.Add(_cmbNextStatus);

        _txtComment = new TextBox
        {
            Left = 395,
            Top = 61,
            Width = 325,
            PlaceholderText = "Коментар (обов'язковий при відхиленні)"
        };
        grpWorkflow.Controls.Add(_txtComment);

        _btnApply = new Button
        {
            Text = "Застосувати рішення",
            Left = 530,
            Top = 94,
            Width = 190,
            Height = 30
        };
        _btnApply.Click += BtnApply_Click;
        grpWorkflow.Controls.Add(_btnApply);

        var grpHistory = new GroupBox
        {
            Text = "Журнал історії погодження",
            Left = 14,
            Top = 392,
            Width = 736,
            Height = 170
        };
        Controls.Add(grpHistory);

        _lstHistory = new ListBox
        {
            Left = 12,
            Top = 22,
            Width = 712,
            Height = 138,
            HorizontalScrollbar = true
        };
        grpHistory.Controls.Add(_lstHistory);

        var btnClose = new Button
        {
            Text = "Закрити",
            Left = 640,
            Top = 570,
            Width = 110,
            Height = 32,
            DialogResult = DialogResult.OK
        };
        Controls.Add(btnClose);
    }

    private void LoadRequestData()
    {
        var req = _hrService.GetRequestById(_requestId);
        var emp = _hrService.GetEmployeeById(req.EmployeeId);
        var dept = _hrService.GetDepartmentById(emp.DepartmentId);
        var sched = _hrService.GetWorkScheduleById(emp.WorkScheduleId);

        _lblHeader.Text = $"{req.RequestTypeName}: {emp.FullName} ({req.StartDate:dd.MM.yyyy} — {req.EndDate:dd.MM.yyyy})";
        _lblEmployee.Text = $"Працівник: {emp.FullName} ({emp.Position})";
        _lblStatus.Text = $"Статус: {UiDisplayHelper.FormatStatus(req.Status)}";
        _lblDepartment.Text = $"Підрозділ: {dept.Name} (кер. {dept.ManagerName})";
        _lblBalance.Text = $"Залишок оплачуваної відпустки: {emp.AnnualVacationBalance} дн.";
        _lblSchedule.Text = $"Робочий графік: {sched}";
        _lblPeriod.Text = $"Тривалість: {req.TotalCalendarDays} кал. дн. ({sched.CountWorkingDays(req.StartDate, req.EndDate)} роб. дн.)";
        _lblDetails.Text = $"Деталі: {req.GetSpecificDetails(sched)}";
        _txtReason.Text = req.Reason;

        // Розумне підставлення погоджувача залежно від поточного етапу
        _txtActor.Text = req.Status switch
        {
            RequestStatus.Submitted => dept.ManagerName,
            RequestStatus.ManagerApproved => "HR-менеджер Іванчук О.",
            _ => "Відділ кадрів"
        };

        // Заповнення лише дозволених переходів із ApprovalWorkflow
        _cmbNextStatus.Items.Clear();
        var allowed = ApprovalWorkflow.GetAllowedTransitions(req.Status);
        foreach (var next in allowed)
            _cmbNextStatus.Items.Add(new ComboItem<RequestStatus>(UiDisplayHelper.FormatStatus(next), next));

        bool hasActions = _cmbNextStatus.Items.Count > 0;
        if (hasActions)
            _cmbNextStatus.SelectedIndex = 0;

        _txtActor.Enabled = hasActions;
        _cmbNextStatus.Enabled = hasActions;
        _txtComment.Enabled = hasActions;
        _btnApply.Enabled = hasActions;

        // Журнал змін
        _lstHistory.Items.Clear();
        foreach (var entry in req.History)
            _lstHistory.Items.Add(entry.ToString());
    }

    private void BtnApply_Click(object? sender, EventArgs e)
    {
        try
        {
            if (_cmbNextStatus.SelectedItem is not ComboItem<RequestStatus> selected)
                return;

            _hrService.ChangeRequestStatus(_requestId, selected.Value, _txtActor.Text, _txtComment.Text);
            _txtComment.Clear();
            HasChanges = true;
            LoadRequestData();
        }
        catch (Exception ex) when (ex is ArgumentException or HrDomainException)
        {
            MessageBox.Show(ex.Message, "Порушення правил погодження", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}