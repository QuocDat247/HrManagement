using HrManagement.Application.Auditing;
using HrManagement.Application.Authentication;
using HrManagement.Domain.Auditing;

namespace HrManagement.Tests.Auditing;

public sealed class AuditEntryFactoryTests
{
    [Fact]
    public void Create_WithAuthenticatedUser_UsesActorAndUtcTimestamp()
    {
        var user =
            new AuthenticatedUser(
                UserId: "demo-admin",
                Username: "admin",
                DisplayName: "Quản trị viên");

        var currentUserContext =
            new StubCurrentUserContext(
                user);

        DateTimeOffset utcNow =
            new DateTimeOffset(
                2026,
                8,
                19,
                6,
                15,
                0,
                TimeSpan.Zero);

        var timeProvider =
            new StubTimeProvider(
                utcNow);

        var factory =
            new AuditEntryFactory(
                currentUserContext,
                timeProvider);

        Guid employeeId =
            Guid.NewGuid();

        AuditEntry entry =
            factory.Create(
                AuditAction.Updated,
                AuditEntityTypes.EmployeePersonalProfile,
                employeeId,
                employeeId);

        Assert.NotEqual(
            Guid.Empty,
            entry.Id);

        Assert.Equal(
            utcNow.UtcDateTime,
            entry.OccurredAtUtc);

        Assert.Equal(
            DateTimeKind.Utc,
            entry.OccurredAtUtc.Kind);

        Assert.Equal(
            "demo-admin",
            entry.ActorUserId);

        Assert.Equal(
            "admin",
            entry.ActorUsername);

        Assert.Equal(
            AuditAction.Updated,
            entry.Action);

        Assert.Equal(
            AuditEntityTypes.EmployeePersonalProfile,
            entry.EntityType);

        Assert.Equal(
            employeeId,
            entry.EntityId);

        Assert.Equal(
            employeeId,
            entry.EmployeeId);
    }

    [Fact]
    public void Create_WhenUserIsNotAuthenticated_Throws()
    {
        var currentUserContext =
            new StubCurrentUserContext(
                null);

        var timeProvider =
            new StubTimeProvider(
                new DateTimeOffset(
                    2026,
                    8,
                    19,
                    6,
                    15,
                    0,
                    TimeSpan.Zero));

        var factory =
            new AuditEntryFactory(
                currentUserContext,
                timeProvider);

        Assert.Throws<InvalidOperationException>(
            () =>
                factory.Create(
                    AuditAction.Updated,
                    AuditEntityTypes.EmployeePersonalProfile,
                    Guid.NewGuid(),
                    Guid.NewGuid()));
    }

    private sealed class StubCurrentUserContext
        : ICurrentUserContext
    {
        public StubCurrentUserContext(
            AuthenticatedUser? currentUser)
        {
            CurrentUser =
                currentUser;
        }

        public AuthenticatedUser? CurrentUser
        {
            get;
        }

        public bool IsAuthenticated =>
            CurrentUser is not null;
    }

    private sealed class StubTimeProvider
        : TimeProvider
    {
        private readonly DateTimeOffset
            _utcNow;

        public StubTimeProvider(
            DateTimeOffset utcNow)
        {
            _utcNow =
                utcNow;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
