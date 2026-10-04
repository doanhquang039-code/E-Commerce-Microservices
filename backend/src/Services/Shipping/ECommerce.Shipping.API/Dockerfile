FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Shipping/ECommerce.Shipping.API/ECommerce.Shipping.API.csproj", "src/Services/Shipping/ECommerce.Shipping.API/"]
COPY ["src/Shared/ECommerce.Shared/ECommerce.Shared.csproj", "src/Shared/ECommerce.Shared/"]

RUN dotnet restore "src/Services/Shipping/ECommerce.Shipping.API/ECommerce.Shipping.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Shipping/ECommerce.Shipping.API"
RUN dotnet build "ECommerce.Shipping.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.Shipping.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.Shipping.API.dll"]
