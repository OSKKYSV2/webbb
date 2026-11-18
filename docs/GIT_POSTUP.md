# Git – historie a předání

Původní `VehicleRegistry-main.zip` neobsahoval složku `.git`, proto nelze obnovit původní autory a časové značky. **Datumy a commity z dřívějšího semestru nevymýšlíme.**

## Jak historie vznikla

Repozitář na GitHubu začíná importem opravené verze odevzdané v 7. týdnu. Další commity jsou skutečné změny z ověřování a dokončování projektu v listopadu 2026, v tomto pořadí:

1. import opravené verze projektu (stav odevzdání 7. týdne),
2. doplnění skutečných výsledků ověření do dokumentace,
3. opravy z ověřování: hláška u role administrátora, české chyby Identity,
4. opravy z end-to-end testu: okamžitá platnost změn rolí, čitelné chybové stránky, limit velikosti fotek,
5. end-to-end test `scripts/e2e_smoke.sh` a finální dokumentace.

Přesný seznam ukáže `git log --oneline`.

## Práce s repozitářem

```bash
git clone <adresa repozitáře na GitHubu>
cd VehicleRegistry
git status
```

Každý push spustí GitHub Actions (`.github/workflows/dotnet.yml`): restore s NuGet auditem, build, testy, kontrolu shody EF modelu s migracemi, vygenerování SQL skriptu a výpis zranitelných balíčků. Výsledek je na záložce **Actions** v repozitáři. Pokud CI neprojde, aplikace není připravená k odevzdání.

**Nedělejte force push** a nepřepisujte historii. Na GitHub nepatří `bin`, `obj`, `.env`, User Secrets, fotky uživatelů (`wwwroot/uploads`) ani `upgrade.sql`; všechno hlídá `.gitignore`.

## Co patří k odevzdání

- **7. týden**: `docs/Navrh_aplikace_7_tyden.docx`, `docs/ER_diagram.mmd`, solution, Domain entity, Infrastructure konfigurace, migrace, doklad buildu a DB update (`docs/STAV_OVERENI.md`).
- **Finální odevzdání (14. týden)**: celá solution v repozitáři, zelený běh GitHub Actions, testy, ověřené role a CRUD (`docs/STAV_OVERENI.md`, `docs/E2E_VYSLEDEK.txt`) a podklady k obhajobě (`docs/OBHAJOBA.md`).
