using AutoMapper;
using Moq;
using Yandex.Application.Dtos.Events;
using Yandex.Application.Exceptions;
using Yandex.Application.Requests.Events;
using Yandex.Application.Services;
using Yandex.Domain.Abstractions;
using Yandex.Domain.Entities;

namespace Yandex.UnitTests;

public class EventServiceTests
{
    private readonly Mock<IEntityRepository<Event>> _repositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly EventService _service;

    public EventServiceTests()
    {
        _repositoryMock = new Mock<IEntityRepository<Event>>();
        _mapperMock = new Mock<IMapper>();
        _service = new EventService(_repositoryMock.Object, _mapperMock.Object);
    }

    // Success scenarios
    [Fact]
    public void CreateEvent_WithValidData_ShouldReturnEvent()
    {
        // Arrange
        var request = new CreateEventRequest("New Event", "Description", DateTime.Parse("2026-10-05 10:00:00"),
            DateTime.Parse("2026-10-05 18:00:00"));

        var eventEntity = new Event
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            StartAt = request.StartAt,
            EndAt = request.EndAt
        };

        var expectedDto = new EventDto(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.StartAt,
            eventEntity.EndAt
        );

        _repositoryMock.Setup(x => x.Add(eventEntity));
        _mapperMock.Setup(x => x.Map<Event>(request)).Returns(eventEntity);
        _mapperMock.Setup(x => x.Map<EventDto>(eventEntity)).Returns(expectedDto);

