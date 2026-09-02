using HrManagement.Application.Payroll.Calculations;

namespace HrManagement.Infrastructure.Payroll.Calculations;

public sealed class ConfiguredPayrollPolicyProfileSource
    : IPayrollPolicyProfileSource
{
    private readonly PayrollPolicyProfile
        _profile;

    public ConfiguredPayrollPolicyProfileSource(
        PayrollPolicyProfile profile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        _profile =
            profile;
    }

    public PayrollPolicyProfile GetCurrent()
    {
        return _profile;
    }
}
