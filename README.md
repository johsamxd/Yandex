# Yandex API
Учебный проект для [Яндекс.Практикум](https://practicum.yandex.ru/middle-csharp/)

## Описание
API для сервиса бронировния

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

## API Документация
- [Base URL](http://localhost:5128)
- [Swagger](http://localhost:5128/swagger/index.html)

| Метод | Endpoint | Описание | Статус |
|-------|----------|----------|--------|
| GET | `/events` | Получить все события | 200 OK |
| GET | `/events/{id}` | Получить событие по ID | 200 OK / 404 Not Found |
| POST | `/events` | Создать новое событие | 201 Created / 400 Bad Request |
| PUT | `/events/{id}` | Обновить событие | 204 No Content / 404 Not Found |
| DELETE | `/events/{id}` | Удалить событие | 204 No Content / 404 Not Found |