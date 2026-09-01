// <copyright file="PublicApiClient.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// HTTP client for CipherBank public POST endpoints on api.cipherbank.money.
/// </summary>
public sealed partial class PublicApiClient : IPublicQuoteService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly ILogger<PublicApiClient> _logger;

    private readonly TimeProvider _timeProvider;

    public PublicApiClient(HttpClient http, ILogger<PublicApiClient> logger, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _http = http;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendPublicPostAsync("/test", new { }, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<string>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendPublicPostAsync("/currencies", new { }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!document.RootElement.TryGetProperty("CURRENCIES", out var currenciesElement))
        {
            return [];
        }

        List<string> result = new List<string>();
        if (currenciesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in currenciesElement.EnumerateArray())
            {
                var raw = item.ValueKind == JsonValueKind.String
                    ? item.GetString()
                    : item.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    result.Add(CurrencySymbolMap.ToAppSymbol(raw));
                }
            }
        }
        else if (currenciesElement.ValueKind == JsonValueKind.String)
        {
            // Wire format may stringify collections.
            var raw = currenciesElement.GetString() ?? string.Empty;
            foreach (var part in raw.Split([',', ' ', '[', ']', '"'], StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(CurrencySymbolMap.ToAppSymbol(part));
            }
        }

        return result;
    }

    public async Task<PublicQuote> GetInverseQuoteAsync(
        string inputSymbol,
        decimal inputAmount,
        string outputSymbol,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> body = new Dictionary<string, object>
        {
            ["INPUT_CURRENCY"] = CurrencySymbolMap.ToApiCurrency(inputSymbol),
            ["INPUT_AMOUNT"] = decimal.ToDouble(inputAmount),
            ["OUTPUT_CURRENCY"] = CurrencySymbolMap.ToApiCurrency(outputSymbol),
        };

        using var response = await SendPublicPostAsync("/iquote", body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadQuoteAsync(response, cancellationToken);
    }

    public async Task<PublicQuote> GetQuoteAsync(
        string inputSymbol,
        decimal outputAmount,
        string outputSymbol,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, object> body = new Dictionary<string, object>
        {
            ["INPUT_CURRENCY"] = CurrencySymbolMap.ToApiCurrency(inputSymbol),
            ["OUTPUT_AMOUNT"] = decimal.ToDouble(outputAmount),
            ["OUTPUT_CURRENCY"] = CurrencySymbolMap.ToApiCurrency(outputSymbol),
        };

        using var response = await SendPublicPostAsync("/quote", body, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await ReadQuoteAsync(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendPublicPostAsync(
        string path,
        object body,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, path.TrimStart('/'));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Date = _timeProvider.GetUtcNow();
        request.Content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions),
            Encoding.UTF8,
            "application/json");

        LogSending(_logger, path);
        return await _http.SendAsync(request, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var detail = await response.Content.ReadAsStringAsync(cancellationToken);
        var code = (int)response.StatusCode;
        var message = code switch
        {
            424 => "Price or wallet dependency unavailable.",
            422 => "Quote or currency request was invalid.",
            417 => "Request body or Date header was rejected.",
            415 => "Content-Type must be application/json.",
            406 => "Accept header must allow JSON.",
            _ => $"Public API request failed with HTTP {code}.",
        };

        throw new InvalidOperationException($"{message} {detail}".Trim());
    }

    private static async Task<PublicQuote> ReadQuoteAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        var inputCurrency = CurrencySymbolMap.ToAppSymbol(ReadString(root, "INPUT_CURRENCY"));
        var outputCurrency = CurrencySymbolMap.ToAppSymbol(ReadString(root, "OUTPUT_CURRENCY"));
        var inputAmount = ReadDecimal(root, "INPUT_AMOUNT");
        var outputAmount = ReadDecimal(root, "OUTPUT_AMOUNT");

        return new PublicQuote(inputCurrency, inputAmount, outputCurrency, outputAmount);
    }

    private static string ReadString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            throw new InvalidOperationException($"Public API response missing '{name}'.");
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.ToString();
    }

    private static decimal ReadDecimal(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            throw new InvalidOperationException($"Public API response missing '{name}'.");
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetDecimal(out var d) ? d : (decimal)value.GetDouble(),
            JsonValueKind.String => decimal.Parse(value.GetString() ?? "0", CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException($"Public API field '{name}' was not numeric."),
        };
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending public API POST {Path}")]
    private static partial void LogSending(ILogger logger, string path);
}
