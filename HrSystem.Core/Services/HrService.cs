using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Interfaces;
using HrSystem.Core.Models;
using HrSystem.Core.Workflow;

namespace HrSystem.Core.Services;

public class HrService
{
    private readonly IHrRepository _repository;
    private readonly List<Department> _departments = new();
    private readonly List<WorkSchedule> _schedules = new();
    private readonly List<Employee> _employees = new();
    private readonly List<HrRequest> _requests = new();

    public HrService(IHrRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Reload();
    }

    public void Reload()
    {
        var data = _repository.Load();
        _departments.Clear();
        _schedules.Clear();
        _employees.Clear();
        _requests.Clear();

        if (data.Departments is not null) _departments.AddRange(data.Departments);
        if (data.WorkSchedules is not null) _schedules.AddRange(data.WorkSchedules);
        if (data.Employees is not null) _employees.AddRange(data.Employees);
        if (data.Requests is not null) _requests.AddRange(data.Requests);
    }

    // --- Підрозділи ---

    public IReadOnlyList<Department> GetAllDepartments() =>
        _departments.OrderBy(d => d.Name).ToList().AsReadOnly();

    public Department GetDepartmentById(Guid id) => FindDepartmentOrThrow(id);

    public Department AddDepartment(string name, string managerName, string description = "")
    {
        if (_departments.Any(d => string.Equals(d.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new HrDomainException($"Підрозділ із назвою «{name.Trim()}» вже існує.");

        var department = new Department(name, managerName, description);
        _departments.Add(department);
        SaveChanges();
        return department;
    }

    public string GetDepartmentName(Guid departmentId) =>
        _departments.FirstOrDefault(d => d.Id == departmentId)?.Name ?? "Невідомий підрозділ";

    // --- Робочі графіки ---

    public IReadOnlyList<WorkSchedule> GetAllWorkSchedules() =>
        _schedules.OrderBy(s => s.Name).ToList().AsReadOnly();

    public WorkSchedule GetWorkScheduleById(Guid id) => FindScheduleOrThrow(id);

    public WorkSchedule AddWorkSchedule(
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int weeklyHours,
        IEnumerable<DayOfWeek> workingDays,
        bool isShiftTwoByTwo = false)
    {
        if (_schedules.Any(s => string.Equals(s.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new HrDomainException($"Робочий графік «{name.Trim()}» вже існує.");

        var schedule = new WorkSchedule(name, startTime, endTime, weeklyHours, workingDays, isShiftTwoByTwo);
        _schedules.Add(schedule);
        SaveChanges();
        return schedule;
    }

    public string GetScheduleName(Guid scheduleId) =>
        _schedules.FirstOrDefault(s => s.Id == scheduleId)?.Name ?? "Невідомий графік";

    // --- Працівники ---

    public IReadOnlyList<Employee> GetAllEmployees() =>
        _employees.OrderBy(e => e.FullName).ToList().AsReadOnly();

    public IReadOnlyList<Employee> GetActiveEmployees() =>
        _employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToList().AsReadOnly();

    public Employee GetEmployeeById(Guid id) => FindEmployeeOrThrow(id);

    public Employee AddEmployee(
        string fullName,
        string position,
        Guid departmentId,
        Guid workScheduleId,
        DateOnly hireDate,
        int annualVacationBalance = 24)
    {
        FindDepartmentOrThrow(departmentId);
        FindScheduleOrThrow(workScheduleId);

        var employee = new Employee(
            fullName, position, departmentId, workScheduleId, hireDate, annualVacationBalance);

        _employees.Add(employee);
        SaveChanges();
        return employee;
    }

    public void UpdateEmployeeAssignment(Guid employeeId, Guid departmentId, Guid workScheduleId)
    {
        var employee = FindEmployeeOrThrow(employeeId);
        FindDepartmentOrThrow(departmentId);
        FindScheduleOrThrow(workScheduleId);

        employee.ChangeDepartment(departmentId);
        employee.ChangeWorkSchedule(workScheduleId);
        SaveChanges();
    }

    public void UpdateEmployeeDetails(Guid employeeId, string newPosition, Guid departmentId, Guid workScheduleId)
    {
        var employee = FindEmployeeOrThrow(employeeId);
        FindDepartmentOrThrow(departmentId);
        FindScheduleOrThrow(workScheduleId);

        employee.ChangePosition(newPosition);
        employee.ChangeDepartment(departmentId);
        employee.ChangeWorkSchedule(workScheduleId);
        SaveChanges();
    }

    public int DismissEmployee(Guid employeeId, DateOnly dismissalDate, string reason, string performedBy = "Відділ кадрів")
    {
        var employee = FindEmployeeOrThrow(employeeId);
        employee.Dismiss(dismissalDate, reason);

        // Автоматично скасовуємо всі незавершені заявки працівника з фіксацією в історії
        var pendingRequests = _requests
            .Where(r => r.EmployeeId == employeeId &&
                        r.Status is RequestStatus.Submitted or RequestStatus.ManagerApproved)
            .ToList();

        foreach (var req in pendingRequests)
        {
            req.ChangeStatus(
                RequestStatus.Cancelled,
                performedBy,
                $"Автоматично скасовано у зв'язку зі звільненням ({reason.Trim()})");
        }

        SaveChanges();
        return pendingRequests.Count;
    }

    public void SetEmployeeActiveStatus(Guid employeeId, bool isActive)
    {
        var employee = FindEmployeeOrThrow(employeeId);
        employee.SetActiveStatus(isActive);
        SaveChanges();
    }

    public string GetEmployeeDisplayName(Guid employeeId) =>
        _employees.FirstOrDefault(e => e.Id == employeeId)?.FullName ?? "Невідомий працівник";

    // --- Кадрові заявки ---

    public IReadOnlyList<HrRequest> GetAllRequests() =>
        _requests.OrderByDescending(r => r.CreatedAt).ToList().AsReadOnly();

    public HrRequest GetRequestById(Guid id) => FindRequestOrThrow(id);

    public HrRequest SubmitRequest(HrRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var employee = FindEmployeeOrThrow(request.EmployeeId);
        if (employee.IsDismissed)
            throw new HrDomainException($"Працівник {employee.FullName} звільнений ({employee.DismissalDate:dd.MM.yyyy}) і не може подавати заявки.");

        if (!employee.IsActive)
            throw new HrDomainException($"Працівник {employee.FullName} неактивний і не може подавати заявки.");

        var schedule = FindScheduleOrThrow(employee.WorkScheduleId);

        // Поліморфна валідація (наприклад, перевірка залишку днів відпустки)
        request.ValidateForEmployee(employee, schedule);

        // Перевірка перетину дат із уже активними заявками працівника
        var conflict = _requests.FirstOrDefault(r =>
            r.EmployeeId == request.EmployeeId &&
            r.IsActiveForOverlapCheck() &&
            r.OverlapsWith(request.StartDate, request.EndDate));

        if (conflict is not null)
        {
            throw new HrDomainException(
                $"Період ({request.StartDate:dd.MM.yyyy}–{request.EndDate:dd.MM.yyyy}) перетинається з іншою активною заявкою " +
                $"«{conflict.RequestTypeName}» ({conflict.StartDate:dd.MM.yyyy}–{conflict.EndDate:dd.MM.yyyy}).");
        }

        _requests.Add(request);
        SaveChanges();
        return request;
    }

    public void ChangeRequestStatus(Guid requestId, RequestStatus newStatus, string changedBy, string comment = "")
    {
        var request = FindRequestOrThrow(requestId);
        var employee = FindEmployeeOrThrow(request.EmployeeId);
        var schedule = FindScheduleOrThrow(employee.WorkScheduleId);

        // Спочатку перевіряємо допустимість переходу за машиною станів
        ApprovalWorkflow.ValidateTransition(request.Status, newStatus);

        // Якщо це фінальне затвердження HR — повторно перевіряємо баланс перед зміною статусу
        if (newStatus == RequestStatus.HrApproved)
        {
            request.ValidateForEmployee(employee, schedule);
        }

        request.ChangeStatus(newStatus, changedBy, comment);

        // Застосовуємо поліморфний ефект затвердження (списання днів оплачуваної відпустки)
        if (newStatus == RequestStatus.HrApproved)
        {
            request.ApplyFinalApprovalEffects(employee);
        }

        SaveChanges();
    }

    public void DeleteRequest(Guid requestId)
    {
        var request = FindRequestOrThrow(requestId);

        if (request.Status is RequestStatus.ManagerApproved or RequestStatus.HrApproved)
        {
            throw new HrDomainException(
                "Видаляти дозволено лише подані, відхилені або скасовані заявки. Погоджені й затверджені заявки зберігаються для обліку.");
        }

        _requests.Remove(request);
        SaveChanges();
    }

    // --- Фільтрація та статистика ---

    public IReadOnlyList<HrRequest> FilterRequests(RequestFilter? filter)
    {
        IEnumerable<HrRequest> query = _requests;

        if (filter is not null)
        {
            if (filter.Status.HasValue)
                query = query.Where(r => r.Status == filter.Status.Value);

            if (!string.IsNullOrWhiteSpace(filter.RequestTypeName))
                query = query.Where(r =>
                    string.Equals(r.RequestTypeName, filter.RequestTypeName, StringComparison.OrdinalIgnoreCase));

            if (filter.EmployeeId.HasValue)
                query = query.Where(r => r.EmployeeId == filter.EmployeeId.Value);

            if (filter.DepartmentId.HasValue)
            {
                var deptEmployeeIds = _employees
                    .Where(e => e.DepartmentId == filter.DepartmentId.Value)
                    .Select(e => e.Id)
                    .ToHashSet();

                query = query.Where(r => deptEmployeeIds.Contains(r.EmployeeId));
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(r =>
                {
                    var emp = _employees.FirstOrDefault(e => e.Id == r.EmployeeId);
                    var deptName = emp is not null ? GetDepartmentName(emp.DepartmentId) : string.Empty;
                    var sched = emp is not null ? _schedules.FirstOrDefault(s => s.Id == emp.WorkScheduleId) : null;

                    return (emp?.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           deptName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                           r.Reason.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                           r.GetSpecificDetails(sched).Contains(term, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        return query
            .OrderByDescending(r => r.CreatedAt)
            .ToList()
            .AsReadOnly();
    }

    public HrStatistics GetStatistics() => new(
        TotalEmployees: _employees.Count,
        ActiveEmployees: _employees.Count(e => e.IsActive),
        TotalDepartments: _departments.Count,
        TotalRequests: _requests.Count,
        SubmittedRequests: _requests.Count(r => r.Status == RequestStatus.Submitted),
        ManagerApprovedRequests: _requests.Count(r => r.Status == RequestStatus.ManagerApproved),
        HrApprovedRequests: _requests.Count(r => r.Status == RequestStatus.HrApproved),
        RejectedRequests: _requests.Count(r => r.Status == RequestStatus.Rejected),
        CancelledRequests: _requests.Count(r => r.Status == RequestStatus.Cancelled));

    private Department FindDepartmentOrThrow(Guid id) =>
        _departments.FirstOrDefault(d => d.Id == id)
        ?? throw new HrDomainException($"Підрозділ із ID {id} не знайдено.");

    private WorkSchedule FindScheduleOrThrow(Guid id) =>
        _schedules.FirstOrDefault(s => s.Id == id)
        ?? throw new HrDomainException($"Робочий графік із ID {id} не знайдено.");

    private Employee FindEmployeeOrThrow(Guid id) =>
        _employees.FirstOrDefault(e => e.Id == id)
        ?? throw new HrDomainException($"Працівника з ID {id} не знайдено.");

    private HrRequest FindRequestOrThrow(Guid id) =>
        _requests.FirstOrDefault(r => r.Id == id)
        ?? throw new HrDomainException($"Кадрову заявку з ID {id} не знайдено.");

    private void SaveChanges() =>
        _repository.Save(new HrData(_departments, _schedules, _employees, _requests));
}