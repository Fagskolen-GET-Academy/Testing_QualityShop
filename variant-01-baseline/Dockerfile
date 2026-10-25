FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY variant-01-baseline/ variant-01-baseline/
RUN dotnet publish variant-01-baseline/Testing_QualityShop.Api/Testing_QualityShop.Api.csproj --configuration Release --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Testing_QualityShop.Api.dll"]
