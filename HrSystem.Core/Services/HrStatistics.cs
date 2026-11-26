using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Services;

public record HrStatistics(
    int TotalEmployees,
    int ActiveEmployees,
    int TotalDepartments,
    int TotalRequests,
    int SubmittedRequests,
    int ManagerApprovedRequests,
    int HrApprovedRequests,
    int RejectedRequests,
    int CancelledRequests);