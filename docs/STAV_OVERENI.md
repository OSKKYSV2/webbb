# Stav ověření

Tento soubor odlišuje vytvořený kód od skutečně ověřené aplikace. Všechny výsledky níže pocházejí ze skutečného spuštění, ne ze statické kontroly. Co spuštěno nebylo, je označeno jako **NEOVĚŘENO**.

- **Datum posledního ověření:** 18. 11. 2026 (předchozí běhy 13. a 17. 11. 2026)
- **Prostředí:** Windows 11 Pro, .NET SDK 10.0.400, lokální nástroj `dotnet-ef` 10.0.15, SQL Server 2022 Express LocalDB 16.0.1000.6 (`(localdb)\mssqllocaldb`), Python 3.13, Git Bash s curl
- **Postup:** `scripts/verify_full.ps1 -ApplyDatabase` na čisté kopii projektu a nové databázi, `scripts/e2e_smoke.sh` proti běžící aplikaci s prázdnou databází, kontrola schématu v `sys.foreign_keys` a `sys.indexes`, upgrade starší databáze s daty a kontroly v prohlížeči

## Souhrn

| Oblast | Výsledek |
|---|---|
| Build (Release) | **PASS**: 0 chyb, 0 upozornění |
| Automatické testy (xUnit) | **PASS**: 26/26 |
| Shoda EF modelu a migrací | **PASS** |
| Migrace na SQL Serveru, čistá i starší DB s daty | **PASS** |
| NuGet audit včetně transitivních balíčků | **PASS**: žádné zranitelnosti |
| End-to-end test všech rolí (`scripts/e2e_smoke.sh`) | **PASS**: 76/76, výstup v `docs/E2E_VYSLEDEK.txt` |
| Scénáře T01–T22 z `TESTOVACI_PLAN.md` | **PASS** všechny kromě T22 (GitHub Actions, viz níže) |

## Automatické kontroly

| Kontrola | Stav | Výsledek |
|---|---|---|
| Závislosti vrstev, verze EF Design/Tools, názvy MVC polí | **PASS** | `python scripts/verify_architecture.py` |
| Restore a NuGet audit | **PASS** | `dotnet package list --vulnerable --include-transitive`: žádný z 5 projektů nemá zranitelné balíčky |
| Sestavení .NET 10 (Release) | **PASS** | `dotnet build VehicleRegistry.slnx`: 0 chyb, 0 upozornění |
| Automatické testy | **PASS** | `dotnet test`: 26 úspěšných, 0 neúspěšných, 0 přeskočených |
| Shoda EF modelu a migrací (T21) | **PASS** | `has-pending-model-changes`: „No changes have been made to the model since the last migration.“ |
| Idempotentní SQL skript | **PASS** | `dotnet ef migrations script --idempotent` vygeneroval `upgrade.sql` |
| Migrace na čisté SQL Server DB (T19) | **PASS** | `database update` aplikoval obě migrace. Existují všechny FK, včetně `FK_ServiceRecords_AspNetUsers_CreatedById` (ON DELETE SET NULL), a indexy, včetně unikátních indexů VIN, SPZ a názvu výrobce. Seed: 3 výrobci, 4 druhy úkonů. |
| Upgrade starší DB s daty (T20) | **PASS** | DB na migraci `InitialCreate` naplněná daty: uživatel, vozidlo a 3 servisní záznamy (platný autor, neexistující autor, bez autora). Po `database update`: data zachována, platný autor zůstal napojený, neexistující autor převeden na `NULL`, `CreatedById` je `nvarchar(450)` s FK a indexem. |
| GitHub Actions (T22) | **NEOVĚŘENO** | Workflow `.github/workflows/dotnet.yml` je připravený. Výsledek běhu na GitHubu se doplní po nahrání repozitáře. Stejné kroky lokálně prošly. |

## End-to-end test (`scripts/e2e_smoke.sh`)

Skript projde aplikaci přes HTTP stejně jako prohlížeč: přihlašuje se, posílá formuláře s antiforgery tokenem, nahrává soubory a kontroluje odpovědi. Spouští se proti běžící aplikaci s prázdnou databází (návod je v hlavičce skriptu a v `README.md`). Poslední běh: **76 PASS, 0 FAIL**, celý výstup je v `docs/E2E_VYSLEDEK.txt`.

