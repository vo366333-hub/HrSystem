using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HrSystem.Core.Enums;
using HrSystem.Core.Exceptions;
using HrSystem.Core.Models;
using HrSystem.Core.Services;

namespace HrSystem.Tests;

// Модульні тести доменної логіки та сервісного шару HR-системи
public class HrDomainAndServiceTests
{
    // Допоміжний метод для швидкого створення базового тестового середовища (сервіс, відділ, графік, працівник)
    private static (HrService service, Department dept, WorkSchedule schedule, Employee emp) CreateDefaultSetup(int vacationDays = 24)
    {
        var service = new HrService(new InMemoryHrRepository());
        var dept = service.AddDepartment("Відділ розробки ПЗ", "Савчук Олександр");
        var weekdays = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };
        var schedule = service.AddWorkSchedule("Стандартний 5/2", new TimeOnly(9, 0), new TimeOnly(18, 0), 40, weekdays);
        var emp = service.AddEmployee("Петренко Олена", "Developer", dept.Id, schedule.Id, new DateOnly(2024, 1, 10), vacationDays);
        return (service, dept, schedule, emp);
    }

    // Перевіряє підрахунок робочих днів для стандартного графіка 5/2 та змінного графіка 2/2
    [Fact]
    public void WorkSchedule_CountWorkingDays_ShouldHandleStandardAndShiftSchedules()
    {
        var weekdays = new[]
        {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday
        };
        var standard = new WorkSchedule("5/2", new TimeOnly(9, 0), new TimeOnly(18, 0), 40, weekdays);
        var shift2x2 = new WorkSchedule("2/2", new TimeOnly(8, 0), new TimeOnly(20, 0), 42, Array.Empty<DayOfWeek>(), isShiftTwoByTwo: true);

        var start = new DateOnly(2026, 12, 14);
        var end = new DateOnly(2026, 12, 20);

        Assert.Equal(5, standard.CountWorkingDays(start, end));
        Assert.Equal(4, shift2x2.CountWorkingDays(start, end));
    }

    // Перевіряє двоетапне погодження оплачуваної відпустки, списання днів і запис історії переходів
    [Fact]
    public void PaidVacation_FullTwoStageApproval_ShouldDeductBalanceAndWriteHistory()
    {
        var (service, dept, _, emp) = CreateDefaultSetup(vacationDays: 20);
        var req = new VacationRequest(
            emp.Id,
            new DateOnly(2026, 12, 14),
            new DateOnly(2026, 12, 18),
            "Щорічна відпустка",
            emp.FullName,
            VacationType.AnnualPaid);

        service.SubmitRequest(req);
        service.ChangeRequestStatus(req.Id, RequestStatus.ManagerApproved, dept.ManagerName, "Погоджено");
        // Після погодження керівником дні ще не списуються
        Assert.Equal(20, service.GetEmployeeById(emp.Id).AnnualVacationBalance);

        // Фінальне погодження HR має списати дні відпустки
        service.ChangeRequestStatus(req.Id, RequestStatus.HrApproved, "HR-менеджер", "Наказ оформлено");

        var updatedEmp = service.GetEmployeeById(emp.Id);
        var updatedReq = service.GetRequestById(req.Id);
        Assert.Equal(15, updatedEmp.AnnualVacationBalance);
        Assert.Equal(RequestStatus.HrApproved, updatedReq.Status);
        Assert.Equal(3, updatedReq.History.Count);
    }

    // Перевіряє, що лікарняний та відрядження не зменшують баланс відпустки, а добові рахуються коректно
    [Fact]
    public void SickLeaveAndBusinessTrip_OnFinalApproval_ShouldNotDeductVacationBalance()
    {
        var (service, dept, _, emp) = CreateDefaultSetup(vacationDays: 18);

        // Оформлення та повне погодження лікарняного
        var sick = service.SubmitRequest(new SickLeaveRequest(
            emp.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3),
            "ГРВІ", emp.FullName, "EL-100200"));
        service.ChangeRequestStatus(sick.Id, RequestStatus.ManagerApproved, dept.ManagerName, "ОК");
        service.ChangeRequestStatus(sick.Id, RequestStatus.HrApproved, "HR", "ОК");

        // Оформлення та повне погодження відрядження
        var trip = new BusinessTripRequest(
            emp.Id, new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 12),
            "Аудит", emp.FullName, "Київ", "Зустріч з клієнтом", 700m);
        service.SubmitRequest(trip);
        service.ChangeRequestStatus(trip.Id, RequestStatus.ManagerApproved, dept.ManagerName, "ОК");
        service.ChangeRequestStatus(trip.Id, RequestStatus.HrApproved, "HR", "ОК");

        Assert.Equal(18, service.GetEmployeeById(emp.Id).AnnualVacationBalance);
        Assert.Equal(2100m, trip.CalculateTotalAllowance());
    }

    // Перевіряє заборону подання заявки на відпустку, якщо запитано більше днів, ніж є на балансі
    [Fact]
    public void SubmitVacation_ExceedingBalance_ShouldThrowDomainException()
    {
        var (service, _, _, emp) = CreateDefaultSetup(vacationDays: 5);
        var tooLong = new VacationRequest(
            emp.Id,
            new DateOnly(2026, 12, 1),
            new DateOnly(2026, 12, 10),
            "Довга відпустка",
            emp.FullName,
            VacationType.AnnualPaid);

        Assert.Throws<HrDomainException>(() => service.SubmitRequest(tooLong));
    }

    // Перевіряє заборону створення заявок із датами, що перетинаються, для одного працівника
    [Fact]
    public void SubmitRequest_OverlappingDatesForSameEmployee_ShouldThrowDomainException()
    {
        var (service, _, _, emp) = CreateDefaultSetup();
        service.SubmitRequest(new VacationRequest(
            emp.Id, new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 15),
            "Відпустка", emp.FullName, VacationType.AnnualPaid));

        var overlappingTrip = new BusinessTripRequest(
            emp.Id, new DateOnly(2026, 12, 14), new DateOnly(2026, 12, 16),
            "Відрядження", emp.FullName, "Львів", "Конференція");

        Assert.Throws<HrDomainException>(() => service.SubmitRequest(overlappingTrip));
    }

    // Перевіряє дотримання маршруту погодження: HR не може погодити заявку без попереднього рішення керівника
    [Theory]
    [InlineData(RequestStatus.Submitted, RequestStatus.HrApproved)] // Не можна оминути керівника
    public void ChangeStatus_SkippingManagerApproval_ShouldThrowInvalidTransitionException(
        RequestStatus initial,
        RequestStatus forbiddenTarget)
    {
        var (service, _, _, emp) = CreateDefaultSetup();
        var req = service.SubmitRequest(new SickLeaveRequest(
            emp.Id, new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 6),
            "Застуда", emp.FullName, "EL-555"));

        Assert.Equal(initial, req.Status);
        Assert.Throws<InvalidApprovalTransitionException>(() =>
            service.ChangeRequestStatus(req.Id, forbiddenTarget, "HR", "Спроба обійти керівника"));
    }

    // Перевіряє обов'язковість коментаря при відхиленні заявки
    [Fact]
    public void ChangeStatus_ToRejectedWithoutComment_ShouldThrowDomainException()
    {
        var (service, dept, _, emp) = CreateDefaultSetup();
        var req = service.SubmitRequest(new VacationRequest(
            emp.Id, new DateOnly(2026, 12, 20), new DateOnly(2026, 12, 22),
            "Відпочинок", emp.FullName, VacationType.Unpaid));

        Assert.Throws<HrDomainException>(() =>
            service.ChangeRequestStatus(req.Id, RequestStatus.Rejected, dept.ManagerName, "   "));
    }

    // Перевіряє заборону подання заявок від імені неактивного працівника
    [Fact]
    public void SubmitRequest_ForInactiveEmployee_ShouldThrowDomainException()
    {
        var (service, _, _, emp) = CreateDefaultSetup();
        service.SetEmployeeActiveStatus(emp.Id, false);

        var req = new SickLeaveRequest(
            emp.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2),
            "Хвороба", emp.FullName, "EL-999");

        Assert.Throws<HrDomainException>(() => service.SubmitRequest(req));
    }

    // Перевіряє, що видалити можна лише щойно подану заявку, але не вже погоджену
    [Fact]
    public void DeleteRequest_ShouldAllowForSubmitted_AndForbidForApproved()
    {
        var (service, dept, _, emp) = CreateDefaultSetup();
        var submitted = service.SubmitRequest(new VacationRequest(
            emp.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2),
            "Тест 1", emp.FullName, VacationType.Unpaid));

        var approved = service.SubmitRequest(new BusinessTripRequest(
            emp.Id, new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 6),
            "Тест 2", emp.FullName, "Одеса", "Переговори"));
        service.ChangeRequestStatus(approved.Id, RequestStatus.ManagerApproved, dept.ManagerName, "Погоджено");

        service.DeleteRequest(submitted.Id);
        Assert.Single(service.GetAllRequests());

        Assert.Throws<HrDomainException>(() => service.DeleteRequest(approved.Id));
    }

    // Перевіряє коректність фільтрації заявок та підрахунку зведеної статистики
    [Fact]
    public void FilterRequests_AndStatistics_ShouldReturnAccurateResults()
    {
        var (service, dept, _, emp) = CreateDefaultSetup();
        var r1 = service.SubmitRequest(new BusinessTripRequest(
            emp.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2),
            "Зустріч", emp.FullName, "Харків", "Аудит"));
        service.ChangeRequestStatus(r1.Id, RequestStatus.ManagerApproved, dept.ManagerName, "ОК");

        service.SubmitRequest(new SickLeaveRequest(
            emp.Id, new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 12),
            "Лікування", emp.FullName, "EL-777"));

        var trips = service.FilterRequests(new RequestFilter { RequestTypeName = "Відрядження" });
        var searchEl = service.FilterRequests(new RequestFilter { SearchText = "EL-777" });
        var stats = service.GetStatistics();

        Assert.Single(trips);
        Assert.Single(searchEl);
        Assert.Equal(2, stats.TotalRequests);
        Assert.Equal(1, stats.ManagerApprovedRequests);
        Assert.Equal(1, stats.SubmittedRequests);
    }

    // Перевіряє оновлення даних працівника та заборону їх редагування після звільнення
    [Fact]
    public void UpdateEmployeeDetails_ShouldChangePosition_AndForbidForDismissed()
    {
        var (service, dept, schedule, emp) = CreateDefaultSetup();

        service.UpdateEmployeeDetails(emp.Id, "Tech Lead .NET", dept.Id, schedule.Id);
        Assert.Equal("Tech Lead .NET", service.GetEmployeeById(emp.Id).Position);

        service.DismissEmployee(emp.Id, new DateOnly(2026, 11, 25), "За власним бажанням");

        Assert.Throws<HrDomainException>(() =>
            service.UpdateEmployeeDetails(emp.Id, "Architect", dept.Id, schedule.Id));
    }

    // Перевіряє сценарій звільнення: зміну статусу працівника та автоматичне скасування незавершених заявок
    [Fact]
    public void DismissEmployee_ShouldSetDismissedState_AndAutoCancelPendingRequests()
    {
        var (service, dept, _, emp) = CreateDefaultSetup();

        // Незавершена заявка — має бути скасована при звільненні
        var pending = service.SubmitRequest(new VacationRequest(
            emp.Id, new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 14),
            "Планова відпустка", emp.FullName, VacationType.AnnualPaid));

        // Вже погоджена заявка — має зберегти свій фінальний статус
        var approved = service.SubmitRequest(new SickLeaveRequest(
            emp.Id, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 3),
            "ГРВІ", emp.FullName, "EL-111"));
        service.ChangeRequestStatus(approved.Id, RequestStatus.ManagerApproved, dept.ManagerName, "ОК");
        service.ChangeRequestStatus(approved.Id, RequestStatus.HrApproved, "HR", "ОК");

        int cancelledCount = service.DismissEmployee(emp.Id, new DateOnly(2026, 11, 25), "За угодою сторін");

        var dismissedEmp = service.GetEmployeeById(emp.Id);
        Assert.True(dismissedEmp.IsDismissed);
        Assert.False(dismissedEmp.IsActive);
        Assert.Equal("За угодою сторін", dismissedEmp.DismissalReason);
        Assert.Equal(1, cancelledCount);
        Assert.Equal(RequestStatus.Cancelled, service.GetRequestById(pending.Id).Status);
        Assert.Equal(RequestStatus.HrApproved, service.GetRequestById(approved.Id).Status);
        Assert.Throws<HrDomainException>(() => service.SetEmployeeActiveStatus(emp.Id, true));
    }
}