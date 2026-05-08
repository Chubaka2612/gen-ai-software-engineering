// tests/AiTicketHub.Tests/API/TicketControllerTests.cs
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AiTicketHub.Application.DTOs;
using AiTicketHub.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace AiTicketHub.Tests.API;

[TestFixture]
public class TicketControllerTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient                     _client  = null!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters            = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

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

    // ── POST /api/tickets/{id}/auto-classify ────────────────────────────────

    [Test]
    public async Task AutoClassify_ExistingTicket_Returns200WithConfidence()
    {
        var ticketId = await CreateTicketAndGetId("cannot login", "I cannot login with my password");

        var response = await _client.PostAsync($"/api/tickets/{ticketId}/auto-classify", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AutoClassifyResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Confidence.Should().BeInRange(0.0, 1.0);
        body.Reasoning.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task AutoClassify_NonExistentTicket_Returns404()
    {
        var response = await _client.PostAsync($"/api/tickets/{Guid.NewGuid()}/auto-classify", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AutoClassify_LoginKeywords_ReturnsAccountAccessCategory()
    {
        var ticketId = await CreateTicketAndGetId("login password reset", "cannot login using my password");

        var response = await _client.PostAsync($"/api/tickets/{ticketId}/auto-classify", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var category = body.GetProperty("category").GetString();
        category.Should().Be("AccountAccess");
    }

    [Test]
    public async Task AutoClassify_WithCategoryOverride_ReturnsThatCategory()
    {
        var ticketId = await CreateTicketAndGetId("some issue", "random description");

        var overrideBody = JsonContent.Create(new { categoryOverride = "BugReport" });
        var response = await _client.PostAsync($"/api/tickets/{ticketId}/auto-classify", overrideBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("category").GetString().Should().Be("BugReport");
    }

    // ── POST /api/tickets with AutoClassify flag ───────────────────────────

    [Test]
    public async Task CreateTicket_AutoClassifyTrue_Returns201WithClassifiedCategory()
    {
        var request = new
        {
            customerId    = "cust-1",
            customerEmail = "a@b.com",
            customerName  = "Alice",
            subject       = "login password issue",
            description   = "I cannot login with my password since yesterday",
            category      = "Other",
            priority      = "Medium",
            source        = "Api",
            deviceType    = "Desktop",
            tags          = Array.Empty<string>(),
            autoClassify  = true
        };

        var response = await _client.PostAsJsonAsync("/api/tickets", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("category").GetString().Should().Be("AccountAccess");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<Guid> CreateTicketAndGetId(string subject, string description)
    {
        var request = new
        {
            customerId    = "cust-1",
            customerEmail = "test@example.com",
            customerName  = "Test User",
            subject,
            description,
            category      = "Other",
            priority      = "Medium",
            source        = "Api",
            deviceType    = "Desktop",
            tags          = Array.Empty<string>()
        };

        var response = await _client.PostAsJsonAsync("/api/tickets", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }
}
