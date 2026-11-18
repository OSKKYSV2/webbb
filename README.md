# VehicleRegistry – registr vozidel se servisní knihou

Semestrální projekt pro předmět **Pokročilé webové technologie**. Technologie: **.NET 10, ASP.NET Core MVC, EF Core 10.0.15, SQL Server, ASP.NET Core Identity, Serilog a xUnit**. Zachovává téma a databázový základ původně odevzdané aplikace. Opravy vycházejí z připomínek vyučujícího a následného posouzení funkčnosti.

> **Stav ověření:** V tomto pracovním prostředí nebylo možné spustit `dotnet build`, `dotnet test`, ani se připojit k SQL Serveru – chybí zde .NET SDK a kontejner nemá síťový přístup pro jeho instalaci. Byl spuštěn statický audit architektury a vytvořeny testy, migrační skripty i GitHub Actions, ale **úspěšný běh kompilace, testů, migrací a webu není prohlášen za ověřený**. Před odevzdáním je nutné provést ověřovací postup níže a opravit případné problémy z výstupu.

## Funkce aplikace

| Role | Funkce |
|---|---|
| Anonymní | Úvod, vlastní MVC registrace a přihlášení. Nový účet získá roli `Customer`. |
| Customer | CRUD vlastních vozidel, JPG/PNG fotografie do 2 MB, historie servisů, doporučení údržby. |
| Manager | Hledání vozidel podle VIN/SPZ, správa servisních záznamů, typů úkonů a servisních dílen. |
| Admin | Administrace účtů a rolí, správa vozidel, číselníků a všech servisních záznamů. |

**Bezpečnost:** Identity hashuje hesla, POST metody používají antiforgery validaci, Customer vidí jen vlastní vozidla (kontrola i v aplikačních službách), role se kontrolují v controllerech, VIN a SPZ musí být unikátní. Fotky mají kontrolu signatury i maximální velikosti. Hesla se nesmí commitovat.

## Architektura a školní připomínky

```text
VehicleRegistry.Web                 MVC Controllers + Views + konfigurace DI
  |-> VehicleRegistry.Application  Aplikační služby, DTO, business pravidla
  |      |-> VehicleRegistry.Domain
  |-> VehicleRegistry.Infrastructure  DbContext, EF Core, migrace, Identity, repozitáře
         |-> VehicleRegistry.Domain

VehicleRegistry.Domain              Entity, explicitní rozhraní repozitářů, IUnitOfWork
VehicleRegistry.Tests               xUnit a relační testy s SQLite in-memory
```

- `Infrastructure` **nemá referenci na Application**; obě vrstvy závisejí samostatně na `Domain`.
- `Domain/Repositories` obsahuje `IVehicleRepository`, `IServiceRecordRepository`, `IManufacturerRepository`, `IServiceTypeRepository`, `IServiceCenterRepository`, `IUnitOfWork` (žádný generický `IRepository<T>`).
- `Vehicle.OwnerId` / `Vehicle.Owner` a `ServiceRecord.CreatedById` / `ServiceRecord.CreatedBy` mají Identity navigace i odpovídající inverzní kolekce.
- `Microsoft.EntityFrameworkCore.SqlServer`, `.Design`, `.Tools`, Identity EF jsou ve verzi **10.0.15**. `Microsoft.EntityFrameworkCore.Design` je i v **Web** startup projektu. Lokální `dotnet-ef` 10.0.15 je v `.config/dotnet-tools.json`.
- Žádný `Microsoft.AspNetCore.Identity.UI`, `AddRazorPages` nebo generované Identity Razor Pages. Registrace a login jsou vlastní MVC `AccountController` a `Views/Account/`.
- `VehicleForm.VehicleModel` odstraňuje kolizi s MVC prefixem `Model`; testovací uživatelé jsou v SQLite skutečně uloženi.
- Serilog je nakonfigurovaný pro strukturované logování na konzoli.

Detailní odpověď k připomínkám najdete v `docs/OBHAJOBA.md`, ER diagram v `docs/ER_diagram.mmd` a **opravené odevzdání pro 7. týden** v `docs/Navrh_aplikace_7_tyden.docx`.

## Požadavky

- .NET **10 SDK** a funkční NuGet připojení.
- Visual Studio **2026**, Rider nebo VS Code. Visual Studio 2022 net10 projekt neotevře korektně.
- SQL Server nebo LocalDB na Windows; volitelně SQL Server 2022 v Dockeru.

## Spuštění s LocalDB (Windows)

Výchozí nastavení `src/VehicleRegistry.Web/appsettings.json` ukazuje na lokální `(localdb)\\mssqllocaldb`, databázi `VehicleRegistryDb`. Nepřepisujte zde hesla veřejného serveru, použijte **User Secrets**.

