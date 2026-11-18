# VehicleRegistry – registr vozidel se servisní knihou

Semestrální projekt pro předmět **Pokročilé webové technologie**. Technologie: **.NET 10, ASP.NET Core MVC, EF Core 10.0.15, SQL Server, ASP.NET Core Identity, Serilog a xUnit**. Zachovává téma a databázový základ původně odevzdané aplikace. Opravy vycházejí z připomínek vyučujícího a následného posouzení funkčnosti.

> **Stav ověření (18. 11. 2026):** Ověřeno skutečným spuštěním na Windows 11 s .NET SDK 10.0.400 a SQL Server 2022 LocalDB:
> - build proběhl bez chyb i upozornění a prošlo **26/26 testů**,
> - migrace se shodují s modelem a `database update` uspěl na nové i na starší databázi s daty,
> - NuGet audit nenašel žádné zranitelné balíčky,
> - end-to-end test `scripts/e2e_smoke.sh` prošel **76/76** kontrol, takže jsou ověřeny všechny scénáře T01–T21 z `docs/TESTOVACI_PLAN.md` ve všech rolích, včetně fotek, CRUD a zabezpečení.
>
> Podrobné výsledky a sedm nalezených a opravených nedostatků jsou v `docs/STAV_OVERENI.md`.

## Funkce aplikace

| Role | Funkce |
|---|---|
| Anonymní | Úvod, vlastní MVC registrace a přihlášení. Nový účet získá roli `Customer`. |
| Customer | CRUD vlastních vozidel, JPG/PNG fotografie do 2 MB, historie servisů, doporučení údržby. |
| Manager | Hledání vozidel podle VIN/SPZ, správa servisních záznamů, typů úkonů a servisních dílen. |
| Admin | Administrace účtů a rolí, správa vozidel, číselníků a všech servisních záznamů. |

**Bezpečnost:** Identity hashuje hesla a po 5 špatných pokusech účet dočasně zamkne. POST metody používají antiforgery validaci. Customer vidí jen vlastní vozidla (kontrola i v aplikačních službách), role se kontrolují v controllerech a změna role nebo smazání účtu platí hned při dalším požadavku. VIN a SPZ musí být unikátní. Fotky mají kontrolu signatury i maximální velikosti (v prohlížeči i na serveru). Chyby 400/404 mají vlastní českou stránku. Hesla se nesmí commitovat.

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

**LocalDB není součástí .NET SDK.** Pokud chybí, `database update` skončí chybou 52 („Nepovedlo se najít instalaci modulu runtime místní databáze“). LocalDB se nainstaluje s Visual Studiem (workload „ASP.NET a vývoj webu“), nebo samostatně jako `SqlLocalDB.msi` z instalátoru SQL Server Express (volba „Download Media“ → LocalDB). Kontrola: `SqlLocalDB info`. Projekt umístěte do krátké cesty (např. `C:\projekty\VehicleRegistry`), jinak může `dotnet test` narazit na limit délky cesty ve Windows.

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

### End-to-end test všech rolí

`scripts/e2e_smoke.sh` projde běžící aplikaci přes HTTP jako uživatel: registrace, zamčení účtu, vozidla, fotky, servisní záznamy, číselníky, správa uživatelů a kontroly oprávnění (76 kontrol, scénáře T01–T18). Potřebuje **prázdnou databázi**, protože zakládá pevné testovací účty.

```powershell
# 1) Aplikace nad novou prázdnou databází s testovacím adminem (PowerShell)
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=VehicleRegistryDb_E2E;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
dotnet ef database update --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web
$env:BootstrapAdmin__Email = "admin@e2e.test"; $env:BootstrapAdmin__Password = "ZVOLTE_SILNE_HESLO1"
dotnet run --project src/VehicleRegistry.Web --launch-profile http
```

```bash
# 2) V druhém okně v Git Bash (potřebuje curl a Python 3)
ADMIN_EMAIL=admin@e2e.test ADMIN_PASS='ZVOLTE_SILNE_HESLO1' CUST_PASS='Zakaznik123' bash scripts/e2e_smoke.sh
```

Výsledek posledního běhu je v `docs/E2E_VYSLEDEK.txt`. Testovací databázi pak lze smazat.

## Git a předání

- Repozitář na GitHubu obsahuje celý projekt a GitHub Actions při každém pushi spustí build, testy, kontrolu migrací a audit balíčků (`.github/workflows/dotnet.yml`). Postup je v `docs/GIT_POSTUP.md`.
- Historie začíná importem opravené verze ze 7. týdne a pokračuje skutečnými opravami z ověřování. Nepředstírá práci z minulých týdnů.
- Odevzdání 7. týdne: DOCX návrh, solution, entity, ER diagram, EF migrace a výsledek buildu, testů a migrací (`docs/STAV_OVERENI.md`).
- Finální odevzdání: celý projekt, stav CI, otestované role a všechny obrazovky (`docs/STAV_OVERENI.md`, `docs/E2E_VYSLEDEK.txt`, `docs/OBHAJOBA.md`).
