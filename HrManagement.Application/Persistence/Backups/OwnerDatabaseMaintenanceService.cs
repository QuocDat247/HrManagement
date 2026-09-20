using HrManagement.Application.Authentication;
using HrManagement.Application.Authentication.Accounts;
using HrManagement.Domain.Authentication.Accounts;

namespace HrManagement.Application.Persistence.Backups;

public sealed class OwnerDatabaseMaintenanceService
    : IOwnerDatabaseMaintenanceService
{
    private readonly ICurrentUserContext
        _currentUserContext;

    private readonly IUserAccountRepository
        _userAccountRepository;

    private readonly IDatabaseBackupService
        _backupService;

    private readonly IDatabaseRestoreService
        _restoreService;

    public OwnerDatabaseMaintenanceService(
        ICurrentUserContext currentUserContext,
        IUserAccountRepository userAccountRepository,
        IDatabaseBackupService backupService,
        IDatabaseRestoreService restoreService)
    {
        _currentUserContext =
            currentUserContext;

        _userAccountRepository =
            userAccountRepository;

        _backupService =
            backupService;

        _restoreService =
            restoreService;
    }

    public async Task<bool> CanManageAsync(
        CancellationToken cancellationToken = default)
    {
        UserAccount? account =
            await GetCurrentAccountAsync(
                cancellationToken);

        return account is
        {
            IsActive: true,
            Kind: UserAccountKind.Owner
        };
    }

    public async Task<DatabaseBackupResult>
        CreateBackupAsync(
            string destinationFilePath,
            CancellationToken cancellationToken = default)
    {
        await RequireOwnerAsync(
            cancellationToken);

        return await _backupService
            .CreateAsync(
                destinationFilePath,
                cancellationToken);
    }

    public async Task<DatabaseRestoreResult>
        RestoreAsync(
            string backupFilePath,
            CancellationToken cancellationToken = default)
    {
        await RequireOwnerAsync(
            cancellationToken);

        return await _restoreService
            .RestoreAsync(
                backupFilePath,
                cancellationToken);
    }

    private async Task RequireOwnerAsync(
        CancellationToken cancellationToken)
    {
        if (!await CanManageAsync(
                cancellationToken))
        {
            throw new UnauthorizedAccessException(
                "Chỉ tài khoản Owner đang hoạt động "
                + "mới được phép sao lưu hoặc khôi phục dữ liệu.");
        }
    }

    private async Task<UserAccount?>
        GetCurrentAccountAsync(
            CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        if (!_currentUserContext.IsAuthenticated
            || _currentUserContext.CurrentUser is null)
        {
            return null;
        }

        if (!Guid.TryParse(
                _currentUserContext
                    .CurrentUser
                    .UserId,
                out Guid accountId))
        {
            return null;
        }

        return await _userAccountRepository
            .GetByIdAsync(
                accountId,
                cancellationToken);
    }
}
