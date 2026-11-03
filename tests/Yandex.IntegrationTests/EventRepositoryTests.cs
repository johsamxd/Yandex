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
        var evt = CreateEvent("Concert", DateTime.UtcNow.AddDays(1));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetByIdAsync(evt.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Concert", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEvent_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFilteredAsync_NoFilters_ReturnsAll()
    {
        _context.Events.AddRange(
            CreateEvent("A", DateTime.UtcNow.AddDays(1)),
            CreateEvent("B", DateTime.UtcNow.AddDays(2)),
            CreateEvent("C", DateTime.UtcNow.AddDays(3)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 1, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_WithTitleFilter_ReturnsMatchingEvents()
    {
        _context.Events.AddRange(
            CreateEvent("Concert", DateTime.UtcNow.AddDays(1)),
            CreateEvent("Theater", DateTime.UtcNow.AddDays(2)),
            CreateEvent("Concert Hall", DateTime.UtcNow.AddDays(3)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(Title: "Concert"), 1, 10,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Contains("Concert", e.Title));
    }

    [Fact]
    public async Task GetFilteredAsync_WithTitleFilter_CaseInsensitive()
    {
        _context.Events.Add(CreateEvent("Concert", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(Title: "concert"), 1, 10,
            TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetFilteredAsync_WithFromFilter_ReturnsEventsStartingAfterDate()
    {
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1)),
            CreateEvent("B", baseDate.AddDays(5)),
            CreateEvent("C", baseDate.AddDays(10)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(From: baseDate.AddDays(3)), 1, 10,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Title == "A");
    }

    [Fact]
    public async Task GetFilteredAsync_WithToFilter_ReturnsEventsEndingBeforeDate()
    {
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1), baseDate.AddDays(2)),
            CreateEvent("B", baseDate.AddDays(3), baseDate.AddDays(3)),
            CreateEvent("C", baseDate.AddDays(5), baseDate.AddDays(6)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(To: baseDate.AddDays(3)), 1, 10,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.DoesNotContain(result, e => e.Title == "C");
    }

    [Fact]
    public async Task GetFilteredAsync_WithCombinedFilters_ReturnsMatchingEvents()
    {
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("Concert", baseDate.AddDays(1), baseDate.AddDays(2)),
            CreateEvent("Concert", baseDate.AddDays(5), baseDate.AddDays(6)),
            CreateEvent("Theater", baseDate.AddDays(1), baseDate.AddDays(2)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(Title: "Concert", From: baseDate.AddDays(3)), 1,
            10, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.Equal(baseDate.AddDays(5), result[0].StartAt.Date);
    }

    [Fact]
    public async Task GetFilteredAsync_FirstPage_ReturnsCorrectPageSize()
    {
        for (var i = 0; i < 15; i++)
            _context.Events.Add(CreateEvent($"Event {i}", DateTime.UtcNow.AddDays(i)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 1, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_SecondPage_ReturnsRemainingEvents()
    {
        for (var i = 0; i < 15; i++)
            _context.Events.Add(CreateEvent($"Event {i}", DateTime.UtcNow.AddDays(i)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 2, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetFilteredAsync_PageBeyondTotal_ReturnsEmpty()
    {
        _context.Events.Add(CreateEvent("A", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(), page: 5, pageSize: 10,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFilteredAsync_OrderedByStartAt()
    {
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("C", baseDate.AddDays(3)),
            CreateEvent("A", baseDate.AddDays(1)),
            CreateEvent("B", baseDate.AddDays(2)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _repository.GetFilteredAsync(new EventQuery(), 1, 10, TestContext.Current.CancellationToken);

        Assert.Equal(["A", "B", "C"], result.Select(e => e.Title));
    }

    [Fact]
    public async Task CountAsync_NoFilter_ReturnsTotalCount()
    {
        _context.Events.AddRange(
            CreateEvent("A", DateTime.UtcNow.AddDays(1)),
            CreateEvent("B", DateTime.UtcNow.AddDays(2)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await _repository.CountAsync(new EventQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_WithTitleFilter_ReturnsFilteredCount()
    {
        _context.Events.AddRange(
            CreateEvent("Concert", DateTime.UtcNow.AddDays(1)),
            CreateEvent("Theater", DateTime.UtcNow.AddDays(2)),
            CreateEvent("Concert Hall", DateTime.UtcNow.AddDays(3)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await _repository.CountAsync(new EventQuery(Title: "Concert"),
            TestContext.Current.CancellationToken);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_WithDateFilter_ReturnsFilteredCount()
    {
        var baseDate = DateTime.UtcNow.Date;
        _context.Events.AddRange(
            CreateEvent("A", baseDate.AddDays(1), baseDate.AddDays(2)),
            CreateEvent("B", baseDate.AddDays(5), baseDate.AddDays(6)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var count = await _repository.CountAsync(new EventQuery(From: baseDate.AddDays(3)),
            TestContext.Current.CancellationToken);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AddAsync_ValidEvent_PersistsEvent()
    {
        var evt = CreateEvent("New Event", DateTime.UtcNow.AddDays(1));

        await _repository.AddAsync(evt, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fromDb = await _context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evt.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal("New Event", fromDb.Title);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEvent_UpdatesFields()
    {
        var evt = CreateEvent("Old Title", DateTime.UtcNow.AddDays(1));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        evt.Title = "New Title";
        await _repository.UpdateAsync(evt, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fromDb = await _context.Events.AsNoTracking()
            .FirstAsync(e => e.Id == evt.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("New Title", fromDb.Title);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingEvent_ThrowsKeyNotFound()
    {
        var evt = CreateEvent("Ghost", DateTime.UtcNow.AddDays(1));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.UpdateAsync(evt, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RemoveAsync_ExistingEvent_DeletesEvent()
    {
        var evt = CreateEvent("To Delete", DateTime.UtcNow.AddDays(1));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _repository.RemoveAsync(evt.Id, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var fromDb = await _context.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evt.Id, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(fromDb);
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEvent_ThrowsKeyNotFound()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.RemoveAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
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