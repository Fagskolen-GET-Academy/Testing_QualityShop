FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Testing_QualityShop.Core/Testing_QualityShop.Core.csproj", "Testing_QualityShop.Core/"]
COPY ["Testing_QualityShop.Api/Testing_QualityShop.Api.csproj", "Testing_QualityShop.Api/"]
RUN dotnet restore "Testing_QualityShop.Api/Testing_QualityShop.Api.csproj"

COPY . .
RUN dotnet publish "Testing_QualityShop.Api/Testing_QualityShop.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Testing_QualityShop.Api.dll"]
