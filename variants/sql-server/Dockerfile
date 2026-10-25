FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY variants/sql-server/ variants/sql-server/
RUN dotnet publish variants/sql-server/Testing_QualityShop.SqlServer.Api/Testing_QualityShop.SqlServer.Api.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Testing_QualityShop.SqlServer.Api.dll"]
