FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Shared/Smartship.Shared/SmartShip.Shared.csproj", "Shared/Smartship.Shared/"]
COPY ["Services/SmartShip.NotificationService/SmartShip.NotificationService.csproj", "Services/SmartShip.NotificationService/"]
RUN dotnet restore "Services/SmartShip.NotificationService/SmartShip.NotificationService.csproj"
COPY Shared/ Shared/
COPY Services/SmartShip.NotificationService/ Services/SmartShip.NotificationService/
WORKDIR "/src/Services/SmartShip.NotificationService"
RUN mkdir -p /app/logs/notification
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartShip.NotificationService.dll"]
