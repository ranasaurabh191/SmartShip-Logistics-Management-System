FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config .
COPY localfeed/ localfeed/
COPY ["Shared/Smartship.Shared/SmartShip.Shared.csproj", "Shared/Smartship.Shared/"]
COPY ["Gateway/SmartShip.Gateway/SmartShip.Gateway.csproj", "Gateway/SmartShip.Gateway/"]
RUN dotnet restore "Gateway/SmartShip.Gateway/SmartShip.Gateway.csproj"
COPY Shared/ Shared/
COPY Gateway/SmartShip.Gateway/ Gateway/SmartShip.Gateway/
WORKDIR "/src/Gateway/SmartShip.Gateway"
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartShip.Gateway.dll"]
