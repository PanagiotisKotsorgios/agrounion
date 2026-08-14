# AGRO UNION I.K.E.

Production-ready εφαρμογή ASP.NET Core 8 για το δημόσιο site και το portal συνεργατών της AGRO UNION. Περιλαμβάνει πραγματική βάση MySQL, Identity/cookie authentication, JWT API, τέσσερις ρόλους, workflows συναλλαγών δύο σκελών, margin reporting, migrations, seed data και Docker packaging.

## Τεχνολογική επιλογή

Επιλέχθηκε **Razor MVC με server rendering**. Η επιλογή κρατά το public site και τα ασφαλή dashboards στο ίδιο ASP.NET Core host, αποφεύγει ξεχωριστό SPA build pipeline και αναπαράγει άμεσα το ζητούμενο design system. Τα εξωτερικά clients εξυπηρετούνται από REST endpoints με JWT και Swagger.

Η λύση ακολουθεί layered architecture:

```text
AgroUnion.sln
├── src/
│   ├── AgroUnion.Domain/          entities, enums, margin/workflow rules
│   ├── AgroUnion.Application/     DTOs, validators, service contracts
│   ├── AgroUnion.Infrastructure/  EF Core, Identity, MySQL, email, JWT, seeding
│   └── AgroUnion.Web/             MVC, API, policies, public site, role portals
├── tests/AgroUnion.Tests/         xUnit business and validation tests
├── Dockerfile
└── docker-compose.yml
```

## Άμεση εκτέλεση με Docker

Προαπαιτείται Docker Desktop ή Docker Engine με Compose.

```bash
docker compose up --build
```

Η εφαρμογή ανοίγει στο `http://localhost:8080` και το Swagger στο `http://localhost:8080/swagger`. Το MySQL container περιμένει μέχρι να γίνει healthy, έπειτα η εφαρμογή εφαρμόζει αυτόματα τα EF migrations και εκτελεί idempotent seed.

Για πραγματικό περιβάλλον, αντιγράψτε το `.env.example` σε `.env` και αλλάξτε οπωσδήποτε MySQL passwords, JWT key και admin password πριν από την εκκίνηση.

## Τοπική εκτέλεση χωρίς Docker

Με εγκατεστημένο .NET 8 SDK/runtime:

```bash
dotnet restore
dotnet run --project src/AgroUnion.Web
```

Σε `Development` χρησιμοποιείται EF InMemory για γρήγορο demo χωρίς εξωτερική βάση. Για τοπικό MySQL ορίστε:

```powershell
$env:DatabaseProvider='MySql'
$env:ConnectionStrings__DefaultConnection='server=localhost;port=3306;database=agro_union;user=agro;password=...'
dotnet run --project src/AgroUnion.Web
```

## Demo λογαριασμοί

| Ρόλος | Email | Κωδικός |
|---|---|---|
| Admin | `admin@agrounion.local` | `Admin!2026Demo` |
| Producer | `producer@agrounion.local` | `Demo!2026User` |
| Trader | `trader@agrounion.local` | `Demo!2026User` |
| Company | `company@agrounion.local` | `Demo!2026User` |

Οι κωδικοί προέρχονται από configuration/environment variables και οι παραπάνω είναι μόνο demo defaults.

## Migrations

Η αρχική MySQL migration βρίσκεται στο `src/AgroUnion.Infrastructure/Migrations`. Εφαρμόζεται αυτόματα στο startup για relational database.

```bash
dotnet ef database update --project src/AgroUnion.Infrastructure --startup-project src/AgroUnion.Web
dotnet ef migrations add MigrationName --project src/AgroUnion.Infrastructure --startup-project src/AgroUnion.Web --output-dir Migrations
```

## Authentication και API

- Cookie authentication για MVC portal (`/account/login`).
- Identity password hashing, lockout, secure/HTTP-only cookies και CSRF tokens.
- JWT endpoint: `POST /api/auth/token`.
- Role-filtered endpoint: `GET /api/portal/dashboard` με Bearer token.
- Swagger/OpenAPI: `/swagger`.
- Policies: `AdminOnly`, `FarmerOnly`, `MerchantOnly`, `PartnerCompanyOnly`, `BuyerOnly`.

Ο διαχωρισμός εμπορικών δεδομένων γίνεται στο service/query επίπεδο: ο Farmer λαμβάνει μόνο `FarmerDealDto`/buy-side, ενώ Trader και Company μόνο `BuyerDealDto`/sell-side. Μόνο ο Admin λαμβάνει `AdminDealDto` με τα δύο σκέλη και margin. Δεν βασίζεται σε απόκρυψη HTML.

## Κύριες ροές

- Δημόσια αίτηση → αποθήκευση → admin review → έγκριση → Identity user + προσωρινός κωδικός + draft contract → ενεργοποίηση σύμβασης.
- Δήλωση παραγωγής → buy offer → sell offer/counteroffer → brokerage deal → επιβεβαίωση δύο σκελών → αυτόματο pickup + δύο role-scoped transactions.
- Facilitation deal χωρίς trading margin.
- Συλλογική παραγγελία → συμμετοχές παραγωγών → κλείσιμο από admin.
- Price lists, contract/history views, CSV transaction export και admin margin CSV.
- Contact form → persistence + email notification.
- Account activation, role change, reset password και GDPR anonymization.
- Ιδιωτική **Αγορά Δικτύου** για ενεργούς συνεργάτες: φίλτρα ρόλου/περιοχής/προϊόντος, δημοσίευση μόνο της μη δεσμευμένης παραγωγής, ταξινομημένη ζήτηση εμπόρων βάσει καλύτερης τιμής και καταγεγραμμένες εμπορικές επαφές με ειδοποίηση email.

## Email

Όλα τα email της πλατφόρμας αποστέλλονται μέσω του Brevo API: προσκλήσεις, επαναφορά κωδικού, δοκιμαστικές αποστολές και ενημερώσεις newsletter/συνεργατών. Ο διαχειριστής ορίζει το API key, τον αποστολέα και το reply-to από **Portal → Email & Newsletter**. Το API key αποθηκεύεται κρυπτογραφημένο με ASP.NET Core Data Protection και δεν εμφανίζεται ξανά μετά την αποθήκευση.

Οι εγγραφές newsletter τηρούνται στη βάση, αφαιρούνται αυτόματα οι διπλοεγγραφές και κάθε μαζικό μήνυμα προς newsletter περιλαμβάνει προσωπικό σύνδεσμο διαγραφής. Το ιστορικό αποστολών και τα επιμέρους αποτελέσματα διατηρούνται στο admin portal.

## Tests

```bash
dotnet test
```

Υπάρχουν 25 xUnit tests για margin, δεσμεύσεις παραγωγής, ιδιωτική αγορά συνεργατών, facilitation, transitions επιβεβαίωσης, role mapping, seeding, κρυπτογράφηση και request contract της Brevo, newsletter deduplication και validation παραγωγής/αντιπροσφοράς.

## Production checklist

- Αλλάξτε όλα τα demo secrets και απενεργοποιήστε ή αντικαταστήστε demo seed credentials.
- Τερματίστε TLS σε reverse proxy και προωθήστε `X-Forwarded-*` headers.
- Ενεργοποιήστε SMTP και μόνιμο backup για το `agro_mysql_data` volume.
- Περιορίστε την πρόσβαση στο Swagger αν η πολιτική του deployment το απαιτεί.
- Συνδέστε πραγματικό PDF/document storage πριν από ηλεκτρονική υπογραφή συμβάσεων.
