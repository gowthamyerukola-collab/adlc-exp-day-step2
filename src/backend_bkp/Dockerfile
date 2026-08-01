FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY OuterloopLabApi.sln ./
COPY OuterloopLabApi/OuterloopLabApi.csproj OuterloopLabApi/
COPY OuterloopLabApi.Tests/OuterloopLabApi.Tests.csproj OuterloopLabApi.Tests/
RUN dotnet restore OuterloopLabApi.sln

COPY OuterloopLabApi/ OuterloopLabApi/
COPY OuterloopLabApi.Tests/ OuterloopLabApi.Tests/
RUN dotnet build OuterloopLabApi.sln -c Release --no-restore
RUN dotnet publish OuterloopLabApi/OuterloopLabApi.csproj -c Release -o /app/publish --no-build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 3000
ENTRYPOINT ["dotnet", "OuterloopLabApi.dll"]
