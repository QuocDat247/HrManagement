using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeProfileVocabularyTests
{
    [Fact]
    public void EmployeeAddressType_HasStableValues()
    {
        Assert.Equal(
            1,
            (int)EmployeeAddressType.Permanent);

        Assert.Equal(
            2,
            (int)EmployeeAddressType.Current);
    }

    [Fact]
    public void EmployeeIdentificationType_HasStableValues()
    {
        Assert.Equal(
            1,
            (int)EmployeeIdentificationType.NationalId);

        Assert.Equal(
            2,
            (int)EmployeeIdentificationType.Passport);

        Assert.Equal(
            99,
            (int)EmployeeIdentificationType.Other);
    }
}
