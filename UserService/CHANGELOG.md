# Changelog — UserService

Формат — [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/), версии — [SemVer](https://semver.org/lang/ru/).
Изменения всей системы — в [корневом CHANGELOG.md](../CHANGELOG.md).

## [0.9.0] — 2026-11-25

Сервис выделен из монолита (до этого — версии 0.1.0–0.8.0 в [корневом CHANGELOG.md](../CHANGELOG.md#история-монолита)).

### Добавлено

- Самостоятельный сервис регистрации и входа: `POST /auth/register`, `POST /auth/login`, хеширование пароля (SHA-256), генерация JWT.
- Собственная база PostgreSQL `users` (`users-db`) и своя миграция EF Core `Initial` (таблица `Users`, уникальный индекс по `Login`).
- Слои Domain / Application / Infrastructure / Presentation, свои юнит- и интеграционные тесты.
- Dockerfile с многоступенчатой сборкой; сервис добавлен в `docker-compose.yml` (порт `5129`).
- `README.md` сервиса.

### Изменено

- Токен по-прежнему выдаёт только этот сервис; EventService и BookingService проверяют его с теми же `Secret`, `Issuer`, `Audience`.
- Swagger получил название «User Service API» и кнопку **Authorize**.
- Интеграционные тесты миграций проверяют таблицы только этого сервиса и отсутствие внешних ключей.
- Версия сборки `UserService.Presentation` — `0.9.0`.

### Удалено

- Неиспользуемые `BookingBackgroundOptions` и секция `BookingBackground` в конфигурации (остались от монолита).
