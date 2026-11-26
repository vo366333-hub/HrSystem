using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Workflow;

namespace HrSystem.Core.Models;

public abstract class HrRequest
{
    private readonly List<RequestHistoryEntry> _history = new();

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string Reason { get; private set; }
    public RequestStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyList<RequestHistoryEntry> History => _history.AsReadOnly();
    public int TotalCalendarDays => EndDate.DayNumber - StartDate.DayNumber + 1;

    public abstract string RequestTypeName { get; }

    protected HrRequest(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        string reason,
        string submittedBy)
    {
        if (endDate < startDate)
            throw new ArgumentException("Дата завершення не може бути раніше дати початку.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Потрібно вказати підставу або коментар до заявки.", nameof(reason));

        if (string.IsNullOrWhiteSpace(submittedBy))
            throw new ArgumentException("Потрібно вказати автора подання.", nameof(submittedBy));

        Id = Guid.NewGuid();
        EmployeeId = employeeId;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason.Trim();
        Status = RequestStatus.Submitted;
        CreatedAt = DateTime.Now;

        _history.Add(new RequestHistoryEntry(
            oldStatus: null,
            newStatus: RequestStatus.Submitted,
            changedBy: submittedBy,
            comment: $"Подано заявку типу «{RequestTypeName}» ({StartDate:dd.MM.yyyy}–{EndDate:dd.MM.yyyy})",
            timestamp: CreatedAt));
    }

    // Поліморфні методи бізнес-логіки
    public abstract void ValidateForEmployee(Employee employee, WorkSchedule schedule);
    public abstract void ApplyFinalApprovalEffects(Employee employee);
    public abstract string GetSpecificDetails(WorkSchedule? schedule = null);

    public bool OverlapsWith(DateOnly otherStart, DateOnly otherEnd) =>
        StartDate <= otherEnd && otherStart <= EndDate;

    public bool IsActiveForOverlapCheck() =>
        Status is RequestStatus.Submitted or RequestStatus.ManagerApproved or RequestStatus.HrApproved;

    public void ChangeStatus(RequestStatus newStatus, string changedBy, string comment = "")
    {
        ApprovalWorkflow.ValidateTransition(Status, newStatus);

        if (newStatus == RequestStatus.Rejected && string.IsNullOrWhiteSpace(comment))
        {
            throw new HrDomainException("При відхиленні заявки необхідно обов'язково вказати причину.");
        }

        var oldStatus = Status;
        Status = newStatus;

        _history.Add(new RequestHistoryEntry(
            oldStatus: oldStatus,
            newStatus: newStatus,
            changedBy: changedBy,
            comment: comment));
    }

    // Для відновлення стану із JSON-сховища
    public void RestoreFromStorage(
        Guid id,
        RequestStatus status,
        DateTime createdAt,
        IEnumerable<RequestHistoryEntry> history)
    {
        var snapshot = history.ToList();

        Id = id;
        Status = status;
        CreatedAt = createdAt;

        _history.Clear();
        _history.AddRange(snapshot);
    }
}