using System.Net;
using System.Net.Http.Json;
using CurrencyConverter.Web;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Web.Tests.Base;

namespace Web.Tests;

public class ConvertActionTest() : TestBase(services =>
{
    services.RemoveAll<IFrankfurterClient>()
        .AddSingleton(Mock.Of<IFrankfurterClient>(c =>
            c.ConvertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()) ==
            Task.FromResult(HttpResult<FrankfurterResponse>.Success(new FrankfurterResponse(10, "USD", "2024-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m }, { "GBP", 0.78m } })))
            &&
            c.ConvertAsync(It.Is<string>(s => s == "AAA"), It.Is<string>(s => s == "AAA"), It.IsAny<decimal>(), It.IsAny<CancellationToken>()) ==
            Task.FromResult(HttpResult<FrankfurterResponse>.Failure(HttpStatusCode.NotFound))
        ));
})
{
    [Theory]
    [InlineData("USD", "USD", 10)]
    [InlineData("BBB", "BBB", 10)]
    [InlineData("EUR", "EUR", 10)]
    public async Task Get_returns_200(string baseCode, string targetCode, decimal amount)
    {
        // Arrange

        // Act
        var response = await Client.GetAsync($"/convert/{baseCode}/{targetCode}/{amount:F1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = response.Content.ReadFromJsonAsync<FrankfurterResponse>();
        dto.Should().NotBeNull();
    }

    [Theory]
    [InlineData("", "", 10)]
    [InlineData("AAA", "AAA", 10)]
    public async Task Get_returns_404(string baseCode, string targetCode, decimal amount)
    {
        // Arrange

        // Act
        var response = await Client.GetAsync($"/convert/{baseCode}/{targetCode}/{amount:F1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("XX", "XX", 10)]
    [InlineData("AB", "AB", 10)]
    [InlineData("123", "123", 10)]
    [InlineData("aa", "aa", 10)]
    [InlineData("USD", "USD", -10)]
    [InlineData("USD", "TRY", 10)]
    [InlineData("USD", "PLN", 10)]
    [InlineData("USD", "THB", 10)]
    [InlineData("USD", "MXN", 10)]
    public async Task Get_returns_400(string baseCode, string targetCode, decimal amount)
    {
        // Arrange

        // Act
        var response = await Client.GetAsync($"/convert/{baseCode}/{targetCode}/{amount:F1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}