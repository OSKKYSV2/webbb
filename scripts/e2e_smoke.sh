#!/usr/bin/env bash
# End-to-end HTTP check of a running VehicleRegistry instance (all roles, CRUD, photos, security).
#
# Needs an EMPTY database, because it registers fixed test accounts. Start the app first, e.g.:
#   $env:ConnectionStrings__DefaultConnection = "Server=(localdb)\mssqllocaldb;Database=VehicleRegistryDb_E2E;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
#   dotnet ef database update --project src/VehicleRegistry.Infrastructure --startup-project src/VehicleRegistry.Web
#   $env:BootstrapAdmin__Email = "admin@e2e.test"; $env:BootstrapAdmin__Password = "<silné heslo>"
#   dotnet run --project src/VehicleRegistry.Web --launch-profile http
# Then, in Git Bash (Windows) or bash (Linux/macOS):
#   ADMIN_EMAIL=admin@e2e.test ADMIN_PASS='<heslo admina>' CUST_PASS='<heslo pro testovací zákazníky>' bash scripts/e2e_smoke.sh
# Requires bash, curl and Python 3. Exit code 0 means every check passed.
set -u
BASE=${1:-http://localhost:5144}
: "${ADMIN_EMAIL:?Set ADMIN_EMAIL}" "${ADMIN_PASS:?Set ADMIN_PASS}" "${CUST_PASS:?Set CUST_PASS (8+ chars, digit, upper and lower case)}"
PY=""; for c in python3 python; do if "$c" -c 'import sys' >/dev/null 2>&1; then PY=$c; break; fi; done
[ -n "$PY" ] || { echo "Python 3 is required"; exit 2; }
W=$(mktemp -d); trap 'rm -rf "$W"' EXIT
PASSN=0; FAILN=0

# Test files: the server checks the file signature (magic bytes), so generated content is enough.
"$PY" -I - "$W" <<'EOF'
import sys, os
d = sys.argv[1]
png, jpg = bytes([137, 80, 78, 71, 13, 10, 26, 10]), bytes([0xFF, 0xD8, 0xFF, 0xE0])
def write(name, head, size): open(os.path.join(d, name), "wb").write(head + bytes(size - len(head)))
write("auto.png", png, 2048); write("auto.jpg", jpg, 2048); write("program.png", b"MZ", 4096)
write("2_5MB.png", png, 2_621_440); write("3_5MB.png", png, 3_670_016); write("11MB.png", png, 11 * 1024 * 1024)
EOF
F=$W

txt() { "$PY" -I -c 'import html,re,sys; sys.stdout.reconfigure(encoding="utf-8"); t=open(sys.argv[1],encoding="utf-8",errors="replace").read(); t=re.sub(r"(?s)<script.*?</script>|<style.*?</style>","",t); t=re.sub(r"<[^>]+>"," ",t); print(re.sub(r"\s+"," ",html.unescape(t)))' "$1"; }
# row_id FILE NEEDLE REGEX -> first group of REGEX inside the table row whose text contains NEEDLE (ASCII needle)
row_id() { "$PY" -I -c 'import html,re,sys; s=open(sys.argv[1],encoding="utf-8").read()
for row in re.findall(r"(?s)<tr.*?</tr>", s):
    if sys.argv[2] in html.unescape(re.sub(r"<[^>]+>"," ",row)):
        m=re.search(sys.argv[3], row)
        if m: print(m.group(1)); break' "$1" "$2" "$3"; }
check() { # ID DESCRIPTION RESULT(0=ok) DETAIL
  if [ "$3" = 0 ]; then PASSN=$((PASSN+1)); echo "PASS  $1  $2"; else FAILN=$((FAILN+1)); echo "FAIL  $1  $2  -> $4"; fi; }
has() { txt "$W/res.html" | grep -qF -- "$1"; echo $?; }
snippet() { txt "$W/res.html" | cut -c1-300; }
fpath() { if command -v cygpath >/dev/null 2>&1; then cygpath -m "$1"; else printf '%s' "$1"; fi; }

token() { # JAR GETPATH -> antiforgery token from any page of the signed-in user
  curl -s -L -b "$1" -c "$1" "$BASE$2" -o "$W/form.html"
  grep -o 'name="__RequestVerificationToken" type="hidden" value="[^"]*"' "$W/form.html" | head -1 | sed 's/.*value="//;s/"$//'; }
# Windows curl reads argv in the ANSI code page, so UTF-8 form values are encoded by Python via stdin.
urlenc() { printf '%s\n' "$@" | "$PY" -I -c 'import sys,urllib.parse; lines=sys.stdin.buffer.read().decode("utf-8").splitlines(); print(urllib.parse.urlencode([tuple(l.split("=",1)) for l in lines]))'; }
post() { # JAR GETPATH POSTPATH key=value...
  local jar=$1 get=$2 path=$3; shift 3
  local body; body=$(urlenc "__RequestVerificationToken=$(token "$jar" "$get")" "$@")
  rm -f "$W/res.html"
  read -r CODE URL < <(curl -s -L -b "$jar" -c "$jar" --data "$body" "$BASE$path" -o "$W/res.html" -w '%{http_code} %{url_effective}')
  touch "$W/res.html"; }
get() { rm -f "$W/res.html"; read -r CODE URL < <(curl -s -L -b "$1" -c "$1" "$BASE$2" -o "$W/res.html" -w '%{http_code} %{url_effective}'); touch "$W/res.html"; }
upload() { # JAR VEHICLEID FILE MIME
  local tok; tok=$(token "$1" /Vehicles)
  rm -f "$W/res.html"
  read -r CODE URL < <(curl -s -L -b "$1" -c "$1" -F "__RequestVerificationToken=$tok" -F "photo=@$(fpath "$3");type=$4" "$BASE/Vehicles/UploadPhoto/$2" -o "$W/res.html" -w '%{http_code} %{url_effective}')
  touch "$W/res.html"; }
login() { post "$1" /Account/Login /Account/Login "Email=$2" "Password=$3"; }
register() { post "$1" /Account/Register /Account/Register "FirstName=$3" "LastName=Test" "Email=$2" "Password=$CUST_PASS" "ConfirmPassword=$CUST_PASS"; }
photo_path() { grep -o '/uploads/vehicles/[0-9a-f]*\.\(png\|jpg\)' "$W/res.html" | head -1; }
status_of() { curl -s -o /dev/null -w '%{http_code}' "$BASE$1"; }
denied() { [ "${URL#*Account/Denied}" != "$URL" ]; echo $?; }

A=$W/a.jar; B=$W/b.jar; AD=$W/admin.jar; X=$W/anon.jar
TODAY=$(date +%Y-%m-%d)

echo "== Účty"
register "$A" anna@e2e.test Anna;     check T01 "Registrace zákazníka A" $([ "${URL##*/}" = Vehicles ]; echo $?) "$CODE $URL"
register "$B" bohdan@e2e.test Bohdan; check T01 "Registrace zákazníka B" $([ "${URL##*/}" = Vehicles ]; echo $?) "$CODE $URL"
register "$X" cyril@e2e.test Cyril;   check T01 "Registrace zákazníka C" $([ "${URL##*/}" = Vehicles ]; echo $?) "$CODE $URL"
rm -f "$X"
register "$X" anna@e2e.test Duplicita
check T02 "Duplicitní e-mail: jedna česká hláška" $(has "Účet s e-mailem 'anna@e2e.test' už existuje.") "$(snippet)"
login "$AD" "$ADMIN_EMAIL" "$ADMIN_PASS"; check "--" "Přihlášení admina" $([ "${URL##*/}" = Vehicles ]; echo $?) "$CODE $URL"

echo "== T03 zamčení účtu"
for i in 1 2 3 4; do login "$X" cyril@e2e.test SpatneHeslo999; done
check T03 "4. špatné heslo: obecná hláška" $(has "Nesprávný e-mail nebo heslo.") "$(snippet)"
login "$X" cyril@e2e.test SpatneHeslo999
check T03 "5. špatné heslo: účet uzamčen" $(has "Účet je dočasně uzamčen") "$(snippet)"
login "$X" cyril@e2e.test "$CUST_PASS"
check T03 "Správné heslo během uzamčení: odmítnuto" $(has "Účet je dočasně uzamčen") "$(snippet)"

echo "== T04–T06 vozidla zákazníka"
post "$A" /Vehicles/Create /Vehicles/Create Vin=TMBJJ7NE8J0123456 LicensePlate=1AB2345 ManufacturerId=1 VehicleModel=Octavia YearOfManufacture=2018 FuelType=Diesel Mileage=120000 Color=Modrá
VA=${URL##*/}; check T04 "A vytvoří vozidlo" $(has "Vozidlo bylo přidáno.") "$(snippet)"
post "$A" /Vehicles/Create /Vehicles/Create Vin=TMBJJ7NE8J012345O LicensePlate=7XY7777 ManufacturerId=1 VehicleModel=Octavia YearOfManufacture=2018 FuelType=Diesel Mileage=1
check T05 "VIN s písmenem O odmítnut" $(has "VIN musí obsahovat přesně 17 znaků bez I, O a Q.") "$(snippet)"
post "$A" /Vehicles/Create /Vehicles/Create Vin=TMBJJ7NE8J0123456 LicensePlate=7XY7777 ManufacturerId=1 VehicleModel=Octavia YearOfManufacture=2018 FuelType=Diesel Mileage=1
check T06 "Duplicitní VIN odmítnut" $(has "Toto VIN už je evidováno.") "$(snippet)"
post "$A" /Vehicles/Create /Vehicles/Create Vin=WVWZZZ1KZAW000001 LicensePlate=1AB2345 ManufacturerId=2 VehicleModel=Golf YearOfManufacture=2020 FuelType=Petrol Mileage=50000
check T06 "Duplicitní SPZ odmítnuta" $(has "Tato SPZ už je evidována.") "$(snippet)"
post "$A" "/Vehicles/Edit/$VA" "/Vehicles/Edit/$VA" Id=$VA Vin=TMBJJ7NE8J0123456 LicensePlate=1AB2345 ManufacturerId=1 VehicleModel="Octavia Combi" YearOfManufacture=2018 FuelType=Diesel Mileage=125000 Color=Černá
check CRUD "A upraví vozidlo" $(has "Změny vozidla byly uloženy.") "$(snippet)"
check CRUD "Úprava se projeví v detailu (včetně diakritiky)" $(has "Octavia Combi") "$(snippet)"

echo "== T08/T09 fotografie"
upload "$A" "$VA" "$F/auto.png" image/png
check T08 "Nahrání PNG" $(has "Fotografie byla nahrána.") "$(snippet)"
P1=$(photo_path); check T08 "PNG je dostupné na webu" $([ -n "$P1" ] && [ "$(status_of "$P1")" = 200 ]; echo $?) "path=$P1"
upload "$A" "$VA" "$F/auto.jpg" image/jpeg
check T08 "Nahrání JPG (nahradí PNG)" $(has "Fotografie byla nahrána.") "$(snippet)"
P2=$(photo_path); check T08 "Stará fotografie smazána z disku" $([ "$(status_of "$P1")" = 404 ] && [ "$(status_of "$P2")" = 200 ]; echo $?) "old=$P1 new=$P2"
upload "$A" "$VA" "$F/program.png" image/png
check T09 "Podvržený .exe jako .png odmítnut" $(has "Povoleny jsou pouze JPEG a PNG fotografie.") "$(snippet)"
upload "$A" "$VA" "$F/2_5MB.png" image/png
check T09 "Soubor 2,5 MB odmítnut" $(has "Obrázek musí mít maximálně 2 MB.") "$(snippet)"
upload "$A" "$VA" "$F/3_5MB.png" image/png
check T09 "Soubor 3,5 MB odmítnut srozumitelnou hláškou" $(has "Obrázek musí mít maximálně 2 MB.") "HTTP $CODE: $(snippet)"
upload "$A" "$VA" "$F/11MB.png" image/png
check T09 "Soubor 11 MB: čitelná stránka 400" $([ "$CODE" = 400 ] && [ "$(has "Neplatný požadavek")" = 0 ]; echo $?) "HTTP $CODE: $(snippet)"
get "$A" "/Vehicles/Details/$VA"; P3=$(photo_path)
check T09 "Po odmítnutí zůstala původní fotografie" $([ "$P3" = "$P2" ]; echo $?) "now=$P3 expected=$P2"
check T09 "Formulář hlídá 2 MB už v prohlížeči" $(grep -q 'data-max-bytes="2097152"' "$W/res.html"; echo $?) "atribut chybí"

echo "== T07 cizí vozidlo a zabezpečení"
get "$B" "/Vehicles/Details/$VA"; check T07 "B: detail cizího vozidla 404" $([ "$CODE" = 404 ]; echo $?) "$CODE"
check T07 "…se srozumitelnou stránkou" $(has "Stránka nenalezena") "$(snippet)"
get "$B" "/Vehicles/Edit/$VA"; check T07 "B: úprava cizího vozidla 404" $([ "$CODE" = 404 ]; echo $?) "$CODE"
upload "$B" "$VA" "$F/auto.png" image/png; check T07 "B: fotka k cizímu vozidlu 404" $([ "$CODE" = 404 ]; echo $?) "$CODE"
post "$B" /Vehicles "/Vehicles/Delete/$VA"; check T07 "B: smazání cizího vozidla 404" $([ "$CODE" = 404 ]; echo $?) "$CODE"
get "$B" /Admin; check SEC "Customer nemá přístup do administrace" $(denied) "$CODE $URL"
get "$B" /ServiceRecords; check SEC "Customer nemá přístup k servisním záznamům" $(denied) "$CODE $URL"
read -r CODE < <(curl -s -b "$A" -X POST "$BASE/Vehicles/Delete/$VA" -o /dev/null -w '%{http_code}')
check SEC "POST bez antiforgery tokenu odmítnut (400)" $([ "$CODE" = 400 ]; echo $?) "$CODE"

echo "== T15/T16 admin: role a uživatelé"
get "$AD" /Admin/Users; BID=$(row_id "$W/res.html" bohdan@e2e.test '/Admin/Users/Edit/([0-9a-f-]+)')
post "$AD" "/Admin/Users/Edit/$BID" "/Admin/Users/Edit/$BID" Id=$BID FirstName=Bohdan LastName=Test Email=bohdan@e2e.test Role=Manager
check T15 "Admin nastaví B roli Manager" $(has "Uživatel byl upraven.") "$(snippet)"
get "$B" /ServiceRecords
check SEC "Nová role platí hned, bez nového přihlášení" $(has "Přehled servisů napříč vozidly") "$CODE $URL"
post "$AD" /Admin/Users/Create /Admin/Users/Create FirstName=Dana LastName=Nová Email=dana@e2e.test Role=Customer "Password=$CUST_PASS"
check T15 "Admin vytvoří uživatele" $(has "dana@e2e.test") "$(snippet)"
get "$AD" /Admin/Users; DID=$(row_id "$W/res.html" dana@e2e.test '/Admin/Users/Edit/([0-9a-f-]+)')
post "$AD" /Admin/Users "/Admin/Users/Delete/$DID"
check T15 "Admin smaže uživatele bez vozidel" $(has "Uživatel byl smazán.") "$(snippet)"
get "$AD" /Admin/Users; ADID=$(row_id "$W/res.html" "$ADMIN_EMAIL" '/Admin/Users/Edit/([0-9a-f-]+)')
post "$AD" "/Admin/Users/Edit/$ADID" "/Admin/Users/Edit/$ADID" Id=$ADID FirstName=Admin LastName=Test "Email=$ADMIN_EMAIL" Role=Customer
check T16 "Admin si nemůže odebrat roli (hláška u pole)" $(has "Nelze odebrat roli aktuálnímu nebo poslednímu administrátorovi.") "$(snippet)"
post "$AD" /Admin/Users "/Admin/Users/Delete/$ADID"
check T16 "Admin nemůže smazat sám sebe (hláška v seznamu)" $([ "$CODE" = 200 ] && [ "$(has "Nelze odstranit vlastní účet.")" = 0 ]; echo $?) "HTTP $CODE: $(snippet)"
get "$AD" /Admin/Users; check T16 "Admin účet zůstal" $(has "$ADMIN_EMAIL") "$(snippet)"
post "$AD" /Admin/Users "/Admin/Users/Delete/$(row_id "$W/res.html" anna@e2e.test '/Admin/Users/Edit/([0-9a-f-]+)')"
check T17 "Uživatele s vozidlem nelze smazat" $(has "Uživatel vlastní vozidla") "$(snippet)"

echo "== T10–T13 Manager a servisní záznamy"
get "$B" "/Vehicles/Search?q=TMBJJ7NE8J0123456"; check T10 "Manager najde vozidlo podle VIN" $(has "1AB2345") "$(snippet)"
get "$B" "/Vehicles/Search?q=1AB2345"; check T10 "Manager najde vozidlo podle SPZ" $(has "Octavia") "$(snippet)"
post "$B" "/ServiceRecords/Create?vehicleId=$VA" /ServiceRecords/Create VehicleId=$VA ServiceTypeId=1 ServiceCenterId= Status=Completed ServiceDate=2099-01-01 MileageAtService=150000 Price=2500 Note=Olej
check T12 "Dokončený servis s budoucím datem odmítnut" $(has "nesmí být v budoucnu") "$(snippet)"
post "$B" "/ServiceRecords/Create?vehicleId=$VA" /ServiceRecords/Create VehicleId=$VA ServiceTypeId=1 ServiceCenterId= Status=Completed ServiceDate=$TODAY MileageAtService=150000 Price=2500 Note=Olej
check T11 "Manager zapíše dokončený servis" $(has "Servisní úkon byl uložen.") "$(snippet)"
check T11 "Nájezd vozidla 150 000 km" $(has "150 000 km") "$(snippet)"
RID=$(grep -o '/ServiceRecords/Edit/[0-9]*' "$W/res.html" | head -1 | sed 's#.*/##')
post "$B" "/ServiceRecords/Edit/$RID" "/ServiceRecords/Edit/$RID" Id=$RID VehicleId=$VA ServiceTypeId=1 ServiceCenterId= Status=Completed ServiceDate=$TODAY MileageAtService=150000 Price=3100 Note="Olej a filtr"
get "$B" "/Vehicles/Details/$VA"; check T13 "Manager upraví servisní záznam" $(has "3 100,00") "$(snippet)"
post "$A" "/Vehicles/Edit/$VA" "/Vehicles/Edit/$VA" Id=$VA Vin=TMBJJ7NE8J0123456 LicensePlate=1AB2345 ManufacturerId=1 VehicleModel="Octavia Combi" YearOfManufacture=2018 FuelType=Diesel Mileage=100000 Color=Černá
check T11 "Tachometr nelze snížit pod dokončený servis" $(has "Tachometr nemůže být menší") "$(snippet)"
post "$B" "/ServiceRecords/Create?vehicleId=$VA" /ServiceRecords/Create VehicleId=$VA ServiceTypeId=2 ServiceCenterId= Status=Planned ServiceDate=$TODAY MileageAtService=150000 Price=900 Note=STK
get "$B" "/Vehicles/Details/$VA"; RID2=$(grep -o '/ServiceRecords/Edit/[0-9]*' "$W/res.html" | sed 's#.*/##' | sort -n | tail -1)
post "$B" "/ServiceRecords/Edit/$RID2" "/ServiceRecords/Delete/$RID2"
check T13 "Manager smaže servisní záznam" $(has "Servisní záznam byl odstraněn.") "$(snippet)"
post "$B" /Vehicles/Create /Vehicles/Create Vin=WVWZZZ1KZAW000002 LicensePlate=9ZZ9999 ManufacturerId=2 VehicleModel=Golf YearOfManufacture=2020 FuelType=Petrol Mileage=50000
check SEC "Manager nemůže zakládat vozidla" $(denied) "$CODE $URL"

echo "== T14 číselníky (Manager)"
post "$B" "/Catalogs/Create?kind=types" /Catalogs/Create Kind=types Name="Výměna rozvodů" Description="Rozvodový řemen" RecommendedIntervalKm=120000 RecommendedIntervalMonths=60
check T14 "Manager přidá druh úkonu" $(has "Záznam byl uložen.") "$(snippet)"
TID=$(row_id "$W/res.html" "rozvod" '/Catalogs/Edit/([0-9]+)')
post "$B" "/Catalogs/Edit/$TID?kind=types" "/Catalogs/Edit/$TID?kind=types" Id=$TID Kind=types Name="Výměna rozvodů a pumpy" RecommendedIntervalKm=120000
check T14 "Manager upraví druh úkonu" $(has "Výměna rozvodů a pumpy") "$(snippet)"
post "$B" "/Catalogs?kind=types" "/Catalogs/Delete/$TID?kind=types"
check T14 "Manager smaže nepoužitý druh úkonu" $(has "Záznam byl smazán.") "$(snippet)"
post "$B" "/Catalogs?kind=types" "/Catalogs/Delete/1?kind=types"
check T14 "Použitý druh úkonu nelze smazat" $(has "má servisní historii") "$(snippet)"
post "$B" "/Catalogs/Create?kind=centers" /Catalogs/Create Kind=centers Name="Autoservis Brno" Address="Masarykova 1, Brno" Phone="+420 123 456 789" Email=servis@e2e.test
check T14 "Manager přidá servis" $(has "Záznam byl uložen.") "$(snippet)"
CID=$(row_id "$W/res.html" "Autoservis Brno" '/Catalogs/Edit/([0-9]+)')
post "$B" "/Catalogs/Edit/$CID?kind=centers" "/Catalogs/Edit/$CID?kind=centers" Id=$CID Kind=centers Name="Autoservis Brno-střed" Address="Masarykova 1, Brno"
check T14 "Manager upraví servis" $(has "Autoservis Brno-střed") "$(snippet)"
post "$B" "/Catalogs/Create?kind=centers" /Catalogs/Create Kind=centers Name="Bez adresy"
check T14 "Servis bez adresy odmítnut" $(has "Adresa je povinný") "$(snippet)"
post "$B" "/Catalogs?kind=centers" "/Catalogs/Delete/$CID?kind=centers"
check T14 "Manager smaže servis" $(has "Záznam byl smazán.") "$(snippet)"
get "$B" "/Catalogs?kind=manufacturers"; check T14 "Manager nemá přístup k výrobcům" $(denied) "$CODE $URL"
get "$B" /Admin/Users; check T14 "Manager nemá přístup ke správě uživatelů" $(denied) "$CODE $URL"

echo "== T15 admin: výrobci, vozidla, záznamy"
post "$AD" "/Catalogs/Create?kind=manufacturers" /Catalogs/Create Kind=manufacturers Name=Hyundai Country="Jižní Korea"
check T15 "Admin přidá výrobce" $(has "Záznam byl uložen.") "$(snippet)"
MID=$(row_id "$W/res.html" Hyundai '/Catalogs/Edit/([0-9]+)')
post "$AD" "/Catalogs/Create?kind=manufacturers" /Catalogs/Create Kind=manufacturers Name=Hyundai
check T15 "Duplicitní výrobce odmítnut" $(has "Výrobce s tímto názvem již existuje.") "$(snippet)"
post "$AD" "/Catalogs/Edit/$MID?kind=manufacturers" "/Catalogs/Edit/$MID?kind=manufacturers" Id=$MID Kind=manufacturers Name="Hyundai Motor" Country="Jižní Korea"
check T15 "Admin upraví výrobce" $(has "Hyundai Motor") "$(snippet)"
get "$AD" /Admin/Users; AID=$(row_id "$W/res.html" anna@e2e.test '/Admin/Users/Edit/([0-9a-f-]+)')
post "$AD" /Vehicles/Create /Vehicles/Create Vin=KMHD841CBMU000003 LicensePlate=2CD3456 ManufacturerId=$MID VehicleModel=i30 YearOfManufacture=2021 FuelType=Hybrid Mileage=30000 OwnerId=$AID
VH=${URL##*/}; check T15 "Admin vytvoří vozidlo pro zákazníka A" $(has "Vozidlo bylo přidáno.") "$(snippet)"
get "$A" /Vehicles; check T15 "Vozidlo vidí zákazník A" $(has "2CD3456") "$(snippet)"
post "$AD" "/Catalogs?kind=manufacturers" "/Catalogs/Delete/$MID?kind=manufacturers"
check T15 "Použitého výrobce nelze smazat" $(has "Výrobce je použit u vozidla") "$(snippet)"
post "$AD" "/Vehicles/Edit/$VH" "/Vehicles/Edit/$VH" Id=$VH Vin=KMHD841CBMU000003 LicensePlate=2CD3456 ManufacturerId=$MID VehicleModel="i30 Kombi" YearOfManufacture=2021 FuelType=Hybrid Mileage=31000 OwnerId=$AID
check T15 "Admin upraví cizí vozidlo" $(has "Změny vozidla byly uloženy.") "$(snippet)"
get "$AD" /ServiceRecords; check T15 "Admin vidí všechny servisní záznamy" $(has "1AB2345") "$(snippet)"
post "$AD" "/Vehicles/Details/$VH" "/Vehicles/Delete/$VH"
check T15 "Admin smaže vozidlo" $(has "Vozidlo bylo odstraněno.") "$(snippet)"
post "$AD" "/Catalogs?kind=manufacturers" "/Catalogs/Delete/$MID?kind=manufacturers"
check T15 "Admin smaže nepoužitého výrobce" $(has "Záznam byl smazán.") "$(snippet)"

echo "== T18 smazání autora servisního záznamu"
get "$AD" /Admin/Users; BID=$(row_id "$W/res.html" bohdan@e2e.test '/Admin/Users/Edit/([0-9a-f-]+)')
post "$AD" /Admin/Users "/Admin/Users/Delete/$BID"
check T18 "Admin smaže Managera bez vozidel" $(has "Uživatel byl smazán.") "$(snippet)"
get "$A" "/Vehicles/Details/$VA"; check T18 "Servisní záznam po smazání autora zůstal" $(has "3 100,00") "$(snippet)"
get "$B" /ServiceRecords; check SEC "Smazaný uživatel je hned odhlášen" $([ "${URL#*Account/Login}" != "$URL" ]; echo $?) "$CODE $URL"

echo "== Úklid a odhlášení"
post "$A" "/Vehicles/Details/$VA" "/Vehicles/Delete/$VA"
check CRUD "A smaže vlastní vozidlo" $(has "Vozidlo bylo odstraněno.") "$(snippet)"
check CRUD "Fotografie smazaného vozidla odstraněna" $([ "$(status_of "$P2")" = 404 ]; echo $?) "$P2"
post "$A" /Vehicles /Account/Logout
check "--" "Odhlášení" $([ "${URL%/}" = "$BASE" ]; echo $?) "$CODE $URL"
get "$A" /Vehicles; check "--" "Po odhlášení přesměrování na přihlášení" $([ "${URL#*Account/Login}" != "$URL" ]; echo $?) "$CODE $URL"

echo
echo "VÝSLEDEK: $PASSN PASS, $FAILN FAIL"
[ "$FAILN" = 0 ]