| ID | Scénář | Stav | Co skript ověřuje |
|---|---|---|---|
| T01 | Registrace | **PASS** | Tři noví zákazníci, každý je po registraci přihlášený. |
| T02 | Duplicitní e-mail | **PASS** | Účet nevznikne, zobrazí se jedna česká hláška. |
| T03 | Špatné heslo a zamčení | **PASS** | Po 4 pokusech obecná hláška, po 5. pokusu je účet uzamčen a odmítne i správné heslo. |
| T04 | Customer vytvoří a upraví vozidlo | **PASS** | Vytvoření, úprava a zobrazení včetně diakritiky. |
| T05 | Neplatné VIN | **PASS** | VIN s písmenem O odmítnut. |
| T06 | Duplicitní VIN a SPZ | **PASS** | Obojí odmítnuto s hláškou. |
| T07 | Cizí vozidlo | **PASS** | Detail, úprava, fotka i smazání cizího vozidla vrací 404 se srozumitelnou stránkou. |
| T08 | Fotografie PNG a JPG | **PASS** | Nahrání PNG a pak JPG, nová fotka je na webu dostupná a stará se smaže z disku. |
| T09 | Nepovolené a velké soubory | **PASS** | Podvržený .exe, 2,5 MB a 3,5 MB odmítnuty s hláškou „max. 2 MB“, 11 MB skončí čitelnou stránkou 400. Původní fotka zůstane. Formulář hlídá 2 MB i v prohlížeči. |
| T10 | Manager hledá | **PASS** | Najde vozidlo podle VIN i podle SPZ. |
| T11 | Dokončený servis | **PASS** | Nájezd 125 000 → 150 000 km. Tachometr pak nejde snížit pod hodnotu z dokončeného servisu. |
| T12 | Servis s budoucím datem | **PASS** | Odmítnut. |
| T13 | Úprava a smazání servisu | **PASS** | Změna ceny se projeví, smazání proběhne s hláškou. |
| T14 | Manager a číselníky | **PASS** | Přidání, úprava a smazání druhu úkonu i servisu. Použitý druh úkonu a servis bez adresy odmítnuty. Výrobci a správa uživatelů odepřeny. |
| T15 | Správa v roli Admin | **PASS** | Role uživatele, vytvoření a smazání účtu. Přidání, úprava a smazání výrobce, duplicitní výrobce odmítnut, použitého výrobce nelze smazat. Vozidlo pro zákazníka: vytvoření, úprava, smazání. Přehled všech servisních záznamů. |
| T16 | Ochrana admina | **PASS** | Odebrání vlastní role i smazání vlastního účtu odmítnuto s hláškou, účet zůstane. |
| T17 | Smazání uživatele s vozidlem | **PASS** | Odmítnuto s vysvětlením. |
| T18 | Smazání autora servisního záznamu | **PASS** | Záznam zůstane v historii vozidla (FK ON DELETE SET NULL ověřen i v T20). |
| – | Další zabezpečení | **PASS** | Customer nemá přístup do administrace ani k servisům. Manager nemůže zakládat vozidla. POST bez antiforgery tokenu vrací 400. Nová role platí hned a smazaný uživatel je hned odhlášen. |
| – | Odhlášení | **PASS** | Po odhlášení přesměruje chráněná stránka na přihlášení. |

## Kontroly v prohlížeči

Kromě skriptu bylo ručně ověřeno (17. a 18. 11. 2026):

- formuláře registrace, přihlášení, vozidla, servisu a správy uživatelů včetně klientské validace VIN a budoucího data,
- hláška u pole Role při pokusu odebrat si roli Admin,
- kontrola velikosti fotky v prohlížeči: 3 MB soubor zablokuje hláškou „Obrázek musí mít maximálně 2 MB.“, malý soubor pustí,
- stránka 404 pro neexistující nebo cizí vozidlo („Stránka nenalezena“).

## Nedostatky nalezené při ověření (vše opraveno)

| # | Problém | Oprava |
|---|---|---|
| 1 | Při pokusu odebrat si roli Admin se nezobrazila chybová hláška. | `Areas/Admin/Views/Users/Form.cshtml`: validační `<span>` u pole Role. |
| 2 | Chyby ASP.NET Identity byly anglicky a u duplicitního účtu dvakrát. | `Infrastructure/Identity/CzechIdentityErrorDescriber.cs`, registrace v `DependencyInjection.cs`, `AccountController` a `Admin/UsersController` zobrazují každou hlášku jednou. Testy: `IdentityErrorTests.cs`. |
| 3 | Změna role nebo smazání účtu se projevily až po novém přihlášení nebo za 30 minut, takže sesazený Admin/Manager měl ještě stará práva. | `DependencyInjection.cs`: `SecurityStampValidatorOptions.ValidationInterval = TimeSpan.Zero`, účet se ověřuje při každém požadavku. Test: `IdentityConfigurationTests.cs`, E2E kontroly „Nová role platí hned“ a „Smazaný uživatel je hned odhlášen“. |
| 4 | Pokus smazat vlastní účet nebo posledního admina skončil holou stránkou 400 s textem. | `Admin/UsersController.Delete`: hláška v seznamu uživatelů a přesměrování. |
| 5 | Fotka nad 3 MB skončila prázdnou stránkou 400 místo hlášky „max. 2 MB“. | `VehiclesController.UploadPhoto`: limit požadavku 10 MB, takže hlášku dostane vše nad 2 MB. `Details.cshtml` a `site.js`: kontrola velikosti už v prohlížeči. |
| 6 | Chyby 400/404 zobrazovaly prázdnou stránku. | `Program.cs`: `UseStatusCodePagesWithReExecute`, `HomeController.Status` a `Views/Home/Status.cshtml` s českým textem. Původní stavový kód zůstává. |
| 7 | `.gitignore` nehlídal fotky nahrané uživateli ani vygenerovaný `upgrade.sql`. | Doplněno `src/VehicleRegistry.Web/wwwroot/uploads/` a `upgrade.sql`. |

## Co aplikace záměrně nemá

- **Obnova zapomenutého hesla e-mailem:** aplikace nemá napojenou e-mailovou službu, takže tuto funkci nenabízí. Účet zamčený po 5 špatných pokusech se odemkne sám po 5 minutách (výchozí nastavení ASP.NET Identity).

## Poznámky k prostředí (nejde o chyby kódu)

- **LocalDB není součástí .NET SDK.** Bez ní `dotnet ef database update` skončí chybou 52 („Nepovedlo se najít instalaci modulu runtime místní databáze“). LocalDB se nainstaluje s Visual Studiem (workload „ASP.NET a vývoj webu“), nebo samostatně jako `SqlLocalDB.msi` z instalátoru SQL Server Express (volba „Download Media“ → LocalDB). Kontrola: `SqlLocalDB info`.
- **Dlouhá cesta k projektu:** když projekt leží ve velmi dlouhé cestě, `dotnet test` selže s hláškou „Název nebo přípona souboru je příliš dlouhá“ (limit Windows 260 znaků). Stačí kratší cesta, např. `C:\projekty\VehicleRegistry`.
