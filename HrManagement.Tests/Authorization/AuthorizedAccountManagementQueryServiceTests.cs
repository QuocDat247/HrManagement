using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authorization;

public sealed class AuthorizedAccountManagementQueryServiceTests
{
    [Fact]
    public async Task
        GetAsync_RequiresAccountViewAndForwards()
    {
        var inner =
            new TestAccountManagementQueryService();

        var guard =
            new TestAuthorizationGuard();

        var service =
            new AuthorizedAccountManagementQueryService(
                inner,
                guard);

        await service.GetAsync();

        Assert.Equal(
            PermissionCodes.AccountView,
            guard.LastPermissionCode);

        Assert.True(
            inner.GetCalled);
    }

    [Fact]
    public async Task
        GetAsync_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestAccountManagementQueryService();

        var service =
            new AuthorizedAccountManagementQueryService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.GetAsync());

        Assert.False(
            inner.GetCalled);
    }

    [Fact]
    public void
        AccountReadModel_DoesNotExposeCredentialMaterial()
    {
        string[] forbiddenFragments =
        [
            "Password",
            "Hash",
            "Credential",
            "Secret"
        ];

        var properties =
            typeof(AccountManagementAccountItem)
                .GetProperties();

        foreach (string fragment in forbiddenFragments)
        {
            Assert.DoesNotContain(
                properties,
                property =>
                    property.Name.Contains(
                        fragment,
                        StringComparison.OrdinalIgnoreCase));
        }
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

    private sealed class TestAccountManagementQueryService
        : IAccountManagementQueryService
    {
        public bool GetCalled
        {
            get;
            private set;
        }

        public Task<AccountManagementSnapshot> GetAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            GetCalled =
                true;

            return Task.FromResult(
                new AccountManagementSnapshot(
                    [],
                    [],
                    []));
        }
    }
}
