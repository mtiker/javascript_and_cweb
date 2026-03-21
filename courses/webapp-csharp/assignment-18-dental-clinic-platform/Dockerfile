FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY dental-clinic-platform.slnx ./
COPY src/App.Domain/App.Domain.csproj src/App.Domain/
COPY src/App.DAL.EF/App.DAL.EF.csproj src/App.DAL.EF/
COPY src/App.BLL/App.BLL.csproj src/App.BLL/
COPY src/App.DTO/App.DTO.csproj src/App.DTO/
COPY src/WebApp/WebApp.csproj src/WebApp/
COPY tests/WebApp.Tests/WebApp.Tests.csproj tests/WebApp.Tests/

RUN dotnet restore dental-clinic-platform.slnx

COPY src ./src
COPY tests ./tests

RUN dotnet publish src/WebApp/WebApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WebApp.dll"]
