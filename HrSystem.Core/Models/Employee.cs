using HrSystem.Core.Exceptions;

namespace HrSystem.Core.Models;

// Доменна сутність працівника з інкапсуляцією стану та бізнес-правил
public class Employee
{
    // Ідентифікатор та основні кадрові атрибути
    public Guid Id { get; private set; }
    public string FullName { get; private set; }
    public string Position { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid WorkScheduleId { get; private set; }
    public DateOnly HireDate { get; private set; }

    // Відпустки та поточний статус активності
    public int AnnualVacationBalance { get; private set; }
    public bool IsActive { get; private set; }

    // Дані про звільнення
    public DateOnly? DismissalDate { get; private set; }
    public string? DismissalReason { get; private set; }

    // Обчислювана ознака того, чи працівник уже звільнений
    public bool IsDismissed => DismissalDate.HasValue;

    // Створює екземпляр працівника та перевіряє базові інваріанти домену
    public Employee(
        string fullName,
        string position,
        Guid departmentId,
        Guid workScheduleId,
        DateOnly hireDate,
        int annualVacationBalance = 24,
        bool isActive = true,
        Guid? id = null,
        DateOnly? dismissalDate = null,
        string? dismissalReason = null)
    {
        // Валідація вхідних параметрів
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("ПІБ працівника не може бути порожнім.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(position))
            throw new ArgumentException("Посада не може бути порожньою.", nameof(position));

        if (annualVacationBalance < 0)
            throw new ArgumentException("Залишок днів відпустки не може бути від'ємним.", nameof(annualVacationBalance));

        if (dismissalDate.HasValue && dismissalDate.Value < hireDate)
            throw new ArgumentException("Дата звільнення не може бути раніше дати прийняття на роботу.");

        // Ініціалізація полів і нормалізація рядків
        Id = id ?? Guid.NewGuid();
        FullName = fullName.Trim();
        Position = position.Trim();
        DepartmentId = departmentId;
        WorkScheduleId = workScheduleId;
        HireDate = hireDate;
        AnnualVacationBalance = annualVacationBalance;
        DismissalDate = dismissalDate;
        DismissalReason = string.IsNullOrWhiteSpace(dismissalReason) ? null : dismissalReason.Trim();

        // Звільнений працівник не може залишатися активним
        IsActive = dismissalDate.HasValue ? false : isActive;
    }

    // Змінює посаду активного (не звільненого) працівника
    public void ChangePosition(string newPosition)
    {
        EnsureNotDismissed();

        if (string.IsNullOrWhiteSpace(newPosition))
            throw new ArgumentException("Нова посада не може бути порожньою.", nameof(newPosition));

        Position = newPosition.Trim();
    }

    // Переводить працівника в інший підрозділ
    public void ChangeDepartment(Guid newDepartmentId)
    {
        EnsureNotDismissed();
        DepartmentId = newDepartmentId;
    }

    // Змінює робочий графік працівника
    public void ChangeWorkSchedule(Guid newScheduleId)
    {
        EnsureNotDismissed();
        WorkScheduleId = newScheduleId;
    }

    // Встановлює прапор активності працівника
    public void SetActiveStatus(bool isActive)
    {
        EnsureNotDismissed();
        IsActive = isActive;
    }

    // Оформлює звільнення працівника та деактивує його
    public void Dismiss(DateOnly dismissalDate, string reason)
    {
        EnsureNotDismissed();

        if (dismissalDate < HireDate)
            throw new HrDomainException("Дата звільнення не може бути раніше дати прийняття на роботу.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Потрібно вказати причину звільнення.", nameof(reason));

        DismissalDate = dismissalDate;
        DismissalReason = reason.Trim();
        IsActive = false;
    }

    // Списує використані дні щорічної оплачуваної відпустки
    public void DeductVacationDays(int days)
    {
        EnsureNotDismissed();

        if (days <= 0)
            throw new ArgumentException("Кількість днів для списання має бути більшою за нуль.", nameof(days));

        if (days > AnnualVacationBalance)
        {
            throw new HrDomainException(
                $"Недостатньо днів оплачуваної відпустки у працівника {FullName}: доступно {AnnualVacationBalance}, запитано {days}.");
        }

        AnnualVacationBalance -= days;
    }

    // Забороняє будь-які модифікації, якщо працівник уже звільнений
    private void EnsureNotDismissed()
    {
        if (IsDismissed)
        {
            throw new HrDomainException(
                $"Працівник {FullName} вже звільнений ({DismissalDate:dd.MM.yyyy}). Зміни заборонено.");
        }
    }

    public override string ToString() => $"{FullName} — {Position}";
}