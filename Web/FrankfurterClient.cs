using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CurrencyConverter.Web;

public sealed class FrankfurterClientOptions
{
    public string BaseUrl { get; init; }
}

public interface IFrankfurterClient
{
    Task<HttpResult<FrankfurterResponse>> GetLatestAsync(string currencyCode, CancellationToken cToken);
    Task<HttpResult<FrankfurterResponse>> ConvertAsync(string baseCurrencyCode, string targetCurrencyCode, decimal amount, CancellationToken cToken);
    Task<HttpResult<FrankfurterSeriesResponse>> GetHistoryAsync(DateOnly beginDate, DateOnly endDate, string currencyCode, CancellationToken cToken);
}

public class FrankfurterClient : IFrankfurterClient
{
    private const string LatestEndpointTemplate = "/latest?from={0}";
    private const string ConvertEndpointTemplate = $"{LatestEndpointTemplate}&to={{1}}&amount={{2}}";
    private const string SeriesEndpointTemplate = "/{0}..{1}?from={2}";

    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterClient> _logger;

    public FrankfurterClient(HttpClient httpClient, ILogger<FrankfurterClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<HttpResult<FrankfurterResponse>> GetLatestAsync(string currencyCode, CancellationToken cToken) =>
        GetAsync<FrankfurterResponse>(string.Format(LatestEndpointTemplate, currencyCode), cToken);

    public Task<HttpResult<FrankfurterResponse>> ConvertAsync(string baseCurrencyCode, string targetCurrencyCode, decimal amount, CancellationToken cToken) =>
        GetAsync<FrankfurterResponse>(string.Format(ConvertEndpointTemplate, baseCurrencyCode, targetCurrencyCode, amount), cToken);

    public Task<HttpResult<FrankfurterSeriesResponse>> GetHistoryAsync(DateOnly beginDate, DateOnly endDate, string currencyCode, CancellationToken cToken) =>
        GetAsync<FrankfurterSeriesResponse>(string.Format(SeriesEndpointTemplate, beginDate.ToString("O"), endDate.ToString("O"), currencyCode), cToken);

    private async Task<HttpResult<TResult>> GetAsync<TResult>(string endpoint, CancellationToken cToken)
    {
        using var httpResponseMessage = await _httpClient.GetAsync(endpoint, cToken);
        var contentString = await httpResponseMessage.Content.ReadAsStringAsync(cToken);
        if (!httpResponseMessage.IsSuccessStatusCode)
        {
            _logger.LogError("[Frankfurter] Error getting {Endpoint}: [{Code}] {Message}", endpoint, httpResponseMessage.StatusCode, contentString);

            return HttpResult<TResult>.Failure(httpResponseMessage.StatusCode);
        }

        try
        {
            var response = JsonSerializer.Deserialize<TResult>(contentString);
            if (response != null)
                return HttpResult<TResult>.Success(response);

            _logger.LogError("[Frankfurter] Response is null: {Content}", contentString);
            return HttpResult<TResult>.InternalError();
        }
        catch (JsonException e)
        {
            _logger.LogError(e, "[Frankfurter] Error parsing JSON response: {Content}", contentString);
            return HttpResult<TResult>.InternalError();
        }
    }
}

public abstract record FrankfurterResponseBase(
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("base")] string Base);

public record FrankfurterResponse(
    decimal Amount,
    string Base,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("rates")] Dictionary<string, decimal> Rates)
    : FrankfurterResponseBase(Amount, Base);

public record FrankfurterSeriesResponse(
    decimal Amount,
    string Base,
    [property: JsonPropertyName("start_date")] string BeginDate,
    [property: JsonPropertyName("end_date")] string EndDate,
    [property: JsonPropertyName("rates")] Dictionary<string, Dictionary<string, decimal>> Rates)
    : FrankfurterResponseBase(Amount, Base);

public readonly struct HttpResult<TResult>(TResult? result, int statusCode)
{
    public static HttpResult<TResult> Success(TResult result) => new(result, StatusCodes.Status200OK);
    public static HttpResult<TResult> Failure(int statusCode) => new(default, statusCode);
    public static HttpResult<TResult> Failure(HttpStatusCode statusCode) => Failure((int)statusCode);
    public static HttpResult<TResult> InternalError() => Failure(StatusCodes.Status500InternalServerError);

    [MemberNotNullWhen(true, nameof(Result))]
    [MemberNotNullWhen(false, nameof(StatusCode))]
    public bool IsSuccess => StatusCode is >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices;
    public TResult? Result { get; init; } = result;
    public int StatusCode { get; init; } = statusCode;
}