using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Exceptions;

public class HrDomainException : Exception
{
    public HrDomainException(string message) : base(message) { }
}

public class InvalidApprovalTransitionException : HrDomainException
{
    public InvalidApprovalTransitionException(string message) : base(message) { }
}