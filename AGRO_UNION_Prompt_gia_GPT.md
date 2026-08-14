# PROMPT ΓΙΑ AI CODING ASSISTANT (GPT / Codex) — Πλήρης Ανάπτυξη Web Εφαρμογής "AGRO UNION I.K.E."

> Αντιγράψτε ολόκληρο αυτό το κείμενο και δώστε το ως πρώτο μήνυμα στο εργαλείο (ChatGPT/Codex, Claude Code, Cursor κ.λπ.). Επισυνάψτε επίσης το υπάρχον αρχείο `index.html` (το static UI mockup) ως σημείο αναφοράς σχεδίασης.

---

## 0. Ρόλος σου

Είσαι senior full-stack engineer. Θα μου φτιάξεις **ολόκληρη, λειτουργική, production-ready εφαρμογή web** για την εταιρεία **AGRO UNION I.K.E.**, ξεκινώντας από ένα υπάρχον στατικό HTML/CSS mockup (θα σου το επισυνάψω) που ορίζει το design system, τα χρώματα, τη γραμματοσειρά και το περιεχόμενο της δημόσιας ιστοσελίδας. Το mockup **δεν έχει καμία λειτουργικότητα** (no backend, no database, τα forms απλά κάνουν προσομοίωση submit). Ο στόχος σου είναι να το μετατρέψεις σε πλήρη εφαρμογή με πραγματικό backend, βάση δεδομένων, authentication, ρόλους χρηστών και portal συνεργατών.

Θέλω **ολοκληρωμένο, τρέξιμο πακέτο** (δεν θέλω αποσπασματικό κώδικα) — δηλαδή solution που να «χτίζει» και να τρέχει τοπικά με `docker-compose up`, με migrations και seed data, έτοιμο για επίδειξη.

---

## 1. Επιχειρηματικό πλαίσιο (business context)

Η **AGRO UNION I.K.E.** είναι ενδιάμεσος κρίκος/δίκτυο συνεργασιών στον αγροτικό τομέα στην Αιτωλοακαρνανία (Μεσολόγγι, Αιτωλικό, Αμφιλοχία, Πεντάλοφος κ.λπ.). Λειτουργεί ως broker/συντονιστής ανάμεσα σε:

- **Παραγωγούς / Αγρότες (Farmers)** (ελαιόλαδο, επιτραπέζια ελιά, λοιποί καρποί)
- **Εμπόρους (Merchants)**
- **Εργοστάσια/τυποποιητές/προμηθευτές εφοδίων — συνεργαζόμενες Εταιρείες (Partner Companies)**

### 1.1 Βασικό επιχειρηματικό μοντέλο — ΜΕΣΙΤΕΙΑ (brokerage margin model) — ΚΡΙΣΙΜΟ

Η κύρια δραστηριότητα **δεν είναι απλή διαμεσολάβηση/σύσταση** μεταξύ δύο μερών που κλείνουν τη δική τους απευθείας συμφωνία. Η AGRO UNION **αγοράζει η ίδια** την παραγωγή από τους παραγωγούς σε μία τιμή (τιμή αγοράς/κόστους) και **πουλάει η ίδια** το προϊόν σε εργοστάσια/τυποποιητές/εμπόρους σε υψηλότερη τιμή (τιμή πώλησης), κρατώντας τη **διαφορά (margin/περιθώριο κέρδους)** ως έσοδο της εταιρείας. Δηλαδή η εταιρεία λειτουργεί ως **αγοραστής-μεταπωλητής (trading/brokerage house)**, όχι μόνο ως μεσίτης προμήθειας.

Αυτό πρέπει να αντικατοπτρίζεται **ρητά** στο data model και στη λειτουργικότητα:

