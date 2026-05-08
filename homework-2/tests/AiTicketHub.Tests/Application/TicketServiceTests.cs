// tests/AiTicketHub.Tests/Application/TicketServiceTests.cs
using AiTicketHub.Application.DTOs;
using AiTicketHub.Application.Interfaces;
using AiTicketHub.Application.Services;
using AiTicketHub.Application.Validators;
using AiTicketHub.Domain.Common;
using AiTicketHub.Domain.Entities;
using AiTicketHub.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using Moq;
using NUnit.Framework;

namespace AiTicketHub.Tests.Application;

[TestFixture]
public class TicketServiceTests
{
    private Mock<ITicketRepository>    _repoMock       = null!;
    private Mock<IClassificationService> _classifierMock = null!;
    private TicketService              _service        = null!;

    [SetUp]
    public void SetUp()
    {
        _repoMock       = new Mock<ITicketRepository>();
        _classifierMock = new Mock<IClassificationService>();

        _service = new TicketService(
            _repoMock.Object,
            new CreateTicketValidator(),
            new UpdateTicketValidator(),
            Mock.Of<ITicketImportService>(),
            _classifierMock.Object);
    }

    // ── AutoClassify ────────────────────────────────────────────────────────────

    [Test]
    public async Task AutoClassify_TicketNotFound_ReturnsFailure()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
                 .ReturnsAsync(Result<Ticket>.Failure(Errors.TicketNotFound));

