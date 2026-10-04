FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/ApiGateway/ECommerce.ApiGateway/ECommerce.ApiGateway.csproj", "src/ApiGateway/ECommerce.ApiGateway/"]
RUN dotnet restore "src/ApiGateway/ECommerce.ApiGateway/ECommerce.ApiGateway.csproj"
COPY . .
WORKDIR "/src/src/ApiGateway/ECommerce.ApiGateway"
RUN dotnet build "ECommerce.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.ApiGateway.dll"]