- Κάθε **Deal / Συναλλαγή** έχει δύο σκέλη: **Buy-side** (τιμή & ποσότητα αγοράς από τον παραγωγό) και **Sell-side** (τιμή & ποσότητα πώλησης στο εργοστάσιο/έμπορο). Το σύστημα πρέπει να υπολογίζει αυτόματα το **περιθώριο (margin)** = (SellPrice − BuyPrice) × Quantity, ανά συναλλαγή, ανά προϊόν, ανά περίοδο, ανά περιοχή, ανά συνεργάτη.
- Ο **Farmer/Producer** βλέπει μόνο την τιμή αγοράς που του προσφέρεται/συμφωνήθηκε (**δεν βλέπει ποτέ** την τελική τιμή πώλησης ούτε το περιθύριο κέρδους της εταιρείας — αυτό είναι εσωτερική/εμπιστευτική πληροφορία).
- Η **συνεργαζόμενη Εταιρεία/Merchant** (αγοραστής) βλέπει μόνο την τιμή πώλησης που της προσφέρεται — όχι το κόστος αγοράς της AGRO UNION από τον παραγωγό.
- Μόνο ο **Platform Admin** έχει πλήρη ορατότητα και στα δύο σκέλη κάθε deal, καθώς και σε συγκεντρωτικά **P&L/margin reports** (κέρδος ανά προϊόν, ανά μήνα, ανά περιοχή, ανά συνεργάτη-πωλητή/αγοραστή).
- Το σύστημα πρέπει να υποστηρίζει **και** τη δευτερεύουσα λειτουργία «απλής διαμεσολάβησης» (π.χ. στα λιπάσματα/εφόδια όπου μπορεί να λειτουργεί ως συλλογική παραγγελία/negotiation χωρίς η ίδια η εταιρεία να αγοράζει/μεταπωλεί) — άρα το μοντέλο πρέπει να έχει flag/τύπο ανά deal: `DealType = Brokerage (buy-resell με margin)` ή `DealType = Facilitation (απλή διαμεσολάβηση/συλλογική παραγγελία χωρίς margin)`.

Συγκεντρώνει όγκο παραγωγής και ζήτηση, διαπραγματεύεται συλλογικά καλύτερες τιμές (πώληση καρπών, αγορά λιπασμάτων/εφοδίων, γενικές εμπορικές συνεργασίες), αγοράζει από παραγωγούς και μεταπωλεί με κέρδος σε εργοστάσια/εμπόρους, και επισημοποιεί κάθε συνεργασία με γραπτή σύμβαση.

Η ιστοσελίδα/πλατφόρμα πρέπει να έχει:
1. Δημόσιο **corporate/marketing site** (ήδη σχεδιασμένο στο mockup) — Αρχική, Ποιοι είμαστε, Υπηρεσίες, Πώς λειτουργεί, Portal (preview), Αίτηση ενδιαφέροντος, Σύμβαση, Επικοινωνία.
2. **Portal συνεργατών** με login, με **4 ξεχωριστά panels/dashboards ανά ρόλο**:
   - **Platform Admin panel** (προσωπικό AGRO UNION — πλήρης έλεγχος, ορατότητα margin, deals, χρήστες)
   - **Farmer / Producer panel** (Αγρότες/Παραγωγοί)
   - **Merchant / Trader panel** (Έμποροι)
   - **Partner Company panel** (Συνεργαζόμενες Εταιρείες — εργοστάσια/τυποποιητές/προμηθευτές)
3. Πλήρες **backend + βάση δεδομένων** που υποστηρίζει όλα τα παραπάνω, με σωστό διαχωρισμό ορατότητας δεδομένων ανά ρόλο (βλ. ενότητα 5 & 5.1).

---

## 2. Τεχνολογική στοίβα (υποχρεωτική)

- **Backend:** ASP.NET Core 8 (Web API) σε C#, με **Clean/Layered Architecture** (Domain / Application / Infrastructure / API projects).
- **Frontend:** Server-rendered **Blazor Server** *ή* Razor Pages/MVC (διάλεξε ό,τι σου επιτρέπει να αναπαράγεις πιστά το υπάρχον HTML/CSS design χωρίς SPA build pipeline περιπλοκές). Αν προτιμάς SPA, μπορείς εναλλακτικά React + Vite, αλλά τότε το backend πρέπει να είναι καθαρό REST API με Swagger. **Ανέφερε ρητά ποια επιλογή διάλεξες και γιατί.**
- **ORM:** Entity Framework Core 8, με **Pomelo.EntityFrameworkCore.MySql** provider.
- **Database:** MySQL 8.
- **Auth:** ASP.NET Core Identity (cookie auth για το portal) + JWT-based authentication για τυχόν API κλήσεις από εξωτερικά clients. Ρόλοι: `Admin`, `Producer`, `Trader`, `Company`.
- **Validation:** FluentValidation.
- **Mapping:** AutoMapper ή manual mapping (dto-first).
- **Email:** MailKit/SMTP abstraction (interface `IEmailSender`) για ειδοποιήσεις — μπορεί να είναι mock/console implementation στο dev environment.
- **Logging:** Serilog (console + αρχείο).
- **Testing:** τουλάχιστον ένα xUnit test project με βασικά unit tests στη business logic (π.χ. workflow αιτήσεων → σύμβασης).
- **Containerization:** `Dockerfile` για το app + `docker-compose.yml` που σηκώνει: app container + mysql container (με named volume) + αρχικοποίηση migrations.
- **Documentation:** Swagger/OpenAPI για τα API endpoints, plus ένα `README.md` με οδηγίες εγκατάστασης/εκτέλεσης.

