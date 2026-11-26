using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Enums;

public enum RequestStatus
{
    Submitted,
    ManagerApproved,
    HrApproved,
    Rejected,
    Cancelled
}
