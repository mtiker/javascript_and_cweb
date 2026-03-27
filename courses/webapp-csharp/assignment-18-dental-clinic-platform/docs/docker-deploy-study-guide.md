# Docker ja deploy study guide

## Mille jaoks see materjal on?

See juhend on tehtud konkreetselt `assignment-18-dental-clinic-platform` projekti jaoks, et saaksid kaitsmisel seletada:

- miks Docker failid olemas on;
- mis vahe on local ja production compose failil;
- kuidas CI/CD pipeline buildib, testib ja deployb;
- kuidas `deploy.sh` koondab production deploy voo uhte kaesku;
- mida iga olulisem rida nendes failides teeb.

Kui tahad seda uhte lausesse kokku votta, siis selle assignmenti deploy mudel on:

1. GitLab CI teeb `restore -> build -> test -> docker build -> deploy`.
2. Docker image tehakse `Dockerfile` abil.
3. Productionis kaivitatakse `docker-compose.prod.yml`, mis paneb kaima nii PostgreSQL-i kui ASP.NET Core rakenduse.
4. `scripts/deploy.sh` on vaike entrypoint, mida GitLab deploy job kaivitab.

## Uldpilt: mis fail mis rolli taisab?

`Dockerfile`

- defineerib, kuidas .NET rakendusest tehakse Docker image;
- kasutab multi-stage build'i, et loplik image jaaks vaiksem.

`docker-compose.yml`

- kohaliku arenduse fail;
- kaivitab ainult PostgreSQL konteineri;
- rakendust jooksutatakse lokaalselt `dotnet run` kaudu.

`docker-compose.prod.yml`

- production/VPS fail;
- kaivitab nii andmebaasi kui web rakenduse;
- seob kokku env muutujad, pordid, healthchecki ja service dependency.

`scripts/deploy.sh`

- koondab deploy kaigu uhte skripti;
- kontrollib enne deployd, et `JWT__Key` oleks olemas;
- kaivitab production compose stacki buildiga.

`.dockerignore`

- utleb Dockerile, mida build contexti mitte kaasa votta;
- aitab image buildi kiiremaks ja puhtamaks teha.

`.gitlab-ci.yml`

- assignmenti pipeline;
- buildib, testib, teeb Docker build kontrolli ja deployb vaikimisi ainult default branchi voi tagi pealt.

## Kuidas kogu voog otsast lopuni kaib?

## 1. Local development

Local arenduses kasutatakse:

- `docker-compose.yml` andmebaasi jaoks;
- `src/WebApp` projekti otse `dotnet run`-iga rakenduse jaoks.

See on mugav, sest:

- andmebaas on konteineris ja lihtsalt korratav;
- backendi ei pea iga muudatuse jaoks imageks buildima;
- debugimine on lihtsam kui rakendus jookseb lokaalselt.

Taislause kaitsmiseks:

"Localis kasutan Dockerit ainult PostgreSQL jaoks, aga ASP.NET rakendus jookseb otse .NET runtime peal, et arendus oleks kiirem."

## 2. CI build ja test

GitLab pipeline teeb enne deployd:

1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. `docker build`

See on oluline, sest deploy ei tohiks olla esimene koht, kus avastan, et:

- NuGet paketid ei taastu;
- kood ei kompileeru;
- testid ei labi;
- Docker image ei buildi.

## 3. Production deploy

Deploys:

1. runner liigub assignmenti kausta;
2. teeb `chmod +x scripts/deploy.sh`;
3. kaivitab `./scripts/deploy.sh`;
4. skript kutsub `docker compose -f docker-compose.prod.yml up -d --build --remove-orphans`;
5. Compose buildib vajadusel image ja kaivitab `postgres` + `web` teenused;
6. `web` teenus ootab enne `postgres` healthchecki.

Taislause kaitsmiseks:

"Productionis ma ei jooksuta rakendust enam `dotnet run`-iga, vaid Compose stackina, kus app ja database tulevad kontrollitud konteineritest."

## Dockerfile rea kaupa

