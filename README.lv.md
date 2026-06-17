# ExcelFileMappingEngine

.NET WPF aplikācija Excel failu automātiskai pielāgošanai un transformācijai.

---

## Par projektu

Šis projekts radās no reālas darba vajadzības — regulāras viena un tā paša tipa Excel failu manuālas pielāgošanas. Tā kā šis process atkārtojās atkal un atkal, nolēmu izmantot savas jauniegūtās programmēšanas zināšanas, lai izveidotu rīku, kas šo darbu varētu automatizēt.

Pirmais prototips tika izveidots Windows Forms vidē ar .NET Core. Tas atrisināja konkrēto problēmu, bet bija ļoti specifisks un pielāgots tikai noteiktiem failiem. Tādēļ tika pieņemts lēmums projektu pārveidot un izveidot daudz elastīgāku risinājumu — rīku, kuru varētu izmantot dažādu Excel failu pielāgošanai.

---

## Ideja

Aplikācijas darbības princips:

1. Lietotājs atver Excel failu aplikācijā.
2. Lietotājs vienu reizi sakārto failu pēc savām vajadzībām:

   * izvēlas pareizo header rindu;
   * dzēš nevajadzīgas kolonnas;
   * pievieno jaunas kolonnas;
   * pārsauc kolonnu nosaukumus;
   * veic citas nepieciešamās izmaiņas.
3. Lietotājs saglabā izveidoto mapping.
4. Nākamajā reizē pietiek:

   * atvērt jaunu failu;
   * izvēlēties nepieciešamo mapping;
   * iegūt jau sakārtotu rezultātu.

Mērķis — samazināt atkārtotu manuālu Excel datu apstrādi un pārvērst to par vienu automatizētu procesu.

---

## Pašreizējā izstrādes stadija

Projekts joprojām atrodas aktīvā izstrādes procesā.

Šobrīd aplikācija spēj:

* atvērt Excel failus;
* saglabāt un eksportēt rezultātu;
* izvēlēties Excel header rindu;
* pārveidot tabulas struktūru;
* dzēst kolonnas;
* pievienot jaunas kolonnas;
* mainīt kolonnu nosaukumus;
* saglabāt veiktās darbības JSON failā;
* atkārtoti pielietot saglabātos mapping iestatījumus.

Pirms arhitektūras pārveides saglabātos mapping jau izdevās izmantot praktiskā darbībā.

---

## Arhitektūra un refaktorēšana

Pēc sākotnējā prototipa izveides tika pieņemts lēmums projektu pārveidot uz tīrāku un paplašināmāku struktūru.

Pašlaik notiek pakāpeniska pārbūve, lai:

* nodalītu UI loģiku no biznesa loģikas;
* izveidotu skaidras atbildības starp klasēm;
* padarītu kodu vieglāk uzturamu;
* sagatavotu projektu nākotnes paplašinājumiem.

Galvenās daļas:

* **UI (WPF)** — lietotāja darbības un vizuālā attēlošana;
* **AppManager** — aplikācijas darbību koordinēšana;
* **ExcelService** — darbs ar Excel datiem;
* **FileState** — konkrētā atvērtā faila stāvokļa glabāšana.

---

## Datu apstrādes pieeja

Lai izvairītos no atkārtotas Excel faila ielādes, dati tiek sadalīti:

**RawData**

Oriģinālie dati no Excel faila.

**CurrentData**

Pašreizējais lietotāja redzamais un apstrādātais rezultāts.

Tas ļauj:

* mainīt header rindu bez atkārtotas faila atvēršanas;
* atkārtoti veidot tabulas struktūru;
* saglabāt sākotnējos datus kā nemainīgu avotu.

---

## Nākotnes plāni

Plānotās funkcijas:

* formulu saglabāšana un pielietošana;
* aprēķinu darbības;
* papildus failu pievienošana mapping scenārijiem;
* datu aizpildīšana balstoties uz citu kolonnu vērtībām;
* sarežģītāki datu transformācijas scenāriji.

Piemērs:

Ja Excel failā ir klientu dati, aplikācija varētu automātiski pievienot atbilstošu aģentu katram klientam, izmantojot papildus datu avotu. Ko tādu jau pielietoju pirmajā projektā, plānoju izveidot elastīgāku risinājumu arī šeit.

---

## Validācija un drošība

Viens no nākotnes uzdevumiem ir izveidot validācijas sistēmu, kas palīdz izvairīties no nepareizu mapping pielietošanas.

Plānots:

* pārbaudīt, vai mapping atbilst konkrētajam faila saturam;
* rādīt lietotājam tikai piemērotus mapping variantus;
* novērst situācijas, kur lietotājs mēģina pielietot nepareizu konfigurāciju;
* sniegt skaidrus brīdinājumus par iespējamām problēmām.

---

## Mērķis

Izveidot elastīgu Excel datu apstrādes rīku, kas spēj pielāgoties dažādiem failiem un samazināt atkārtotu manuālu darbu.

Projekts tiek izstrādāts ne tikai kā praktisks darba rīks, bet arī kā mācību projekts labas programmatūras izstrādes prakses apgūšanai.
