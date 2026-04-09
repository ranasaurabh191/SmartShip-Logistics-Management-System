FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config .
COPY localfeed/ localfeed/
COPY ["Shared/Smartship.Shared/SmartShip.Shared.csproj", "Shared/Smartship.Shared/"]
COPY ["Services/SmartShip.ShipmentService/SmartShip.ShipmentService.csproj", "Services/SmartShip.ShipmentService/"]
RUN dotnet restore "Services/SmartShip.ShipmentService/SmartShip.ShipmentService.csproj"
COPY Shared/ Shared/
COPY Services/SmartShip.ShipmentService/ Services/SmartShip.ShipmentService/
WORKDIR "/src/Services/SmartShip.ShipmentService"
RUN mkdir -p /app/logs/shipment
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartShip.ShipmentService.dll"]