```dockerfile
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
```

### `FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build`

- votab build stage jaoks .NET 10 SDK image'i;
- SDK image sisaldab kompileerimiseks vajalikke tooriistu;
- `AS build` annab sellele stage'ile nime, et hiljem saaks sealt faile kopeerida.

### `WORKDIR /src`

- maarab kausta, kus jargmised kaesud jooksma hakkavad;
- lihtsustab `COPY` ja `RUN` kasutamist.

### `COPY Directory.Build.props ./`
### `COPY dental-clinic-platform.slnx ./`
### `COPY ...csproj ...`

- kopeerivad alguses ainult solutioni ja projektifailid, mitte kogu source'i;
- see on Docker cache jaoks tark votte.

Kaitsmisel voib oelda:

"Ma kopeerin enne ainult project file'id, et Docker saaks restore layerit cachida."

### `RUN dotnet restore dental-clinic-platform.slnx`

- taastab NuGet paketid kogu solutionile;
- toimub build stage'is parast seda, kui vajalikud projektifailid on konteinerisse toodud.

### `COPY src ./src`
### `COPY tests ./tests`

- alles nuud tuuakse sisse kogu tegelik source code ja testid.

### `RUN dotnet publish src/WebApp/WebApp.csproj -c Release -o /app/publish --no-restore`

- teeb rakendusest production publish outputi;
- `-c Release` tahendab release build;
- `-o /app/publish` paneb valmid failid eraldi kausta;
- `--no-restore` tahendab, et restore on juba eelnevalt tehtud.

### `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime`

- alustab teist stage'i;
- see image sisaldab ainult ASP.NET Core runtime'i, mitte SDK-d.

### `WORKDIR /app`

- runtime konteineri tootav kaust.

### `ENV ASPNETCORE_URLS=http://+:8080`

- utleb ASP.NET Core'ile, et ta kuulaks konteineris porti `8080`;
- `+` tahendab "kuula koigil interface'idel".

### `EXPOSE 8080`

- dokumenteerib, et konteiner kasutab porti `8080`;
- see ei ava ise porti hostile, vaid annab metadata.

### `COPY --from=build /app/publish .`

- kopeerib build stage'i publish valjundi runtime stage'i;
- see on multi-stage build'i tuum.

### `ENTRYPOINT ["dotnet", "WebApp.dll"]`

- konteineri kaivitamisel jooksutatakse `dotnet WebApp.dll`;
- see on rakenduse start command.

## `.dockerignore` rea kaupa

```text
**/bin
**/obj
**/out
**/.vscode
**/.vs
.dotnet
.dotnet-cli
.git
*.log
```

### `**/bin`, `**/obj`, `**/out`

- ei saada Docker build contexti build artefakte;
- need oleksid ainult uleliigne ballast.

### `**/.vscode`, `**/.vs`

- editori lokaalsed seadistused ei kuulu image sisse.

### `.dotnet`, `.dotnet-cli`

- lokaalsed SDK/tooling kaustad ei ole production image jaoks vajalikud.

### `.git`

- git ajalugu ei ole image buildimiseks vajalik.

### `*.log`

- logifailid ei pea build contextis kaasas olema.

## `docker-compose.yml` rea kaupa

```yaml
services:
  postgres:
    image: postgres:16
    container_name: dental-clinic-postgres
    restart: unless-stopped
    environment:
      POSTGRES_DB: dental_saas
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - dental-clinic-postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres -d dental_saas"]
      interval: 5s
      timeout: 3s
      retries: 15

volumes:
  dental-clinic-postgres-data:
```

### `services:`

- Compose failis algab siit teenuste defineerimine.

### `postgres:`

- siin defineeritakse andmebaasi teenus.

### `image: postgres:16`

- kasutatakse ametlikku PostgreSQL 16 image'it;
- localis ei buildita enda DB image'it.

### `container_name: dental-clinic-postgres`

- annab konteinerile kindla nime;
- teeb local troubleshooting'u lihtsamaks.

### `restart: unless-stopped`

