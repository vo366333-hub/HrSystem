using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;

namespace HrSystem.Core.Models;

public class VacationRequest : HrRequest
{
    public VacationType VacationType { get; private set; }
    public string SubstituteName { get; private set; }

    public override string RequestTypeName => "Відпустка";

    public VacationRequest(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        string submittedBy,
        VacationType vacationType,
        string substituteName = "Не вказано")
        : base(employeeId, startDate, endDate, reason, submittedBy)
    {
        VacationType = vacationType;
        SubstituteName = string.IsNullOrWhiteSpace(substituteName) ? "Не вказано" : substituteName.Trim();
    }

    public override void ValidateForEmployee(Employee employee, WorkSchedule schedule)
    {
        if (VacationType == VacationType.AnnualPaid && TotalCalendarDays > employee.AnnualVacationBalance)
        {
            throw new HrDomainException(
                $"Запитано {TotalCalendarDays} дн. оплачуваної відпустки, але доступний залишок становить лише {employee.AnnualVacationBalance} дн.");
        }
    }

    public override void ApplyFinalApprovalEffects(Employee employee)
    {
        if (VacationType == VacationType.AnnualPaid)
        {
            employee.DeductVacationDays(TotalCalendarDays);
        }
    }

    public override string GetSpecificDetails(WorkSchedule? schedule = null)
    {
        var typeText = VacationType switch
        {
            VacationType.AnnualPaid => "Щорічна оплачувана",
            VacationType.Unpaid => "За власний рахунок",
            VacationType.Study => "Навчальна",
            _ => VacationType.ToString()
        };

        var workDaysInfo = schedule is not null
            ? $" | Робочих днів за графіком: {schedule.CountWorkingDays(StartDate, EndDate)}"
            : string.Empty;

        return $"Вид: {typeText} | Заміщує: {SubstituteName}{workDaysInfo}";
    }
}