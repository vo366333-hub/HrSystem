using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Exceptions;
using HrSystem.Core.Models;
using HrSystem.Core.Services;

namespace HrSystem.UI.Forms;

public class DismissEmployeeForm : Form
{
    private readonly HrService _hrService;
    private readonly Employee _employee;

    private DateTimePicker _dtpDismissalDate = null!;
    private TextBox _txtReason = null!;
    private TextBox _txtPerformedBy = null!;

    public int CancelledRequestsCount { get; private set; }

    public DismissEmployeeForm(HrService hrService, Employee employee)
    {
        _hrService = hrService;
        _employee = employee;
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "Оформлення звільнення працівника";
        Size = new Size(490, 295);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F);

        int y = 16;

        Controls.Add(new Label
        {
            Text = $"Працівник: {_employee.FullName} ({_employee.Position})",
            Left = 16,
            Top = y,
            Width = 440,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        });
        y += 30;

        Controls.Add(new Label
        {
            Text = "Увага: звільнення є незворотним. Усі незавершені заявки працівника будуть автоматично скасовані.",
            Left = 16,
            Top = y,
            Width = 440,
            Height = 36,
            ForeColor = Color.Firebrick
        });
        y += 42;

        Controls.Add(new Label { Text = "Дата звільнення *:", Left = 16, Top = y + 3, Width = 145 });
        _dtpDismissalDate = new DateTimePicker
        {
            Left = 168,
            Top = y,
            Width = 155,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };
        Controls.Add(_dtpDismissalDate);
        y += 36;

        Controls.Add(new Label { Text = "Причина звільнення *:", Left = 16, Top = y + 3, Width = 145 });
        _txtReason = new TextBox
        {
            Left = 168,
            Top = y,
            Width = 288,
            PlaceholderText = "Наприклад: За власним бажанням (ст. 38 КЗпП)"
        };
        Controls.Add(_txtReason);
        y += 36;

        Controls.Add(new Label { Text = "Хто оформив:", Left = 16, Top = y + 3, Width = 145 });
        _txtPerformedBy = new TextBox
        {
            Left = 168,
            Top = y,
            Width = 288,
            Text = "HR-менеджер Іванчук О."
        };
        Controls.Add(_txtPerformedBy);
        y += 44;

        var btnConfirm = new Button
        {
            Text = "Підтвердити звільнення",
            Left = 178,
            Top = y,
            Width = 175,
            Height = 32
        };
        btnConfirm.Click += BtnConfirm_Click;
        Controls.Add(btnConfirm);

        var btnCancel = new Button
        {
            Text = "Скасувати",
            Left = 363,
            Top = y,
            Width = 93,
            Height = 32,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(btnCancel);

        AcceptButton = btnConfirm;
        CancelButton = btnCancel;
    }

    private void BtnConfirm_Click(object? sender, EventArgs e)
    {
        try
        {
            var date = DateOnly.FromDateTime(_dtpDismissalDate.Value);
            CancelledRequestsCount = _hrService.DismissEmployee(
                _employee.Id,
                date,
                _txtReason.Text,
                _txtPerformedBy.Text);

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is ArgumentException or HrDomainException)
        {
            MessageBox.Show(ex.Message, "Помилка звільнення", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}