```powershell
dotnet restore VehicleRegistry.slnx
dotnet tool restore
python scripts/verify_architecture.py
dotnet build VehicleRegistry.slnx

# Zkontrolovat, že entitní model neobsahuje neuložené změny
 dotnet ef migrations has-pending-model-changes --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web

# Aplikovat InitialCreate a následující migraci na lokální databázi
 dotnet ef database update --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web

# Nastavit testovacího admina jen lokálně (použijte své bezpečné heslo)
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.test" --project src/VehicleRegistry.Web
dotnet user-secrets set "BootstrapAdmin:Password" "ZVOLTE_SILNE_HESLO_NIKDY_NEKOMMITOVAT" --project src/VehicleRegistry.Web

dotnet run --project src/VehicleRegistry.Web --launch-profile https
```

Při startu aplikace se založí role `Admin`, `Manager`, `Customer`, pokud již existují správné databázové tabulky. Účet Admin se založí při prvním startu, pouze pokud jsou zadané User Secrets a e-mail ještě neexistuje. Veřejná registrace vždy vytváří Customer. Po bootstrapu lze secrets pro admina odstranit.

### Alternativa: Docker SQL Server na Windows/Linux

```bash
cp .env.example .env
# Upravte .env a zadejte vlastní silné MSSQL_SA_PASSWORD
docker compose up -d
```

Potom nastavte connection string pomocí User Secrets (heslo musí odpovídat `.env`):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=VehicleRegistryDb;User Id=sa;Password=VAŠE_VLASTNÍ_HESLO;Encrypt=True;TrustServerCertificate=True" --project src/VehicleRegistry.Web
```

Počkejte, až bude SQL Server připravený, poté spusťte `dotnet ef database update` a aplikaci. Kontejner vystavuje port pouze na `127.0.0.1`. Pro Apple Silicon lze použít vzdálený SQL Server nebo LocalDB ve Windows VM.

## Migrace a SQL schéma

- `20261021122700_InitialCreate` – původní databázová struktura včetně tabulek Identity, Vehicle.OwnerId a základních číselníků.
- `20261111123000_AddServiceRecordCreatorReference` – opravuje chybějící FK a navigaci `ServiceRecord.CreatedBy`, přidává index, přechází na `nvarchar(450)` a pro historické sirotčí odkazy převádí Creator na `NULL`. Původní migrace zůstala nezměněná, lze tak aktualizovat i existující databázi.

```bash
# Vytvořit přenosný idempotentní SQL skript (bez přístupu k databázi)
dotnet ef migrations script --idempotent --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web --output upgrade.sql

# Příští modelová změna
 dotnet ef migrations add JmenoZmeny --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web
 dotnet ef database update --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web
```

**Nevymazávejte existující migrace** na databázi, která už má data. Pro čistý projekt lze použít novou prázdnou databázi, pro rozpracovaný projekt migrační update.

## Testy, security a GitHub CI

```bash
dotnet test tests/VehicleRegistry.Tests/VehicleRegistry.Tests.csproj
python scripts/verify_architecture.py
dotnet package list --project VehicleRegistry.slnx --vulnerable --include-transitive
```

Testy používají **SQLite v paměti**, aby se skutečně ověřily FK vazby, přístup Customer, změny tachometru, vztah `CreatedBy` a validace. Nejsou náhradou integrace se **SQL Serverem**. GitHub Actions při pushi navíc spouští sestavení, EF `has-pending-model-changes` a generování SQL upgrade skriptu. `Directory.Build.props` nastavil NuGet audit i na transitivní balíčky a vysoké / kritické security problémy jako chyby restore. Pokud CI neprojde, aplikace není připravena k odevzdání.

Manuální kontrolní scénáře: `docs/TESTOVACI_PLAN.md`.

**Jedním příkazem ve Windows** lze spustit kontrolu restore/build/test/migrací bez zápisu do DB:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify_full.ps1
```

Volitelně s `-ApplyDatabase` aplikuje migrace také do nastavené lokální SQL Server databáze. Aktuální stav skutečně provedených kontrol: `docs/STAV_OVERENI.md`.

## Git a předání

- Pro aktuální GitHub repozitář s existující historií použijte nově dodanou sadu **git patchů** a `git am` (viz `docs/GIT_POSTUP.md`), nikoli `force push`.
- Pokud žádný repozitář nemáte, lze začít s přiloženou úplnou pracovní kopií včetně lokální Git historie.
- Všechny commity v přiložené historii vznikly z konkrétních změn; nepředstírají práci z minulých týdnů.
- Odevzdání 7. týdne: DOCX návrh, solution, entity, ER diagram, EF migrace a (po vašem ověření) výsledek buildu / migrací.
- Finální odevzdání: celý projekt, stav CI, otestované role a všechny obrazovky.
