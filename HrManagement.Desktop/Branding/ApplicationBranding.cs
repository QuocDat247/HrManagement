namespace HrManagement.Desktop.Branding;

public sealed record ApplicationBranding(
    string ProductDisplayName,
    string CustomerDisplayName,
    string SupportLabel,
    string SupportContact)
{
    public static ApplicationBranding Default
    {
        get;
    } =
        new(
            ProductDisplayName:
                "HR Management",
            CustomerDisplayName:
                "Bản tiêu chuẩn",
            SupportLabel:
                "Hỗ trợ kỹ thuật",
            SupportContact:
                "Liên hệ nhà cung cấp triển khai");
}
