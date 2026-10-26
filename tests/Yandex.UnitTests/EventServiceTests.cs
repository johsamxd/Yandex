using Microsoft.Extensions.DependencyInjection;
using Yandex.Application.Abstractions;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Exceptions;
using Yandex.Application.Requests.Events;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.UnitTests;

public class EventServiceTests : IDisposable
{
    private readonly TestServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IEventService _service;
    private readonly IEntityRepository<Event> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public EventServiceTests()
    {
        _provider = new TestServiceProvider();
        _scope = _provider.CreateScope();

        _service = _scope.ServiceProvider.GetRequiredService<IEventService>();
        _repository = _scope.ServiceProvider.GetRequiredService<IEntityRepository<Event>>();
        _unitOfWork = _scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private async Task<Event> SeedEventAsync(
        string title = "Test Event",
        string description = "Test Description",
        DateTime? startAt = null,
        DateTime? endAt = null,
        int totalSeats = 100)
    {
        var entity = new Event
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            StartAt = startAt ?? DateTime.UtcNow.AddDays(1),
            EndAt = endAt ?? DateTime.UtcNow.AddDays(1).AddHours(2),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };

        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        return entity;
    }

    // ===== Success scenarios =====

    [Fact]
    public async Task CreateEvent_WithValidData_ShouldReturnEvent()
    {
        // Arrange
        var request = new CreateEventRequest(
            "New Event", "Description",
            DateTime.Parse("2026-10-05 10:00:00"),
            DateTime.Parse("2026-10-05 18:00:00"), 100);

        // Act
        var result = await _service.CreateEventAsync(request, TestContext.Current.CancellationToken);

        // Assert — DTO
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.StartAt, result.StartAt);
        Assert.Equal(request.EndAt, result.EndAt);
        Assert.Equal(100, result.TotalSeats);
        Assert.Equal(100, result.AvailableSeats);

        // Assert — реально сохранилось в БД
        var saved = await _repository.GetByIdAsync(result.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(request.Title, saved.Title);
    }

