using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Models;

public class BusinessTripRequest : HrRequest
{
    public string DestinationCity { get; private set; }
    public string TripPurpose { get; private set; }
    public decimal DailyAllowanceUah { get; private set; }

    public override string RequestTypeName => "Відрядження";

    // Конструктор для створення нового запиту на відрядження
    public BusinessTripRequest(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        string submittedBy,
        string destinationCity,
        string tripPurpose,
        decimal dailyAllowanceUah = 600m)
        : base(employeeId, startDate, endDate, reason, submittedBy)
    {
        if (string.IsNullOrWhiteSpace(destinationCity))
            throw new ArgumentException("Потрібно вказати місто відрядження.", nameof(destinationCity));

        if (string.IsNullOrWhiteSpace(tripPurpose))
            throw new ArgumentException("Потрібно вказати мету відрядження.", nameof(tripPurpose));

        if (dailyAllowanceUah < 0)
            throw new ArgumentException("Сума добових не може бути від'ємною.", nameof(dailyAllowanceUah));

        DestinationCity = destinationCity.Trim();
        TripPurpose = tripPurpose.Trim();
        DailyAllowanceUah = dailyAllowanceUah;
    }

    public decimal CalculateTotalAllowance() => TotalCalendarDays * DailyAllowanceUah;

    public override void ValidateForEmployee(Employee employee, WorkSchedule schedule)
    {
        // Перевірка активності та перетину дат виконується централізовано в сервісі
    }

    public override void ApplyFinalApprovalEffects(Employee employee)
    {
        // Відрядження зараховується як робочий час і не списує відпустку
    }

    // Метод для отримання специфічних деталей запиту на відрядження
    public override string GetSpecificDetails(WorkSchedule? schedule = null)
    {
        return $"Місто: {DestinationCity} | Мета: {TripPurpose} | Добові: {CalculateTotalAllowance():N0} грн ({DailyAllowanceUah:N0} грн/день)";
    }
}