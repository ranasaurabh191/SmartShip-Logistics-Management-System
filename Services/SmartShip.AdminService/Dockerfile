FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config .
COPY localfeed/ localfeed/
COPY ["Shared/Smartship.Shared/SmartShip.Shared.csproj", "Shared/Smartship.Shared/"]
COPY ["Services/SmartShip.AdminService/SmartShip.AdminService.csproj", "Services/SmartShip.AdminService/"]
RUN dotnet restore "Services/SmartShip.AdminService/SmartShip.AdminService.csproj"
COPY Shared/ Shared/
COPY Services/SmartShip.AdminService/ Services/SmartShip.AdminService/
WORKDIR "/src/Services/SmartShip.AdminService"
RUN mkdir -p /app/logs/admin
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartShip.AdminService.dll"]
