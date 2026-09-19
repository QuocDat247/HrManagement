using HrManagement.Application.Authentication.Recovery;

namespace HrManagement.Tests.Authentication;

public sealed class RecoveryCodeFormatTests
{
    [Theory]
    [InlineData(
        "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA")]
    [InlineData(
        "7f3a91c82d6eb447a1208f9c35d261ea")]
    [InlineData(
        "7f3a 91c8 2d6e b447 a120 8f9c 35d2 61ea")]
    public void
        Normalize_WithEquivalentRepresentations_ReturnsCanonicalValue(
            string value)
    {
        string? normalized =
            RecoveryCodeFormat.Normalize(
                value);

        Assert.Equal(
            "7F3A91C82D6EB447A1208F9C35D261EA",
            normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData(
        "ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ-ZZZZ")]
    public void
        Normalize_WithInvalidValue_ReturnsNull(
            string value)
    {
        Assert.Null(
            RecoveryCodeFormat.Normalize(
                value));
    }

    [Fact]
    public void
        Format_WithNormalizedCode_GroupsCodeForDisplay()
    {
        string result =
            RecoveryCodeFormat.Format(
                "7F3A91C82D6EB447A1208F9C35D261EA");

        Assert.Equal(
            "7F3A-91C8-2D6E-B447-A120-8F9C-35D2-61EA",
            result);
    }
}
