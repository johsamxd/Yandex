# Yandex API
Учебный проект для [Яндекс.Практикум](https://practicum.yandex.ru/middle-csharp/)

## Описание
API для сервиса бронирования

## Используемые технологии
* .NET 10.0
* ASP.NET Core Web API
* Swagger / OpenAPI
* AutoMapper
* FluentValidation

## Установка и запуск
1. Клонирование репозитория
```
git clone https://github.com/johsamxd/Yandex.git
cd Yandex
```

2. Запуск приложения
```
dotnet run --project Yandex.Web
```
## Запуск тестов
```
dotnet test
```

## API Документация
- [Base URL](http://localhost:5128)
- [Swagger](http://localhost:5128/swagger/index.html)

| Метод | Endpoint | Описание | Статус |
|-------|----------|----------|--------|
| GET | `/events` | Получить список событий с фильтрацией и пагинацией | 200 OK |
| GET | `/events/{id}` | Получить событие по ID | 200 OK / 404 Not Found |
| POST | `/events` | Создать новое событие | 201 Created / 400 Bad Request |
| PUT | `/events/{id}` | Обновить событие | 204 No Content / 404 Not Found |
| DELETE | `/events/{id}` | Удалить событие | 204 No Content / 404 Not Found |

## Список изменений:

### 0.0.2

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

### 0.0.1

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