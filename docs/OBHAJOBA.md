# VehicleRegistry – obhajoba a odpověď na připomínky

## Opravy původního odevzdání

| Připomínka | Soubor / řešení |
|---|---|
| `Infrastructure` nesmí odkazovat na `Application` | `src/VehicleRegistry.Infrastructure/*.csproj`: jediná přímá projektová reference je `Domain`. Aplikační služby i Infrastructure jsou nezávislé. |
| Repo interface a `IUnitOfWork` nepatří do Application | Rozhraní v `Domain/Repositories`; specifická pro Manufacturer, Vehicle, ServiceRecord, ServiceType, ServiceCenter. |
| Tools pro migrace + startup Design | `Infrastructure.csproj` má EF Tools i Design 10.0.15; `Web.csproj` má Design 10.0.15; dotnet-ef 10.0.15 v tool manifestu. |
| Starý EF 10.0.0 a zranitelný PKCS | Všechny aktuální EF/Identity package reference jsou 10.0.15. SQL Server provider požaduje opravené transitivní závislosti. NuGetAudit=all; vysoké/kritické chyby zablokují restore. |
| Chybějící navigace na vlastníka | `Vehicle.Owner` a `ApplicationUser.Vehicles`; `OwnerId` je povinný FK. |
| Chybějící navigace na autora servisu | `ServiceRecord.CreatedBy`, `ApplicationUser.CreatedServiceRecords`; následná migrace přidává index a FK. |
| Identity.UI + generované Razor Pages nejsou povoleny | Vlastní MVC `AccountController`, `Views/Account/Login.cshtml`, `Register.cshtml`. Bez `.AddRazorPages`. |
| Neplatný binding vozidla | `VehicleForm.VehicleModel` namísto `Model` (shodný s MVC prefixem akce); odpovídající View a Controller. |
| Nepravdivě zelené testy | Testovací Identity uživatelé se opravdu ukládají. Testy nyní používají SQLite s FK. |
| Snapshot / chybějící migrace | Zachován původní InitialCreate; přidána `AddServiceRecordCreatorReference` bez ztráty historie. CI kontroluje model i SQL skript. |

## Scénář obhajoby (demo)

1. V Solution Exploreru ukázat 4 vrstvy a jejich `.csproj` reference. `Web` registruje služby v `Program.cs`; `Infrastructure` neodkazuje na Application.
2. Ukázat `Domain/Repositories` a konkrétní rozhraní. Rozkliknout EF `DbContext`, `EntityConfigurations`, `Migration` a `ApplicationUser` navigační vlastnosti.
3. Otevřít vlastní MVC `AccountController` a jeho `Login/Register` views. Zaregistrovat Customer, přihlásit se.
4. Přidat vlastní vozidlo, otestovat VIN, SPZ, cenu a tachometr, nahrát JPG/PNG fotografii a zkusit nepovolený soubor.
5. Na druhém Customer účtu se pokusit otevřít cizí auto přímým URL (očekáván 404). Manager umí vyhledávat všechna auta, ale neupravuje vozidla ani uživatele.
6. V Manager roli zapsat dokončený servis – stav tachometru se nesmí snížit. Upravit a smazat servisní záznam, upravit číselníky servisu.
7. V Admin roli otevřít `/Admin`, spravovat výrobce a uživatele, přiřadit role. Pokusit se odebrat si roli Admin nebo smazat vlastní účet (musí být odmítnuto s hláškou). Po změně role ukázat, že platí hned bez nového přihlášení.
8. `dotnet build`, `dotnet test`, `dotnet ef migrations has-pending-model-changes` a `database update` ukázat ve výstupu (připravit předem). Na GitHubu ukázat zelený běh GitHub Actions.
9. Volitelně spustit `scripts/e2e_smoke.sh` proti prázdné databázi (76 automatických kontrol všech rolí, viz `README.md`) nebo ukázat `docs/E2E_VYSLEDEK.txt`.

## Co je ověřeno (18. 11. 2026)

- [x] .NET 10 build je úspěšný (0 chyb, 0 upozornění).
- [x] SQLite relační testy procházejí (26/26).
- [x] `dotnet ef migrations has-pending-model-changes` vrací bez změn.
- [x] `dotnet ef database update` uspěje na nové i na již migrované SQL Server databázi s daty (SQL Server 2022 LocalDB).
- [x] NuGet audit nevypisuje žádné zranitelnosti, ani u transitivních balíčků.
- [x] Vlastní MVC registrace, přihlášení, zamčení účtu po 5 špatných pokusech a odhlášení fungují. Obnovu zapomenutého hesla e-mailem aplikace nemá (bez e-mailové služby).
- [x] Role a bezpečnostní scénáře fungují: cizí vozidlo vrací 404, Customer a Manager nemají přístup tam, kam nemají, ochrana admina funguje s hláškou, nová role platí hned, POST bez antiforgery tokenu je odmítnut.
- [x] Fotografie, CRUD a typy servisů fungují end-to-end: vozidla, servisní záznamy, druhy úkonů, servisy, výrobci a uživatelé včetně úprav a mazání (`scripts/e2e_smoke.sh`, 76/76).
- [ ] Zelený běh GitHub Actions doložený na GitHubu.

Výsledky pocházejí ze skutečného spuštění, podrobnosti jsou v `docs/STAV_OVERENI.md` a `docs/E2E_VYSLEDEK.txt`.
