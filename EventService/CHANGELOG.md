# Changelog — EventService

Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/), версии — [SemVer](https://semver.org/lang/ru/).
Изменения всей системы — в [корневом CHANGELOG.md](../CHANGELOG.md).

## [0.9.1] — 2026-11-26

### Исправлено

- Из конструктора `Event` убрана строка `AvailableSeats = TotalSeats`: required-свойства заполняются после конструктора,
  поэтому она всегда давала 0. Начальное значение выставляет `EventService.CreateEventAsync`.

## [0.9.0] — 2026-11-25

Сервис выделен из монолита (до этого — версии 0.1.0–0.8.0 в [корневом CHANGELOG.md](../CHANGELOG.md#история-монолита)).

### Добавлено

- Самостоятельный сервис событий: CRUD `/events` (создание, изменение и удаление — только роль `Admin`), фильтры и пагинация.
- Собственная база PostgreSQL `events` (`events-db`) и своя миграция EF Core `Initial` (таблица `Events`).
- **Kafka-подписчик** `BookingConfirmedConsumer` (`BackgroundService`, `Confluent.Kafka`): читает топик `booking-confirmed`
  в группе `event-service`, на каждое сообщение создаёт отдельный scope, фиксирует offset вручную после успешной обработки.
- `BookingConfirmedHandler` (Application): уменьшает `AvailableSeats`; событие не найдено, нет свободных мест или некорректное
  сообщение — пропуск с логированием, без падения подписчика.
- **Идемпотентность**: таблица `ProcessedBookings` (миграция `AddProcessedBookings`) — повторная доставка той же брони не уменьшает места второй раз.
- `KafkaTopicInitializer` — hosted-сервис, создающий топик при старте (3 раздела), если его нет; ошибки не валят запуск.
- Настройки `Kafka:BootstrapServers`, `Kafka:ConsumerGroup`, `Kafka:TopicPartitions`, `Kafka:TopicReplicationFactor`.
- Юнит-тесты `BookingConfirmedHandlerTests`.
- Dockerfile с многоступенчатой сборкой; сервис добавлен в `docker-compose.yml` (порт `5128`, зависит от `events-db` и `kafka`).
- `README.md` сервиса.

### Изменено

- Свободные места уменьшаются только по сообщению из Kafka (согласованность в конечном счёте); прямых вызовов других сервисов нет.
- Интеграционные тесты миграций проверяют таблицы только этого сервиса и отсутствие внешних ключей.
- Swagger получил название «Event Service API».
- Версия сборки `EventService.Presentation` — `0.9.0`.

### Удалено

- Эндпоинт `POST /events/{id}/book` (закомментированный код в `EventController`) — теперь `POST /bookings` в BookingService.
- Лишняя секция `BookingBackground` в конфигурации.