- Docker proovib konteinerit uuesti kaivitada, kui see kukub maha;
- aga kui arendaja ise selle stopib, siis ei sunnita kohe tagasi kaima.

### `environment:`

- DB algseadistus konteineri sees.

### `POSTGRES_DB: dental_saas`

- loob andmebaasi nimega `dental_saas`.

### `POSTGRES_USER: postgres`
### `POSTGRES_PASSWORD: postgres`

- defineerivad vaikekasutaja ja parooli.

### `ports:`
### `- "5432:5432"`

- hosti port `5432` suunatakse konteineri porti `5432`;
- see lubab lokaalsel `dotnet run` rakendusel DB-ga suhelda aadressil `127.0.0.1:5432`.

### `volumes:`
### `- dental-clinic-postgres-data:/var/lib/postgresql/data`

- Postgres salvestab andmed named volume'isse;
- konteineri kustutamisel andmed ei kao automaatselt.

### `healthcheck:`

- Compose saab kontrollida, kas DB on tegelikult valmis.

### `test: ["CMD-SHELL", "pg_isready -U postgres -d dental_saas"]`

- Postgres enda utiliit kontrollib, kas DB votab uhendusi vastu.

### `interval: 5s`

- healthcheck jookseb iga 5 sekundi tagant.

### `timeout: 3s`

- kui kontroll 3 sekundiga ei vasta, loetakse see failed katseks.

### `retries: 15`

- annab DB-le startupiks rohkem aega.

### `volumes: dental-clinic-postgres-data:`

- deklareerib named volume'i, mida teenus kasutab.

## Miks local compose failis web teenust ei ole?

Sest siin projektis on local flow teadlikult selline:

- DB konteineris;
- backend lokaalselt.

See on arenduse jaoks mugavam kui iga koodimuudatuse peale kogu image uuesti buildida.

## `docker-compose.prod.yml` rea kaupa

```yaml
services:
  postgres:
    image: postgres:16
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-dental_saas}
      POSTGRES_USER: ${POSTGRES_USER:-postgres}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-postgres}
    volumes:
      - dental-clinic-postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-postgres} -d ${POSTGRES_DB:-dental_saas}"]
      interval: 5s
      timeout: 3s
      retries: 15

  web:
    image: ${DENTAL_CLINIC_IMAGE:-dental-clinic-platform:local}
    build:
      context: .
      dockerfile: Dockerfile
    restart: unless-stopped
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      Cors__AllowedOrigins__0: ${CORS_ALLOWED_ORIGIN:-https://mtiker-cweb-a3.proxy.itcollege.ee}
      ConnectionStrings__DefaultConnection: Host=postgres;Port=5432;Database=${POSTGRES_DB:-dental_saas};Username=${POSTGRES_USER:-postgres};Password=${POSTGRES_PASSWORD:-postgres}
      JWT__Key: ${JWT__Key:?JWT__Key must be set}
      JWT__Issuer: ${JWT__Issuer:-DentalClinicSaaS}
      JWT__Audience: ${JWT__Audience:-DentalClinicSaaS}
      JWT__ExpiresInSeconds: ${JWT__ExpiresInSeconds:-1200}
      JWT__RefreshTokenExpiresInSeconds: ${JWT__RefreshTokenExpiresInSeconds:-604800}
      DataInitialization__MigrateDatabase: ${DATA_INIT_MIGRATE_DATABASE:-true}
      DataInitialization__SeedIdentity: ${DATA_INIT_SEED_IDENTITY:-true}
      DataInitialization__SeedData: ${DATA_INIT_SEED_DATA:-true}
      Diagnostics__EnableDetailedErrors: "false"
      Diagnostics__EnableSensitiveDataLogging: "false"
    ports:
      - "${WEBAPP_PORT:-80}:8080"

volumes:
  dental-clinic-postgres-data:
```

### Peamine mote

Local fail:

- DB only;
- app jookseb hostis.

Production fail:

- DB + app molemad konteinerites;
- env muutujad tulevad keskkonnast;
- hosti port mapitakse valja;
- startup jargib DB healthchecki.

