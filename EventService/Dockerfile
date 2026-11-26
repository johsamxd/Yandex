# syntax=docker/dockerfile:1
# Контекст сборки — КОРЕНЬ репозитория (нужна папка Shared):
#   docker build -f EventService/Dockerfile -t eventapi-event-service .

# ---------- Стадия сборки ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой с restore кэшируется, пока не менялись зависимости
COPY Shared/Shared.Domain/Shared.Domain.csproj Shared/Shared.Domain/
COPY Shared/Shared.Application/Shared.Application.csproj Shared/Shared.Application/
COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Shared/Shared.Presentation/Shared.Presentation.csproj Shared/Shared.Presentation/
COPY EventService/EventService.Domain/EventService.Domain.csproj EventService/EventService.Domain/
COPY EventService/EventService.Application/EventService.Application.csproj EventService/EventService.Application/
COPY EventService/EventService.Infrastructure/EventService.Infrastructure.csproj EventService/EventService.Infrastructure/
COPY EventService/EventService.Presentation/EventService.Presentation.csproj EventService/EventService.Presentation/
RUN dotnet restore EventService/EventService.Presentation/EventService.Presentation.csproj

# Затем исходники и публикация
COPY Shared/ Shared/
COPY EventService/EventService.Domain/ EventService/EventService.Domain/
COPY EventService/EventService.Application/ EventService/EventService.Application/
COPY EventService/EventService.Infrastructure/ EventService/EventService.Infrastructure/
COPY EventService/EventService.Presentation/ EventService/EventService.Presentation/
RUN dotnet publish EventService/EventService.Presentation/EventService.Presentation.csproj -c Release -o /app/publish --no-restore

# ---------- Финальный образ (только рантайм) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Не запускаем приложение от root
USER $APP_UID
ENTRYPOINT ["dotnet", "EventService.Presentation.dll"]
