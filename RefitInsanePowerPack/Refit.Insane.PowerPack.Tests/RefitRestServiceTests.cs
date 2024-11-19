using Refit.Insane.PowerPack.Services;
using Refit.Insane.PowerPack.Tests.Interfaces;

namespace Refit.Insane.PowerPack.Tests;

[TestFixture]
public class RefitRestServiceTests
{
    [Test]
    public void GetApiImplementation_SynchronousCall_ShouldNotThrowException()
    {
        var restService = new RefitRestService();

        Assert.DoesNotThrow(() =>
        {
            var restApiImplementation = restService.GetRestApiImplementation<ITestApiWithApiDefinition>();
            var restApiImplementation2 = restService.GetRestApiImplementation<ITestApiWithApiDefinition>();
            var restApiImplementation3 = restService.GetRestApiImplementation<ITestApiWithApiDefinition>();
        });
    }

    [Test]
    public async Task GetApiImplementation_ConcurentCall_ShouldNotThrowException()
    {
        var restService = new RefitRestService();

        List<Task> tasks = new();
        for (var i = 0; i < 10; ++i)
        {
            tasks.Add(Task.Run(() => { restService.GetRestApiImplementation<ITestApiWithApiDefinition>(); }));
        }

        await Task.WhenAll(tasks);
    }

    [Test]
    public async Task GetDelegatingHandler_SynchronousCallForAppClientHandler_ShouldReturnAppClientHandler()
    {
        var restService = new RefitRestService();

        var delegatingHandler = restService.GetHandler(typeof(AppClientDelegatingHandler));
        var secondDelegatingHandler = restService.GetHandler(typeof(AppClientDelegatingHandler));

        Assert.That(delegatingHandler, Is.InstanceOf<AppClientDelegatingHandler>());
        Assert.That(secondDelegatingHandler, Is.InstanceOf<AppClientDelegatingHandler>());
    }

    [Test]
    public async Task GetDelegatingHandler_ConcurrentCallForAppClientHandler_ShouldReturnAppClientHandler()
    {
        var restService = new RefitRestService();

        List<Task> tasks = new();
        List<DelegatingHandler> delegatingHandlers = new();
        for (var i = 0; i < 10; ++i)
        {
            tasks.Add(new Task(() => { delegatingHandlers.Add(restService.GetHandler(typeof(AppClientDelegatingHandler))); }));
        }

        foreach (var task in tasks)
        {
            task.Start();
        }

        await Task.WhenAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(delegatingHandlers, Is.All.InstanceOf<AppClientDelegatingHandler>());
            Assert.That(10, Is.EqualTo(delegatingHandlers.Count));
        });
    }
}