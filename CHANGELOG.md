# Changelog

Все заметные изменения проекта. Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/), версии — [SemVer](https://semver.org/lang/ru/).

У каждого сервиса есть собственный журнал изменений:
[UserService](UserService/CHANGELOG.md) · [EventService](EventService/CHANGELOG.md) · [BookingService](BookingService/CHANGELOG.md).
Здесь записываются изменения всей системы: структура решения, общий код (`Shared`), Kafka-контракты, Docker и документация.

## [0.9.0] — 2026-11-25

Спринт 9: монолит разделён на микросервисы, добавлен асинхронный обмен через Apache Kafka.

### Добавлено

- Три независимых сервиса — `UserService`, `EventService`, `BookingService`; у каждого свои слои
  Domain / Application / Infrastructure / Presentation, своя база PostgreSQL, свой `AppDbContext` и своя миграция EF Core.
- Проект `Shared.Contracts` — общий контракт события `BookingConfirmed` и имя топика `KafkaTopics.BookingConfirmed`
  (`booking-confirmed`), на который ссылаются и издатель, и подписчик.
- `KafkaOptions` в `Shared.Application` — секция конфигурации `Kafka` (`BootstrapServers`, `ConsumerGroup`, параметры топика).
- Публикация `BookingConfirmed` в Kafka из BookingService (`Confluent.Kafka`, ключ сообщения — `EventId`).
- Подписчик в EventService: фоновое чтение топика, уменьшение `AvailableSeats`, ручная фиксация offset,
  создание топика при старте, идемпотентность через таблицу `ProcessedBookings`.
- `POST /bookings` в BookingService (создание брони; раньше было `POST /events/{id}/book` в монолите).
- `docker-compose.yml`: Zookeeper, Kafka, три базы и три сервиса, запуск одной командой `docker compose up --build`;
  общие настройки JWT вынесены в `x-jwt-env`.
- Dockerfile с многоступенчатой сборкой для каждого сервиса, `.dockerignore`.
- Корневое решение `Yandex.slnx` — `dotnet build` / `dotnet test` из корня репозитория.
- Документация: новый корневой `README.md`, `README.md` и `CHANGELOG.md` для каждого сервиса.
- Юнит-тесты `BookingConfirmedHandlerTests` (уменьшение мест, повторная доставка, событие не найдено, нет мест).

### Изменено

- Сервисы не вызывают друг друга по HTTP: Bookings больше не знает про устройство Events, места у события уменьшаются
  только по сообщению из Kafka (согласованность в конечном счёте).
- Подтверждение брони: сначала статус сохраняется в БД Bookings, затем публикуется событие; сбой публикации не откатывает бронь.
- JWT выдаёт только UserService, EventService и BookingService проверяют тот же токен (общие `Secret`, `Issuer`, `Audience`).
- Интеграционные тесты миграций адаптированы под базы сервисов: проверяется, что в базе только таблицы своего сервиса и нет внешних ключей.
- Порт BookingService в `launchSettings.json` — `5130` (совпадал с EventService).
- Swagger каждого сервиса получил собственное название и описание.

### Удалено

- Связи между сущностями разных сервисов (навигационные свойства, внешние ключи): остались только идентификаторы.
- Закомментированный код бронирования в `EventController` и лишняя секция `BookingBackground` в UserService/EventService.

## История монолита

Версии ниже относятся к монолитному приложению (до разделения на сервисы); перенесены из прежнего README.

## [0.8.0]

### Добавлено

- Аутентификация и авторизация на основе JWT:
    - сущность `User` (логин, SHA-256 хеш пароля, роль), `UserRole { User, Admin }`
    - `IPasswordHasher` / `Sha256PasswordHasher` — хеширование и проверка паролей
    - `IJwtTokenGenerator` / `JwtTokenGenerator` — генерация подписанного JWT (claims: `NameIdentifier`, `Name`, `Role`)
    - `POST /auth/register`, `POST /auth/login`
    - JWT-аутентификация в `Program.cs` (`AddJwtBearer`), Swagger с кнопкой **Authorize**
- Ролевая модель: `[Authorize]` на бронировании, `[Authorize(Roles = "Admin")]` на управлении событиями
- Доменные исключения: `EventStartedException` (400), `BookingLimitExceededException` (409), `ForbiddenException` (403)
- Бизнес-правила: запрет бронирования прошедшего события, лимит 10 активных броней, отмена только своей брони (админ — любой)
- `BookingStatus.Cancelled` и метод `Booking.Cancel()` с защитой от повторной отмены
- `CountActiveByUserAsync` в `IBookingRepository`
- Миграция `AddUsersAndBookingUserId`: таблица `Users`, уникальный индекс на `Login`, FK `Bookings.UserId → Users.Id`

### Изменено

- `CreateBookingAsync` принимает `userId`; `CancelBookingAsync` и `GetBookingByIdAsync` — `userId` и проверку прав
- `BookingBackgroundService` корректно обрабатывает отмену (не подтверждает `Cancelled`)
- README: описана ролевая модель, инструкция по JWT через Swagger, настройка секрета JWT

## [0.7.0]

### Изменено

- Проект Web переименован в Presentetion
- Проект Infrastructure теперь зависит от Application все абстракции из Domain перенесены в Application

## [0.6.0]

### Добавлено

- Миграции EF Core (`InitialCreate`): схема БД управляется через `dotnet ef migrations` / `dotnet ef database update`
- `EventRepository` и `BookingRepository` — индивидуальные репозитории вместо generic-репозитория:
    - `EventRepository`: `GetByIdAsync`, `GetFilteredAsync` (фильтры + пагинация + стабильная сортировка
      `OrderBy(StartAt).ThenBy(Id)`), `CountAsync`, `AddAsync`, `RemoveAsync`
    - `BookingRepository`: `GetByIdAsync`, `GetFilteredAsync` (фильтр по статусу), `AddAsync`, `UpdateAsync`
- Интеграционные тесты с Testcontainers:
    - `PostgresFixture` — общий контейнер PostgreSQL на всё решение
    - `EventRepositoryTests` — фильтры (`Title`, `From`, `To`), пагинация, сортировка, CRUD, `Unspecified`-даты из
      Swagger
    - `BookingRepositoryTests` — фильтрация по статусу, CRUD
    - `MigrationTests` — проверка применённых миграций, наличия таблиц, внешнего ключа и каскадного удаления

### Изменено

- `IEventRepository.UpdateAsync` удалён: в `BookingService` событие уже отслеживается контекстом после `GetByIdAsync`,
  изменения сохраняются через `SaveChangesAsync`
- `EventRepository.GetFilteredAsync`: `To`-фильтр включает весь день (`EndAt < To.Date.AddDays(1)`), границы приводятся
  к UTC (`DateTime.SpecifyKind(..., Utc)`) — исправлен 500 при дате без `Z` из Swagger

### Удалено

- `EntityRepository<T>` — заменён на `EventRepository` и `BookingRepository`
- `EventRepository.UpdateAsync` (метод и тест `UpdateAsync_ExistingEvent_UpdatesFields`)

## [0.5.0]

### Добавлено

- PostgreSQL + Entity Framework Core:
- `AppDbContext`, `EventConfiguration`, `BookingConfiguration`

### Изменено

- Сервисы (`EventService`, `BookingService`) переведены с in-memory коллекций на `IEventRepository` /
  `IBookingRepository` + `IUnitOfWork`
- `BookingBackgroundService` переведён на `IServiceScopeFactory`: каждая бронь обрабатывается в отдельном scope,
  `DbContext` не шарится между задачами
- Юнит-тесты (`EventServiceTests`, `BookingServiceTests`) адаптированы под новую архитектуру: DI-контейнер с
  `AppDbContext` (InMemory для юнит-тестов), репозитории и `IUnitOfWork`
- Синхронизация в `BookingService.CreateBookingAsync`: `lock` заменён на `SemaphoreSlim(1, 1)` из-за асинхронного
  `SaveChangesAsync`

### Удалено

- In-memory коллекции событий и броней в сервисах
- `EnsureCreated()` при старте приложения (заменён на миграции)

## [0.4.0]

### Добавлено

- Свойства `TotalSeats` и `AvailableSeats` в сущность Event
- Возврат `409 Conflict` при бронировании события без свободных мест

### Изменено

- `POST /events` теперь требует `totalSeats` в теле запроса
- `POST /events/{id}/book` теперь может возвращать `409 Conflict`
- `BookingService` и `BookingBackgroundService` теперь потокобезопасны

### Удалено

- Нет

## [0.3.0]

### Добавлено

- **Модуль Booking** — функционал управления бронированиями мероприятий

    - **Модель данных `Booking`**
        - `Id` (Guid) — уникальный идентификатор
        - `EventId` (Guid) — связь с событием
        - `Status` (BookingStatus) — статус брони
        - `CreatedAt` (DateTime) — дата создания
        - `ProcessedAt` (DateTime?) — дата обработки

    - **Статусы бронирований**
        - `Pending` — ожидает обработки
        - `Confirmed` — подтверждена
        - `Rejected` — отклонена

    - **Эндпоинты API**
        - `POST /events/{id}/book` — создание брони
            - Возврат `202 Accepted` с `Location`-заголовком
            - Мгновенный ответ, асинхронная обработка
        - `GET /bookings/{id}` — получение статуса брони
            - Возврат `200 OK` с данными брони
            - `404 Not Found` при отсутствии брони

    - **Фоновая обработка**
        - `BookingBackgroundService` — автоматическая обработка броней
        - Интервал опроса: 5 секунд
        - Имитация внешнего вызова: 2 секунды
        - Автоматическое обновление статуса и поля `ProcessedAt`

    - **Архитектура**
        - Паттерн "быстрый ответ + отложенная обработка" (Async Request-Reply)
        - `202 Accepted` для асинхронных операций
        - `Location`-заголовок для отслеживания статуса

### Изменено

- Нет

### Удалено

- Нет

## [0.2.0]

### Добавлено

- Пагинация для GET `/events` с параметрами `page` и `pageSize`
- Фильтрация для GET `/events` по параметрам:
    - `title` - поиск по названию (регистронезависимый, частичное совпадение)
    - `startAt` - события, начинающиеся не раньше указанной даты
    - `endAt` - события, заканчивающиеся не позже указанной даты
- Middleware для глобальной обработки исключений с единым форматом ответа (Problem Details RFC 7807)
- Логирование ошибок с использованием `ILogger`
- Набор юнит-тестов для `EventService` с покрытием успешных и неуспешных сценариев
- `PaginatedResult<T>` DTO для возврата пагинированных данных

### Изменено

- Метод `GetEvents` в `EventService` расширен поддержкой фильтрации и пагинации

### Удалено

- `ExceptionFilter` (заменен на глобальный middleware для обработки ошибок)

## [0.1.0]

### Добавлено

- REST API для управления мероприятиями:
    - `POST /events` - создание мероприятия
    - `GET /events` - получение всех мероприятий
    - `GET /events/{id}` - получение мероприятия по ID
    - `PUT /events/{id}` - обновление мероприятия
    - `DELETE /events/{id}` - удаление мероприятия
- Swagger/OpenAPI документация
- AutoMapper для маппинга DTO
- FluentValidation для валидации запросов
- Базовая структура проекта (Domain, Application, Web слои)

### Изменено

- Нет

### Удалено

- Нет
