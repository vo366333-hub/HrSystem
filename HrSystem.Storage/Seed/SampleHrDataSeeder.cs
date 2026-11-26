using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrSystem.Core.Enums;
using HrSystem.Core.Interfaces;
using HrSystem.Core.Models;

namespace HrSystem.Storage.Seed;

public static class SampleHrDataSeeder
{
    public static void SeedIfEmpty(IHrRepository repository)
    {
        var existing = repository.Load();
        if (existing.Departments.Count > 0 || existing.Employees.Count > 0 || existing.Requests.Count > 0)
            return;

        // 1. Підрозділи
        var deptDev = new Department("Відділ розробки ПЗ", "Савчук Олександр", "Проєктування та супровід корпоративних систем");
        var deptSupport = new Department("Відділ технічної підтримки", "Коваленко Марія", "Обслуговування робочих місць та інфраструктури");
        var deptFinance = new Department("Бухгалтерія та фінанси", "Мельник Ігор", "Фінансовий облік, нарахування та звітність");

        var departments = new List<Department> { deptDev, deptSupport, deptFinance };

        // 2. Робочі графіки
        var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

        var schedStandard = new WorkSchedule("Стандартний 5/2", new TimeOnly(9, 0), new TimeOnly(18, 0), 40, weekdays);
        var schedFlexible = new WorkSchedule("Гнучкий 5/2 (скорочений)", new TimeOnly(10, 0), new TimeOnly(17, 0), 35, weekdays);
        var schedShift = new WorkSchedule("Змінний 2/2", new TimeOnly(8, 0), new TimeOnly(20, 0), 42, Array.Empty<DayOfWeek>(), isShiftTwoByTwo: true);

        var schedules = new List<WorkSchedule> { schedStandard, schedFlexible, schedShift };

        // 3. Працівники
        var emp1 = new Employee("Петренко Олена", "Senior .NET Developer", deptDev.Id, schedStandard.Id, new DateOnly(2023, 3, 15), 24);
        var emp2 = new Employee("Ткачук Віктор", "QA Engineer", deptDev.Id, schedFlexible.Id, new DateOnly(2024, 6, 1), 14);
        var emp3 = new Employee("Романюк Наталія", "Інженер підтримки", deptSupport.Id, schedShift.Id, new DateOnly(2022, 9, 10), 18);
        var emp4 = new Employee("Гнатюк Дмитро", "Провідний бухгалтер", deptFinance.Id, schedStandard.Id, new DateOnly(2021, 2, 20), 20);
        var emp5 = new Employee("Бондар Андрій", "Системний адміністратор", deptSupport.Id, schedShift.Id, new DateOnly(2023, 11, 5), 6, isActive: false);

        var employees = new List<Employee> { emp1, emp2, emp3, emp4, emp5 };

        // 4. Кадрові заявки (всі 3 типи та всі 5 статусів)
        // Заявка 1: Подана відпустка (очікує керівника)
        var r1 = new VacationRequest(
            emp1.Id,
            new DateOnly(2026, 12, 14),
            new DateOnly(2026, 12, 20),
            "Планова зимова відпустка за графіком",
            emp1.FullName,
            VacationType.AnnualPaid,
            "Ткачук Віктор");

        // Заявка 2: Відрядження, погоджене керівником (очікує фінального затвердження HR)
        var r2 = new BusinessTripRequest(
            emp2.Id,
            new DateOnly(2026, 12, 2),
            new DateOnly(2026, 12, 4),
            "Участь у технічному аудиті клієнтського середовища",
            emp2.FullName,
            "Львів",
            "Проведення інтеграційного тестування",
            750m);
        r2.ChangeStatus(RequestStatus.ManagerApproved, deptDev.ManagerName, "Погоджено, завдання на спринт розподілено.");

        // Заявка 3: Остаточно затверджена відпустка (дні вже списано з балансу emp3: 18 -> 13)
        var r3 = new VacationRequest(
            emp3.Id,
            new DateOnly(2026, 12, 7),
            new DateOnly(2026, 12, 11),
            "Частина щорічної відпустки",
            emp3.FullName,
            VacationType.AnnualPaid,
            "Коваленко Марія");
        r3.ChangeStatus(RequestStatus.ManagerApproved, deptSupport.ManagerName, "Зміни на тиждень перекрито.");
        r3.ChangeStatus(RequestStatus.HrApproved, "HR-менеджер Іванчук О.", "Наказ № 142-к оформлено.");
        r3.ApplyFinalApprovalEffects(emp3);

        // Заявка 4: Лікарняний, погоджений керівником (очікує HR)
        var r4 = new SickLeaveRequest(
            emp4.Id,
            new DateOnly(2026, 11, 26),
            new DateOnly(2026, 11, 30),
            "Тимчасова непрацездатність (ГРВІ)",
            emp4.FullName,
            "EL-845120-2026",
            isHospitalized: false);
        r4.ChangeStatus(RequestStatus.ManagerApproved, deptFinance.ManagerName, "Листок непрацездатності підтверджено.");

        // Заявка 5: Відхилена відпустка з поясненням причини
        var r5 = new VacationRequest(
            emp1.Id,
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 12, 3),
            "Додаткові вихідні за сімейними обставинами",
            emp1.FullName,
            VacationType.Unpaid,
            "Не вказано");
        r5.ChangeStatus(RequestStatus.Rejected, deptDev.ManagerName, "На ці дати заплановано критичний реліз системи.");

        // Заявка 6: Скасоване відрядження
        var r6 = new BusinessTripRequest(
            emp3.Id,
            new DateOnly(2026, 12, 21),
            new DateOnly(2026, 12, 22),
            "Налаштування серверного обладнання у філії",
            emp3.FullName,
            "Київ",
            "Заміна маршрутизатора",
            800m);
        r6.ChangeStatus(RequestStatus.Cancelled, emp3.FullName, "Роботи вдалося виконати віддалено.");

        var requests = new List<HrRequest> { r1, r2, r3, r4, r5, r6 };
        repository.Save(new HrData(departments, schedules, employees, requests));
    }
}
