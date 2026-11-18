# Akceptační a regresní testovací scénáře

Tento dokument je kontrolní plán. Neoznačuje testy automaticky za úspěšné.

| ID | Uživatelský scénář | Očekávaný výsledek |
|---|---|---|
| T01 | Nová registrace s validním e-mailem a heslem | Vznikne Customer, je přihlášený. |
| T02 | Duplicitní e-mail | Chybová zpráva bez vytvoření účtu. |
| T03 | Špatné heslo při loginu | Zůstane nepřihlášen; po opakovaných pokusech lockout. |
| T04 | Customer vytvoří auto s VIN/SPZ | Nový detail, výrobce, nájezd a vlastnictví. |
| T05 | VIN kratší než 17 / s I,O,Q | Validace odmítne uložení. |
| T06 | Stejné VIN nebo SPZ u dalšího auta | Aplikace odmítne duplicitu. |
| T07 | Cizí Customer zadá URL detailu/editace/smazání | Nevidí ani neupraví jiné auto. |
| T08 | Customer nahraje PNG/JPG do 2 MB | Fotografie se zobrazí v detailu. |
| T09 | Nahrání .exe, obsahu jiné přípony nebo souboru >2 MB | Odmítnuto, stará fotografie zachována. |
| T10 | Manager najde vozidlo podle VIN/SPZ | Najde auto a vidí servisní historii. |
| T11 | Manager přidá dokončený servis 150 000 km | Nájezd auta se zaktualizuje na max(nynější,150000). |
| T12 | Dokončený servis s budoucím datem | Odmítnuto. |
| T13 | Manager editace a smazání servisního záznamu | Změny se projeví v historii. |
| T14 | Manager spravuje druhy úkonů a dílny | CRUD povoleno, výrobce a uživatele nesmí. |
| T15 | Admin spravuje výrobce, vozidla, záznamy, účty | CRUD dostupné podle rolí. |
| T16 | Admin odebere svou nebo poslední administrátorskou roli | Odmítnuto. |
| T17 | Admin smaže uživatele vlastnícího vozidlo | Odmítnuto s vysvětlením. |
| T18 | Admin smaže autora starého servisního záznamu bez aut | Záznam zůstane, CreatedById přejde na null. |
| T19 | Čistá SQL Server databáze + `database update` | Všechny migrace aplikovány, existují FK i indexy. |
| T20 | Migrace z původní DB s platnými servisními daty | Data zachována, validní CreatedBy vazby funkční. |
| T21 | `has-pending-model-changes` | Žádný rozdíl mezi aktuálním modelem a poslední migrací. |
| T22 | CI restore/build/test + security audit | Vše zelené bez high/critical NU1903/NU1904. |

Doporučený postup: začít novou databází, zaregistrovat 2 zákazníky, nastavit role Manager/Admin, provést scénáře a zaznamenat případné selhání do issue na GitHubu. Pro 7. týden stačí doložit architekturu, entity, návazné migrace, funkční databázi a návrh aplikace. Pro finální obhajobu projít všechny relevantní scénáře.
