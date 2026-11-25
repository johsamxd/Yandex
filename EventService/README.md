# EventService

Сервис событий: управление событиями (CRUD) и учёт свободных мест. Количество свободных мест уменьшается по сообщению
`BookingConfirmed`, которое приходит из Kafka от [BookingService](../BookingService/README.md). Описание всей системы —
в [корневом README](../README.md), история изменений — в [CHANGELOG.md](CHANGELOG.md).

| | |
|---|---|
| Порт | `5128` (в Docker контейнер слушает `8080`) |
| База данных | PostgreSQL `events` (`events-db`, порт хоста `5434`) |
| Kafka | подписчик топика `booking-confirmed`, группа `event-service`; создаёт топик при старте |
| Swagger | <http://localhost:5128/swagger> |

## API

| Метод | Endpoint | Доступ | Описание | Ответ |
|---|---|---|---|---|
| GET | `/events` | все | Список с фильтрами (`title`, `from`, `to`) и пагинацией (`page`, `pageSize`) | `200` |
| GET | `/events/{id}` | все | Событие по id | `200` / `404` |
| POST | `/events` | **Admin** | Создать событие (`title`, `description`, `startAt`, `endAt`, `totalSeats`) | `201` / `400` / `401` / `403` |
| PUT | `/events/{id}` | **Admin** | Обновить событие | `200` / `400` / `404` / `401` / `403` |
| DELETE | `/events/{id}` | **Admin** | Удалить событие | `204` / `404` / `401` / `403` |

Управление событиями защищено `[Authorize(Roles = "Admin")]`; токен выдаёт UserService. Свободные места напрямую изменить
нельзя: `availableSeats` стартует равным `totalSeats` и уменьшается только обработчиком сообщений из Kafka.

> Эндпоинта бронирования (`POST /events/{id}/book`) в этом сервисе больше нет — бронями занимается BookingService.

## Подписка на Kafka

```text
KafkaTopicInitializer ──(создаёт топик, если его нет)──▶ BookingConfirmedConsumer ──▶ BookingConfirmedHandler
   hosted-сервис, стартует первым                      BackgroundService                  Application-слой
```

- `KafkaTopicInitializer` (Infrastructure) через админ-клиент создаёт топик `booking-confirmed` (разделов — `Kafka:TopicPartitions`,
  репликация — `Kafka:TopicReplicationFactor`). `TopicAlreadyExists` — не ошибка; если создать не удалось (10 попыток по 3 с),
  сервис всё равно запускается.
- `BookingConfirmedConsumer` (Infrastructure, `BackgroundService`) подписывается на топик, в цикле читает сообщения и
  десериализует `BookingConfirmed`. Блокирующий `Consume` вынесен в `Task.Run`, чтобы не задерживать старт приложения.
  Так как `BackgroundService` — singleton, а репозитории и `DbContext` — scoped, **на каждое сообщение создаётся отдельный
  scope**.
- `BookingConfirmedHandler` (Application) выполняет бизнес-логику:
  1. повторная бронь (`BookingId` уже есть в `ProcessedBookings`) — пропуск (**идемпотентность**);
  2. событие не найдено — пропуск с `Warning`;
  3. мест не хватает — пропуск с `Warning`;
  4. иначе `AvailableSeats -= Seats` и запись в `ProcessedBookings` — **одним `SaveChanges`**.
- Offset фиксируется вручную **после** успешной обработки (`EnableAutoCommit = false`), группа читает топик с начала
  (`AutoOffsetReset = Earliest`). Некорректный JSON пропускается; при непредвиденной ошибке (например, недоступна БД)
  консьюмер возвращается к тому же сообщению и повторяет через 3 с.

## Данные

| Таблица | Назначение |
|---|---|
| `Events` | События: `Id`, `Title`, `Description`, `StartAt`, `EndAt`, `TotalSeats`, `AvailableSeats`, `CreatedAt` |
| `ProcessedBookings` | Inbox для идемпотентности: `BookingId` (PK), `EventId`, `ProcessedAt` |

Миграции EF Core (`Initial`, `AddProcessedBookings`) лежат в `EventService.Infrastructure/Persistence/Migrations`,
применяются при старте сервиса. Связи с таблицами других сервисов — только по идентификаторам, без внешних ключей.

## Конфигурация

| Ключ | Описание | По умолчанию |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Подключение к `events` | `Host=localhost;Port=5434;Database=events;...` |
| `Jwt:Secret` / `Issuer` / `Audience` | Параметры проверки JWT — **как в UserService** | см. `appsettings.json` |
| `Kafka:BootstrapServers` | Адрес брокера | `localhost:9092` (в Docker — `kafka:29092`) |
| `Kafka:ConsumerGroup` | Группа потребителей | `event-service` |
| `Kafka:TopicPartitions` | Разделов у создаваемого топика | `3` |
| `Kafka:TopicReplicationFactor` | Фактор репликации топика | `1` |

В Docker переопределяется переменными окружения: `Kafka__BootstrapServers`, `Kafka__ConsumerGroup`,
`ConnectionStrings__DefaultConnection`, `Jwt__*`.

## Запуск

```bash
# вместе со всей системой
docker compose up --build event-service     # из корня репозитория (поднимет Kafka и events-db)

# локально (нужны events-db и Kafka: docker compose up -d zookeeper kafka events-db)
dotnet run --project EventService/EventService.Presentation
```

## Структура

```text
EventService.Domain          # Event, ProcessedBooking
EventService.Application     # IEventService, IBookingConfirmedHandler + BookingConfirmedHandler, абстракции репозиториев
EventService.Infrastructure  # EF Core (AppDbContext, миграции), репозитории, Messaging/ (KafkaTopicInitializer, BookingConfirmedConsumer)
EventService.Presentation    # EventController, Program.cs, Swagger
tests/                       # UnitTests (в т. ч. BookingConfirmedHandlerTests), IntegrationTests
Dockerfile                   # многоступенчатая сборка (контекст — корень репозитория)
```

## Тесты

```bash
dotnet test EventService/EventService.slnx
```
