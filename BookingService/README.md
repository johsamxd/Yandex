# BookingService

Сервис броней: создание и отмена броней. После подтверждения брони публикует в Kafka событие `BookingConfirmed`;
сервис [EventService](../EventService/README.md) по нему уменьшает количество свободных мест. BookingService **не вызывает
EventService напрямую**. Описание всей системы — в [корневом README](../README.md), история изменений —
в [CHANGELOG.md](CHANGELOG.md).

| | |
|---|---|
| Порт | `5130` (в Docker контейнер слушает `8080`) |
| База данных | PostgreSQL `bookings` (`bookings-db`, порт хоста `5435`) |
| Kafka | издатель топика `booking-confirmed` (ключ сообщения — `EventId`) |
| Swagger | <http://localhost:5130/swagger> |

## API

Все эндпоинты требуют JWT (`401` без токена). Токен выдаёт UserService; id пользователя и роль читаются из claims.

| Метод | Endpoint | Описание | Ответ |
|---|---|---|---|
| POST | `/bookings` | Создать бронь. Тело: `{ "eventId": "<guid>" }` | `202 Accepted` + `Location` / `400` / `401` / `409` |
| GET | `/bookings/{id}` | Статус брони (своя; админ — любая) | `200` / `401` / `403` / `404` |
| DELETE | `/bookings/{id}` | Отменить бронь (своя; админ — любую). Повторная отмена идемпотентна | `204` / `401` / `403` / `404` |

Бизнес-правило: у пользователя не больше **10 активных** броней (`Pending` + `Confirmed`) → `409 Conflict`.

Статусы брони: `Pending` → `Confirmed` / `Rejected`; из любого — `Cancelled`.

## Публикация в Kafka

Бронь создаётся как `Pending`. Фоновый `BookingBackgroundService` каждые 5 с обрабатывает такие брони
(имитация обращения к внешней системе — 2 с, не более 3 одновременно). Для каждой брони:

1. `Confirm()` и `SaveChanges` — **сначала статус сохраняется в базу сервиса**;
2. только после этого `IBookingEventPublisher.PublishBookingConfirmedAsync(...)` отправляет `BookingConfirmed` в Kafka.

Если публикация не удалась, статус брони **не откатывается** — делается ещё две попытки (пауза 1 и 2 с), затем ошибка пишется в лог
(см. ограничения в [корневом README](../README.md#известные-ограничения)).

| Слой | Что находится |
|---|---|
| Application | `IBookingEventPublisher` — абстракция публикатора; `BookingBackgroundService` |
| Infrastructure | `KafkaBookingEventPublisher` — реализация на `Confluent.Kafka` |
| Shared.Contracts | `BookingConfirmed` и `KafkaTopics.BookingConfirmed` — общие с EventService |

Особенности `KafkaBookingEventPublisher`:

- продюсер — тяжёлый потокобезопасный объект, поэтому класс регистрируется как **singleton** и реализует `IDisposable`
  (при остановке приложения буфер сбрасывается через `Flush`, затем продюсер освобождается);
- сообщение сериализуется в JSON (camelCase) и отправляется с ключом **`EventId`**: все брони одного события попадают в один
  раздел и обрабатываются подписчиком по порядку;
- `Acks = All`, `EnableIdempotence = true` — защита от потерь и дублей на стороне клиента Kafka.

Сообщение `BookingConfirmed`: `bookingId`, `eventId`, `userId`, `seats` (сейчас всегда `1`), `confirmedAt`.

## Данные

Таблица `Bookings`: `Id`, `EventId`, `UserId`, `Status`, `ProcessedAt`, `CreatedAt`. На `EventId` и `UserId` нет внешних
ключей — это идентификаторы из других сервисов. Миграция EF Core `Initial` лежит в
`BookingService.Infrastructure/Persistence/Migrations` и применяется при старте сервиса.

## Конфигурация

| Ключ | Описание | По умолчанию |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Подключение к `bookings` | `Host=localhost;Port=5435;Database=bookings;...` |
| `Jwt:Secret` / `Issuer` / `Audience` | Параметры проверки JWT — **как в UserService** | см. `appsettings.json` |
| `Kafka:BootstrapServers` | Адрес брокера | `localhost:9092` (в Docker — `kafka:29092`) |
| `BookingBackground:PollingIntervalSeconds` | Интервал опроса `Pending`-броней | `5` |
| `BookingBackground:ProcessingDelaySeconds` | Имитация обработки одной брони | `2` |

В Docker переопределяется переменными окружения: `Kafka__BootstrapServers`, `ConnectionStrings__DefaultConnection`, `Jwt__*`.

## Запуск

```bash
# вместе со всей системой
docker compose up --build booking-service  # из корня репозитория (поднимет Kafka и bookings-db)

# локально (нужны bookings-db и Kafka: docker compose up -d zookeeper kafka bookings-db)
dotnet run --project BookingService/BookingService.Presentation
```

> Топик `booking-confirmed` создаёт EventService при старте — запускайте его, чтобы события доходили до подписчика.

## Структура

```text
BookingService.Domain          # Booking, BookingStatus, доменные исключения
BookingService.Application     # IBookingService, BookingBackgroundService, IBookingEventPublisher, валидаторы
BookingService.Infrastructure  # EF Core (AppDbContext, миграции), BookingRepository, Messaging/KafkaBookingEventPublisher
BookingService.Presentation    # BookingController, Program.cs, Swagger
tests/                         # UnitTests, IntegrationTests
Dockerfile                     # многоступенчатая сборка (контекст — корень репозитория)
```

## Тесты

```bash
dotnet test --solution BookingService/BookingService.slnx
```