        var result = await _service.AutoClassifyAsync(Guid.NewGuid(), new AutoClassifyRequest());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Ticket.NotFound");
    }

    [Test]
    public async Task AutoClassify_HappyPath_ReturnsClassificationWithConfidence()
    {
        var ticket = MakeTicket("cannot login password reset", "I cannot login to my account");

        _repoMock.Setup(r => r.GetByIdAsync(ticket.Id))
                 .ReturnsAsync(Result<Ticket>.Success(ticket));

        _classifierMock.Setup(c => c.Classify(ticket.Subject, ticket.Description))
                       .Returns(new ClassificationResult(
                           TicketCategory.AccountAccess,
                           TicketPriority.Medium,
                           0.3,
                           "Matched keywords [login, password]",
                           ["login", "password"]));

        _repoMock.Setup(r => r.UpdateClassificationAsync(ticket.Id, TicketCategory.AccountAccess, TicketPriority.Medium))
                 .ReturnsAsync(Result<Ticket>.Success(ticket));

        var result = await _service.AutoClassifyAsync(ticket.Id, new AutoClassifyRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Category.Should().Be(TicketCategory.AccountAccess);
        result.Value!.Priority.Should().Be(TicketPriority.Medium);
        result.Value!.Confidence.Should().BeInRange(0.0, 1.0);
        result.Value!.KeywordsFound.Should().Contain("login");
    }

    [Test]
    public async Task AutoClassify_WithCategoryOverride_UsesCategoryOverride()
    {
        var ticket = MakeTicket("billing issue", "payment failed");

        _repoMock.Setup(r => r.GetByIdAsync(ticket.Id))
                 .ReturnsAsync(Result<Ticket>.Success(ticket));

        _classifierMock.Setup(c => c.Classify(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(new ClassificationResult(
                           TicketCategory.BillingQuestion,
                           TicketPriority.Medium,
                           0.2,
                           "Matched payment",
                           ["payment"]));

        _repoMock.Setup(r => r.UpdateClassificationAsync(ticket.Id, TicketCategory.FeatureRequest, TicketPriority.Medium))
                 .ReturnsAsync(Result<Ticket>.Success(ticket));

        var result = await _service.AutoClassifyAsync(
            ticket.Id,
            new AutoClassifyRequest(CategoryOverride: TicketCategory.FeatureRequest));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Category.Should().Be(TicketCategory.FeatureRequest);
    }

    [Test]
    public async Task AutoClassify_WithPriorityOverride_UsesPriorityOverride()
    {
        var ticket = MakeTicket("minor ui glitch", "button slightly misaligned");

        _repoMock.Setup(r => r.GetByIdAsync(ticket.Id))
                 .ReturnsAsync(Result<Ticket>.Success(ticket));

        _classifierMock.Setup(c => c.Classify(It.IsAny<string>(), It.IsAny<string>()))
                       .Returns(new ClassificationResult(
                           TicketCategory.TechnicalIssue,
                           TicketPriority.Low,
                           0.1,
                           "Matched minor",
                           ["minor"]));

        _repoMock.Setup(r => r.UpdateClassificationAsync(ticket.Id, TicketCategory.TechnicalIssue, TicketPriority.Urgent))
                 .ReturnsAsync(Result<Ticket>.Success(ticket));

        var result = await _service.AutoClassifyAsync(
            ticket.Id,
            new AutoClassifyRequest(PriorityOverride: TicketPriority.Urgent));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Priority.Should().Be(TicketPriority.Urgent);
    }

    // ── CreateTicket with AutoClassify flag ────────────────────────────────────

    [Test]
    public async Task CreateTicket_AutoClassifyTrue_AppliesClassification()
    {
        var request = new CreateTicketRequest(
            "cust-1", "a@b.com", "Alice", "login issue", "cannot login since 2 days",
            TicketCategory.Other, TicketPriority.Medium, TicketSource.Api,
            DeviceType.Desktop, [], null, null, AutoClassify: true);

        _classifierMock.Setup(c => c.Classify("login issue", "cannot login since 2 days"))
                       .Returns(new ClassificationResult(
                           TicketCategory.AccountAccess,
                           TicketPriority.Urgent,
                           0.25,
                           "Matched login",
                           ["login"]));

        _repoMock.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                 .ReturnsAsync((Ticket t) => Result<Ticket>.Success(t));

        var result = await _service.CreateTicketAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Category.Should().Be(TicketCategory.AccountAccess);
        result.Value!.Priority.Should().Be(TicketPriority.Urgent);
    }

    [Test]
    public async Task CreateTicket_AutoClassifyFalse_KeepsOriginalCategory()
    {
        var request = new CreateTicketRequest(
            "cust-1", "a@b.com", "Alice", "billing question", "invoice not received",
            TicketCategory.BillingQuestion, TicketPriority.High, TicketSource.Api,
            DeviceType.Desktop, [], null, null, AutoClassify: false);

        _repoMock.Setup(r => r.AddAsync(It.IsAny<Ticket>()))
                 .ReturnsAsync((Ticket t) => Result<Ticket>.Success(t));

        var result = await _service.CreateTicketAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Category.Should().Be(TicketCategory.BillingQuestion);
        _classifierMock.Verify(c => c.Classify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ── KeywordClassifier behaviour (via real classifier) ─────────────────────

    [Test]
    public void KeywordClassifier_UrgentPriorityKeyword_SetsUrgent()
    {
        var classifier = new AiTicketHub.Infrastructure.Services.KeywordClassifier(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AiTicketHub.Infrastructure.Services.KeywordClassifier>.Instance);

        var result = classifier.Classify("production down critical", "we cannot access anything");

        result.Priority.Should().Be(TicketPriority.Urgent);
        result.Confidence.Should().BeInRange(0.0, 1.0);
        result.KeywordsFound.Should().NotBeEmpty();
    }

    [Test]
    public void KeywordClassifier_LoginKeyword_SetsAccountAccess()
    {
        var classifier = new AiTicketHub.Infrastructure.Services.KeywordClassifier(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AiTicketHub.Infrastructure.Services.KeywordClassifier>.Instance);

        var result = classifier.Classify("login problem", "I cannot login with my password");

        result.Category.Should().Be(TicketCategory.AccountAccess);
    }

    [Test]
    public void KeywordClassifier_NoKeywordsMatched_DefaultsToOtherAndMedium()
    {
        var classifier = new AiTicketHub.Infrastructure.Services.KeywordClassifier(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AiTicketHub.Infrastructure.Services.KeywordClassifier>.Instance);

        var result = classifier.Classify("hello world", "nothing special here");

        result.Category.Should().Be(TicketCategory.Other);
        result.Priority.Should().Be(TicketPriority.Medium);
        result.Confidence.Should().Be(0.0);
        result.KeywordsFound.Should().BeEmpty();
    }

    [Test]
    public void KeywordClassifier_ConfidenceAlwaysInRange()
    {
        var classifier = new AiTicketHub.Infrastructure.Services.KeywordClassifier(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AiTicketHub.Infrastructure.Services.KeywordClassifier>.Instance);

        var result = classifier.Classify(
            "login password 2fa payment invoice refund bug crash reproduction enhancement suggestion error technical broken can't access critical production down security important blocking asap minor cosmetic",
            "more keywords");

        result.Confidence.Should().BeInRange(0.0, 1.0);
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private static Ticket MakeTicket(string subject, string description) =>
        new(Guid.NewGuid(), "cust-1", "a@b.com", "Alice",
            subject, description,
            TicketCategory.Other, TicketPriority.Medium, TicketStatus.New,
            DateTime.UtcNow, DateTime.UtcNow, null, null, [],
            TicketSource.Api, null, DeviceType.Desktop);
}
