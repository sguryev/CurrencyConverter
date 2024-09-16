using CurrencyConverter.Web;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;

namespace Web.Tests.Base;

public abstract class TestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected TestBase(Action<IServiceCollection>? configureServices)
    {
        Factory = new TestWebApplicationFactory<Program>(configureServices);
        Client = Factory.CreateClient();
    }

    protected TestWebApplicationFactory<Program> Factory { get; }
    protected HttpClient Client { get; }
}

public class TestWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    private readonly Action<IServiceCollection>? _configureServices;

    public TestWebApplicationFactory(Action<IServiceCollection>? configureServices)
    {
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .ConfigureAppConfiguration((_, _) =>
            {
                ConfigureEnvironmentVariables();
            })
            .ConfigureTestServices(services =>
            {
                _configureServices?.Invoke(services);

                using var scope = ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services)
                    .CreateScope();
            });
    }

    private void ConfigureEnvironmentVariables()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }
}