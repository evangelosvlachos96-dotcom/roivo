using FluentAssertions;
using Roivo.Core.Domain.Businesses;
using Roivo.Core.Domain.Enums;

namespace Roivo.Core.Tests.Domain.Businesses;

public class BusinessPermissionsTests
{
    [Fact]
    public void CanCreate_true_for_Accountant_false_for_Business()
    {
        BusinessPermissions.CanCreate(TenantType.Accountant).Should().BeTrue();
        BusinessPermissions.CanCreate(TenantType.Business).Should().BeFalse();
    }

    [Fact]
    public void CanDeactivate_true_for_Accountant_false_for_Business()
    {
        BusinessPermissions.CanDeactivate(TenantType.Accountant).Should().BeTrue();
        BusinessPermissions.CanDeactivate(TenantType.Business).Should().BeFalse();
    }

    [Fact]
    public void CanReactivate_true_for_Accountant_false_for_Business()
    {
        BusinessPermissions.CanReactivate(TenantType.Accountant).Should().BeTrue();
        BusinessPermissions.CanReactivate(TenantType.Business).Should().BeFalse();
    }

    [Fact]
    public void CanEditAfm_true_for_Accountant_false_for_Business()
    {
        BusinessPermissions.CanEditAfm(TenantType.Accountant).Should().BeTrue();
        BusinessPermissions.CanEditAfm(TenantType.Business).Should().BeFalse();
    }
}
