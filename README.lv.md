# ExcelFileMappingEngine

Rīks, kas automatizē garlaicīgo un atkārtojošos Excel failu apstrādes daļu — veidots ar C#, WPF un PostgreSQL.

## Par projektu

Šis rīks tiek izstrādāts ikvienam, kurš ikdienā strādā ar Excel failiem un atkal un atkal dara vienas un tās pašas garlaicīgās darbības — dzēš vienas un tās pašas kolonnas, veic vienus un tos pašus aprēķinus vai sakārto vienus un tos pašus datus, lai pielāgotu failu tālākam darbam - nosūtīšanai klientam vai kolēģim, vai izmantošanai savā darbā.

Tieši tas mani pamudināja izmantot jauniegūtās zināšanas programmēšanā un izveidot rīku, kas darītu šo darbu manā vietā.

Tā vietā, lai katru reizi atkārtotu vienu un to pašu manuālo darbu, pietiek vienreiz izveidot instrukciju (mapping). Pēc tam aplikācija seko šai instrukcijai un sakārto līdzīgus failus tieši tādā pašā kārtībā vien ar dažiem klikšķiem.

Izstrādes procesā šis ir kļuvis arī par lielisku mācību projektu. Tajā mācos rakstīt tīru un pārdomātu kodu, kā arī izmēģinu jaunus rīkus, piemēram, PostgreSQL, kas man pašai vēl tikko bija jaunums, bet šim projektam izrādījās lieliski piemērots, pateicoties tā jsonb datu tipam.



## Ko šī aplikācija dara?


### Pirmo reizi strādājot ar jauna tipa failu

1. 📂 **Atver** Excel failu aplikācijā.

2. 🔎 **Izvēlies** galvenes rindu (to, kurā atrodas kolonnu nosaukumi).

3. 🛠️ **Izpildi** nepieciešamās pārveides, izmantojot pieejamās funkcijas.

4. 💾 **Saglabā** veiktās darbības kā atkārtoti izmantojamu instrukciju.

5. 📤 Ja nepieciešams, **eksportē** apstrādāto failu.


### Nākamajā reizē, saņemot līdzīgu failu

1. 📂 **Atver** failu aplikācijā.

2. 🔍 **Aplikācija atpazīst** faila tipu un piedāvā pieejamās instrukcijas.

3. ⚙️ **Pielieto** kādu no esošajām instrukcijām vai ✨ izveido jaunu.

4. 📤 **Eksportē** apstrādāto failu.



## Pašreizējais izstrādes statuss un funkcionalitāte

🚧 Aktīva izstrāde turpinās.

### 📄 Darbs ar failiem

- Atver Excel failu aplikācijā.
- Saglabā sākotnējos datus, lai vajadzības gadījumā varētu sākt no jauna, bez nepieciešamības atvērt failu vēlreiz.
- Maina galvenes rindu, pēc kā pārlādē datus un pieejamās instrukcijas.
- Atver citu failu, pilnībā notīrot iepriekšējās sesijas datus.

### ✏️ Datu pārveidošana

- Dzēš vienu vai vairākas kolonnas.
- Pārsauc kolonnas.
- Pievieno jaunu kolonnu pa kreisi vai pa labi no izvēlētās.
- Apvieno divas kolonnas, izmantojot izvēlētu atdalītāju.
- Veido formulas, izmantojot vienkāršus aprēķinus un atbalstītās Excel funkcijas (pašlaik pieejama ROUND).
- Kārto datus izvēlētajā kolonnā augošā vai dilstošā secībā.
- Maina kolonnas datu tipu (šobrīd vēl nepieciešami uzlabojumi).

### 💾 Instrukciju un datu glabāšana

- Saglabā visas veiktās darbības JSON veidā.
- Saglabā instrukcijas PostgreSQL datubāzē.

### 🔍 Failu atpazīšana

- Izveido faila "pirkstu nospiedumu" no galvenes kolonnām datu ielādes laikā un pārvērš to hash vērtībā.
- Saglabā faila definīciju (pirkstu nospiedumu, hash un nosaukumu) datubāzē.
- Salīdzina atvērtā faila hash ar datubāzē saglabātajām definīcijām, lai noteiktu, vai faila veids jau eksistē datubāzē un parāda pieejamās instrukcijas.



## 🏗️ Arhitektūra, tehnoloģijas un izvēlētie risinājumi

Sākotnēji projektu veidoju vienkārši kā strādājošu prototipu, lai pārbaudītu pašu ideju un darbplūsmu. Attīstoties funkcionalitātei, sapratu, ka ir jāveic izmaiņas, lai kods būtu saprotams un strukturēts.

Sadalīju klašu atbildibas, atdalīju lietotāja saskarni no pārējā projekta un gatavoju projektu nākotnei - paplašināmībai un vieglākai uzturēšanai.

## Projekta struktūra

### Galvenās komponentes

- **WPF UI** — nodrošina lietotāja saskarni un vizuālo attēlojumu.
- **AppManager** — koordinē aplikācijas darbplūsmas un savieno lietotāja saskarni ar biznesa loģiku.
- **Services** — satur galveno aplikācijas loģiku:
    FileService — failu ielāde un ar failiem saistītās darbības;
    DataService — datu pārveidošana un darbs ar datiem atmiņā;
    MappingService — instrukciju izveide, saglabāšana un pielietošana.
- **Repositories** — nodrošina saziņu ar datubāzi un datu saglabāšanu.
**DataSession** — glabā pašreizējās darba sesijas informāciju. Tajā iekļauti tādi stāvokļa modeļi kā **FileState** un **DataState**, kuros tiek glabāta informācija par atvērto failu, datiem, instrukcijām un citiem saistītajiem datiem.

