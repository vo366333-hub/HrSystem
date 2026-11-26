using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Models;

namespace HrSystem.Core.Interfaces;

public record HrData(
    List<Department> Departments,
    List<WorkSchedule> WorkSchedules,
    List<Employee> Employees,
    List<HrRequest> Requests);

public interface IHrRepository
{
    HrData Load();
    void Save(HrData data);
}