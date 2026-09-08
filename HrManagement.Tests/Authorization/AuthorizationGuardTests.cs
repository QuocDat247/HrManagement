using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizationGuardTests
{
    [Fact]
    public async Task
        RequirePermissionAsync_WhenAuthorized_Completes()
    {
        var authorizationService =
            new TestAuthorizationService(
                isAuthorized:
                    true);

        var guard =
            new AuthorizationGuard(
                authorizationService);

        await guard.RequirePermissionAsync(
            PermissionCodes.EmployeeView);

        Assert.Equal(
            PermissionCodes.EmployeeView,
            authorizationService
                .LastPermissionCode);
    }

    [Fact]
    public async Task
        RequirePermissionAsync_WhenDenied_Throws()
    {
        var guard =
            new AuthorizationGuard(
                new TestAuthorizationService(
                    isAuthorized:
                        false));

        AuthorizationDeniedException exception =
            await Assert.ThrowsAsync<
                AuthorizationDeniedException>(
                    () =>
                        guard.RequirePermissionAsync(
                            PermissionCodes.PayrollClose));

        Assert.Equal(
            PermissionCodes.PayrollClose,
            exception.PermissionCode);

        Assert.Equal(
            "Bạn không có quyền thực hiện thao tác này.",
            exception.Message);
    }

    [Fact]
    public async Task
        RequirePermissionAsync_ForwardsCancellationToken()
    {
        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        var guard =
            new AuthorizationGuard(
                new TestAuthorizationService(
                    isAuthorized:
                        true));

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
                () =>
                    guard.RequirePermissionAsync(
                        PermissionCodes.EmployeeView,
                        cancellation.Token));
    }

    private sealed class TestAuthorizationService
        : IAuthorizationService
    {
        private readonly bool
            _isAuthorized;

        public TestAuthorizationService(
            bool isAuthorized)
        {
            _isAuthorized =
                isAuthorized;
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
            cancellationToken.ThrowIfCancellationRequested();

            LastPermissionCode =
                permissionCode;

            return Task.FromResult(
                _isAuthorized);
        }
    }
}
