using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;

namespace HrSystem.UI;

internal sealed class ComboItem<T>
{
    public string Text { get; }
    public T Value { get; }

    public ComboItem(string text, T value)
    {
        Text = text;
        Value = value;
    }

    public override string ToString() => Text;
}

internal static class UiDisplayHelper
{
    public static string FormatStatus(RequestStatus status) => status switch
    {
        RequestStatus.Submitted => "Подана",
        RequestStatus.ManagerApproved => "Погоджено керівником",
        RequestStatus.HrApproved => "Затверджено HR",
        RequestStatus.Rejected => "Відхилено",
        RequestStatus.Cancelled => "Скасовано",
        _ => status.ToString()
    };

    public static string FormatVacationType(VacationType type) => type switch
    {
        VacationType.AnnualPaid => "Щорічна оплачувана",
        VacationType.Unpaid => "Без збереження зарплати",
        VacationType.Study => "Навчальна",
        _ => type.ToString()
    };

    public static string FormatDayShort(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Пн",
        DayOfWeek.Tuesday => "Вт",
        DayOfWeek.Wednesday => "Ср",
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        DayOfWeek.Sunday => "Нд",
        _ => day.ToString()
    };
}