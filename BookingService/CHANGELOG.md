# Changelog — BookingService

Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/), версии — [SemVer](https://semver.org/lang/ru/).
Изменения всей системы — в [корневом CHANGELOG.md](../CHANGELOG.md).

## [0.9.0] — 2026-11-25

Сервис выделен из монолита (до этого — версии 0.1.0–0.8.0 в [корневом CHANGELOG.md](../CHANGELOG.md#история-монолита)).

### Добавлено

- Самостоятельный сервис броней: `POST /bookings`, `GET /bookings/{id}`, `DELETE /bookings/{id}`; все эндпоинты требуют JWT.
- Собственная база PostgreSQL `bookings` (`bookings-db`) и своя миграция EF Core `Initial` (таблица `Bookings`, без внешних ключей).
- **Kafka-издатель**: абстракция `IBookingEventPublisher` (Application) и реализация `KafkaBookingEventPublisher`
  (Infrastructure, `Confluent.Kafka`). Продюсер зарегистрирован как singleton и освобождается через `IDisposable`;
  ключ сообщения — `EventId`; `Acks = All`, идемпотентный продюсер.
- Публикация `BookingConfirmed` из `BookingBackgroundService` сразу после подтверждения брони.
- Настройка `Kafka:BootstrapServers`.
- Запрос `CreateBookingRequest` и его валидатор.
- Dockerfile с многоступенчатой сборкой; сервис добавлен в `docker-compose.yml` (порт `5130`, зависит от `bookings-db` и `kafka`).
- `README.md` сервиса.

### Изменено

- Сначала статус брони сохраняется в базу, и только потом публикуется событие. Сбой публикации (3 попытки) логируется и
  **не** переводит уже подтверждённую бронь в `Rejected`.
- Сервис не обращается к EventService: места у события уменьшает сам EventService по сообщению из Kafka.
- Интеграционные тесты миграций проверяют таблицы только этого сервиса и отсутствие внешних ключей.
- Порт по умолчанию в `launchSettings.json` — `5130` (раньше совпадал с EventService), Swagger называется «Booking Service API».
- Версия сборки `BookingService.Presentation` — `0.9.0`.
