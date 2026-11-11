# Yandex API

Учебный проект для [Яндекс.Практикум](https://practicum.yandex.ru/middle-csharp/)

## Описание

API для сервиса бронирования

## Используемые технологии

* .NET 10.0
* ASP.NET Core Web API
* PostgreSQL + Entity Framework Core
* Swagger / OpenAPI
* AutoMapper
* FluentValidation
* TestContainers
* xUnit

## Установка и запуск

1. Клонирование репозитория

```
git clone https://github.com/johsamxd/Yandex.git
cd Yandex
```

2. Настройка подключения к PostgreSQL

Строка подключения задаётся в `appsettings.json` (или `appsettings.Development.json`) проекта `Yandex.Presentation`, в
секции
`ConnectionStrings`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=yandex_db;Username=postgres;Password=your_password"
  }
}
```

3. Применение миграций

Схема базы данных управляется миграциями EF Core (а не EnsureCreated()).

Если миграций ещё нет (новая копия репозитория без папки Migrations), создайте начальную миграцию:

```
dotnet ef migrations add --project Yandex.Infrastructure\Yandex.Infrastructure.csproj --startup-project Yandex.Presentation\Yandex.Presentation.csproj --context Yandex.Infrastructure.Persistence.AppDbContext --configuration Debug Initial --output-dir Persistence/Migrations
```

Для выполнения команд dotnet ef установите инструмент, если он ещё не установлен:

```
dotnet tool install --global dotnet-ef
```

4. Запуск приложения

```
dotnet run --project Yandex.Presentation
```

При старте:

* запускается BookingBackgroundService, который каждые 5 секунд обрабатывает Pending-брони.

## Запуск тестов

```
dotnet test
```

Юнит-тесты не требуют PostgreSQL. Они используют InMemory-провайдер EF Core:

* для каждого тестового класса создаётся уникальная in-memory база (Guid.NewGuid().ToString()), чтобы тесты не влияли
  друг на друга;
* DI-контейнер собирается через ServiceCollection, в него регистрируются AppDbContext с UseInMemoryDatabase(dbName) и
  тестируемые сервисы;
* для тестов на конкурентность каждый параллельный запрос выполняется в отдельном scope (IServiceScope), что эмулирует
  отдельный HTTP-запрос.

Интеграционные тесты работают с реальным PostgreSQL, который автоматически поднимается в Docker-контейнере через
Testcontainers. Они проверяют методы репозиториев (EventRepository, BookingRepository) на настоящей базе данных:
фильтрацию, пагинацию, сортировку, CRUD-операции и поведение при отсутствии записей.

Требования для запуска интеграционных тестов:

* Docker должен быть запущен (Docker Desktop на Windows/macOS, dockerd на Linux). Testcontainers сам скачает образ
  postgres:17-alpine и запустит контейнер;
* доступ к Docker Hub для загрузки образа PostgreSQL и служебного образа testcontainers/ryuk.

## Структура проекта

Решение разделено на четыре проекта по принципам чистой (луковой) архитектуры. Зависимости направлены
строго внутрь: `Presentation → Infrastructure → Application → Domain`. Domain не зависит ни от чего,
Infrastructure и Presentation знают только о внутренних слоях.

## API Документация

- [Base URL](http://localhost:5128)
- [Swagger](http://localhost:5128/swagger/index.html)

### Мероприятия (Events)

| Метод  | Endpoint            | Описание                                           | Статус                                      |
|--------|---------------------|----------------------------------------------------|---------------------------------------------|
| GET    | `/events`           | Получить список событий с фильтрацией и пагинацией | 200 OK                                      |
| GET    | `/events/{id}`      | Получить событие по ID                             | 200 OK / 404 Not Found                      |
| POST   | `/events`           | Создать новое событие                              | 201 Created / 400 Bad Request               |
| PUT    | `/events/{id}`      | Обновить событие                                   | 200 OK / 404 Not Found                      |
| DELETE | `/events/{id}`      | Удалить событие                                    | 204 No Content / 404 Not Found              |
| POST   | `/events/{id}/book` | Создать бронь на событие                           | 202 Accepted / 404 Not Found / 409 Conflict |

### Бронирования (Bookings)

| Метод | Endpoint         | Описание                    | Статус                 |
|-------|------------------|-----------------------------|------------------------|
| GET   | `/bookings/{id}` | Получить информацию о брони | 200 OK / 404 Not Found |

## Модель данных

### Модель Event

| Поле             | Тип        | Описание                                        |
|------------------|------------|-------------------------------------------------|
| `Id`             | `Guid`     | Уникальный идентификатор события                |
| `Title`          | `string`   | Название события (обязательное)                 |
| `Description`    | `string?`  | Описание события (опциональное)                 |
| `StartAt`        | `DateTime` | Дата и время начала события (обязательное)      |
| `EndAt`          | `DateTime` | Дата и время окончания события (обязательное)   |
| `CreatedAt`      | `DateTime` | Дата и время создания записи                    |
| `TotalSeats`     | `int`      | Общее количество мест на событии (обязательное) |
| `AvailableSeats` | `int`      | Текущее количество свободных мест               |

### Модель Booking

| Поле          | Тип             | Описание                       |
|---------------|-----------------|--------------------------------|
| `Id`          | `Guid`          | Уникальный идентификатор брони |
| `EventId`     | `Guid`          | Идентификатор события          |
| `Status`      | `BookingStatus` | Текущий статус брони           |
| `CreatedAt`   | `DateTime`      | Дата и время создания          |
| `ProcessedAt` | `DateTime?`     | Дата и время обработки         |

### Статусы бронирования (BookingStatus)

| Статус      | Описание                         |
|-------------|----------------------------------|
| `Pending`   | Бронь создана, ожидает обработки |
| `Confirmed` | Бронь подтверждена               |
| `Rejected`  | Бронь отклонена                  |

## Сценарий использования: Бронирование места на событие

### 1. Создание события

```http request
POST /events
Content-Type: application/json

