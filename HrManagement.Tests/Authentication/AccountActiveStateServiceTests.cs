using HrManagement.Application.Authentication.Accounts;
using HrManagement.Application.Authorization;
using HrManagement.Domain.Authorization.Permissions;

namespace HrManagement.Tests.Authentication;

public sealed class AccountActiveStateServiceTests
{
    [Theory]
    [InlineData(
        AccountActiveStatePersistenceResult.Updated)]
    [InlineData(
        AccountActiveStatePersistenceResult.Unchanged)]
    public async Task
        SetAsync_WhenPersistenceAccepts_ReturnsSuccess(
            AccountActiveStatePersistenceResult persistenceResult)
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    persistenceResult
            };

        var service =
            new AccountActiveStateService(
                persistence);

        Guid accountId =
            Guid.NewGuid();

        SetAccountActiveStateResult result =
            await service.SetAsync(
                new SetAccountActiveStateRequest(
                    accountId,
                    false));

        Assert.True(
            result.IsSuccessful);

        Assert.Equal(
            accountId,
            persistence.AccountId);

        Assert.False(
            persistence.IsActive);
    }

    [Theory]
    [InlineData(
        AccountActiveStatePersistenceResult.AccountNotFound)]
    [InlineData(
        AccountActiveStatePersistenceResult.LastActiveOwner)]
    public async Task
        SetAsync_WhenPersistenceRejects_ReturnsFailure(
            AccountActiveStatePersistenceResult persistenceResult)
    {
        var persistence =
            new TestPersistence
            {
                Result =
                    persistenceResult
            };

        var service =
            new AccountActiveStateService(
                persistence);

        SetAccountActiveStateResult result =
            await service.SetAsync(
                new SetAccountActiveStateRequest(
                    Guid.NewGuid(),
                    false));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.ErrorMessage));
    }

    [Fact]
    public async Task
        SetAsync_WithEmptyAccountId_DoesNotPersist()
    {
        var persistence =
            new TestPersistence();

        var service =
            new AccountActiveStateService(
                persistence);

        SetAccountActiveStateResult result =
            await service.SetAsync(
                new SetAccountActiveStateRequest(
                    Guid.Empty,
                    false));

        Assert.False(
            result.IsSuccessful);

        Assert.False(
            persistence.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_RequiresAccountLock()
    {
        var guard =
            new TestAuthorizationGuard();

        var inner =
            new TestActiveStateService();

        var service =
            new AuthorizedAccountActiveStateService(
                inner,
                guard);

        await service.SetAsync(
            new SetAccountActiveStateRequest(
                Guid.NewGuid(),
                false));

        Assert.Equal(
            PermissionCodes.AccountLock,
            guard.LastPermissionCode);

        Assert.True(
            inner.Called);
    }

    [Fact]
    public async Task
        AuthorizedService_WhenDenied_DoesNotInvokeInner()
    {
        var inner =
            new TestActiveStateService();

        var service =
            new AuthorizedAccountActiveStateService(
                inner,
                new TestAuthorizationGuard(
                    deny:
                        true));

        await Assert.ThrowsAsync<
            AuthorizationDeniedException>(
                () =>
                    service.SetAsync(
                        new SetAccountActiveStateRequest(
                            Guid.NewGuid(),
                            false)));

        Assert.False(
            inner.Called);
    }

    private sealed class TestPersistence
        : IAccountActiveStatePersistence
    {
        public AccountActiveStatePersistenceResult Result
        {
            get;
            set;
        } =
            AccountActiveStatePersistenceResult.Updated;

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

        public bool IsActive
        {
            get;
            private set;
        }

        public Task<AccountActiveStatePersistenceResult>
            TrySetAsync(
                Guid accountId,
                bool isActive,
                CancellationToken cancellationToken = default)
        {
            Called =
                true;

            AccountId =
                accountId;

            IsActive =
                isActive;

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

    private sealed class TestActiveStateService
        : IAccountActiveStateService
    {
        public bool Called
        {
            get;
            private set;
        }

        public Task<SetAccountActiveStateResult> SetAsync(
            SetAccountActiveStateRequest request,
            CancellationToken cancellationToken = default)
        {
            Called =
                true;

            return Task.FromResult(
                new SetAccountActiveStateResult(
                    true));
        }
    }
}
