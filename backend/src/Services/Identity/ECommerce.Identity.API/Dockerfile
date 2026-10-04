FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Services/Identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj", "src/Services/Identity/ECommerce.Identity.API/"]

RUN dotnet restore "src/Services/Identity/ECommerce.Identity.API/ECommerce.Identity.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Identity/ECommerce.Identity.API"
RUN dotnet build "ECommerce.Identity.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ECommerce.Identity.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECommerce.Identity.API.dll"]
