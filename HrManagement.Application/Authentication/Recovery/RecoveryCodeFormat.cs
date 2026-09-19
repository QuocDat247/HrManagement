namespace HrManagement.Application.Authentication.Recovery;

public static class RecoveryCodeFormat
{
    public const int NormalizedLength =
        32;

    public static string? Normalize(
        string? recoveryCode)
    {
        if (string.IsNullOrWhiteSpace(
                recoveryCode))
        {
            return null;
        }

        string normalized =
            new string(
                recoveryCode
                    .Where(
                        character =>
                            character != '-'
                            && !char.IsWhiteSpace(
                                character))
                    .Select(
                        char.ToUpperInvariant)
                    .ToArray());

        if (normalized.Length !=
            NormalizedLength)
        {
            return null;
        }

        if (normalized.Any(
                character =>
                    !Uri.IsHexDigit(
                        character)))
        {
            return null;
        }

        return normalized;
    }

    public static string Format(
        string normalizedRecoveryCode)
    {
        ArgumentNullException.ThrowIfNull(
            normalizedRecoveryCode);

        string? normalized =
            Normalize(
                normalizedRecoveryCode);

        if (normalized is null)
        {
            throw new ArgumentException(
                "Recovery code không hợp lệ.",
                nameof(normalizedRecoveryCode));
        }

        return string.Join(
            "-",
            Enumerable
                .Range(
                    0,
                    8)
                .Select(
                    index =>
                        normalized.Substring(
                            index * 4,
                            4)));
    }
}
