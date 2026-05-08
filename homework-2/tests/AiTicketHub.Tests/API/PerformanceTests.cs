// tests/AiTicketHub.Tests/API/PerformanceTests.cs
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace AiTicketHub.Tests.API;

[TestFixture]
public class PerformanceTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient                     _client  = null!;

    private const string BaseRoute  = "/api/tickets";
    private const int    TicketCount = 1000;
    private const int    Iterations  = 100;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>();
        _client  = _factory.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── ListTickets benchmark ─────────────────────────────────────────────

    [Test]
    public async Task ListTickets_1000Tickets_P95ResponseUnder200ms()
    {
        await SeedTicketsViaImportAsync(TicketCount);

        // Warm-up request — not included in measurements
        await _client.GetAsync($"{BaseRoute}?pageSize=1");

        var elapsed = new List<long>(Iterations);
        for (var i = 0; i < Iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await _client.GetAsync($"{BaseRoute}?pageSize=20");
            sw.Stop();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            elapsed.Add(sw.ElapsedMilliseconds);
        }

        elapsed.Sort();
        var p95Index = (int)Math.Ceiling(Iterations * 0.95) - 1;
        var p95Ms    = elapsed[p95Index];

        p95Ms.Should().BeLessThan(200,
            $"p95 response time was {p95Ms} ms — expected < 200 ms");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task SeedTicketsViaImportAsync(int count)
    {
        var rows = Enumerable.Range(1, count).Select(i =>
            $$$"""{"customerId":"c{{{i}}}","customerEmail":"user{{{i}}}@example.com","customerName":"User {{{i}}}","subject":"Performance test ticket {{{i}}}","description":"Performance benchmark seed data for ticket number {{{i}}}","category":"Other","priority":"Medium"}""");

        var json = "[" + string.Join(",", rows) + "]";
        using var content = BuildImportContent(json, "seed.json", "application/json");

        var response = await _client.PostAsync($"{BaseRoute}/import", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("successful").GetInt32().Should().Be(count);
    }

    private static MultipartFormDataContent BuildImportContent(string body, string fileName, string contentType)
    {
        var form    = new MultipartFormDataContent();
        var content = new StringContent(body, Encoding.UTF8, contentType);
        form.Add(content, "file", fileName);
        return form;
    }
}
