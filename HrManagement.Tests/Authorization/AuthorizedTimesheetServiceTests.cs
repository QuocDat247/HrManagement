using HrManagement.Application.Attendance.Timesheets;
using HrManagement.Application.Authentication;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedTimesheetServiceTests
{
    [Fact]
    public async Task
        QueryGet_RequiresTimesheetViewPermission()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestMonthlyTimesheetQueryService();

        var service =
            new AuthorizedMonthlyTimesheetQueryService(
                inner,
                guard);

        await service.GetAsync(
            2026,
            9);

        Assert.Equal(
            PermissionCodes.TimesheetView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        QueryGet_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestMonthlyTimesheetQueryService();

        var service =
            new AuthorizedMonthlyTimesheetQueryService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetAsync(
                        2026,
                        9));

        Assert.False(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        ClosingPolicy_RequiresTimesheetClosePermission()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    true);

        var policy =
            new PermissionTimesheetPeriodClosingAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanCloseAsync(
                CreateClosingRequest());

        Assert.True(
            allowed);

        Assert.Equal(
            PermissionCodes.TimesheetClose,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        ClosingPolicy_WhenPermissionDenied_ReturnsFalse()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    false);

        var policy =
            new PermissionTimesheetPeriodClosingAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanCloseAsync(
                CreateClosingRequest());

        Assert.False(
            allowed);

        Assert.Equal(
            PermissionCodes.TimesheetClose,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        ClosingPolicy_ForwardsCancellation()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    true);

        var policy =
            new PermissionTimesheetPeriodClosingAuthorizationPolicy(
                authorizationService);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
                () =>
                    policy.CanCloseAsync(
                        CreateClosingRequest(),
                        cancellationTokenSource.Token));
    }

    private static TimesheetPeriodClosingAuthorizationRequest
        CreateClosingRequest()
    {
        return new TimesheetPeriodClosingAuthorizationRequest(
            new AuthenticatedUser(
                Guid.NewGuid()
                    .ToString("D"),
                "test-user",
                "Test User"),
            2026,
            9);
    }

    private sealed class TestAuthorizationGuard
        : IAuthorizationGuard
    {
        private readonly bool
            _deny;

        public TestAuthorizationGuard(
            bool deny = false)
        {
            _deny =
                deny;
        }

        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task RequirePermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LastPermissionCode =
                permissionCode;

            if (_deny)
            {
                throw new AuthorizationDeniedException(
                    permissionCode);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestAuthorizationService
        : IAuthorizationService
    {
        private readonly bool
            _allowed;

        public TestAuthorizationService(
            bool allowed)
        {
            _allowed =
                allowed;
        }

        public string? LastPermissionCode
        {
            get;
            private set;
        }

        public Task<bool> HasPermissionAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            LastPermissionCode =
                permissionCode;

            return Task.FromResult(
                _allowed);
        }
    }

    private sealed class TestMonthlyTimesheetQueryService
        : IMonthlyTimesheetQueryService
    {
        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<MonthlyTimesheetReadModel> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult(
                default(MonthlyTimesheetReadModel)!);
        }
    }
}