> Αν κρίνεις τεχνικά ότι κάποιο άλλο ισοδύναμο εργαλείο (π.χ. Dapper αντί για EF Core, ή Minimal APIs αντί για Controllers) είναι προτιμότερο, μπορείς να το προτείνεις, αλλά κράτα τη στοίβα **.NET + C# + MySQL** όπως ζητήθηκε.

---

## 3. Οπτική ταυτότητα — ΔΕΝ αλλάζει

Το static mockup (`index.html`) ορίζει ήδη το design system. **Μην το ξανασχεδιάσεις από την αρχή** — μετέτρεψέ το πιστά σε views/components του νέου project, κρατώντας ακριβώς:

- Χρωματική παλέτα (CSS custom properties): πράσινα (`--green-900:#122A1E`, `--green-800:#173B29`, `--green-700:#1F4D33`, `--green-600:#2C6242`), χρυσαφί (`--gold-500:#C89B3C`, `--gold-400:#D6AF57`, `--gold-200:#E9D3A0`), cream (`--cream-50:#FAF7EF`, `--cream-100:#F3EDE0`), ink (`--ink-900:#20241D`, `--ink-600:#54594C`), sage (`--sage-200:#E3E7DA`), line (`--line:#D9D2BF`).
- Γραμματοσειρές: **Fraunces** (τίτλοι), **Inter** (κείμενο), **IBM Plex Mono** (eyebrows/labels/mono στοιχεία).
- Layout patterns: sticky header με blur, hero με network-graphic SVG, pillars-strip 3 στηλών, service cards grid, "how it works" 3 βήματα με connecting line, role-tabs portal preview, φόρμα αίτησης με role-picker chips, contract "document" card, footer με 3 στήλες links.
- Όλο το υπάρχον ελληνικό κείμενο/αντίγραφο (copy) διατηρείται ως έχει, εκτός αν χρειάζεται προσθήκη νέων στοιχείων UI (π.χ. login/dashboard οθόνες) — τότε ακολούθησε το ίδιο ύφος/τόνο (επίσημο, professional, ελληνικά).
- Πλήρως **responsive** (mobile breakpoint στο 900px όπως στο mockup).

Παρέδωσέ μου ένα component/partial library (π.χ. `_Buttons.razor`, `_EyebrowLabel.razor` κ.λπ.) ώστε το design να είναι επαναχρησιμοποιήσιμο και στις νέες οθόνες (login, dashboards, admin) — **μην ξεφύγεις από την υπάρχουσα αισθητική σε καμία νέα οθόνη.**

---

## 4. Δομή δημόσιου site (ήδη σχεδιασμένη — να "συνδεθεί" με backend)

| Ενότητα | URL | Λειτουργικότητα που πρέπει να προστεθεί |
|---|---|---|
| Αρχική / Hero | `/` | Στατικό περιεχόμενο |
| Ποιοι είμαστε | `/#about` | Στατικό περιεχόμενο |
| Υπηρεσίες | `/#services` | Στατικό περιεχόμενο |
| Πώς λειτουργεί | `/#how` | Στατικό περιεχόμενο |
| Portal (preview) | `/#portal` | Preview μόνο· link → πραγματικό login `/account/login` |
| **Αίτηση ενδιαφέροντος** | `/#apply` | **Πραγματικό submit** → αποθήκευση στη βάση (πίνακας `InterestApplications`) + email ειδοποίησης στο admin + email επιβεβαίωσης στον αιτούντα + εμφάνιση στο Admin panel για επεξεργασία/έγκριση |
| Σύμβαση (ενημερωτικό) | `/#contract` | Στατικό, CTA → φόρμα αίτησης |
| Επικοινωνία | `/#contact` | **Πραγματικό submit** μηνύματος επικοινωνίας → αποθήκευση (`ContactMessages`) + email ειδοποίησης |

