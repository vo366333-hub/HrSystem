using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Interfaces;
using HrSystem.Core.Models;
using HrSystem.Storage;

namespace HrSystem.Tests;

// Модульні тести для JSON-репозиторію
public class JsonHrRepositoryTests
{
    // Перевіряє збереження й завантаження всіх сутностей, поліморфних типів заявок та їхньої історії
    [Fact]
    public void SaveAndLoad_ShouldPreserveAllEntitiesPolymorphicTypesAndHistory()
    {
        // Створення унікального шляху до тимчасового JSON-файлу
        var tempFile = Path.Combine(Path.GetTempPath(), $"hr_test_{Guid.NewGuid()}.json");

        try
        {
            // Підготовка тестових даних: відділ, графік роботи, працівник і три різні типи заявок
            var repo = new JsonHrRepository(tempFile);
            var dept = new Department("IT", "Савчук О.");
            var sched = new WorkSchedule("5/2", new TimeOnly(9, 0), new TimeOnly(18, 0), 40,
                new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday });
            var emp = new Employee("Петренко Олена", "Engineer", dept.Id, sched.Id, new DateOnly(2024, 5, 1), 24);

            var vac = new VacationRequest(emp.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 5),
                "Відпочинок", emp.FullName, VacationType.AnnualPaid, "Колега");
            vac.ChangeStatus(RequestStatus.ManagerApproved, dept.ManagerName, "Погоджено");

            var sick = new SickLeaveRequest(emp.Id, new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 12),
                "ГРВІ", emp.FullName, "EL-333", isHospitalized: true);

            var trip = new BusinessTripRequest(emp.Id, new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 22),
                "Конференція", emp.FullName, "Львів", "Доповідь", 850m);

            // Збереження агрегованих даних у файл та повторне завантаження
            repo.Save(new HrData(
                new List<Department> { dept },
                new List<WorkSchedule> { sched },
                new List<Employee> { emp },
                new List<HrRequest> { vac, sick, trip }));

            var loaded = repo.Load();

            // Перевірка кількості відновлених колекцій
            Assert.Single(loaded.Departments);
            Assert.Single(loaded.WorkSchedules);
            Assert.Single(loaded.Employees);
            Assert.Equal(3, loaded.Requests.Count);

            // Перевірка відновлення конкретних похідних типів заявок та їхніх полів
            var loadedVac = Assert.IsType<VacationRequest>(loaded.Requests.First(r => r.Id == vac.Id));
            Assert.Equal(RequestStatus.ManagerApproved, loadedVac.Status);
            Assert.Equal(2, loadedVac.History.Count);

            var loadedSick = Assert.IsType<SickLeaveRequest>(loaded.Requests.First(r => r.Id == sick.Id));
            Assert.True(loadedSick.IsHospitalized);
            Assert.Equal("EL-333", loadedSick.MedicalCertificateNumber);

            var loadedTrip = Assert.IsType<BusinessTripRequest>(loaded.Requests.First(r => r.Id == trip.Id));
            Assert.Equal("Львів", loadedTrip.DestinationCity);
            Assert.Equal(2550m, loadedTrip.CalculateTotalAllowance());
        }
        finally
        {
            // Гарантоване очищення тимчасового файлу після завершення тесту
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    // Перевіряє, що спроба завантажити пошкоджений JSON генерує доменний виняток
    [Fact]
    public void Load_CorruptedJson_ShouldThrowHrDomainException()
    {
        // Запис невалідного JSON у тимчасовий файл
        var tempFile = Path.Combine(Path.GetTempPath(), $"hr_broken_{Guid.NewGuid()}.json");
        File.WriteAllText(tempFile, "{ invalid json !!!");

        try
        {
            var repo = new JsonHrRepository(tempFile);
            Assert.Throws<HrDomainException>(() => repo.Load());
        }
        finally
        {
            // Видалення тимчасового файлу після перевірки
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}