FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Shared/Smartship.Shared/SmartShip.Shared.csproj", "Shared/Smartship.Shared/"]
COPY ["Services/SmartShip.PaymentService/SmartShip.PaymentService.csproj", "Services/SmartShip.PaymentService/"]
RUN dotnet restore "Services/SmartShip.PaymentService/SmartShip.PaymentService.csproj"
COPY Shared/ Shared/
COPY Services/SmartShip.PaymentService/ Services/SmartShip.PaymentService/
WORKDIR "/src/Services/SmartShip.PaymentService"
RUN mkdir -p /app/logs/payment
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SmartShip.PaymentService.dll"]
