FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Product/ECommerce.Product.API/ECommerce.Product.API.csproj", "src/Services/Product/ECommerce.Product.API/"]
COPY ["src/Shared/ECommerce.Shared/ECommerce.Shared.csproj", "src/Shared/ECommerce.Shared/"]

RUN dotnet restore "src/Services/Product/ECommerce.Product.API/ECommerce.Product.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Product/ECommerce.Product.API"
RUN dotnet build "ECommerce.Product.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.Product.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.Product.API.dll"]
