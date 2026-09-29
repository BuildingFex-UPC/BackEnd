# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json ./
COPY buildingfex-api.sln ./
COPY BuildingFex.Api/BuildingFex.Api.csproj BuildingFex.Api/

RUN dotnet restore BuildingFex.Api/BuildingFex.Api.csproj

COPY BuildingFex.Api/ BuildingFex.Api/

RUN dotnet publish BuildingFex.Api/BuildingFex.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "BuildingFex.Api.dll"]