    [Fact]
    public async Task GetEvents_ShouldReturnAllEvents()
    {
        // Arrange
        var e1 = await SeedEventAsync("Event 1", "Description 1",
            DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"));
        var e2 = await SeedEventAsync("Event 2", "Description 2",
            DateTime.Parse("2026-11-05 10:10:10"), DateTime.Parse("2026-11-05 20:10:10"));

        // Act
        var result = await _service.GetEventsAsync(new EventFilter(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, result.TotalItems);
        Assert.Contains(items, i => i.Id == e1.Id && i.Title == "Event 1");
        Assert.Contains(items, i => i.Id == e2.Id && i.Title == "Event 2");
    }

    [Fact]
    public async Task GetEventById_WithValidId_ShouldReturnEvent()
    {
        // Arrange
        var seeded = await SeedEventAsync("Test Event", "Test Description",
            DateTime.Parse("2026-10-05 10:00:00"), DateTime.Parse("2026-10-05 18:00:00"));

        // Act
        var result = await _service.GetEventAsync(seeded.Id, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(seeded.Id, result.Id);
        Assert.Equal("Test Event", result.Title);
        Assert.Equal("Test Description", result.Description);
        Assert.Equal(seeded.StartAt, result.StartAt);
        Assert.Equal(seeded.EndAt, result.EndAt);
    }

    [Fact]
    public async Task UpdateEvent_WithValidIdAndData_ShouldUpdateAndReturnEvent()
    {
        // Arrange
        var seeded = await SeedEventAsync("Old Title", "Old Description");
        var updateRequest = new UpdateEventRequest(
            "Updated Title", "Updated Description",
            DateTime.Parse("2026-11-05 10:00:00"),
            DateTime.Parse("2026-11-05 18:00:00"));

        // Act
        var result = await _service.UpdateEventAsync(seeded.Id, updateRequest, TestContext.Current.CancellationToken);

        // Assert — DTO
        Assert.NotNull(result);
        Assert.Equal(seeded.Id, result.Id);
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated Description", result.Description);
        Assert.Equal(updateRequest.StartAt, result.StartAt);
        Assert.Equal(updateRequest.EndAt, result.EndAt);

        // Assert — реально в БД
        var fromDb = await _repository.GetByIdAsync(seeded.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(fromDb);
        Assert.Equal("Updated Title", fromDb.Title);
        Assert.Equal("Updated Description", fromDb.Description);
    }

    [Fact]
    public async Task DeleteEvent_WithValidId_ShouldRemoveEvent()
    {
        // Arrange
        var seeded = await SeedEventAsync("To Delete");

        // Act
        await _service.DeleteEventAsync(seeded.Id, TestContext.Current.CancellationToken);

        // Assert
        var deleted = await _repository.GetByIdAsync(seeded.Id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task GetEvents_WithTitleFilter_ShouldReturnFilteredEvents()
    {
        // Arrange
        await SeedEventAsync("Conference 2026", "Tech conference",
            DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"));
        await SeedEventAsync("Workshop C#", "C# workshop",
            DateTime.Parse("2026-11-05 10:10:10"), DateTime.Parse("2026-11-05 20:10:10"));
        await SeedEventAsync("Meeting", "Team meeting",
            DateTime.Parse("2026-12-05 10:10:10"), DateTime.Parse("2026-12-05 20:10:10"));

        var filter = new EventFilter { Title = "Conference", Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetEventsAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Conference 2026", items[0].Title);
    }

    [Fact]
    public async Task GetEvents_WithStartDateFilter_ShouldReturnEventsAfterStartDate()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-11-01");

        await SeedEventAsync("Event 1", "Description 1",
            DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"));
        await SeedEventAsync("Event 2", "Description 2",
            DateTime.Parse("2026-11-05 10:10:10"), DateTime.Parse("2026-11-05 20:10:10"));
        await SeedEventAsync("Event 3", "Description 3",
            DateTime.Parse("2026-12-05 10:10:10"), DateTime.Parse("2026-12-05 20:10:10"));

        var filter = new EventFilter { From = startDate, Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetEventsAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, result.TotalItems);
        Assert.All(items, item => Assert.True(item.StartAt >= startDate.Date));
    }

    [Fact]
    public async Task GetEvents_WithEndDateFilter_ShouldReturnEventsBeforeEndDate()
    {
        // Arrange
        var endDate = DateTime.Parse("2026-11-01");

        await SeedEventAsync("Event 1", "Description 1",
            DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"));
        await SeedEventAsync("Event 2", "Description 2",
            DateTime.Parse("2026-11-05 10:10:10"), DateTime.Parse("2026-11-05 20:10:10"));
        await SeedEventAsync("Event 3", "Description 3",
            DateTime.Parse("2026-12-05 10:10:10"), DateTime.Parse("2026-12-05 20:10:10"));

        var filter = new EventFilter { To = endDate, Page = 1, PageSize = 10 };

        // Act
        var result = await _service.GetEventsAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(1, result.TotalItems);
        Assert.All(items, item => Assert.True(item.EndAt <= endDate.Date));
    }

    [Fact]
    public async Task GetEvents_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (var i = 1; i <= 10; i++)
        {
            await SeedEventAsync($"Event {i}", $"Description {i}",
                DateTime.UtcNow.AddDays(i), DateTime.UtcNow.AddDays(i).AddHours(2));
        }

        var filter = new EventFilter { Page = 2, PageSize = 3 };

        // Act
        var result = await _service.GetEventsAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(10, result.TotalItems);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task GetEvents_WithCombinedFilters_ShouldReturnFilteredEvents()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-11-01");
        var endDate = DateTime.Parse("2026-11-30");

        await SeedEventAsync("Conference 2026", "Tech conference",
            DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"));
        await SeedEventAsync("Conference 2026", "Tech conference",
            DateTime.Parse("2026-11-15 10:10:10"), DateTime.Parse("2026-11-15 20:10:10"));
        await SeedEventAsync("Workshop C#", "C# workshop",
            DateTime.Parse("2026-11-20 10:10:10"), DateTime.Parse("2026-11-20 20:10:10"));

        var filter = new EventFilter
        {
            Title = "Conference",
            From = startDate,
            To = endDate,
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.GetEventsAsync(filter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Conference 2026", items[0].Title);
        Assert.True(items[0].StartAt >= startDate.Date);
        Assert.True(items[0].EndAt <= endDate.Date);
    }

    // ===== Fail scenarios =====

    [Fact]
    public async Task GetEventById_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange — БД пустая, ничего не сеем
        var nonExistentId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetEventAsync(nonExistentId, TestContext.Current.CancellationToken));
        Assert.Equal($"Event with id {nonExistentId} not found", exception.Message);
    }

    [Fact]
    public async Task UpdateEvent_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateEventRequest(
            "Updated Title", "Updated Description",
            DateTime.Parse("2026-10-05 10:00:00"),
            DateTime.Parse("2026-10-05 18:00:00"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateEventAsync(nonExistentId, updateRequest, TestContext.Current.CancellationToken));
        Assert.Equal($"Event with id {nonExistentId} not found", exception.Message);
    }

    // ===== Валидаторы (не трогают БД — оставлены как есть) =====

    [Fact]
    public async Task CreateEvent_WithInvalidData_ShouldThrowValidationException()
    {
        var validator = new CreateEventValidator();
        var invalidRequest = new CreateEventRequest("", "Description",
            DateTime.Parse("2026-10-05 10:00:00"),
            DateTime.Parse("2026-10-05 18:00:00"), 100);

        var result = await validator.ValidateAsync(invalidRequest, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public async Task CreateEvent_WithEndDateEarlierThanStartDate_ShouldThrowValidationException()
    {
        var validator = new CreateEventValidator();
        var invalidRequest = new CreateEventRequest("Invalid Event", "Description",
            DateTime.Parse("2026-10-05 18:00:00"),
            DateTime.Parse("2026-10-05 10:00:00"), 100);

        var result = await validator.ValidateAsync(invalidRequest, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName is "StartAt" or "EndAt");
    }

    [Fact]
    public async Task UpdateEvent_WithEndDateEarlierThanStartDate_ShouldThrowValidationException()
    {
        var validator = new UpdateEventValidator();
        var invalidRequest = new UpdateEventRequest(
            "Updated Event", "Updated Description",
            DateTime.Parse("2026-10-05 18:00:00"),
            DateTime.Parse("2026-10-05 10:00:00"));

        var result = await validator.ValidateAsync(invalidRequest, TestContext.Current.CancellationToken);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName is "StartAt" or "EndAt");
    }
}