### `POSTGRES_DB: ${POSTGRES_DB:-dental_saas}`

- loeb vaartuse env muutujast `POSTGRES_DB`;
- kui seda pole, kasutab vaikimisi `dental_saas`.

### `healthcheck` productionis

- sama idee nagu localis;
- ainult et kasutajanimi ja andmebaasi nimi voetakse env muutujatest.

### `web:`

- siin defineeritakse rakenduse teenus.

### `image: ${DENTAL_CLINIC_IMAGE:-dental-clinic-platform:local}`

- Compose teenusele antakse image nimi;
- kui `build:` on juures, saab Compose selle image vajadusel ise buildida.

### `build:`
### `context: .`

- Docker build context on assignmenti juurkaust.

### `dockerfile: Dockerfile`

- utleb, millist Dockerfile'i buildiks kasutada.

### `restart: unless-stopped`

- sama taastuvuse reegel nagu Postgresel.

### `depends_on:`
### `condition: service_healthy`

- `web` ei alusta enne, kui `postgres` healthcheck on healthy;
- see on parem kui lihtsalt "container started".

### `ASPNETCORE_ENVIRONMENT: Production`

- rakendus kaivitub production keskkonnana;
- see mojub konfiguratsioonile, loggingule ja middleware kaitumisele.

### `ASPNETCORE_URLS: http://+:8080`

- rakendus kuulab konteineri sees porti `8080`.

### `Cors__AllowedOrigins__0: ${CORS_ALLOWED_ORIGIN:-https://mtiker-cweb-a3.proxy.itcollege.ee}`

- seadistab esimese lubatud CORS origin'i;
- double underscore on .NET config binding syntax env muutujate jaoks.

### `ConnectionStrings__DefaultConnection: ...`

- annab .NET konfi kaudu andmebaasi uhenduse;
- `Host=postgres` on teenuse nimi Compose vorgus.

Oluline point:

"Konteinerid suhtlevad omavahel Compose service nimega, mitte `localhost` abil."

### `JWT__Key: ${JWT__Key:?JWT__Key must be set}`

- see muutuja on kohustuslik;
- kui seda ei ole, siis Compose katkestab enne teenuse kaivitamist.

### `JWT__Issuer`, `JWT__Audience`

- JWT tokenite metadata;
- aitavad valideerida, kes tokeni valjastas ja kellele see moeldud on.

### `JWT__ExpiresInSeconds`

- access tokeni eluiga;
- vaikevaartus `1200` = 20 minutit.

### `JWT__RefreshTokenExpiresInSeconds`

- refresh tokeni eluiga;
- `604800` sekundit = 7 paeva.

### `DataInitialization__MigrateDatabase`

- kas rakendus peaks stardis migratsioonid ara jooksma.

### `DataInitialization__SeedIdentity`

- kas seedida identity kasutajad ja rollid.

### `DataInitialization__SeedData`

- kas seedida demo arandmed.

### `DataInitialization__ResetSeedUserPasswords`

- kui `true`, siis olemasolevate seed/demo kasutajate paroolid sünkroniseeritakse uuesti README-s kirjeldatud vaikeparoolile;
- see on kasulik live deploy puhul, kus PostgreSQL volume jääb alles ja vana `sysadmin` hash muidu ei muutuks;
- kui tahad käsitsi muudetud seed-kontode paroole säilitada, pane see `false`.

### `Diagnostics__EnableDetailedErrors: "false"`
### `Diagnostics__EnableSensitiveDataLogging: "false"`

- productionis hoian detailsed vead ja tundliku EF logimise valjas;
- vaikimisi turvalisem.

### `ports:`
### `- "${WEBAPP_PORT:-80}:8080"`

- hosti port on vaikimisi `80`;
- konteineri sees kuulab app `8080`.

## `scripts/deploy.sh` rea kaupa

```bash
#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

: "${JWT__Key:?JWT__Key must be set before running deployment.}"
PROJECT_NAME="${COMPOSE_PROJECT_NAME:-dental-clinic-platform}"

docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml up -d --build --remove-orphans
```

