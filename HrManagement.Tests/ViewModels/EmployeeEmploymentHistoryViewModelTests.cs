using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HrManagement.Application.Employees.EmploymentHistories;
using HrManagement.Desktop.ViewModels;
using HrManagement.Domain.Employees;

namespace HrManagement.Tests.ViewModels;
public sealed class EmployeeEmploymentHistoryViewModelTests
{
    [Fact]
    public async Task LoadAsync_LoadsEmployeeIdentity()
    {
        var historyService =
            new StubEmploymentHistoryService();

        var viewModel =
            new EmployeeEmploymentHistoryViewModel(
                historyService);

        Employee employee =
            CreateEmployee();

        await viewModel.LoadAsync(
            employee);

        Assert.Equal(
            employee.EmployeeCode,
            viewModel.EmployeeCode);

        Assert.Equal(
            employee.FullName,
            viewModel.FullName);
    }

    [Fact]
    public async Task LoadAsync_WithPeriods_MapsHistoryForDisplay()
    {
        Guid employeeId =
            Guid.NewGuid();

        var historyService =
            new StubEmploymentHistoryService
            {
                Details =
                    new EmployeeEmploymentHistoryDetails(
                        employeeId,
                        [
                            new EmploymentHistoryPeriodItem(
                            Guid.NewGuid(),
                            1,
                            new DateOnly(2022, 1, 1),
                            new DateOnly(2025, 6, 15),
                            false),

                        new EmploymentHistoryPeriodItem(
                            Guid.NewGuid(),
                            2,
                            new DateOnly(2026, 8, 1),
                            null,
                            true)
                        ])
            };

        var viewModel =
            new EmployeeEmploymentHistoryViewModel(
                historyService);

        Employee employee =
            CreateEmployee(employeeId);

        await viewModel.LoadAsync(
            employee);

        Assert.True(
            viewModel.HasPeriods);

        Assert.Equal(
            2,
            viewModel.Periods.Count);

        Assert.Equal(
            "Giai đoạn 1",
            viewModel.Periods[0].Title);

        Assert.Equal(
            "01/01/2022 → 15/06/2025",
            viewModel.Periods[0].DateRange);

        Assert.Equal(
            "Đã kết thúc",
            viewModel.Periods[0].StatusText);

        Assert.Equal(
            "01/08/2026 → Hiện tại",
            viewModel.Periods[1].DateRange);

        Assert.Equal(
            "Đang làm việc",
            viewModel.Periods[1].StatusText);
    }

    [Fact]
    public async Task LoadAsync_WhenHistoryIsEmpty_UsesEmptyState()
    {
        Guid employeeId =
            Guid.NewGuid();

        var historyService =
            new StubEmploymentHistoryService
            {
                Details =
                    new EmployeeEmploymentHistoryDetails(
                        employeeId,
                        Array.Empty<
                            EmploymentHistoryPeriodItem>())
            };

        var viewModel =
            new EmployeeEmploymentHistoryViewModel(
                historyService);

        await viewModel.LoadAsync(
            CreateEmployee(employeeId));

        Assert.False(
            viewModel.HasPeriods);

        Assert.False(
            viewModel.HasError);

        Assert.Empty(
            viewModel.Periods);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceFails_ShowsErrorState()
    {
        var historyService =
            new StubEmploymentHistoryService
            {
                ExceptionToThrow =
                    new InvalidOperationException(
                        "Test failure.")
            };

        var viewModel =
            new EmployeeEmploymentHistoryViewModel(
                historyService);

        await viewModel.LoadAsync(
            CreateEmployee());

        Assert.True(
            viewModel.HasError);

        Assert.False(
            viewModel.HasPeriods);

        Assert.Equal(
            "Không thể tải lịch sử làm việc.",
            viewModel.ErrorMessage);

        Assert.Empty(
            viewModel.Periods);
    }

    private static Employee CreateEmployee(
    Guid? id = null)
    {
        return new Employee(
            id ?? Guid.NewGuid(),
            "EMP-HISTORY-UI",
            "Nguyễn Văn An",
            null,
            null,
            null,
            new DateOnly(2022, 1, 1),
            "Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);
    }

    private sealed class StubEmploymentHistoryService
    : IEmploymentHistoryService
    {
        public EmployeeEmploymentHistoryDetails?
            Details
        {
            get;
            set;
        }

        public Exception?
            ExceptionToThrow
        {
            get;
            set;
        }

        public Task<EmployeeEmploymentHistoryDetails>
            GetHistoryAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            if (ExceptionToThrow is not null)
            {
                return Task.FromException<
                    EmployeeEmploymentHistoryDetails>(
                        ExceptionToThrow);
            }

            return Task.FromResult(
                Details
                ?? new EmployeeEmploymentHistoryDetails(
                    employeeId,
                    Array.Empty<
                        EmploymentHistoryPeriodItem>()));
        }
    }
}
