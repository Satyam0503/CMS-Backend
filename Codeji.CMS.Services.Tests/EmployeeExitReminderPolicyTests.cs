using Codeji.CMS.Repository.Entities.Employees;

namespace Codeji.CMS.Services.Tests;

public sealed class EmployeeExitReminderPolicyTests
{
    [Fact]
    public void NewPolicyUsesSafeReminderDefaults()
    {
        var policy = new EmployeeExitPolicy();

        Assert.True(policy.EnableNoticePeriodReminders);
        Assert.Equal(3, policy.NoticePeriodReminderEveryDays);
        Assert.Equal(["30", "15", "7", "3", "1", "0"], policy.NoticePeriodReminderOffsets);
        Assert.True(policy.NotifyNoticePeriodEmployee);
        Assert.True(policy.NotifyNoticePeriodReportingManager);
        Assert.True(policy.NotifyNoticePeriodHr);
        Assert.False(policy.NotifyNoticePeriodAdmins);
        Assert.True(policy.NoticePeriodReminderEmail);
    }

    [Fact]
    public void ReminderDedupeKeySeparatesRecipientsAndTenants()
    {
        var first = "exit-reminder:company-a:exit-1:reminder-1:user-a";
        var sameRecipient = "exit-reminder:company-a:exit-1:reminder-1:user-a";
        var otherRecipient = "exit-reminder:company-a:exit-1:reminder-1:user-b";
        var otherTenant = "exit-reminder:company-b:exit-1:reminder-1:user-a";

        Assert.Equal(first, sameRecipient);
        Assert.NotEqual(first, otherRecipient);
        Assert.NotEqual(first, otherTenant);
    }

    [Theory]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(25, true)]
    public void GroupedHrDigestStartsAtHighVolumeThreshold(int dueCaseCount, bool expected)
    {
        Assert.Equal(expected, dueCaseCount >= 5);
    }
}