{
    "title": "Конференция по C#",
    "description": "Ежегодная конференция разработчиков",
    "startAt": "2026-12-01T10:00:00Z",
    "endAt": "2026-12-01T18:00:00Z"
}
```

Ответ:

```http request
201 Created
{
    "id": "3f8c9a2b-1d4e-5f6a-7b8c-9d0e1f2a3b4c",
    "title": "Конференция по C#",
    "description": "Ежегодная конференция разработчиков",
    "startAt": "2026-12-01T10:00:00Z",
    "endAt": "2026-12-01T18:00:00Z"
}
```

### 2. Создание брони

```http request
POST /events/3f8c9a2b-1d4e-5f6a-7b8c-9d0e1f2a3b4c/book
```

Ответ:

```http request
202 Accepted
Location: /bookings/7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d
{
    "id": "7a8b9c0d-1e2f-3a4b-5c6d-7e8f9a0b1c2d",
    "eventId": "3f8c9a2b-1d4e-5f6a-7b8c-9d0e1f2a3b4c",
    "status": "Pending",
    "createdAt": "2026-08-12T10:00:00Z",
    "processedAt": null
}
```

### 3. Бронирование при отсутствии мест

Если все места заняты (`AvailableSeats == 0`), `POST /events/{id}/book` возвращает:

```http request
409 Conflict
{
    "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
    "title": "No available seats",
    "status": 409,
    "detail": "No available seats for this event",
    "instance": "/events/3f8c9a2b-1d4e-5f6a-7b8c-9d0e1f2a3b4c/book"
}
```

## Фоновая обработка бронирований

- `BookingBackgroundService` запускается при старте приложения
- Каждые **5 секунд** опрашивает хранилище
- Находит все брони в статусе `Pending`
- Для каждой брони:
    - Имитируется обращение к внешней системе (задержка **2 секунды**)
    - Статус меняется на `Confirmed` или `Rejected`
    - Заполняется поле `ProcessedAt`
- Время выполнения: до 2 секунд на бронь

## Примитивы синхронизации

В проекте используются два примитива синхронизации под разные задачи.

**`BookingService.CreateBookingAsync` — `SemaphoreSlim(1, 1)`**

Статический `SemaphoreSlim BookingSemaphore = new(1, 1)` сериализует проверку и резервирование места: только один
параллельный запрос одновременно проходит критическую секцию (чтение события → `TryReserveSeats` → `AddAsync` →
`SaveChangesAsync`). Это защищает от овербукинга, когда несколько параллельных запросов одновременно пытаются занять
последнее место.

Внутри критической секции есть `await SaveChangesAsync(...)`, поэтому используется `SemaphoreSlim`, а не `lock` (в C#
`lock` запрещает `await` внутри блока). `WaitAsync` вызывается до `try`, `Release` — в `finally`, чтобы счётчик не
«протекал» при отмене или исключении. Семафор объявлен `static`, чтобы быть общим для всех экземпляров `BookingService`
в процессе — иначе каждый DI-scope получил бы свой семафор, и взаимное исключение между запросами не работало бы.

Ограничение: `SemaphoreSlim` работает только в пределах одного процесса. При нескольких инстансах приложения взаимное
исключение нужно обеспечивать на уровне БД — например, транзакцией с `UPDATE ... WHERE AvailableSeats > 0` (или
`SELECT ... FOR UPDATE`), а не семафором в памяти.

**`BookingBackgroundService` — `SemaphoreSlim(3, 3)`**

Семафор ограничивает параллелизм фоновой обработки броней тремя одновременно, чтобы не перегружать внешнюю систему и не
создавать лишние `DbContext`. Здесь именно семафор, а не `lock`, потому что внутри критической секции есть `await` (
имитация внешнего вызова + `SaveChangesAsync`). `WaitAsync` вызывается до `try`, `Release` — в `finally`, чтобы счётчик
не «протекал» при отмене.

Дополнительно каждая бронь обрабатывается в отдельном `scope` (`Task.WhenAll` + `CreateScope` на задачу), потому что
`DbContext` не потокобезопасен, а семафор `(3,3)` ограничивает число одновременных контекстов.

## Список изменений:

### 0.7.0

**Изменено**

- Проект Web переименован в Presentetion
- Проект Infrastructure теперь зависит от Application все абстракции из Domain перенесены в Application

### 0.6.0

**Добавлено:**

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

**Изменено:**

- `IEventRepository.UpdateAsync` удалён: в `BookingService` событие уже отслеживается контекстом после `GetByIdAsync`,
  изменения сохраняются через `SaveChangesAsync`
- `EventRepository.GetFilteredAsync`: `To`-фильтр включает весь день (`EndAt < To.Date.AddDays(1)`), границы приводятся
  к UTC (`DateTime.SpecifyKind(..., Utc)`) — исправлен 500 при дате без `Z` из Swagger

**Удалено:**

- `EntityRepository<T>` — заменён на `EventRepository` и `BookingRepository`
- `EventRepository.UpdateAsync` (метод и тест `UpdateAsync_ExistingEvent_UpdatesFields`)

### 0.5.0

**Добавлено:**

- PostgreSQL + Entity Framework Core:
- `AppDbContext`, `EventConfiguration`, `BookingConfiguration`

**Изменено:**

- Сервисы (`EventService`, `BookingService`) переведены с in-memory коллекций на `IEventRepository` /
  `IBookingRepository` + `IUnitOfWork`
- `BookingBackgroundService` переведён на `IServiceScopeFactory`: каждая бронь обрабатывается в отдельном scope,
  `DbContext` не шарится между задачами
- Юнит-тесты (`EventServiceTests`, `BookingServiceTests`) адаптированы под новую архитектуру: DI-контейнер с
  `AppDbContext` (InMemory для юнит-тестов), репозитории и `IUnitOfWork`
- Синхронизация в `BookingService.CreateBookingAsync`: `lock` заменён на `SemaphoreSlim(1, 1)` из-за асинхронного
  `SaveChangesAsync`

**Удалено:**

- In-memory коллекции событий и броней в сервисах
- `EnsureCreated()` при старте приложения (заменён на миграции)

### 0.4.0

**Добавлено:**

- Свойства `TotalSeats` и `AvailableSeats` в сущность Event
- Возврат `409 Conflict` при бронировании события без свободных мест

**Изменено:**

- `POST /events` теперь требует `totalSeats` в теле запроса
- `POST /events/{id}/book` теперь может возвращать `409 Conflict`
- `BookingService` и `BookingBackgroundService` теперь потокобезопасны

**Удалено:**

- Нет

### 0.3.0

**Добавлено:**

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

**Изменено:**

- Нет

**Удалено:**

- Нет

### 0.2.0

**Добавлено:**

- Пагинация для GET `/events` с параметрами `page` и `pageSize`
- Фильтрация для GET `/events` по параметрам:
    - `title` - поиск по названию (регистронезависимый, частичное совпадение)
    - `startAt` - события, начинающиеся не раньше указанной даты
    - `endAt` - события, заканчивающиеся не позже указанной даты
- Middleware для глобальной обработки исключений с единым форматом ответа (Problem Details RFC 7807)
- Логирование ошибок с использованием `ILogger`
- Набор юнит-тестов для `EventService` с покрытием успешных и неуспешных сценариев
- `PaginatedResult<T>` DTO для возврата пагинированных данных

**Изменено:**

- Метод `GetEvents` в `EventService` расширен поддержкой фильтрации и пагинации

**Удалено:**

- `ExceptionFilter` (заменен на глобальный middleware для обработки ошибок)

### 0.1.0

**Добавлено:**

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

**Изменено:**

- Нет

**Удалено:**

- Нет