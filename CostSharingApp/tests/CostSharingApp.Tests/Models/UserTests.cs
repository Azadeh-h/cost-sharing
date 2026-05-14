using CostSharing.Core.Models;

namespace CostSharingApp.Tests.Models;

public class UserTests
{
    [Fact]
    public void IsDeviceAccount_ReturnsTrue_WhenEmailEndsWithDeviceLocal()
    {
        var user = new User { Email = "auto@device.local" };

        Assert.True(user.IsDeviceAccount);
    }

    [Fact]
    public void IsDeviceAccount_ReturnsFalse_WhenEmailIsRegular()
    {
        var user = new User { Email = "alice@example.com" };

        Assert.False(user.IsDeviceAccount);
    }

    [Fact]
    public void IsDeviceAccount_ReturnsTrue_CaseInsensitive()
    {
        var user = new User { Email = "auto@Device.LOCAL" };

        Assert.True(user.IsDeviceAccount);
    }

    [Fact]
    public void IsDeviceAccount_ReturnsFalse_WhenEmailIsEmpty()
    {
        var user = new User { Email = string.Empty };

        Assert.False(user.IsDeviceAccount);
    }

    [Fact]
    public void IsDeviceAccount_ReturnsFalse_WhenEmailContainsDeviceLocalAsSubstring()
    {
        var user = new User { Email = "auto@device.local.evil.com" };

        Assert.False(user.IsDeviceAccount);
    }
}
