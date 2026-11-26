using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Models;

namespace HrSystem.Storage.Dto;

internal static class HrStorageMapper
{
    public static DepartmentDto ToDto(Department d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        ManagerName = d.ManagerName,
        Description = d.Description
    };

    public static Department FromDto(DepartmentDto dto) =>
        new(dto.Name, dto.ManagerName, dto.Description, dto.Id);

    public static WorkScheduleDto ToDto(WorkSchedule s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        WeeklyHours = s.WeeklyHours,
        IsShiftTwoByTwo = s.IsShiftTwoByTwo,
        WorkingDays = s.WorkingDays.ToList()
    };

    public static WorkSchedule FromDto(WorkScheduleDto dto) =>
        new(dto.Name, dto.StartTime, dto.EndTime, dto.WeeklyHours, dto.WorkingDays, dto.IsShiftTwoByTwo, dto.Id);

    public static EmployeeDto ToDto(Employee e) => new()
    {
        Id = e.Id,
        FullName = e.FullName,
        Position = e.Position,
        DepartmentId = e.DepartmentId,
        WorkScheduleId = e.WorkScheduleId,
        HireDate = e.HireDate,
        AnnualVacationBalance = e.AnnualVacationBalance,
        IsActive = e.IsActive,
        DismissalDate = e.DismissalDate,
        DismissalReason = e.DismissalReason
    };

    public static Employee FromDto(EmployeeDto dto) =>
        new(dto.FullName, dto.Position, dto.DepartmentId, dto.WorkScheduleId,
            dto.HireDate, dto.AnnualVacationBalance, dto.IsActive, dto.Id,
            dto.DismissalDate, dto.DismissalReason);

    public static HrRequestDto ToDto(HrRequest request)
    {
        var dto = new HrRequestDto
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Reason = request.Reason,
            Status = request.Status,
            CreatedAt = request.CreatedAt,
            History = request.History.Select(h => new RequestHistoryEntryDto
            {
                Timestamp = h.Timestamp,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                ChangedBy = h.ChangedBy,
                Comment = h.Comment
            }).ToList()
        };

        switch (request)
        {
            case VacationRequest vac:
                dto.RequestType = "Vacation";
                dto.VacationType = vac.VacationType;
                dto.SubstituteName = vac.SubstituteName;
                break;

            case SickLeaveRequest sick:
                dto.RequestType = "SickLeave";
                dto.MedicalCertificateNumber = sick.MedicalCertificateNumber;
                dto.IsHospitalized = sick.IsHospitalized;
                break;

            case BusinessTripRequest trip:
                dto.RequestType = "BusinessTrip";
                dto.DestinationCity = trip.DestinationCity;
                dto.TripPurpose = trip.TripPurpose;
                dto.DailyAllowanceUah = trip.DailyAllowanceUah;
                break;

            default:
                throw new HrDomainException($"Непідтримуваний тип кадрової заявки: {request.GetType().Name}");
        }

        return dto;
    }

    public static HrRequest FromDto(HrRequestDto dto)
    {
        var submittedBy = dto.History.FirstOrDefault()?.ChangedBy ?? "Система";

        HrRequest request = dto.RequestType switch
        {
            "Vacation" => new VacationRequest(
                dto.EmployeeId,
                dto.StartDate,
                dto.EndDate,
                dto.Reason,
                submittedBy,
                dto.VacationType ?? VacationType.AnnualPaid,
                dto.SubstituteName ?? "Не вказано"),

            "SickLeave" => new SickLeaveRequest(
                dto.EmployeeId,
                dto.StartDate,
                dto.EndDate,
                dto.Reason,
                submittedBy,
                dto.MedicalCertificateNumber ?? "Без номера",
                dto.IsHospitalized ?? false),

            "BusinessTrip" => new BusinessTripRequest(
                dto.EmployeeId,
                dto.StartDate,
                dto.EndDate,
                dto.Reason,
                submittedBy,
                dto.DestinationCity ?? "Не вказано",
                dto.TripPurpose ?? "Службова поїздка",
                dto.DailyAllowanceUah ?? 600m),

            _ => throw new HrDomainException($"Невідомий тип заявки у файлі даних: «{dto.RequestType}».")
        };

        var history = dto.History.Select(h =>
            new RequestHistoryEntry(h.OldStatus, h.NewStatus, h.ChangedBy, h.Comment, h.Timestamp));

        request.RestoreFromStorage(dto.Id, dto.Status, dto.CreatedAt, history);
        return request;
    }
}