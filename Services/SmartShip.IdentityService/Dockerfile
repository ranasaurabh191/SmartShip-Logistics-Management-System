FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config .
COPY localfeed/ localfeed/
COPY ["Shared/Smartship.Shared/SmartShip.Shared.csproj", "Shared/Smartship.Shared/"]
COPY ["Services/SmartShip.NotificationService/SmartShip.NotificationService.csproj", "Services/SmartShip.NotificationService/"]
COPY ["Services/SmartShip.IdentityService/SmartShip.IdentityService.csproj", "Services/SmartShip.IdentityService/"]
RUN dotnet restore "Services/SmartShip.IdentityService/SmartShip.IdentityService.csproj"
COPY Shared/ Shared/
COPY Services/SmartShip.NotificationService/ Services/SmartShip.NotificationService/
COPY Services/SmartShip.IdentityService/ Services/SmartShip.IdentityService/
WORKDIR "/src/Services/SmartShip.IdentityService"
RUN mkdir -p /app/logs/identity
RUN dotnet publish -c Release -o /app/publish \
    /p:ErrorOnDuplicatePublishOutputFiles=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartShip.IdentityService.dll"]