---

## 5. Ρόλοι χρηστών & δικαιώματα

Τέσσερα ξεχωριστά panels/ρόλοι, όπως ζητήθηκε: **Platform Admin**, **Farmer (Παραγωγός/Αγρότης)**, **Merchant (Έμπορος)**, **Partner Company (Συνεργαζόμενη Εταιρεία)**.

### Platform Admin (προσωπικό AGRO UNION)
- Βλέπει/διαχειρίζεται όλες τις αιτήσεις ενδιαφέροντος (`InterestApplications`): αλλαγή status (`Νέα` → `Σε επεξεργασία` → `Εγκρίθηκε`/`Απορρίφθηκε`), σημειώσεις εσωτερικές.
- Με έγκριση αίτησης → δημιουργεί **λογαριασμό χρήστη** (Producer/Trader/Company) με προσωρινό κωδικό (email invite) και **σύμβαση συνεργασίας** (contract record) που ο χρήστης βλέπει στο portal του.
- Διαχειρίζεται όλους τους χρήστες (ενεργοποίηση/απενεργοποίηση, reset password, αλλαγή ρόλου).
- Διαχειρίζεται **τιμοκαταλόγους** (δημοσίευση/επεξεργασία τιμών ανά προϊόν, ορατότητα ανά ρόλο).
- Βλέπει **όλες** τις δηλώσεις παραγωγής, προσφορές εμπόρων, παραγγελίες εφοδίων, ιστορικό συναλλαγών — με φίλτρα ανά περιοχή/προϊόν/περίοδο/συνεργάτη.
- Dashboard με KPIs: αριθμός ενεργών συνεργατών ανά ρόλο, εκκρεμείς αιτήσεις, συνολικός δηλωμένος όγκος ανά περίοδο, ενεργές συμβάσεις.
- **Πλήρης ορατότητα σε κάθε Deal**: και τα δύο σκέλη (τιμή αγοράς από Farmer + τιμή πώλησης σε Merchant/Company) και το υπολογιζόμενο **margin**.
- **Margin / P&L reports**: συνολικό & ανά προϊόν/περιοχή/περίοδο/συνεργάτη κέρδος περιθωρίου, με export σε CSV/Excel και γραφήματα (μηνιαία τάση περιθωρίου).
- Διαχείριση μηνυμάτων επικοινωνίας (`ContactMessages`).

### Farmer / Παραγωγός (Αγρότης)
- **Δήλωση παραγωγής**: CRUD σε `ProductionDeclarations` (προϊόν, ποσότητα, ποιότητα/κατηγορία, περιοχή, διαθεσιμότητα από/έως, κατάσταση: Διαθέσιμο/Δεσμευμένο/Πωλήθηκε).
- **Ενεργές προσφορές αγοράς**: read-only λίστα ενεργών προσφορών **αγοράς από την AGRO UNION** (`PurchaseOffers`, buy-side) που ταιριάζουν στο προϊόν/περιοχή του — βλέπει **μόνο την τιμή αγοράς** που του προσφέρεται, ποτέ την τελική τιμή μεταπώλησης ή το margin.
- **Παραγγελία εφοδίων**: συμμετοχή σε συλλογικές παραγγελίες λιπασμάτων (`SupplyOrders` / `SupplyOrderItems`) — βλέπει ενεργές συλλογικές παραγγελίες, δηλώνει ποσότητα συμμετοχής.
- **Σύμβαση & ιστορικό**: βλέπει τη δική του σύμβαση (`Contracts`) και ιστορικό συναλλαγών (`Transactions`) — μόνο το buy-side σκέλος που τον αφορά.

### Merchant / Έμπορος
- **Διαθέσιμος όγκος δικτύου**: συγκεντρωτικό, ανωνυμοποιημένο view διαθέσιμης παραγωγής ανά περιοχή & προϊόν (aggregate query πάνω στο `ProductionDeclarations` — **χωρίς στοιχεία τιμής αγοράς από παραγωγούς**).
- **Λήψη/υποβολή προσφοράς αγοράς από την AGRO UNION**: βλέπει `SellOffers` (τιμή πώλησης προς αυτόν) και μπορεί να υποβάλει αντιπροσφορά/ζήτηση ποσότητας.
- **Συντονισμός παραλαβών**: `PickupSchedules` — προγραμματισμός παραλαβής μετά από κλείσιμο deal.
- **Σύμβαση συνεργασίας**: read-only view της δικής του σύμβασης — **δεν βλέπει** την τιμή που πλήρωσε η AGRO UNION στον παραγωγό.

