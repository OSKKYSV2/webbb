# Stav ověření k opravné verzi

Tento soubor odlišuje vytvořený kód od skutečně ověřené aplikace.

| Kontrola | Stav | Způsob |
|---|---|---|
| Projektové závislosti vrstev | **PASS (statická)** | `python scripts/verify_architecture.py` |
| Původní / opravené názvy MVC polí | **PASS (statická)** | `python scripts/verify_architecture.py` |
| Umístění Tools / Design, jednotné EF balíčky | **PASS (statická)** | kontrola `.csproj` a tool manifestu |
| Zranitelné transitivní knihovny | **NEOVĚŘENO běžícím restore** | `dotnet restore`, `dotnet package list --vulnerable --include-transitive` |
| Sestavení .NET 10 | **NEOVĚŘENO** | `dotnet build VehicleRegistry.slnx` |
| Automatické testy | **NEOVĚŘENO** | `dotnet test` |
| Shoda EF modelu a migrací | **NEOVĚŘENO** | `dotnet ef migrations has-pending-model-changes` |
| Migrace na SQL Serveru | **NEOVĚŘENO** | `dotnet ef database update` na čisté i původní DB |
| Spuštění webu a obhajoba | **NEOVĚŘENO** | Manuální scénáře v `TESTOVACI_PLAN.md` |
| Git commity / patch import na původní ZIP | **PASS (lokální)** | `git format-patch`, `git am` na čistém importu |

Poznámka: v prostředí, ve kterém tato verze vznikla, nebylo dostupné .NET SDK ani SQL Server. Nevyplňujte zde „PASS“ bez skutečného spuštění. GitHub Actions kontrolují build, testy a migrační shodu po pushi; jejich výsledek se musí ověřit na GitHubu.
