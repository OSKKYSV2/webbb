# Stav ověření

Tento soubor odlišuje vytvořený kód od skutečně ověřené aplikace. Všechny výsledky níže pocházejí ze skutečného spuštění, ne ze statické kontroly. Co spuštěno nebylo, je označeno jako **NEOVĚŘENO**.

- **Datum ověření:** 17. 11. 2026 (build, testy a `database update` proběhly úspěšně už 13. 11. 2026)
- **Prostředí:** Windows 11 Pro, .NET SDK 10.0.400, lokální nástroj `dotnet-ef` 10.0.15, SQL Server 2022 Express LocalDB 16.0.1000.6 (`(localdb)\mssqllocaldb`), Python 3.13
- **Postup:** `scripts/verify_full.ps1 -ApplyDatabase` proti nové prázdné databázi, kontrola schématu dotazem do `sys.foreign_keys` a `sys.indexes`, upgrade starší databáze s daty a ruční scénáře v prohlížeči podle `TESTOVACI_PLAN.md`

## Automatické kontroly

| Kontrola | Stav | Výsledek |
|---|---|---|
| Závislosti vrstev, verze EF Design/Tools, názvy MVC polí | **PASS** | `python scripts/verify_architecture.py` |
| Restore a NuGet audit včetně transitivních balíčků | **PASS** | `dotnet package list --vulnerable --include-transitive`: žádný z 5 projektů nemá zranitelné balíčky |
| Sestavení .NET 10 (Release) | **PASS** | `dotnet build VehicleRegistry.slnx`: 0 chyb, 0 upozornění |
| Automatické testy | **PASS** | `dotnet test`: 21 úspěšných, 0 neúspěšných, 0 přeskočených |
| Shoda EF modelu a migrací (T21) | **PASS** | `has-pending-model-changes`: „No changes have been made to the model since the last migration.“ |
| Idempotentní SQL skript | **PASS** | `dotnet ef migrations script --idempotent` vygeneroval `upgrade.sql` |
| Migrace na čisté SQL Server DB (T19) | **PASS** | `database update` aplikoval obě migrace. Existují všechny FK, včetně `FK_ServiceRecords_AspNetUsers_CreatedById` (ON DELETE SET NULL), a indexy, včetně unikátních indexů VIN, SPZ a názvu výrobce. Seed: 3 výrobci, 4 druhy úkonů. |
| Upgrade starší DB s daty (T20) | **PASS** | DB na migraci `InitialCreate` naplněná daty: uživatel, vozidlo a 3 servisní záznamy (platný autor, neexistující autor, bez autora). Po `database update`: data zachována, platný autor zůstal napojený, neexistující autor převeden na `NULL`, `CreatedById` je `nvarchar(450)` s FK a indexem. |
| GitHub Actions (T22) | **NEOVĚŘENO** | Workflow `.github/workflows/dotnet.yml` existuje, ale na GitHubu zatím neběžel. Stejné kroky prošly lokálně (viz výše). |
| Git commity / patch import na původní ZIP | **PASS (převzato)** | Uvedl autor opravné verze (`git format-patch`, `git am`). Při tomto ověření se znovu nekontrolovalo. |

## Ruční scénáře v prohlížeči

Web byl spuštěn přes `dotnet run --launch-profile http` proti nové databázi. Administrátor byl založen přes `BootstrapAdmin` z proměnných prostředí, tedy mimo repozitář.

