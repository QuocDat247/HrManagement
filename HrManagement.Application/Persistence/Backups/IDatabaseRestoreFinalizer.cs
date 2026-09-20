namespace HrManagement.Application.Persistence.Backups;

public interface IDatabaseRestoreFinalizer
{
    Task FinalizeAsync(
        CancellationToken cancellationToken = default);
}
