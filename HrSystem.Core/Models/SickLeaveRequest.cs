using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Models;

public class SickLeaveRequest : HrRequest
{
    public string MedicalCertificateNumber { get; private set; }
    public bool IsHospitalized { get; private set; }

    public override string RequestTypeName => "Лікарняний";

    public SickLeaveRequest(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        string submittedBy,
        string medicalCertificateNumber,
        bool isHospitalized = false)
        : base(employeeId, startDate, endDate, reason, submittedBy)
    {
        if (string.IsNullOrWhiteSpace(medicalCertificateNumber))
            throw new ArgumentException("Потрібно вказати номер медичного висновку (е-лікарняного).", nameof(medicalCertificateNumber));

        MedicalCertificateNumber = medicalCertificateNumber.Trim();
        IsHospitalized = isHospitalized;
    }

    public override void ValidateForEmployee(Employee employee, WorkSchedule schedule)
    {
        // Лікарняний не залежить від залишку щорічної відпустки
    }

    public override void ApplyFinalApprovalEffects(Employee employee)
    {
        // Баланс щорічної відпустки не зменшується
    }

    public override string GetSpecificDetails(WorkSchedule? schedule = null)
    {
        var mode = IsHospitalized ? "стаціонар" : "амбулаторно";
        var workDaysInfo = schedule is not null
            ? $" | Пропущено змін: {schedule.CountWorkingDays(StartDate, EndDate)}"
            : string.Empty;

        return $"Медвисновок № {MedicalCertificateNumber} | Лікування: {mode}{workDaysInfo}";
    }
}