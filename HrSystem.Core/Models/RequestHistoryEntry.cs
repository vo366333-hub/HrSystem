using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;

namespace HrSystem.Core.Models;

public class RequestHistoryEntry
{
    public DateTime Timestamp { get; }
    public RequestStatus? OldStatus { get; }
    public RequestStatus NewStatus { get; }
    public string ChangedBy { get; }
    public string Comment { get; }

    public RequestHistoryEntry(
        RequestStatus? oldStatus,
        RequestStatus newStatus,
        string changedBy,
        string comment,
        DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
            throw new ArgumentException("Потрібно вказати автора дії.", nameof(changedBy));

        Timestamp = timestamp ?? DateTime.Now;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedBy = changedBy.Trim();
        Comment = string.IsNullOrWhiteSpace(comment) ? "Без коментаря" : comment.Trim();
    }

    public override string ToString()
    {
        var transition = OldStatus.HasValue ? $"{OldStatus} → {NewStatus}" : $"Подано ({NewStatus})";
        return $"[{Timestamp:dd.MM.yyyy HH:mm}] {transition} | {ChangedBy}: {Comment}";
    }
}