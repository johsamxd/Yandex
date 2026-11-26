# UserService

Сервис пользователей: регистрация, вход, хеширование пароля и **выдача JWT-токена**. Токен проверяют сервисы
[EventService](../EventService/README.md) и [BookingService](../BookingService/README.md). Описание всей системы —
в [корневом README](../README.md), история изменений — в [CHANGELOG.md](CHANGELOG.md).

С Kafka этот сервис не работает.

| | |
|---|---|
| Порт | `5129` (в Docker контейнер слушает `8080`) |
| База данных | PostgreSQL `users` (`users-db`, порт хоста `5433`) |
| Swagger | <http://localhost:5129/swagger> |

## API

| Метод | Endpoint | Описание | Ответ |
|---|---|---|---|
| POST | `/auth/register` | Регистрация. Тело: `login`, `password`, `role` (необязательно, по умолчанию `User`). Роль `Admin` может назначить только авторизованный администратор | `201 Created` / `400` (в т. ч. логин занят) / `403` |
| POST | `/auth/login` | Вход. Тело: `login`, `password` | `200 OK` с `token` / `400` / `401` |

Пример:

```json
POST /auth/register
{ "login": "alice", "password": "secret123" }

POST /auth/login
{ "login": "alice", "password": "secret123" }
```

Токен подписывается HMAC-SHA256 и содержит claims `NameIdentifier` (id пользователя), `Name` (логин), `Role`.

## Администраторы

Самостоятельно зарегистрироваться с ролью `Admin` нельзя (`403 Forbidden`). Администратора создаёт:

- **сид при старте** — секция `AdminSeed` (`Login`, `Password`); если пользователь с таким логином уже есть, он не меняется;
- **другой администратор** — `POST /auth/register` с его токеном и `"role": "Admin"`.

Смените пароль из `appsettings.json` / `docker-compose.yml` вне учебного стенда.

## Данные

Таблица `Users` (уникальный индекс по `Login`): `Id`, `Login`, `PasswordHash`, `Role` (`User` / `Admin`), `CreatedAt`.

Пароль хранится как **PBKDF2-HMAC-SHA256 со случайной солью** (600 000 итераций), формат `v1.{итерации}.{соль}.{хеш}` —
одинаковые пароли дают разные хеши. Миграции EF Core (`Initial`, `WidenPasswordHash`) лежат в `UserService.Infrastructure/Persistence/Migrations` и применяется при старте сервиса.

## Конфигурация

| Ключ | Описание | По умолчанию |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Подключение к `users` | `Host=localhost;Port=5433;Database=users;...` |
| `AdminSeed:Login` / `AdminSeed:Password` | Первый администратор | `admin` / `admin123` |
| `Jwt:Secret` | Секрет подписи (≥ 32 символов) | значение для учебного стенда |
| `Jwt:Issuer` | Издатель токена | `YandexApi` |
| `Jwt:Audience` | Аудитория токена | `YandexClient` |
| `Jwt:LifetimeMinutes` | Время жизни токена | `60` |

> `Jwt:Secret`, `Jwt:Issuer` и `Jwt:Audience` **обязаны совпадать** с одноимёнными настройками EventService и BookingService,
> иначе токен, выданный здесь, не пройдёт проверку в других сервисах.

В Docker значения переопределяются переменными окружения (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`).

## Запуск

```bash
# вместе со всей системой
docker compose up --build user-service      # из корня репозитория

# локально (нужна users-db: docker compose up -d users-db)
dotnet run --project UserService/UserService.Presentation
```

## Структура

```text
UserService.Domain          # User
UserService.Application     # IUserService, валидаторы, абстракции IPasswordHasher / IJwtTokenGenerator
UserService.Infrastructure  # EF Core (AppDbContext, миграции), Pbkdf2PasswordHasher, JwtTokenGenerator
UserService.Presentation    # AuthController, Program.cs, Swagger
tests/                      # UserService.UnitTests, UserService.IntegrationTests
Dockerfile                  # многоступенчатая сборка (контекст — корень репозитория)
```

## Тесты

```bash
dotnet test --solution UserService/UserService.slnx
```