### Partner Company (Συνεργαζόμενη Εταιρεία — εργοστάσια/τυποποιητές/προμηθευτές)
- **Προγραμματισμός προμήθειας**: view προβλεπόμενης διαθέσιμης παραγωγής δικτύου ανά περίοδο (aggregate, forecast-style, χωρίς τιμές αγοράς παραγωγών).
- **Λήψη προσφορών πώλησης / Τιμοκατάλογος**: βλέπει τις τιμές πώλησης (`SellOffers`/`PriceListItems`) που της προσφέρει η AGRO UNION για προϊόντα, ή δημοσιεύει δικές της τιμές χονδρικής για εφόδια που προμηθεύει στο δίκτυο (`PriceListItems`, ορατό στους Farmers/Merchants).
- **Διαχείριση συμβάσεων**: view ενεργών συμβάσεων συνεργασίας με το δίκτυο.
- **Αναφορές & ιστορικό**: `Transactions`/`Deals` (μόνο sell-side σκέλος που την αφορά) φιλτραρισμένες ανά προϊόν/περιοχή/περίοδο, με export σε CSV/Excel.

> Χρησιμοποίησε **Policy-based Authorization** στο ASP.NET Core (`[Authorize(Policy = "FarmerOnly")]`, `[Authorize(Policy = "MerchantOnly")]`, `[Authorize(Policy = "PartnerCompanyOnly")]`, `[Authorize(Policy = "AdminOnly")]`) και βεβαιώσου ότι κάθε χρήστης βλέπει **μόνο** τα δικά του δεδομένα εκτός από τα aggregate/ανωνυμοποιημένα views και τον Platform Admin που βλέπει τα πάντα. **Η απόκρυψη του buy-price από τους Merchants/Companies και του sell-price/margin από τους Farmers είναι υποχρεωτικός κανόνας ασφαλείας δεδομένων — να επιβάλλεται και στο επίπεδο API/query, όχι μόνο στο UI.**

---

## 6. Προτεινόμενο σχήμα βάσης δεδομένων (ER — μπορείς να το βελτιστοποιήσεις)

Δημιούργησε EF Core entities + migrations για (τουλάχιστον) τους παρακάτω πίνακες:

- **Users** (ASP.NET Identity: Id, Email, PhoneNumber, FullName/CompanyName, Region, Role, CreatedAt, IsActive)
- **InterestApplications** (Id, Role[Παραγωγός/Έμπορος/Εταιρεία/Άλλο], FullNameOrCompany, Region, ProductInterest, Phone, Email, Message, Status[New/InReview/Approved/Rejected], InternalNotes, CreatedAt, HandledByUserId, HandledAt)
- **ContactMessages** (Id, FullName, Email, Message, CreatedAt, IsRead)
- **Contracts** (Id, UserId, ContractNumber, PartyRole, Subject[Πώληση/Προμήθεια], DurationType[Ορισμένου/Αόριστου], PricingTerms, QuantityTerms, TerminationTerms, StartDate, EndDate, Status[Draft/Active/Terminated], PdfFilePath, CreatedAt)
- **ProductionDeclarations** (Id, ProducerUserId, Product, Quantity, Unit, QualityGrade, Region, AvailableFrom, AvailableTo, Status[Available/Reserved/Sold], CreatedAt, UpdatedAt)
- **PurchaseOffers** (Id, ProducerUserId, Product, BuyPricePerUnit, TargetQuantity, Region, ValidUntil, Status[Active/Closed], CreatedByUserId[Admin], CreatedAt) — προσφορά **αγοράς της AGRO UNION** προς Farmer
- **SellOffers** (Id, BuyerUserId[Merchant/Company], Product, SellPricePerUnit, TargetQuantity, Region, ValidUntil, Status[Active/Closed], CreatedByUserId[Admin], CreatedAt) — προσφορά **πώλησης της AGRO UNION** προς Merchant/Company
- **Deals** (Id, `DealType`[Brokerage/Facilitation], ProductionDeclarationId, FarmerUserId, BuyPricePerUnit, BuyQuantity, BuyerCounterpartyUserId[Merchant/Company], SellPricePerUnit, SellQuantity, `MarginPerUnit` (computed = SellPricePerUnit − BuyPricePerUnit), `TotalMargin` (computed = MarginPerUnit × min(BuyQuantity,SellQuantity)), Status[Proposed/BuySideConfirmed/SellSideConfirmed/Completed/Cancelled], CreatedAt, CompletedAt) — **κεντρική οντότητα του brokerage μοντέλου**, ένωνει το buy-side με το sell-side σκέλος κάθε συναλλαγής και υπολογίζει το περιθώριο κέρδους. Πρόσβαση στα πλήρη στοιχεία (και τα δύο σκέλη + margin) **μόνο** από Admin· ο Farmer βλέπει μόνο τα buy-side πεδία που τον αφορούν, ο Merchant/Company βλέπει μόνο τα sell-side πεδία που τον αφορούν.
- **PickupSchedules** (Id, DealId, ScheduledDate, TransportDetails, Status[Scheduled/Completed/Cancelled])
- **SupplyOrders** (Id, Title, Product[π.χ. Λίπασμα τύπου X], Description, DeadlineDate, Status[Open/Closed], CreatedAt)
- **SupplyOrderItems** (Id, SupplyOrderId, ProducerUserId, Quantity, CreatedAt)
- **PriceListItems** (Id, PublishedByUserId[Company/Admin], Category[Προϊόν/Εφόδιο], ProductName, Price, Unit, EffectiveFrom, EffectiveTo, VisibleToRoles)
- **Transactions** (Id, UserId, RelatedContractId, Product, Quantity, UnitPrice, TotalValue, TransactionDate, Region, Notes)
- **AuditLogs** (Id, UserId, Action, EntityName, EntityId, Timestamp, Details) — προαιρετικό αλλά συνιστάται

Πρόσθεσε τα κατάλληλα **foreign keys, indexes** (π.χ. σε Region, Product, Status για γρήγορα φίλτρα) και **enums** στο C# (π.χ. `ApplicationStatus`, `ContractStatus`, `PartyRole`).

---

## 7. Ροές εργασίας (workflows) που πρέπει να λειτουργούν end-to-end

1. **Δημόσια αίτηση → λογαριασμός**: Επισκέπτης υποβάλλει `InterestApplication` → Admin το βλέπει στο dashboard → Admin το εγκρίνει → σύστημα δημιουργεί `User` (invite email με link ορισμού password) + προσχέδιο `Contract` (status `Draft`) → όταν ο admin το οριστικοποιήσει, γίνεται `Active` και ο χρήστης βλέπει τη σύμβασή του στο portal.
2. **Δήλωση παραγωγής → αγορά από AGRO UNION → μεταπώληση με margin**: Farmer δηλώνει διαθέσιμη ποσότητα (`ProductionDeclaration`) → εμφανίζεται (ανωνυμοποιημένα κατά περιοχή/προϊόν, **χωρίς τιμές**) στους Merchants/Companies ως ενδεικτικός διαθέσιμος όγκος → ο Admin διαπραγματεύεται και καταχωρεί `PurchaseOffer` (τιμή αγοράς) προς τον Farmer, ο Farmer αποδέχεται → παράλληλα ο Admin διαπραγματεύεται `SellOffer` (τιμή πώλησης) προς Merchant/Company, ο αγοραστής αποδέχεται → το σύστημα δημιουργεί `Deal` που συνδέει buy-side + sell-side και **υπολογίζει αυτόματα το margin** → μετά την επιβεβαίωση και των δύο σκελών, δημιουργείται `PickupSchedule` και τελικά καταγράφονται δύο `Transactions` (μία buy-side ορατή στον Farmer, μία sell-side ορατή στον Merchant/Company) συνδεδεμένες στο ίδιο `Deal`.
3. **Συλλογική παραγγελία εφοδίων**: Admin/Company δημιουργεί `SupplyOrder` (π.χ. "Συλλογική παραγγελία λιπάσματος X — προθεσμία 20/09") → Producers συμμετέχουν με ποσότητα (`SupplyOrderItem`) → μετά τη λήξη προθεσμίας, Admin κλείνει την παραγγελία και καταγράφει τελική τιμή/όρους.
4. **Επικοινωνία**: Contact form → αποθήκευση + notification email στο admin inbox.