        // Act
        var result = _service.CreateEvent(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.Id, result.Id);
        Assert.Equal(expectedDto.Title, result.Title);
        Assert.Equal(expectedDto.Description, result.Description);
        Assert.Equal(expectedDto.StartAt, result.StartAt);
        Assert.Equal(expectedDto.EndAt, result.EndAt);
    }

    [Fact]
    public void GetEvents_ShouldReturnAllEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 1", Description = "Description 1",
                StartAt = DateTime.Parse("2026-10-05 10:10:10"), EndAt = DateTime.Parse("2026-10-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 2", Description = "Description 2",
                StartAt = DateTime.Parse("2026-11-05 10:10:10"), EndAt = DateTime.Parse("2026-11-05 20:10:10")
            }
        };

        var expectedDtos = new List<EventDto>
        {
            new(events[0].Id, "Event 1", "Description 1", DateTime.Parse("2026-10-05 10:10:10"),
                DateTime.Parse("2026-10-05 20:10:10")),
            new(events[1].Id, "Event 2", "Description 2", DateTime.Parse("2026-11-05 10:10:10"),
                DateTime.Parse("2026-11-05 20:10:10"))
        };

        _repositoryMock.Setup(x => x.GetAll()).Returns(events);
        _mapperMock.Setup(x => x.Map<IEnumerable<EventDto>>(events)).Returns(expectedDtos);

        // Act
        var result = _service.GetEvents(new EventFilter());

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(expectedDtos.Count, items.Count);

        for (var i = 0; i < expectedDtos.Count; i++)
        {
            Assert.Equal(expectedDtos[i].Id, items[i].Id);
            Assert.Equal(expectedDtos[i].Title, items[i].Title);
            Assert.Equal(expectedDtos[i].Description, items[i].Description);
            Assert.Equal(expectedDtos[i].StartAt, items[i].StartAt);
            Assert.Equal(expectedDtos[i].EndAt, items[i].EndAt);
        }
    }

    [Fact]
    public void GetEventById_WithValidId_ShouldReturnEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var eventEntity = new Event
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Test Description",
            StartAt = DateTime.Parse("2026-10-05 10:00:00"),
            EndAt = DateTime.Parse("2026-10-05 18:00:00")
        };

        var expectedDto = new EventDto(
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.StartAt,
            eventEntity.EndAt
        );

        _repositoryMock.Setup(x => x.GetById(eventId)).Returns(eventEntity);
        _mapperMock.Setup(x => x.Map<EventDto>(eventEntity)).Returns(expectedDto);

        // Act
        var result = _service.GetEvent(eventId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.Id, result.Id);
        Assert.Equal(expectedDto.Title, result.Title);
        Assert.Equal(expectedDto.Description, result.Description);
        Assert.Equal(expectedDto.StartAt, result.StartAt);
        Assert.Equal(expectedDto.EndAt, result.EndAt);
    }

    [Fact]
    public void UpdateEvent_WithValidIdAndData_ShouldUpdateAndReturnEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Event
        {
            Id = eventId,
            Title = "Old Title",
            Description = "Old Description",
            StartAt = DateTime.Parse("2026-10-05 10:00:00"),
            EndAt = DateTime.Parse("2026-10-05 18:00:00")
        };

        var updateRequest = new UpdateEventRequest("Updated Title", "Updated Description",
            DateTime.Parse("2026-11-05 10:00:00"), DateTime.Parse("2026-11-05 18:00:00"));

        var expectedDto = new EventDto(
            eventId,
            updateRequest.Title,
            updateRequest.Description,
            updateRequest.StartAt,
            updateRequest.EndAt
        );

        _repositoryMock.Setup(x => x.GetById(eventId)).Returns(existingEvent);
        _repositoryMock.Setup(x => x.Update(existingEvent));
        _mapperMock.Setup(x => x.Map(updateRequest, existingEvent));
        _mapperMock.Setup(x => x.Map<EventDto>(existingEvent)).Returns(expectedDto);

        // Act
        var result = _service.UpdateEvent(eventId, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.Id, result.Id);
        Assert.Equal(expectedDto.Title, result.Title);
        Assert.Equal(expectedDto.Description, result.Description);
        Assert.Equal(expectedDto.StartAt, result.StartAt);
        Assert.Equal(expectedDto.EndAt, result.EndAt);
    }
    
    [Fact]
    public void DeleteEvent_WithValidId_ShouldRemoveEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var existingEvent = new Event
        {
            Id = eventId, Title = "Test Event", StartAt = DateTime.Parse("2026-11-05 10:00:00"),
            EndAt = DateTime.Parse("2026-11-05 18:00:00")
        };
        var events = new List<Event> { existingEvent };

        _repositoryMock.Setup(x => x.GetById(eventId))
            .Returns(() => events.FirstOrDefault(e => e.Id == eventId));
        _repositoryMock.Setup(x => x.Remove(eventId))
            .Callback<Guid>(id => events.RemoveAll(e => e.Id == id));

        // Act
        _service.DeleteEvent(eventId);
        var deletedEvent = _repositoryMock.Object.GetById(eventId);

        // Assert
        Assert.Null(deletedEvent);
    }

    [Fact]
    public void GetEvents_WithTitleFilter_ShouldReturnFilteredEvents()
    {
        // Arrange
        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Conference 2026", Description = "Tech conference",
                StartAt = DateTime.Parse("2026-10-05 10:10:10"), EndAt = DateTime.Parse("2026-10-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Workshop C#", Description = "C# workshop",
                StartAt = DateTime.Parse("2026-11-05 10:10:10"), EndAt = DateTime.Parse("2026-11-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Meeting", Description = "Team meeting",
                StartAt = DateTime.Parse("2026-12-05 10:10:10"), EndAt = DateTime.Parse("2026-12-05 20:10:10")
            }
        };

        var filter = new EventFilter
        {
            Title = "Conference",
            Page = 1,
            PageSize = 10
        };

        var expectedDtos = new List<EventDto>
        {
            new(events[0].Id, "Conference 2026", "Tech conference",
                DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"))
        };

        _repositoryMock.Setup(x => x.GetAll()).Returns(events);
        _mapperMock.Setup(x => x.Map<IEnumerable<EventDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(expectedDtos);

        // Act
        var result = _service.GetEvents(filter);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Conference 2026", items.First().Title);
        Assert.Equal("Tech conference", items.First().Description);
    }

    [Fact]
    public void GetEvents_WithStartDateFilter_ShouldReturnEventsAfterStartDate()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-11-01");

        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 1", Description = "Description 1",
                StartAt = DateTime.Parse("2026-10-05 10:10:10"), EndAt = DateTime.Parse("2026-10-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 2", Description = "Description 2",
                StartAt = DateTime.Parse("2026-11-05 10:10:10"), EndAt = DateTime.Parse("2026-11-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 3", Description = "Description 3",
                StartAt = DateTime.Parse("2026-12-05 10:10:10"), EndAt = DateTime.Parse("2026-12-05 20:10:10")
            }
        };

        var filter = new EventFilter
        {
            From = startDate,
            Page = 1,
            PageSize = 10
        };

        var expectedDtos = new List<EventDto>
        {
            new(events[1].Id, "Event 2", "Description 2",
                DateTime.Parse("2026-11-05 10:10:10"), DateTime.Parse("2026-11-05 20:10:10")),
            new(events[2].Id, "Event 3", "Description 3",
                DateTime.Parse("2026-12-05 10:10:10"), DateTime.Parse("2026-12-05 20:10:10"))
        };

        _repositoryMock.Setup(x => x.GetAll()).Returns(events);
        _mapperMock.Setup(x => x.Map<IEnumerable<EventDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(expectedDtos);

        // Act
        var result = _service.GetEvents(filter);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal(2, result.TotalItems);
        Assert.All(items, item => Assert.True(item.StartAt >= startDate.Date));
    }

    [Fact]
    public void GetEvents_WithEndDateFilter_ShouldReturnEventsBeforeEndDate()
    {
        // Arrange
        var endDate = DateTime.Parse("2026-11-01");

        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 1", Description = "Description 1",
                StartAt = DateTime.Parse("2026-10-05 10:10:10"), EndAt = DateTime.Parse("2026-10-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 2", Description = "Description 2",
                StartAt = DateTime.Parse("2026-11-05 10:10:10"), EndAt = DateTime.Parse("2026-11-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Event 3", Description = "Description 3",
                StartAt = DateTime.Parse("2026-12-05 10:10:10"), EndAt = DateTime.Parse("2026-12-05 20:10:10")
            }
        };

        var filter = new EventFilter
        {
            To = endDate,
            Page = 1,
            PageSize = 10
        };

        var expectedDtos = new List<EventDto>
        {
            new(events[0].Id, "Event 1", "Description 1",
                DateTime.Parse("2026-10-05 10:10:10"), DateTime.Parse("2026-10-05 20:10:10"))
        };

        _repositoryMock.Setup(x => x.GetAll()).Returns(events);
        _mapperMock.Setup(x => x.Map<IEnumerable<EventDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(expectedDtos);

        // Act
        var result = _service.GetEvents(filter);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(1, result.TotalItems);
        Assert.All(items, item => Assert.True(item.EndAt <= endDate.Date));
    }

    [Fact]
    public void GetEvents_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var events = new List<Event>();

        for (var i = 1; i <= 10; i++)
        {
            events.Add(new Event
            {
                Id = Guid.NewGuid(),
                Title = $"Event {i}",
                Description = $"Description {i}",
                StartAt = DateTime.Now.AddDays(i),
                EndAt = DateTime.Now.AddDays(i).AddHours(2)
            });
        }

        var filter = new EventFilter { Page = 2, PageSize = 3 };

        var expectedDtos = new List<EventDto>
        {
            new(events[3].Id, "Event 4", "Description 4", events[3].StartAt, events[3].EndAt),
            new(events[4].Id, "Event 5", "Description 5", events[4].StartAt, events[4].EndAt),
            new(events[5].Id, "Event 6", "Description 6", events[5].StartAt, events[5].EndAt)
        };

        _repositoryMock.Setup(x => x.GetAll()).Returns(events);
        _mapperMock.Setup(x => x.Map<IEnumerable<EventDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(expectedDtos);

        // Act
        var result = _service.GetEvents(filter);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Equal(3, items.Count);
        Assert.Equal(10, result.TotalItems);
        Assert.Equal(4, result.TotalPages);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public void GetEvents_WithCombinedFilters_ShouldReturnFilteredEvents()
    {
        // Arrange
        var startDate = DateTime.Parse("2026-11-01");
        var endDate = DateTime.Parse("2026-11-30");

        var events = new List<Event>
        {
            new()
            {
                Id = Guid.NewGuid(), Title = "Conference 2026", Description = "Tech conference",
                StartAt = DateTime.Parse("2026-10-05 10:10:10"), EndAt = DateTime.Parse("2026-10-05 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Conference 2026", Description = "Tech conference",
                StartAt = DateTime.Parse("2026-11-15 10:10:10"), EndAt = DateTime.Parse("2026-11-15 20:10:10")
            },
            new()
            {
                Id = Guid.NewGuid(), Title = "Workshop C#", Description = "C# workshop",
                StartAt = DateTime.Parse("2026-11-20 10:10:10"), EndAt = DateTime.Parse("2026-11-20 20:10:10")
            }
        };

        var filter = new EventFilter
        {
            Title = "Conference",
            From = startDate,
            To = endDate,
            Page = 1,
            PageSize = 10
        };

        var expectedDtos = new List<EventDto>
        {
            new(events[1].Id, "Conference 2026", "Tech conference",
                DateTime.Parse("2026-11-15 10:10:10"), DateTime.Parse("2026-11-15 20:10:10"))
        };

        _repositoryMock.Setup(x => x.GetAll()).Returns(events);
        _mapperMock.Setup(x => x.Map<IEnumerable<EventDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(expectedDtos);

        // Act
        var result = _service.GetEvents(filter);

        // Assert
        Assert.NotNull(result);
        var items = result.Items.ToList();
        Assert.Single(items);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Conference 2026", items.First().Title);
        Assert.True(items.First().StartAt >= startDate.Date);
        Assert.True(items.First().EndAt <= endDate.Date);
    }

    // Fail scenarios
    [Fact]
    public void GetEventById_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repositoryMock.Setup(x => x.GetById(nonExistentId)).Returns((Event)null!);

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() => _service.GetEvent(nonExistentId));
        Assert.Equal("Event not found", exception.Message);

        _repositoryMock.Verify(x => x.GetById(nonExistentId), Times.Once);
        _mapperMock.Verify(x => x.Map<EventDto>(It.IsAny<Event>()), Times.Never);
    }

    [Fact]
    public void UpdateEvent_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateEventRequest("Updated Title", "Updated Description",
            DateTime.Parse("2026-10-05 10:00:00"), DateTime.Parse("2026-10-05 18:00:00"));

        _repositoryMock.Setup(x => x.GetById(nonExistentId)).Returns((Event)null!);

        // Act & Assert
        var exception = Assert.Throws<NotFoundException>(() => _service.UpdateEvent(nonExistentId, updateRequest));
        Assert.Equal($"Event with id {nonExistentId} not found", exception.Message);
    }

    [Fact]
    public void CreateEvent_WithInvalidData_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new CreateEventValidator();
        var invalidRequest = new CreateEventRequest("", "Description", DateTime.Parse("2026-10-05 10:00:00"),
            DateTime.Parse("2026-10-05 18:00:00"));
        
        // Act
        var result = validator.Validate(invalidRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Title");
    }

    [Fact]
    public void CreateEvent_WithEndDateEarlierThanStartDate_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new CreateEventValidator();
        var invalidRequest = new CreateEventRequest("Invalid Event", "Description",
            DateTime.Parse("2026-10-05 18:00:00"), DateTime.Parse("2026-10-05 10:00:00"));

        // Act
        var result = validator.Validate(invalidRequest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName is "StartAt" or "EndAt");
    }

    [Fact]
    public void UpdateEvent_WithEndDateEarlierThanStartDate_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new UpdateEventValidator();
        var invalidRequest = new UpdateEventRequest(
            "Updated Event", 
            "Updated Description",
            DateTime.Parse("2026-10-05 18:00:00"),
            DateTime.Parse("2026-10-05 10:00:00")
        );

        // Act
        var result = validator.Validate(invalidRequest);
    
        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName is "StartAt" or "EndAt");
    }
}