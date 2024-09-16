using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using CurrencyConverter.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Web.Tests.Base;

namespace Web.Tests;

public class LatestActionTest() : TestBase(services =>
{
    services.RemoveAll<IFrankfurterClient>()
        .AddSingleton(Mock.Of<IFrankfurterClient>(c =>
            c.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) ==
            Task.FromResult(HttpResult<FrankfurterResponse>.Success(new FrankfurterResponse(10, "USD", "2024-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.78m } })))
            &&
            c.GetLatestAsync(It.Is<string>(s => s == "AAA"), It.IsAny<CancellationToken>()) ==
            Task.FromResult(HttpResult<FrankfurterResponse>.Failure(HttpStatusCode.NotFound))
        ));
})
{
    [Theory]
    [InlineData("USD")]
    [InlineData("BBB")]
    [InlineData("TRY")]
    [InlineData("EUR")]
    public async Task Get_returns_200(string code)
    {
        // Arrange

        // Act
        var response = await Client.GetAsync($"/latest/{code}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = response.Content.ReadFromJsonAsync<FrankfurterResponse>();
        dto.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("XX")]
    [InlineData("AB")]
    [InlineData("123")]
    [InlineData("aa")]
    [InlineData("AAA")]
    public async Task Get_returns_404(string code)
    {
        // Arrange

        // Act
        var response = await Client.GetAsync($"/latest/{code}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}