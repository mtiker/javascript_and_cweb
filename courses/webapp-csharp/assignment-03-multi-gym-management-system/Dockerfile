FROM node:20-alpine AS client-build
WORKDIR /client

COPY client/package*.json ./
RUN npm ci

COPY client ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY multi-gym-management-system.slnx ./
COPY App.BLL.Contracts/App.BLL.Contracts.csproj App.BLL.Contracts/
COPY App.DAL.Contracts/App.DAL.Contracts.csproj App.DAL.Contracts/
COPY App.Domain/App.Domain.csproj App.Domain/
COPY Base.Contracts/Base.Contracts.csproj Base.Contracts/
COPY Base.Domain/Base.Domain.csproj Base.Domain/
COPY Base.Helpers/Base.Helpers.csproj Base.Helpers/
COPY App.DAL.EF/App.DAL.EF.csproj App.DAL.EF/
COPY App.BLL/App.BLL.csproj App.BLL/
COPY App.DTO/App.DTO.csproj App.DTO/
COPY App.Resources/App.Resources.csproj App.Resources/
COPY WebApp/WebApp.csproj WebApp/
COPY WebApp.Tests/WebApp.Tests.csproj WebApp.Tests/

RUN dotnet restore multi-gym-management-system.slnx

COPY App.BLL.Contracts ./App.BLL.Contracts
COPY App.BLL ./App.BLL
COPY App.DAL.Contracts ./App.DAL.Contracts
COPY App.DAL.EF ./App.DAL.EF
COPY App.DTO ./App.DTO
COPY App.Resources ./App.Resources
COPY App.Domain ./App.Domain
COPY Base.Contracts ./Base.Contracts
COPY Base.Domain ./Base.Domain
COPY Base.Helpers ./Base.Helpers
COPY WebApp ./WebApp
COPY WebApp.Tests ./WebApp.Tests

RUN dotnet publish WebApp/WebApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
COPY --from=client-build /client/dist ./wwwroot/client

ENTRYPOINT ["dotnet", "WebApp.dll"]