Κάθε βήμα πρέπει να έχει **validation, error handling, και κατάλληλα notifications (in-app ή/και email)**.

---

## 8. Μη λειτουργικές απαιτήσεις

- Όλα τα μηνύματα validation/UI στα **Ελληνικά**.
- **GDPR-friendly**: consent checkbox στη φόρμα αίτησης ("Με την υποβολή αποδέχεστε..."), δυνατότητα διαγραφής προσωπικών δεδομένων από τον Admin.
- **Ασφάλεια**: password hashing (Identity default), προστασία από CSRF, rate limiting στα public forms (anti-spam / honeypot ή simple captcha), HTTPS redirection, secure cookies.
- **Παραμετροποιήσιμο** μέσω `appsettings.json` / environment variables (connection string, SMTP settings, JWT secret) — **καμία τιμή hardcoded**.
- **Seed data**: δημιούργησε seed script με 1 admin user, 2-3 demo producers/traders/companies, δείγμα δηλώσεων/προσφορών/συμβάσεων, ώστε να μπορώ να κάνω demo αμέσως μετά το `docker-compose up`.
- **Migrations**: αυτόματη εφαρμογή migrations στο startup (ή μέσω script) όταν τρέχει σε container.

---

## 9. Παραδοτέα (deliverables) — τι περιμένω να μου δώσεις

1. Πλήρες **Git repository structure** (μπορείς να το παρουσιάσεις ως file tree + όλα τα αρχεία κώδικα).
2. `docker-compose.yml` + `Dockerfile` έτοιμα για `docker-compose up --build`.
3. EF Core migrations έτοιμα να τρέξουν (`dotnet ef database update` ή αυτόματα στο startup).
4. Seed data script.
5. Πλήρες, λειτουργικό public site πιστό στο επισυναπτόμενο mockup design.
6. Login/Register (invite-based για συνεργάτες, admin bootstrap account).
7. 3 role-based dashboards (Producer/Trader/Company) + Admin panel, με όλες τις λειτουργίες της ενότητας 5.
8. Swagger UI για τα API endpoints.
9. `README.md` με: αρχιτεκτονική, οδηγίες τοπικής εκτέλεσης, default credentials για demo, οδηγίες migrations, τεχνολογικές επιλογές & αιτιολόγηση.
10. Βασικό test project (xUnit) με 5-10 unit tests στη βασική business logic (π.χ. workflow έγκρισης αίτησης, δημιουργία σύμβασης, matching λογική).

---

## 10. Τρόπος εργασίας — βήματα που θέλω να ακολουθήσεις

1. Πρώτα δώσε μου **σύντομο πλάνο αρχιτεκτονικής** (project structure, entities, βασικές αποφάσεις: Blazor Server vs Razor/MVC, JWT vs cookie κ.λπ.) και **περίμενε επιβεβαίωση** πριν γράψεις κώδικα.
2. Μετά την επιβεβαίωση, χτίσε **σταδιακά**: (α) Domain + Infrastructure + DB schema + migrations, (β) Auth + ρόλοι + seed data, (γ) Public site (μετατροπή mockup), (δ) Portal dashboards ανά ρόλο, (ε) Admin panel, (στ) Docker packaging + README.
3. Σε κάθε βήμα, δείξε μου τον πλήρη κώδικα των νέων/τροποποιημένων αρχείων (όχι snippets/αποσπάσματα).
4. Στο τέλος, δώσε μου ένα **checklist** με ό,τι υλοποιήθηκε έναντι της ενότητας 9, ώστε να επαληθεύσω ότι δεν λείπει τίποτα.

---

### Συνημμένο αναφοράς σχεδίασης
Θα σου επισυνάψω το αρχείο `index.html` (το static UI mockup) — χρησιμοποίησέ το ως **πηγή αλήθειας** για: χρώματα, γραμματοσειρές, δομή σελίδων, κείμενα, και τα ακριβή πεδία της φόρμας αίτησης (`role`, `fname`, `fregion`, `fproduct` με τιμές: Ελαιόλαδο/Επιτραπέζια ελιά/Λιπάσματα & εφόδια/Άλλος καρπός/Γενική εμπορική συνεργασία, `fphone`, `femail`, `fmsg`) και της φόρμας επικοινωνίας (`cname`, `cemail`, `cmsg`).

---

*Τέλος prompt.*
