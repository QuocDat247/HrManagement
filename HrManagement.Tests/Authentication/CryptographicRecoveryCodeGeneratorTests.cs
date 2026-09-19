using HrManagement.Application.Authentication.Recovery;
using HrManagement.Infrastructure.Authentication.Recovery;

namespace HrManagement.Tests.Authentication;

public sealed class CryptographicRecoveryCodeGeneratorTests
{
    [Fact]
    public void
        Generate_ReturnsValidFormattedRecoveryCode()
    {
        var generator =
            new CryptographicRecoveryCodeGenerator();

        string recoveryCode =
            generator.Generate();

        string? normalized =
            RecoveryCodeFormat.Normalize(
                recoveryCode);

        Assert.NotNull(
            normalized);

        Assert.Equal(
            RecoveryCodeFormat.NormalizedLength,
            normalized!.Length);

        string[] groups =
            recoveryCode.Split(
                '-');

        Assert.Equal(
            8,
            groups.Length);

        Assert.All(
            groups,
            group =>
                Assert.Equal(
                    4,
                    group.Length));
    }
}
