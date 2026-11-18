# Git – zachování historie a příprava odevzdání

Původní `VehicleRegistry-main.zip` neobsahuje složku `.git`, proto nelze obnovit původní autory/časové značky. Dodaný archiv se zdroji a místní Git historií obsahuje skutečné commity nad nahraným snapshotem. **Datumy a commity z dřívějšího semestru nevymýšlíme a původní GitHub historii nepřepisujeme.**

## Doporučeno: navázat na existující GitHub repozitář

1. Naklonujte původní repozitář a ověřte čistý pracovní strom:

   ```bash
   git clone https://github.com/VAŠE_JMENO/VehicleRegistry.git
   cd VehicleRegistry
   git status
   git switch -c opravy-po-kontrole
   ```

2. Přiložený ZIP `VehicleRegistry-zmeny-format-patch.zip` obsahuje commity s opravami. Patch soubory rozbalte do složky `patches` vedle vašeho klonu; příkazy z jeho kořene:

   ```bash
   git am --ignore-date ../patches/*.patch
   git log --oneline -20
   git status
   ```

   `--ignore-date` znamená, že **committer date bude datum aplikování**, původní autorská data v patchi zůstanou. Pokud používáte cizí původní historii, nejprve ověřte, že base commit odpovídá nahranému ZIPu; při konfliktech příkaz odmítne změnu. `git am --abort` vrátí rozpracované aplikování.

3. **Nedělejte force push.** Až projdou testy, proveďte:

   ```bash
   git push -u origin opravy-po-kontrole
   ```

   Potom otevřete Pull Request do main a zkontrolujte výsledek GitHub Actions.

## Alternativa: založit nový GitHub repozitář z přiložené historie

Rozbalte archiv s `.git`, nastavte vlastní origin a odešlete pracovní větev na nové URL. Upozornění: první commit je import přiloženého ZIPu, ne skutečný původní historický postup vašich starších vývojových týdnů.

```bash
git remote remove origin # jen pokud existuje starý origin
git remote add origin https://github.com/VAŠE_JMENO/VehicleRegistry.git
git branch -M main
git push -u origin main
```

## Obsah commitů a odevzdání

Commity rozdělují logické změny: oddělení vrstev, implementace služeb, UI, vlastní MVC Identity, testy, bezpečnostní opravy, navigace a následnou migraci, CI a dokumentaci. Nenechávejte na GitHubu `obj`, `bin`, `secrets`, fotky uživatelů ani `.env`.

- **7. týden**: `docs/Navrh_aplikace_7_tyden.docx`, `docs/ER_diagram.mmd`, solution, Domain entity, Infrastructure konfigurace, migrace, doklad buildu + DB update.
- **Finální odevzdání**: celá solution, CI výsledky, testy, manuálně ověřené role, CRUD a obhajoba.
