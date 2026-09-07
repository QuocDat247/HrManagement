using System.Reflection.PortableExecutable;
using System.Text;
using HrManagement.Application.Authentication.Credentials;

namespace HrManagement.Infrastructure.Authentication.Credentials;

public sealed class BundledPasswordBlocklist
    : IPasswordBlocklist
{
    private const string ResourceName =
        "HrManagement.Infrastructure.Authentication.Credentials.CommonPasswordBlocklist.txt";

    private readonly HashSet<string>
        _blockedPasswords;

    public BundledPasswordBlocklist()
    {
        _blockedPasswords =
            LoadBlockedPasswords();
    }

    public bool IsBlocked(
        string normalizedPassword)
    {
        ArgumentNullException.ThrowIfNull(
            normalizedPassword);

        string normalizedValue =
            normalizedPassword.Normalize(
                NormalizationForm.FormC);

        return _blockedPasswords.Contains(
            normalizedValue);
    }

    private static HashSet<string>
        LoadBlockedPasswords()
    {
        Stream? stream =
            typeof(BundledPasswordBlocklist)
                .Assembly
                .GetManifestResourceStream(
                    ResourceName);

        if (stream is null)
        {
            throw new InvalidOperationException(
                "Không thể tải password blocklist được nhúng.");
        }

        using var reader =
            new StreamReader(
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true);

        var blockedPasswords =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        while (reader.ReadLine() is string line)
        {
            string value =
                line.Trim();

            if (value.Length == 0
                || value.StartsWith(
                    '#'))
            {
                continue;
            }

            blockedPasswords.Add(
                value.Normalize(
                    NormalizationForm.FormC));
        }

        if (blockedPasswords.Count == 0)
        {
            throw new InvalidOperationException(
                "Password blocklist không được để trống.");
        }

        return blockedPasswords;
    }
}
