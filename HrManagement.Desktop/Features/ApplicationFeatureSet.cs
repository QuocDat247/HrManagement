namespace HrManagement.Desktop.Features;

public sealed record ApplicationFeatureSet(
    bool Employees,
    bool Organization,
    bool TimeManagement,
    bool Payroll)
{
    public static ApplicationFeatureSet AllEnabled
    {
        get;
    } =
        new(
            Employees: true,
            Organization: true,
            TimeManagement: true,
            Payroll: true);
}