### `#!/usr/bin/env bash`

- shebang;
- utleb, et skript tuleb kaivitada Bashiga.

### `set -euo pipefail`

See tahendab:

- `-e`: katkesta kohe, kui moni kask ebaonnestub;
- `-u`: katkesta, kui kasutatakse defineerimata muutujat;
- `-o pipefail`: pipeline loetakse failed'iks, kui moni selle osa failib.

### `ROOT_DIR=...`

- leiab assignmenti juurkausta tee skripti asukoha pohjal;
- skript ei eelda, et kasutaja oleks juba oiges kaustas.

### `cd "$ROOT_DIR"`

- liigub projektijuurkausta.

### `: "${JWT__Key:?JWT__Key must be set before running deployment.}"`

- shelli builtin kontroll kohustuslikule muutujale;
- kui `JWT__Key` puudub, katkestab skript koha peal.

### `PROJECT_NAME="${COMPOSE_PROJECT_NAME:-dental-clinic-platform}"`

- Compose project name on override'itav;
- vaikimisi kasutab nime `dental-clinic-platform`.

### `docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml up -d --build --remove-orphans`

See on deploy skripti koige olulisem rida.

Osadeks:

- `docker compose`: kasutab Compose V2 kaesku;
- `--project-name "$PROJECT_NAME"`: maarab stackile kindla nime;
- `-f docker-compose.prod.yml`: kasutab production compose faili;
- `up`: loob ja kaivitab teenused;
- `-d`: detached mode;
- `--build`: buildib image uuesti, kui vaja;
- `--remove-orphans`: koristab ara vanad teenused, mis failist enam ei tule.

## `.gitlab-ci.yml` rea kaupa

```yaml
.assignment18_changes: &assignment18_changes
  - courses/webapp-csharp/assignment-18-dental-clinic-platform/**/*
  - .gitlab-ci.yml

.assignment18_job: &assignment18_job
  rules:
    - changes: *assignment18_changes
    - when: never

.assignment18_deploy_job: &assignment18_deploy_job
  rules:
    - if: '$CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH || $CI_COMMIT_TAG'
      changes: *assignment18_changes
    - when: never

assignment18_build:
  stage: build
  tags:
    - shared
  <<: *assignment18_job
  before_script:
    - cd courses/webapp-csharp/assignment-18-dental-clinic-platform
  script:
    - dotnet restore dental-clinic-platform.slnx
    - dotnet build dental-clinic-platform.slnx --configuration Release --no-restore

assignment18_test:
  stage: test
  tags:
    - shared
  needs:
    - assignment18_build
  <<: *assignment18_job
  before_script:
    - cd courses/webapp-csharp/assignment-18-dental-clinic-platform
  script:
    - dotnet test dental-clinic-platform.slnx --configuration Release --no-build

assignment18_docker_build:
  stage: package
  tags:
    - shared
  needs:
    - assignment18_test
  <<: *assignment18_job
  script:
    - docker build --pull --tag "$CI_PROJECT_PATH_SLUG-assignment-18:$CI_COMMIT_SHORT_SHA" courses/webapp-csharp/assignment-18-dental-clinic-platform

assignment18_deploy:
  stage: deploy
  tags:
    - shared
  needs:
    - assignment18_docker_build
  environment:
    name: assignment-18-production
  <<: *assignment18_deploy_job
  script:
    - cd courses/webapp-csharp/assignment-18-dental-clinic-platform
    - chmod +x scripts/deploy.sh
    - ./scripts/deploy.sh
```

### Ylemine korduv osa: anchors ja rules

`.assignment18_changes`

- defineerib failimustri, mida kasutatakse mitmes kohas uuesti.

`.assignment18_job`

- korduv rule blokk build/test/package jobidele.

`changes`

- job kaivitub ainult siis, kui muutunud failid vastavad sellele mustrile.

`when: never`

- kui eelmine reegel ei matchi, siis jobi ei kaivitata.

