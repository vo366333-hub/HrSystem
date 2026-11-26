using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HrSystem.Core.Models;

public class WorkSchedule
{
    private readonly HashSet<DayOfWeek> _workingDays;

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }
    public int WeeklyHours { get; private set; }
    public bool IsShiftTwoByTwo { get; private set; } // Для змінного графіка 2/2

    public IReadOnlyCollection<DayOfWeek> WorkingDays => _workingDays;

    public WorkSchedule(
        string name,
        TimeOnly startTime,
        TimeOnly endTime,
        int weeklyHours,
        IEnumerable<DayOfWeek> workingDays,
        bool isShiftTwoByTwo = false,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Назва графіка не може бути порожньою.", nameof(name));

        if (endTime <= startTime)
            throw new ArgumentException("Час завершення зміни має бути пізнішим за час початку.");

        if (weeklyHours is < 8 or > 60)
            throw new ArgumentException("Тижневе навантаження має бути в межах 8–60 годин.", nameof(weeklyHours));

        var daysList = workingDays?.ToHashSet() ?? new HashSet<DayOfWeek>();
        if (!isShiftTwoByTwo && daysList.Count == 0)
            throw new ArgumentException("Потрібно вказати хоча б один робочий день тижня.", nameof(workingDays));

        Id = id ?? Guid.NewGuid();
        Name = name.Trim();
        StartTime = startTime;
        EndTime = endTime;
        WeeklyHours = weeklyHours;
        IsShiftTwoByTwo = isShiftTwoByTwo;
        _workingDays = daysList;
    }

    // Розраховує кількість робочих днів/змін у межах періоду заявки
    public int CountWorkingDays(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            return 0;

        var totalCalendarDays = endDate.DayNumber - startDate.DayNumber + 1;

        if (IsShiftTwoByTwo)
            return (int)Math.Ceiling(totalCalendarDays / 2.0);

        int count = 0;
        for (var d = startDate; d <= endDate; d = d.AddDays(1))
        {
            if (_workingDays.Contains(d.DayOfWeek))
                count++;
        }

        return count;
    }

    public string GetTimeRangeText() => $"{StartTime:HH:mm}–{EndTime:HH:mm} ({WeeklyHours} год/тиж)";

    public override string ToString() => $"{Name} [{StartTime:HH:mm}–{EndTime:HH:mm}]";
}