| ID | Scénář | Stav | Co se ukázalo |
|---|---|---|---|
| – | Start aplikace | **PASS** | Při startu vzniknou role Admin, Manager a Customer a založí se bootstrap admin. |
| T01 | Registrace | **PASS** | Nový účet má roli Customer a je rovnou přihlášený. |
| T02 | Duplicitní e-mail | **PASS*** | Účet nevznikne, ale hlášky jsou anglicky (viz zjištění 2). |
| T03 | Špatné heslo | **PASS** | „Nesprávný e-mail nebo heslo.“ Lockout po 5 pokusech se nezkoušel. |
| T04 | Customer vytvoří vozidlo | **PASS** | Detail zobrazí VIN, SPZ, výrobce a nájezd. |
| T05 | VIN s písmenem O | **PASS** | „VIN musí obsahovat přesně 17 znaků bez I, O a Q.“ |
| T06 | Duplicitní VIN | **PASS** | „Toto VIN už je evidováno.“ Duplicitní SPZ se zvlášť nezkoušela. |
| T07 | Cizí vozidlo přes URL | **PASS** | `Details` i `Edit` cizího vozidla vrací 404, seznam „Moje vozidla“ je prázdný. |
| – | Customer otevře `/Admin` a `/ServiceRecords` | **PASS** | Přesměrování na „Nemáte oprávnění“. |
| T08, T09 | Nahrání fotografie | **NEOVĚŘENO** | Ručně nezkoušeno. |
| T10 | Manager hledá podle VIN a SPZ | **PASS** | Najde vozidlo podle obou údajů. |
| T11 | Dokončený servis se 150 000 km | **PASS** | Nájezd vozidla 120 000 → 150 000 km, v DB je uložen autor záznamu (`CreatedBy`), doporučení údržby se přepočítalo. |
| T12 | Dokončený servis s budoucím datem | **PASS** | „Datum dokončeného servisu nesmí být v budoucnu.“ |
| T13 | Úprava a smazání servisního záznamu | **NEOVĚŘENO** | Ručně nezkoušeno. |
| T14 | Manager a číselníky | **ČÁSTEČNĚ** | Druhy úkonů jsou dostupné, výrobci a správa uživatelů odepřeny. CRUD číselníků se nezkoušel. |
| T15 | Správa v roli Admin | **ČÁSTEČNĚ** | Seznam uživatelů a změna role Customer → Manager fungují. CRUD výrobců, vozidel a záznamů se nezkoušel. |
| T16 | Admin si odebere vlastní roli | **PASS*** | Server změnu odmítne a role zůstane, formulář ale nezobrazí chybovou hlášku (viz zjištění 1). |
| T17 | Smazání uživatele s vozidlem | **PASS** | „Uživatel vlastní vozidla; nejprve je převeďte nebo smažte.“ |
| T18 | Smazání autora servisního záznamu | **ČÁSTEČNĚ** | Chování FK (ON DELETE SET NULL) ověřeno v databázi, přes UI nezkoušeno. |

\* Funkčně v pořádku, ale s drobnou vadou v UI.

## Zjištěné nedostatky

1. **Admin → Upravit uživatele:** v `src/VehicleRegistry.Web/Areas/Admin/Views/Users/Form.cshtml` chybí u pole Role `<span asp-validation-for="Role">` a souhrn chyb je `ModelOnly`. Hláška „Nelze odebrat roli aktuálnímu nebo poslednímu administrátorovi.“ se proto nezobrazí a stránka se jen znovu načte. Samotná ochrana funguje.
2. **Registrace s existujícím e-mailem:** chyby z ASP.NET Identity jsou anglicky („Username '…' is already taken.“, „Email '…' is already taken.“). Chybí česky přeložený `IdentityErrorDescriber`.

## Poznámky k prostředí (nejde o chyby kódu)

- **LocalDB není součástí .NET SDK.** Bez ní `dotnet ef database update` skončí chybou 52 („Nepovedlo se najít instalaci modulu runtime místní databáze“). LocalDB se nainstaluje s Visual Studiem (workload „ASP.NET a vývoj webu“), nebo samostatně jako `SqlLocalDB.msi` z instalátoru SQL Server Express (volba „Download Media“ → LocalDB). Kontrola: `SqlLocalDB info`.
- **Dlouhá cesta k projektu:** když projekt leží ve velmi dlouhé cestě, `dotnet test` selže s hláškou „Název nebo přípona souboru je příliš dlouhá“ (limit Windows 260 znaků). Stačí kratší cesta, např. `C:\projekty\VehicleRegistry`.