`.assignment18_deploy_job`

- deploy jaoks on karmim rule.

`if: '$CI_COMMIT_BRANCH == $CI_DEFAULT_BRANCH || $CI_COMMIT_TAG'`

- deploy lubatakse ainult default branchilt voi tagi pealt;
- feature branchilt automaatdeployd ei tehta.

## Build job

`assignment18_build`

- taastab paketid ja buildib release konfiguratsioonis.

## Test job

`assignment18_test`

- soltub build jobist;
- jooksutab testid `--no-build` valikuga.

## Docker build job

`assignment18_docker_build`

- verifitseerib, et Docker image build on korras;
- tagib image commit short SHA pohjal.

## Deploy job

`assignment18_deploy`

- soltub Docker build jobist;
- annab GitLabile production environment nime;
- muudab deploy skripti kaivitatavaks;
- kaivitab reaalse deploy just `deploy.sh` kaudu.

## Kuidas seda koike kaitsmisel lihtsasti seletada?

## 30 sekundi versioon

"Mul on Assignment 18 jaoks eraldi Dockerfile, mis buildib .NET rakendusest production image'i multi-stage buildiga. Local arenduses kasutan Docker Compose'i ainult PostgreSQL jaoks, aga productionis kasutan `docker-compose.prod.yml` faili, mis kaivitab nii appi kui ka andmebaasi. GitLab pipeline teeb enne deployd restore, buildi, testid ja Docker build kontrolli. Deploy job kaivitab `scripts/deploy.sh` skripti, mis kontrollib olulist JWT saladust ja tostab production stacki ules `docker compose up -d --build --remove-orphans` kaudu."

## Tyyplised kaitsmise kusimused ja luhivastused

### Miks multi-stage Docker build?

- et production image ei sisaldaks SDK-d ega uleliigseid buildi tootriistu;
- tulemuseks on vaiksem ja puhtam runtime image.

### Miks on kaks compose faili?

- local fail on arenduseks ja kaivitab ainult DB;
- production fail kaivitab kogu stacki.

### Miks `Host=postgres`, mitte `localhost`?

- konteinerite vahel kasutatakse Compose service nime;
- `localhost` viitaks web konteineris iseendale.

### Miks `depends_on` koos `service_healthy`?

- et web ei prooviks DB-ga uhenduda enne, kui PostgreSQL on reaalselt valmis.

### Miks `JWT__Key` on kohustuslik env muutuja?

- see on tokenite signeerimise saladus;
- seda ei tohi hardcode'ida reposse ega image sisse.

### Miks deploy skriptis `--remove-orphans`?

- kui compose failist on moni vana teenus eemaldatud, siis vana konteiner koristatakse deploy ajal ara.

### Miks pipeline'is on eraldi Docker build job, kui deploy buildib niikuinii?

- et enne deployd verifitseerida, et Dockerfile toimib;
- muidu voiks image build error tulla alles deploy etapis.

## Mida peab kindlasti peas hoidma?

Kui koik muu ununeb, pea meeles neid 8 lauset:

1. `Dockerfile` teeb .NET rakendusest production image'i.
2. See kasutab multi-stage build'i: `sdk` buildiks, `aspnet` runtime'iks.
3. Local compose fail kaivitab ainult PostgreSQL-i.
4. Production compose fail kaivitab `postgres` ja `web` teenused koos.
5. Web konteiner suhtleb DB-ga hostinime `postgres` kaudu.
6. `JWT__Key` peab productionis olemas olema.
7. GitLab pipeline teeb `restore -> build -> test -> docker build -> deploy`.
8. `deploy.sh` kaivitab `docker compose -f docker-compose.prod.yml up -d --build --remove-orphans`.

## Hea lopulause kaitsmiseks

"Selle assignmenti deploy lahendus on tahtlikult lihtne ja defendable: Dockerfile pakendab rakenduse, Compose paneb teenused kokku, GitLab pipeline kontrollib enne kvaliteeti ning deploy skript teeb production update'i uhe korratava kaesuga."
