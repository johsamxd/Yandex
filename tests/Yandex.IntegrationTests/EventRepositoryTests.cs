using Microsoft.EntityFrameworkCore;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;
using Yandex.Infrastructure.Persistence;
using Yandex.Infrastructure.Persistence.Repositories;

namespace Yandex.IntegrationTests;

[Collection("Postgres collection")]
public class EventRepositoryTests(PostgresFixture fixture) : IAsyncLifetime
{
    private AppDbContext _context = null!;
    private EventRepository _repository = null!;

    public async ValueTask InitializeAsync()
    {
        _context = fixture.CreateContext();

        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();

        _repository = new EventRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEvent_ReturnsEvent()
    {
        // Arrange
        var evt = CreateEvent("Concert", DateTime.UtcNow.AddDays(1));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Concert", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEvent_ReturnsNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredAsync_NoFilters_ReturnsAll()
    {
        // Arrange
        _context.Events.AddRange(
            CreateEvent("A", DateTime.UtcNow.AddDays(1)),
            CreateEvent("B", DateTime.UtcNow.AddDays(2)),
            CreateEvent("C", DateTime.UtcNow.AddDays(3)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 1, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_WithTitleFilter_ReturnsMatchingEvents()
    {
        // Arrange
        _context.Events.AddRange(
            CreateEvent("Concert", DateTime.UtcNow.AddDays(1)),
            CreateEvent("Theater", DateTime.UtcNow.AddDays(2)),
            CreateEvent("Concert Hall", DateTime.UtcNow.AddDays(3)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(Title: "Concert"), 1, 10,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Contains("Concert", e.Title));
    }

    [Fact]
    public async Task GetFilteredAsync_WithTitleFilter_CaseInsensitive()
    {
        // Arrange
        _context.Events.Add(CreateEvent("Concert", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(Title: "concert"), 1, 10,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetFilteredAsync_WithFromFilter_ReturnsEventsStartingAfterDate()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1)),
            CreateEvent("B", baseDate.AddDays(5)),
            CreateEvent("C", baseDate.AddDays(10)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(From: baseDate.AddDays(3)), 1, 10,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Title == "A");
    }

    [Fact]
    public async Task GetFilteredAsync_WithToFilter_ReturnsEventsEndingBeforeDate()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1), baseDate.AddDays(2)),
            CreateEvent("B", baseDate.AddDays(3), baseDate.AddDays(3)),
            CreateEvent("C", baseDate.AddDays(5), baseDate.AddDays(6)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(To: baseDate.AddDays(3)), 1, 10,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Title == "C");
    }

    [Fact]
    public async Task GetFilteredAsync_WithToFilter_IncludesEventsEndingOnToDate()
    {
        // Arrange
        var to = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        _context.Events.AddRange(
            CreateEvent("EndsOnToDate", new DateTime(2026, 11, 30, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 1, 21, 0, 0, DateTimeKind.Utc)),
            CreateEvent("EndsAfterToDate", new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 2, 10, 0, 0, DateTimeKind.Utc)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(
            new EventQuery(To: to), 1, 10, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal("EndsOnToDate", result[0].Title);
    }

    [Fact]
    public async Task GetFilteredAsync_WithCombinedFilters_ReturnsMatchingEvents()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("Concert", baseDate.AddDays(1), baseDate.AddDays(2)),
            CreateEvent("Concert", baseDate.AddDays(5), baseDate.AddDays(6)),
            CreateEvent("Theater", baseDate.AddDays(1), baseDate.AddDays(2)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(Title: "Concert", From: baseDate.AddDays(3)), 1,
            10, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(baseDate.AddDays(5), result[0].StartAt.Date);
    }

    [Fact]
    public async Task GetFilteredAsync_FirstPage_ReturnsCorrectPageSize()
    {
        // Arrange
        for (var i = 0; i < 15; i++)
            _context.Events.Add(CreateEvent($"Event {i}", DateTime.UtcNow.AddDays(i)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 1, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_SecondPage_ReturnsRemainingEvents()
    {
        // Arrange
        for (var i = 0; i < 15; i++)
            _context.Events.Add(CreateEvent($"Event {i}", DateTime.UtcNow.AddDays(i)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 2, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_PageBeyondTotal_ReturnsEmpty()
    {
        // Arrange
        _context.Events.Add(CreateEvent("A", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 5, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFilteredAsync_SameStartAt_OrderIsStableAcrossPages()
    {
        // Arrange: 15 событий с одинаковым StartAt
        var startAt = new DateTime(2026, 12, 1, 10, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 15; i++)
            _context.Events.Add(CreateEvent($"Event {i}", startAt));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act: собираем все страницы
        var page1 = await _repository.GetFilteredAsync(new EventQuery(), 1, 10, TestContext.Current.CancellationToken);
        var page2 = await _repository.GetFilteredAsync(new EventQuery(), 2, 10, TestContext.Current.CancellationToken);
        var all = page1.Concat(page2).Select(e => e.Id).ToList();

        // Assert: 15 уникальных Id, без дублей между страницами
        Assert.Equal(15, all.Distinct().Count());
    }

    [Fact]
    public async Task GetFilteredAsync_OrderedByStartAt()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("C", baseDate.AddDays(3)),
            CreateEvent("A", baseDate.AddDays(1)),
            CreateEvent("B", baseDate.AddDays(2)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetFilteredAsync(new EventQuery(), 1, 10, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["A", "B", "C"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task CountAsync_NoFilter_ReturnsTotalCount()
    {
        // Arrange
        _context.Events.AddRange(
            CreateEvent("A", DateTime.UtcNow.AddDays(1)),
            CreateEvent("B", DateTime.UtcNow.AddDays(2)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var count = await _repository.CountAsync(new EventQuery(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_WithTitleFilter_ReturnsFilteredCount()
    {
        // Arrange
        _context.Events.AddRange(
            CreateEvent("Concert", DateTime.UtcNow.AddDays(1)),
            CreateEvent("Theater", DateTime.UtcNow.AddDays(2)),
            CreateEvent("Concert Hall", DateTime.UtcNow.AddDays(3)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var count = await _repository.CountAsync(new EventQuery(Title: "Concert"),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_WithDateFilter_ReturnsFilteredCount()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1), baseDate.AddDays(2)),
            CreateEvent("B", baseDate.AddDays(5), baseDate.AddDays(6)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var count = await _repository.CountAsync(new EventQuery(From: baseDate.AddDays(3)),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_ValidEvent_PersistsEvent()
    {
        // Arrange
        var evt = CreateEvent("New Event", DateTime.UtcNow.AddDays(1));

        // Act
        await _repository.AddAsync(evt, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var fromDb = await _context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evt.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal("New Event", fromDb.Title);
    }

    [Fact]
    public async Task RemoveAsync_ExistingEvent_DeletesEvent()
    {
        // Arrange
        var evt = CreateEvent("To Delete", DateTime.UtcNow.AddDays(1));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _repository.RemoveAsync(evt.Id, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var fromDb = await _context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evt.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEvent_ThrowsKeyNotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.RemoveAsync(nonExistingId, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetFilteredAsync_WithUnspecifiedKindFromFilter_DoesNotThrow()
    {
        // Arrange
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1)),
            CreateEvent("B", baseDate.AddDays(5)),
            CreateEvent("C", baseDate.AddDays(10)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Имитируем то, что приходит из Swagger/HTTP: дата без Z → Kind = Unspecified
        var from = new DateTime(2026, 11, 30, 0, 0, 0, DateTimeKind.Unspecified);
        var to = new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Unspecified);

        // Act
        var result = await _repository.GetFilteredAsync(
            new EventQuery(From: from, To: to), 1, 10,
            TestContext.Current.CancellationToken);

        // Assert: не бросило исключение, и фильтр реально применился
        Assert.All(result, e =>
        {
            Assert.True(e.StartAt >= from);
            Assert.True(e.EndAt <= to);
        });
    }

    private static Event CreateEvent(string title, DateTime startAt, DateTime? endAt = null)
    {
        return new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            StartAt = startAt,
            EndAt = endAt ?? startAt.AddHours(2),
            TotalSeats = 100,
            AvailableSeats = 100
        };
    }
}