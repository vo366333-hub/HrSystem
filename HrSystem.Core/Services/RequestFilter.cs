using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;

namespace HrSystem.Core.Services;

public class RequestFilter
{
    public RequestStatus? Status { get; set; }
    public string? RequestTypeName { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? EmployeeId { get; set; }
    public string? SearchText { get; set; }
}