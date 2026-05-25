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
COPY App.Resources/App.Resources.csproj App.Resources/
COPY WebApp/WebApp.csproj WebApp/
COPY WebApp.Tests/WebApp.Tests.csproj WebApp.Tests/
COPY Architecture.Tests/Architecture.Tests.csproj Architecture.Tests/
COPY SharedKernel/SharedKernel.csproj SharedKernel/
COPY Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY Modules.Users/Modules.Users.csproj Modules.Users/
COPY Modules.Gyms/Modules.Gyms.csproj Modules.Gyms/
COPY Modules.Memberships/Modules.Memberships.csproj Modules.Memberships/
COPY Modules.Training/Modules.Training.csproj Modules.Training/
COPY Modules.Maintenance/Modules.Maintenance.csproj Modules.Maintenance/

RUN dotnet restore multi-gym-management-system.slnx

COPY App.BLL.Contracts ./App.BLL.Contracts
COPY App.BLL ./App.BLL
COPY App.DAL.Contracts ./App.DAL.Contracts
COPY App.DAL.EF ./App.DAL.EF
COPY App.Resources ./App.Resources
COPY App.Domain ./App.Domain
COPY Base.Contracts ./Base.Contracts
COPY Base.Domain ./Base.Domain
COPY Base.Helpers ./Base.Helpers
COPY WebApp ./WebApp
COPY WebApp.Tests ./WebApp.Tests
COPY Architecture.Tests ./Architecture.Tests
COPY SharedKernel ./SharedKernel
COPY Shared.Contracts ./Shared.Contracts
COPY Modules.Users ./Modules.Users
COPY Modules.Gyms ./Modules.Gyms
COPY Modules.Memberships ./Modules.Memberships
COPY Modules.Training ./Modules.Training
COPY Modules.Maintenance ./Modules.Maintenance

RUN dotnet publish WebApp/WebApp.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
COPY --from=client-build /client/dist ./wwwroot/client

ENTRYPOINT ["dotnet", "WebApp.dll"]
