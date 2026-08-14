using HrManagement.Domain.Organization.Positions;

namespace HrManagement.Tests.Domain.Organization.Positions;

public sealed class PositionTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesPosition()
    {
        Guid id =
            Guid.NewGuid();

        var position =
            new Position(
                id,
                " dev ",
                " Lập trình viên ");

        Assert.Equal(
            id,
            position.Id);

        Assert.Equal(
            "DEV",
            position.Code);

        Assert.Equal(
            "Lập trình viên",
            position.Name);

        Assert.True(
            position.IsActive);
    }

    [Fact]
    public void Constructor_WithEmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Position(
                Guid.Empty,
                "DEV",
                "Lập trình viên"));
    }

    [Fact]
    public void Constructor_WithBlankCode_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Position(
                Guid.NewGuid(),
                "   ",
                "Lập trình viên"));
    }

    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(
            () => new Position(
                Guid.NewGuid(),
                "DEV",
                "   "));
    }
}
