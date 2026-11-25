# syntax=docker/dockerfile:1
# Контекст сборки — КОРЕНЬ репозитория (нужна папка Shared):
#   docker build -f UserService/Dockerfile -t eventapi-user-service .

# ---------- Стадия сборки ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только .csproj — слой с restore кэшируется, пока не менялись зависимости
COPY Shared/Shared.Domain/Shared.Domain.csproj Shared/Shared.Domain/
COPY Shared/Shared.Application/Shared.Application.csproj Shared/Shared.Application/
COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Shared/Shared.Presentation/Shared.Presentation.csproj Shared/Shared.Presentation/
COPY UserService/UserService.Domain/UserService.Domain.csproj UserService/UserService.Domain/
COPY UserService/UserService.Application/UserService.Application.csproj UserService/UserService.Application/
COPY UserService/UserService.Infrastructure/UserService.Infrastructure.csproj UserService/UserService.Infrastructure/
COPY UserService/UserService.Presentation/UserService.Presentation.csproj UserService/UserService.Presentation/
RUN dotnet restore UserService/UserService.Presentation/UserService.Presentation.csproj

# Затем исходники и публикация
COPY Shared/ Shared/
COPY UserService/UserService.Domain/ UserService/UserService.Domain/
COPY UserService/UserService.Application/ UserService/UserService.Application/
COPY UserService/UserService.Infrastructure/ UserService/UserService.Infrastructure/
COPY UserService/UserService.Presentation/ UserService/UserService.Presentation/
RUN dotnet publish UserService/UserService.Presentation/UserService.Presentation.csproj -c Release -o /app/publish --no-restore

# ---------- Финальный образ (только рантайм) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Не запускаем приложение от root
USER $APP_UID
ENTRYPOINT ["dotnet", "UserService.Presentation.dll"]
