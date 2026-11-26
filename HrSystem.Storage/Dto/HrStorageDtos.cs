using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;

namespace HrSystem.Storage.Dto;

internal class HrStorageDto
{
    public List<DepartmentDto> Departments { get; set; } = new();
    public List<WorkScheduleDto> WorkSchedules { get; set; } = new();
    public List<EmployeeDto> Employees { get; set; } = new();
    public List<HrRequestDto> Requests { get; set; } = new();
}

internal class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

internal class WorkScheduleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int WeeklyHours { get; set; }
    public bool IsShiftTwoByTwo { get; set; }
    public List<DayOfWeek> WorkingDays { get; set; } = new();
}

internal class EmployeeDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid WorkScheduleId { get; set; }
    public DateOnly HireDate { get; set; }
    public int AnnualVacationBalance { get; set; }
    public bool IsActive { get; set; }
    public DateOnly? DismissalDate { get; set; }
    public string? DismissalReason { get; set; }
}

internal class RequestHistoryEntryDto
{
    public DateTime Timestamp { get; set; }
    public RequestStatus? OldStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

internal class HrRequestDto
{
    public string RequestType { get; set; } = string.Empty;

    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RequestHistoryEntryDto> History { get; set; } = new();

    // Поля VacationRequest
    public VacationType? VacationType { get; set; }
    public string? SubstituteName { get; set; }

    // Поля SickLeaveRequest
    public string? MedicalCertificateNumber { get; set; }
    public bool? IsHospitalized { get; set; }

    // Поля BusinessTripRequest
    public string? DestinationCity { get; set; }
    public string? TripPurpose { get; set; }
    public decimal? DailyAllowanceUah { get; set; }
}