using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authentication;

public sealed class AccountProfileUpdateServiceTests
{
    [Fact]
    public async Task
        UpdateAsync_WithValidValues_ForwardsNormalizedProfile()
    {
        var persistence =
            new TestPersistence();

        var service =
            new AccountProfileUpdateService(
                persistence);

        Guid accountId =
            Guid.NewGuid();

        Guid employeeId =
            Guid.NewGuid();

        UpdateAccountProfileResult result =
            await service.UpdateAsync(
                new UpdateAccountProfileRequest(
                    accountId,
                    "  Quản lý nhân sự  ",
                    employeeId));

        Assert.True(
            result.IsSuccessful);

        Assert.True(
            persistence.Called);

        Assert.Equal(
            accountId,
            persistence.AccountId);

        Assert.Equal(
            "Quản lý nhân sự",
            persistence.DisplayName);

        Assert.Equal(
            employeeId,
            persistence.EmployeeId);
    }

    [Fact]
    public async Task
        UpdateAsync_WithBlankDisplayName_DoesNotPersist()
    {
        var persistence =
            new TestPersistence();

        var service =
            new AccountProfileUpdateService(
                persistence);

        UpdateAccountProfileResult result =
            await service.UpdateAsync(
                new UpdateAccountProfileRequest(
                    Guid.NewGuid(),
                    "   "));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Theory]
    [InlineData(
        AccountProfileUpdatePersistenceResult.AccountNotFound)]
    [InlineData(
        AccountProfileUpdatePersistenceResult.EmployeeNotFound)]
    [InlineData(
        AccountProfileUpdatePersistenceResult.EmployeeAlreadyLinked)]
    public async Task
        UpdateAsync_WhenPersistenceRejects_ReturnsFailure(
            AccountProfileUpdatePersistenceResult persistenceResult)
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    persistenceResult
            };

        var service =
            new AccountProfileUpdateService(
                persistence);

        UpdateAccountProfileResult result =
            await service.UpdateAsync(
                new UpdateAccountProfileRequest(
                    Guid.NewGuid(),
                    "Quản lý"));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task
        AuthorizedService_RequiresAccountEdit()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestProfileUpdateService();

        var service =
            new AuthorizedAccountProfileUpdateService(
                inner,
                guard);

        await service.UpdateAsync(
            new UpdateAccountProfileRequest(
                Guid.NewGuid(),
                "Quản lý"));

        Assert.Equal(
            PermissionCodes.AccountEdit,
            guard.LastPermissionCode);

        Assert.True(
            inner.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestProfileUpdateService();

        var service =
            new AuthorizedAccountProfileUpdateService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.UpdateAsync(
                        new UpdateAccountProfileRequest(
                            Guid.NewGuid(),
                            "Quản lý")));

        Assert.False(
            inner.Called);
    }

    private sealed class TestPersistence
        : IAccountProfileUpdatePersistence
    {
        public AccountProfileUpdatePersistenceResult Result
        {
            get;
            set;
        } =
            AccountProfileUpdatePersistenceResult.Updated;

        public bool Called
        {
            get;
            private set;
        }

        public Guid AccountId
        {
            get;
            private set;
        }

        public string? DisplayName
        {
            get;
            private set;
        }

        public Guid? EmployeeId
        {
            get;
            private set;
        }

        public Task<AccountProfileUpdatePersistenceResult>
            TryUpdateAsync(
                Guid accountId,
                string displayName,
                Guid? employeeId,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            AccountId =
                accountId;

            DisplayName =
                displayName;

            EmployeeId =
                employeeId;

            return Task.FromResult(
                Result);
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

    private sealed class TestProfileUpdateService
        : IAccountProfileUpdateService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<UpdateAccountProfileResult> UpdateAsync(
            UpdateAccountProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                new UpdateAccountProfileResult(
                    true));
        }
    }
}