Arhitektūra skaidri nodala pašreizējos darba datus no atkārtoti izmantojamajām pārveidošanas definīcijām. Darba laikā veiktās izmaiņas attiecas tikai uz konkrēto datu kopu, savukārt pārveidošanas soļus iespējams saglabāt kā instrukciju un vēlāk izmantot atkārtoti līdzīgiem failiem.

### Izmantotās tehnoloģijas

- **C# / .NET + WPF** — darbvirsmas aplikācija.
- **PostgreSQL** — instrukciju un failu definīciju glabāšanai.
- **Dapper** — datu piekļuvei un saziņai ar datubāzi.
- **ClosedXML** — darbam ar Excel failiem.
- **JSON / PostgreSQL jsonb** — atkārtoti izmantojamu instrukciju, kā arī failu "pirkstu nospiedumu" glabāšanai.
- **Git, GitHub** — versiju kontrolei.
  
  
### Kāpēc ClosedXML?

Tā kā darbs ar Excel failiem ir šī projekta galvenā daļa, piemērotas bibliotēkas izvēle bija svarīgs solis.

Apskatīju vairākas pieejamās bibliotēkas un salīdzināju tās pēc lietošanas ērtuma un tā, cik labi tās atbilst šī projekta vajadzībām. ClosedXML izcēlās ar pieejamību, kā arī darbs ar darblapām, šūnām, formulām un failu saglabāšanu ir saprotams un dabisks.

Šī bibliotēka man bija pazīstama no dažiem citiem mācību projektiem un, atskatoties uz paveikto, domāju, ka arī šoreiz tā bija pareizā izvēle.


### Kāpēc PostgreSQL?

PostgreSQL izvēlējos tāpēc, ka tas apvieno relāciju datubāzes priekšrocības ar elastīgu JSON datu glabāšanu.

Aplikācijas instrukcijas tiek glabātas strukturētā JSON formātā. PostgreSQL jsonb datu tips ļauj tos ērti saglabāt un izgūt, vienlaikus saglabājot visas relāciju datubāzes priekšrocības.

Šāda pieeja ļauj paplašināt instrukciju glabāšanas iespējas, neveidojot atsevišķu datubāzes struktūru katrai izmaiņai.



## 🚀 Nākotnes plāni


### 🔹 Tuvākajā laikā

✅ **Vienību testi**
Projektam augot, testi palīdzēs pārliecināties, ka jaunas izmaiņas nesabojā jau esošo funkcionalitāti.

📝 **Kļūdu reģistrēšana**
Saglabāt kļūdas un svarīgus notikumus datubāzē, lai tās būtu vieglāk atrast un novērst.

✔️ **Datu validācija**
Pārbaudīt, vai importētajiem failiem ir sagaidāmā struktūra, un uzturēt datus korektus.

🔄 **Uzlabota mappingu pārvaldība**
Padarīt instrukciju izveidi, rediģēšanu, saglabāšanu un atkārtotu izmantošanu ērtāku. Mērķis ir, lai lietotājs failu sagatavo vienreiz un pēc tam to pašu procesu var atkārtot ar dažiem klikšķiem.

🧩 **Vairāk datu pārveidošanas iespēju**
Pievienot sarežģītākas darbības, piemēram, kolonnu datu atdalīšanu, nosacījumu pārveides un citus datu sagatavošanas rīkus.

📄 **Galvenes un kājenes atbalsts**

Šobrīd viss fails tiek apstrādāts kā viena tabula. Nākamais solis ir sadalīt to vairākās neatkarīgās daļās:

**Galvene** — informācija virs datu tabulas.
**Dati** — galvenā tabulas daļa.
**Kājene** — kopsavilkumi, aprēķini vai cita papildu informācija.

Rediģēšanas laikā katra daļa tiks pārvaldīta atsevišķi, bet saglabājot failu tās tiks atkal apvienotas vienā Excel dokumentā.

🔍 **Ārējie atsauces faili**

Lietotāji varēs pievienot atsauces failus, lai automātiski aizpildītu trūkstošo informāciju.

Piemēram, tā vietā, lai manuāli pievienotu klientam piesaistīto menedžeri, aplikācija to varēs atrast uzņēmuma nodrošinātā sarakstā.


### 🌐 Ilgtermiņā — tīmekļa versija

Ilgtermiņa mērķis ir izveidot šī rīka tīmekļa versiju, kur lietotāji varēs izveidot kontu un izmantot aplikāciju tieši pārlūkprogrammā.

Pašreizējā refaktorēšana tam veido labu pamatu, jo lietotāja saskarne ir nodalīta no biznesa loģikas, un katrai projekta daļai ir skaidri definēta atbildība.

Šis ir pirmais projekts, kurā es ne tikai mācos par tīru arhitektūru, bet arī cenšos to reāli pielietot, jo vēlos izveidot ko tādu, kas varētu kļūt par noderīgu rīku, nevis tikai mācību projektu.


### 🎯 Mērķis

Izveidot elastīgu rīku, kas novērš atkārtotu manuālu darbu, gatavojot Excel failus.

Mērķis ir radīt kaut ko noderīgu reālām darba situācijām jau šodien, vienlaikus pilnveidojot savas prasmes tīras, uzturamas un mērogojamas programmatūras izstrādē.


💡 Ir idejas vai ieteikumi?

Priecāšos par atsauksmēm, idejām un ierosinājumiem! 😊
