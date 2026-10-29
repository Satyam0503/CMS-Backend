using System;
using System.Threading;
using System.Threading.Tasks;
using Codeji.CMS.Services.BackgroundTasks;
using Codeji.CMS.Services.LeaveManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Codeji.CMS.Services.Tests;

public class LeaveAccrualHostedServiceTests
{
    [Fact]
    public async Task ExecuteAsync_TriggersAccrualOnStartup()
    {
        // Arrange
        var mockLeaveService = new Mock<ILeaveManagementService>();
        var tcs = new TaskCompletionSource<bool>();
        mockLeaveService
            .Setup(s => s.EmployeeLeaveBalanceAccrual())
            .Returns(() =>
            {
                tcs.TrySetResult(true);
                return Task.CompletedTask;
            });

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        var scopedProviderMock = new Mock<IServiceProvider>();
        scopedProviderMock
            .Setup(x => x.GetService(typeof(ILeaveManagementService)))
            .Returns(mockLeaveService.Object);

        serviceScopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(scopedProviderMock.Object);

        serviceScopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(serviceScopeMock.Object);

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactoryMock.Object);

        var mockLogger = new Mock<ILogger<LeaveAccrualHostedService>>();
        var hostedService = new LeaveAccrualHostedService(serviceProviderMock.Object, mockLogger.Object);

        // Act
        await hostedService.StartAsync(CancellationToken.None);
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        await hostedService.StopAsync(CancellationToken.None);

        // Assert
        Assert.Same(tcs.Task, completed);
        mockLeaveService.Verify(s => s.EmployeeLeaveBalanceAccrual(), Times.AtLeastOnce);
    }
}
