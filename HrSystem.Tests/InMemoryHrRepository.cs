using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Interfaces;
using HrSystem.Core.Models;

namespace HrSystem.Tests;

internal class InMemoryHrRepository : IHrRepository
{
    private List<Department> _departments = new();
    private List<WorkSchedule> _schedules = new();
    private List<Employee> _employees = new();
    private List<HrRequest> _requests = new();

    public HrData Load() => new(
        _departments.ToList(),
        _schedules.ToList(),
        _employees.ToList(),
        _requests.ToList());

    public void Save(HrData data)
    {
        _departments = data.Departments.ToList();
        _schedules = data.WorkSchedules.ToList();
        _employees = data.Employees.ToList();
        _requests = data.Requests.ToList();
    }
}