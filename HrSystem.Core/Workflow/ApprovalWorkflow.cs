using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;

namespace HrSystem.Core.Workflow;

// Статичний клас правил переходів між статусами погодження заявок
public static class ApprovalWorkflow
{
    private static readonly Dictionary<RequestStatus, HashSet<RequestStatus>> AllowedTransitions = new()
    {
        [RequestStatus.Submitted] = new()
        {
            RequestStatus.ManagerApproved,
            RequestStatus.Rejected,
            RequestStatus.Cancelled
        },
        [RequestStatus.ManagerApproved] = new()
        {
            RequestStatus.HrApproved,
            RequestStatus.Rejected,
            RequestStatus.Cancelled
        },
        [RequestStatus.HrApproved] = new(),
        [RequestStatus.Rejected] = new(),
        [RequestStatus.Cancelled] = new()
    };

    // Перевіряє, чи дозволений перехід між двома статусами
    public static bool CanTransition(RequestStatus from, RequestStatus to) =>
        AllowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    // Повертає список доступних наступних статусів для поточного стану
    public static IReadOnlyCollection<RequestStatus> GetAllowedTransitions(RequestStatus current) =>
        AllowedTransitions.TryGetValue(current, out var allowed)
            ? allowed.ToList().AsReadOnly()
            : Array.Empty<RequestStatus>();

    // Перевіряє перехід і кидає виняток, якщо він порушує правила workflow
    public static void ValidateTransition(RequestStatus from, RequestStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidApprovalTransitionException(
                $"Перехід зі статусу «{from}» у статус «{to}» заборонено правилами погодження.");
        }
    }
}