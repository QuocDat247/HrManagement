using HrManagement.Application.Authentication;
using HrManagement.Application.Authorization;
using HrManagement.Application.Payroll.Calculations;
using HrManagement.Application.Payroll.Compensation;
using HrManagement.Application.Payroll.Periods;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedPayrollServiceTests
{
    [Fact]
    public async Task
        Preview_RequiresPayrollCalculate()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestPayrollPreviewService();

        var service =
            new AuthorizedPayrollPreviewService(
                inner,
                guard);

        await service.GetAsync(
            2026,
            9);

        Assert.Equal(
            PermissionCodes.PayrollCalculate,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        Preview_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestPayrollPreviewService();

        var service =
            new AuthorizedPayrollPreviewService(
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
        ClosedQuery_RequiresPayrollView()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestClosedPayrollQueryService();

        var service =
            new AuthorizedClosedPayrollQueryService(
                inner,
                guard);

        await service.GetAsync(
            2026,
            9);

        Assert.Equal(
            PermissionCodes.PayrollView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        ClosedQuery_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestClosedPayrollQueryService();

        var service =
            new AuthorizedClosedPayrollQueryService(
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
        ClosingPolicy_RequiresPayrollClose()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    true);

        var policy =
            new PermissionPayrollPeriodClosingAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanCloseAsync(
                CreateClosingRequest());

        Assert.True(
            allowed);

        Assert.Equal(
            PermissionCodes.PayrollClose,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        ClosingPolicy_WhenDenied_ReturnsFalse()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    false);

        var policy =
            new PermissionPayrollPeriodClosingAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanCloseAsync(
                CreateClosingRequest());

        Assert.False(
            allowed);

        Assert.Equal(
            PermissionCodes.PayrollClose,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        CompensationPolicy_RequiresManageCompensation()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    true);

        var policy =
            new PermissionEmployeeCompensationAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanSetAsync(
                CreateCompensationRequest());

        Assert.True(
            allowed);

        Assert.Equal(
            PermissionCodes.PayrollManageCompensation,
            authorizationService.LastPermissionCode);
    }

    [Fact]
    public async Task
        CompensationPolicy_WhenDenied_ReturnsFalse()
    {
        var authorizationService =
            new TestAuthorizationService(
                allowed:
                    false);

        var policy =
            new PermissionEmployeeCompensationAuthorizationPolicy(
                authorizationService);

        bool allowed =
            await policy.CanSetAsync(
                CreateCompensationRequest());

        Assert.False(
            allowed);

        Assert.Equal(
            PermissionCodes.PayrollManageCompensation,
            authorizationService.LastPermissionCode);
    }

    private static EmployeeCompensationAuthorizationRequest
        CreateCompensationRequest()
    {
        return new EmployeeCompensationAuthorizationRequest(
            new AuthenticatedUser(
                Guid.NewGuid()
                    .ToString("D"),
                "test-user",
                "Test User"),
            Guid.NewGuid(),
            new DateOnly(
                2026,
                9,
                9));
    }

    private static PayrollPeriodClosingAuthorizationRequest
        CreateClosingRequest()
    {
        return new PayrollPeriodClosingAuthorizationRequest(
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

    private sealed class TestPayrollPreviewService
        : IPayrollPreviewService
    {
        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<PayrollPreview> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult(
                default(PayrollPreview)!);
        }
    }

    private sealed class TestClosedPayrollQueryService
        : IClosedPayrollQueryService
    {
        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<ClosedPayrollReadModel?> GetAsync(
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            GetCalled =
                true;

            return Task.FromResult<
                ClosedPayrollReadModel?>(
                    null);
        }
    }
}
