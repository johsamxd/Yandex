# syntax=docker/dockerfile:1
# Контекст сборки — КОРЕНЬ репозитория (нужна папка Shared):
#   docker build -f BookingService/Dockerfile -t eventapi-booking-service .

# ---------- Стадия сборки ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой с restore кэшируется, пока не менялись зависимости
COPY Shared/Shared.Domain/Shared.Domain.csproj Shared/Shared.Domain/
COPY Shared/Shared.Application/Shared.Application.csproj Shared/Shared.Application/
COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Shared/Shared.Presentation/Shared.Presentation.csproj Shared/Shared.Presentation/
COPY BookingService/BookingService.Domain/BookingService.Domain.csproj BookingService/BookingService.Domain/
COPY BookingService/BookingService.Application/BookingService.Application.csproj BookingService/BookingService.Application/
COPY BookingService/BookingService.Infrastructure/BookingService.Infrastructure.csproj BookingService/BookingService.Infrastructure/
COPY BookingService/BookingService.Presentation/BookingService.Presentation.csproj BookingService/BookingService.Presentation/
RUN dotnet restore BookingService/BookingService.Presentation/BookingService.Presentation.csproj

# Затем исходники и публикация
COPY Shared/ Shared/
COPY BookingService/BookingService.Domain/ BookingService/BookingService.Domain/
COPY BookingService/BookingService.Application/ BookingService/BookingService.Application/
COPY BookingService/BookingService.Infrastructure/ BookingService/BookingService.Infrastructure/
COPY BookingService/BookingService.Presentation/ BookingService/BookingService.Presentation/
RUN dotnet publish BookingService/BookingService.Presentation/BookingService.Presentation.csproj -c Release -o /app/publish --no-restore

# ---------- Финальный образ (только рантайм) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Не запускаем приложение от root
USER $APP_UID
ENTRYPOINT ["dotnet", "BookingService.Presentation.dll"]
