# Budjeckt

Application de bureau Windows (WPF, .NET 10) de suivi de dépenses quotidiennes, **100 % locale** : aucune base de données lourde, aucune connexion internet. Les données sont persistées dans des fichiers texte JSON, un par année (`depenses-<année>.json`), situés dans `%APPDATA%\Budjeckt\` (créé automatiquement au premier enregistrement).

## Objectif

Permettre à l'utilisateur de consigner et suivre ses dépenses quotidiennes :

- **Ajout d'une dépense** (facture) via un formulaire : montant, catégorie, date et heure optionnelles. Date par défaut : aujourd'hui si la facture tombe dans le mois affiché, sinon le 1er du mois.
- **Factures par défaut (récurrentes)** : une case « Facture par défaut — se répète chaque mois » dans le formulaire enregistre la dépense comme modèle récurrent de l'année ; chaque mois suivant (à partir de son mois de création) la reproduit automatiquement à la première ouverture, tant que le défaut reste actif. La suppression d'une de ses copies (ou de sa catégorie) désactive le récurrent — sur cette copie et les suivantes — sans toucher aux mois antérieurs.
- **Affichage de l'historique** des dépenses enregistrées.
- **Indicateur financier** : total des dépenses et reste du budget, recalculés dynamiquement à chaque ajout/suppression.
- **Suppression** définitive d'une dépense.
- **Filtrage** par catégorie de dépense ; un **Total** en pied de liste reflète le filtre (total de la catégorie sélectionnée, ou du mois si aucune) et est recalculé à chaque mutation.
- **Catégories personnalisables** : une catégorie ajoutée s'applique aux 12 mois de l'année (un mois qui la possède déjà est ignoré) ; une catégorie peut être supprimée définitivement pour toute l'année, **sa suppression emportant toutes les factures qui lui sont rattachées**.
- **Multi-années** : les données sont découpées par année (`depenses-2026.json`, `depenses-2027.json`…). Toutes les années sont navigables, lisibles et modifiables ; des années peuvent être supprimées définitivement via un panneau de sélection multiple. L'année courante est créée automatiquement au lancement et l'année suivante peut être préparée d'avance via le bouton `＋`.
- **Cagnotte globale** : épargne unique commune à toutes les années, persistée dans `%APPDATA%\Budjeckt\cagnotte.json`. Un **dépôt** (bouton « Déposer dans la cagnotte ») retire le montant du reste du budget du mois affiché — plafonné au reste non négatif — et un **retrait** (« Retirer de la cagnotte ») le réinjecte dans le reste du mois affiché — plafonné au solde de la cagnotte. Le solde s'affiche sous « Budget du mois », chaque opération est enregistrée dans `cagnotte.json` puis dans l'année.
- **Persistance** automatique dans le fichier de l'année affichée après chaque ajout, suppression ou changement de budget, et chargement au démarrage. L'ancien fichier unique `depenses.json` est migré automatiquement vers le format par année au premier lancement.

Référence détaillée : `AnalyseProjetBudjeckt.md` (cahier des charges + conception).

## Structure du projet

La solution `Budjeckt.slnx` (format XML `.slnx`) et les trois projets sont regroupés sous le dossier `Budjeckt/` :

```
Budjeckt/
├── Budjeckt.slnx                    # solution (format XML .slnx)
├── BudjecktBackend/                 # bibliothèque de classes net10.0 (modèle métier)
│   ├── Budjeckt.cs                  #   année (lv0) : charge/sauvegarde du JSON (un fichier par année)
│   ├── ArchivesBudjeckt.cs          #   années disponibles, création, suppression, migration du fichier hérité
│   ├── MonthBudget.cs               #   mois (lv1) : catégories, budget, factures, totaux (année-aware)
│   ├── BudgetHebdomadaire.cs        #   répartition « reste par semaine » (semaines à partir du 1er, restant réparti sur les semaines restantes, poids 1 / 0,35 sur la 5e)
│   ├── SemaineBudget.cs             #   semaine du mois (bornes, marque « courante », part du restant)
│   ├── Facture.cs                   #   dépense individuelle (immuable, marque « facture par défaut »)
│   ├── FactureParDefaut.cs          #   dépense récurrente (catégorie + montant + mois de création)
│   ├── Cagnotte.cs              #   cagnotte globale (solde, dépôts/retraits, fichier cagnotte.json)
│   ├── Validation.cs                #   règles métier (noms, montants, ids, date mois + année)
│   └── *Json.cs                     #   DTOs internes de sérialisation JSON
├── BudjecktMstest/                  # tests MSTest net10.0 (321 tests, parallélisés)
└── BudjecktFrontend/                # application WPF net10.0-windows
    ├── MainViewModel.cs             # vue modèle MVVM (CommunityToolkit.Mvvm 8.4.0)
    ├── AnneeSelectionnable.cs       # ligne sélectionnable des années à supprimer
    ├── ApercuFacture.cs             # vue d'une facture pour l'historique
    ├── SemaineApercu.cs             # vue d'une semaine du panneau « Reste par semaine »
    ├── Formatage.cs                 # formatage d'affichage des montants
    └── MainWindow.xaml(.cs)         # fenêtre principale WPF
```

## Fonctionnalités du frontend WPF

- **Navigation entre les 12 mois** : boutons `‹` / `›` (boucle décembre ↔ janvier) et liste déroulante des mois.
- **Navigation entre les années** : liste déroulante des années disponibles (la plus récente en premier) et bouton `＋` pour créer l'année suivante ; le sélecteur de date et la date par défaut du formulaire sont bornés à l'année affichée.
- **Suppression d'années** : bouton « Supprimer… » ouvrant un panneau de sélection multiple ; une ou plusieurs années peuvent être cochées puis supprimées définitivement après confirmation, avec les boutons « Tout sélectionner » / « Tout désélectionner » pour la sélection en masse. L'année courante supprimée est automatiquement recréée avec les valeurs par défaut.
- **Ajout d'une dépense** dans le mois affiché : montant (nombre fini strictement positif), catégorie, date, heure optionnelle au format « HH:mm ». Le bouton reste désactivé tant que la saisie est invalide (montant ≦ 0 ou non numérique, heure hors `[00:00, 24:00)`, date hors du mois et de l'année). Une case **« Facture par défaut — se répète chaque mois »** (sous le champ Heure) enregistre en plus la dépense comme récurrente de l'année : le défaut se pose sur le mois courant et se reproduit automatiquement sur les mois suivants (un seul défaut par couple catégorie + montant ; la catégorie réservée « Cagnotte » n'est pas proposée).
- **Budget mensuel modifiable** (revenue, nombre fini, négatif admis) recalculé immédiatement avec le reste.
- **Cagnotte globale** : groupbox « Cagnotte » sous « Budget du mois » affichant le solde (commun à toutes les années) et un champ montant avec deux boutons. « Déposer dans la cagnotte » soustrait le montant du reste du mois affiché (le bouton reste désactivé si le montant excède le reste non négatif) ; « Retirer de la cagnotte » réinjecte le montant dans le reste du mois (désactivé si le montant excède le solde). Le montant est un nombre fini strictement positif ; après chaque opération `cagnotte.json` puis le mois sont sauvegardés, et la répartition hebdomadaire est recalculée (l'argent mis de côté n'est plus disponible, l'argent récupéré redevient disponible).
- **« Reste par semaine »** : panneau sous « Budget du mois » listant les **semaines comptées à partir du 1er du mois** (7 jours par ligne : 1→7, 8→14, 15→21, 22→28, puis les jours 29 et plus s'ils existent ; les mois de 28 jours — février non bissextil — n'ont donc que 4 lignes). Le **restant du mois** (revenu − total des dépenses) est **réparti proportionnellement sur les semaines restantes** : chaque semaine pleine pèse 1, la 5e semaine, quand elle existe, pèse 0,35 (un mois ≈ 4,35 semaines). Une dépense effectuée n'importe où dans le mois réduit donc la part de toutes les semaines restantes (la répartition est recalculée sur le nouveau restant). La **dernière ligne est ajustée au centime** pour que la somme des montants affichés redonne exactement le restant du mois. Quand le mois affiché est le **mois courant système**, les semaines strictement passées affichent **0,00 €** — leur argent non dépensé a été reporté — et le restant n'est réparti que sur la semaine courante et les suivantes ; la semaine contenant la date système est en gras. Sinon (autre mois affiché), aucune semaine n'est écartée ni mise en gras : le restant est réparti sur toutes les semaines du mois. Recalculé à chaque changement de mois, de budget ou de dépense.
- **Gestion des catégories** : groupbox « Catégories » avec champ de saisie (désactivé si vide) et bouton « + Ajouter » pour ajouter une catégorie à l'année affichée (message de succès/erreur dans le groupbox). Une seconde ligne (liste déroulante + bouton « Supprimer ») supprime une catégorie pour toute l'année après confirmation — **toutes les catégories sont supprimables, y compris celles par défaut** : elles figurent toutes dans la liste de suppression, et **la MessageBox de confirmation rappelle que les factures de la catégorie sont supprimées avec elle** (le message de succès précise ensuite leur sort : aucune, une, ou N). **Un mois doit toujours garder au moins une catégorie : la suppression est refusée avec un message si elle viderait un mois** (le chargeur JSON rejette sinon l'année entière, risquant sa perte).
- **Total de la liste** : dernière ligne de la grille, grisée, en caractères semi-gras et non sélectionnable — elle contient « Total » dans la colonne Catégorie et le montant dans la colonne Montant ; elle suit le filtre (total de la catégorie sélectionnée) et reste visible même liste vide.
- **Raccourcis ergonomiques** : la touche **Entrée** valide le formulaire d'ajout de dépense (montant, catégorie, date ou heure au focus) ; l'historique supporte la **multi-sélection** (Shift+clic / Ctrl+clic pour étendre la sélection, glisser pour une zone), et la touche **Suppr** (ou le bouton « Supprimer la sélection ») supprime les dépenses sélectionnées après confirmation.
- **Filtre par catégorie**, **tri décroissant** (date, puis heure, puis id) et **suppression de la sélection multi** ; toute suppression de dépense (une ou plusieurs) demande confirmation avec le nombre et le montant total. La ligne « Total » et les **mouvements de cagnotte** (non supprimables : annulez un retrait par un dépôt ou l'inverse) sont **exclus de la suppression multi** — seules les lignes supprimables sont supprimées, et la confirmation ne porte que sur elles (le bouton se désactive si la sélection ne contient que des lignes non supprimables).
- **Barre de synthèse** : total du mois et reste du budget (vert / rouge).
- **Gestion des erreurs** : messages français non techniques ; un fichier de données corrompu est mis de côté (`depenses.json.corrompu-*.bak`) au lieu d'être écrasé.

## Prérequis

- **Windows** : le frontend WPF cible `net10.0-windows` et ne se compile/lance que sur Windows.
- **.NET SDK 10** (version `10.0.302` installée sur cette machine).

## Compilation et lancement

Commandes à exécuter depuis la racine du repo (`C:\Users\Terrence\Desktop\Budjeckt`).

**Compilation de la solution :**

```
dotnet build Budjeckt/Budjeckt.slnx
```

**Exécution des tests (321 tests, MSTest) :**

```
dotnet test Budjeckt/Budjeckt.slnx
dotnet test Budjeckt/Budjeckt.slnx --filter "FullyQualifiedName~Budjeckt.Tests.ValidationTests.VerifierMontantPositif_MontantNaN_LèveArgumentException"
```

**Lancement de l'application WPF :**

```
dotnet run --project Budjeckt/BudjecktFrontend/BudjecktFrontend.csproj
```

## Format des fichiers JSON

Fichiers texte UTF-8, désérialisés avec `System.Text.Json`. Un fichier par année, nommé `depenses-<année>.json` (ex. `depenses-2026.json`), plus un fichier global `cagnotte.json` pour le solde de la cagnotte. Structure (les autres mois suivent le même modèle) :

```json
{
  "Annee": "2026",
  "Mois": [
    {
      "Nom": "Janvier",
      "Revenue": 2000,
      "MontantCagnotte": 100,
      "Categories": [
        { "Id": 1, "Nom": "Loyer" },
        { "Id": 2, "Nom": "Eau" },
        { "Id": 3, "Nom": "Electricite" },
        { "Id": 4, "Nom": "Chauffage" },
        { "Id": 5, "Nom": "Alimentation" },
        { "Id": 6, "Nom": "Transports" },
        { "Id": 7, "Nom": "Loisirs" }
      ],
      "Factures": [
        { "Id": 1, "IdCategorie": 1, "Montant": 750, "Date": "2026-01-03T00:00:00", "Heure": "12:30:00", "EstParDefaut": true },
        { "Id": 2, "IdCategorie": 2, "Montant": 45, "Date": "2026-01-05T00:00:00" }
      ]
    }
  ],
  "FacturesParDefaut": [
    { "Categorie": "Loyer", "Montant": 750, "MoisCreation": 1, "Active": true }
  ]
}
```

Notes sur le format :

- Le fichier doit contenir **exactement 12 mois**, chacun avec un nom connu (français) et au moins une catégorie.
- La **dépense faite par catégorie n'est pas stockée** : elle est recalculée depuis les factures au chargement (voir « Décisions d'architecture »).
- Les montants et le revenue doivent être des nombres finis strictement positifs (NaN et ±∞ sont rejetés au chargement) ; chaque facture doit référencer une catégorie existante.
- `MontantCagnotte` (optionnel, absent des fichiers antérieurs à la cagnotte) est le montant net que le mois a mis en cagnotte : positif = argent mis de côté (soustrait du reste), négatif = argent réinjecté depuis la cagnotte (ajouté au reste) ; le reste du mois le compte toujours : `reste = revenue − total des dépenses − MontantCagnotte`. NaN et ±∞ sont rejetés au chargement.
- Le champ `Heure` est **optionnel** (format `"HH:mm:ss"`), absent si l'heure n'a pas été renseignée ; une heure hors `[00:00, 24:00)` est rejetée.
- La date d'une facture doit être plausible (année 1900-2200) et **appartenir au mois ET à l'année déclaré(e)s** ; les ids de catégories et de factures doivent être positifs et uniques au sein d'un mois.
- Un fichier de plus de 10 Mo est rejeté au chargement.
- Si `depenses-<année>.json` n'existe pas, l'application démarre avec les valeurs par défaut (12 mois, 7 catégories par défaut, budget 0) pour l'année concernée.
- **Localisation** : `%APPDATA%\Budjeckt\`. Le dossier est créé automatiquement au premier enregistrement ; un fichier corrompu est renommé (`*.corrompu-<horodatage>.bak`) avant d'être remplacé, et l'écriture est **atomique** (fichier temporaire puis déplacement).
- **Factures par défaut** : champ racine optionnel `FacturesParDefaut` (absent des fichiers antérieurs à cette fonctionnalité = aucune facture récurrente). Chaque entrée contient `Categorie` (nom de catégorie existant dans l'année), `Montant` (nombre fini strictement positif), `MoisCreation` (1-12 : première reproduction à partir de ce mois) et `Active` (booléen). Les entrées en double (catégorie + montant, insensible à la casse), avec `Montant` invalide, `MoisCreation` hors `[1, 12]`, une catégorie inconnue ou la catégorie réservée « Cagnotte » sont rejetées au chargement. Une facture matérialisée par un défaut porte `"EstParDefaut": true` ; un tel mouvement sur la catégorie « Cagnotte » est rejeté au chargement.
- **Cagnotte** : `cagnotte.json` contient `{ "Solde": float }` — le solde global, somme des `MontantCagnotte` de tous les mois de toutes les années. Il ne doit jamais être négatif (NaN, ±∞ et valeurs négatives sont rejetées au chargement ; un retrait supérieur au solde est refusé). Fichier absent, ou mois sans `MontantCagnotte`, valent 0 ; un `cagnotte.json` corrompu est mis de côté (`.corrompu-*.bak`) puis la cagnotte repart à zéro.
- **Migration automatique** : au premier lancement, l'ancien fichier unique `depenses.json` est déplacé vers `depenses-<année>.json` selon son champ `Annee` ; si la cible existe déjà, il est conservé sous `depenses-<année>-legacy.json` au lieu d'être écrasé. Un fichier hérité illisible est mis de côté (`.corrompu-*.bak`) pour ne pas être écrasé.

## Dépendances externes

- **CommunityToolkit.Mvvm 8.4.0** (projet `BudjecktFrontend` uniquement) — pattern MVVM pour WPF : source-générateurs `[ObservableProperty]` (INotifyPropertyChanged) et `[RelayCommand]` (ICommand), éliminant le boilerplate de binding. Justification : bibliothèque officielle Microsoft (licence MIT), uniquement source-générée (aucun assembly ajouté au-delà du code généré ni dépendance transitive), surface d'attaque nulle (pas de réseau, pas de désérialisation dynamique). Vérifiée avec `dotnet list package --vulnerable` : aucune vulnérabilité connue.

## Décisions d'architecture

- **Les factures sont la source unique de vérité** : la dépense faite par catégorie, le total et le reste du budget ne sont jamais stockés — ils sont systématiquement recalculés depuis les factures (`MonthBudget.RecalculerTotaux`), y compris après un chargement JSON. La valeur de dépense fournie dans le JSON est ignorée.
- **Ids auto-incrémentés « max + 1 »** : aucun compteur n'est persisté ; le prochain id de catégorie ou de facture est dérivé des ids existants (max + 1). Les ids supprimés ne sont donc jamais réutilisés, même après rechargement.
- **Suppression de catégorie en cascade** : supprimer une catégorie (`MonthBudget.SupprimerCategorie`, par nom, insensible à la casse) emporte définitivement toutes les factures qui la référencent dans les 12 mois de l'année, et retourne leur nombre (affiché à l'utilisateur). Sans cette cascade, les factures orphelines seraient rejetées au prochain chargement par la validation d'intégrité des références (`IdCategorie`), rendant le fichier invalide ; la cascade garantit qu'un fichier reste valide après toute suppression. Aucune catégorie n'est protégée, y compris les défauts, **sauf la dernière d'un mois : sa suppression est refusée (`ArgumentException`) car un mois sans catégorie rendrait le fichier illisible (voir « Format du fichier »)** ; le même refus est remonté dans l'interface avant la confirmation.
- **Factures par défaut au niveau année** : les modèles récurrents (`FactureParDefaut`) sont stockés au niveau année (`Budjeckt.FacturesParDefaut`) et non par mois, car les ids diffèrent d'un mois à l'autre tandis que la clé métier (catégorie + montant) est stable. La clé de déduplication est `"{nom}|{montant:R}"` (insensible à la casse). L'application (`AppliquerFacturesParDefaut`) ne reproduit que sur les mois dont le rang ≥ `MoisCreation` (non rétroactif), ignore les mois inconnus et les catégories manquantes, et empêche la catégorie réservée « Cagnotte » de cibler un défaut (refus à l'ajout, au chargement et dans `MonthBudget.AjouterFacture`). La suppression de l'une de ses copies ou de sa catégorie la désactive — jamais le contraire — pour ne pas affecter les mois antérieurs.
- **Validation stricte des entrées** (classe `Validation`) : noms non vides et uniques, montants strictement positifs, heure dans `[00:00, 24:00)` si renseignée, dates bornées 1900-2200 et appartenant au mois ET à l'année du mois affiché. La vérification couvre explicitement `NaN` et ±∞ (IEEE 754 : `NaN <= 0` est faux, et un montant comme `1e39` déborde silencieusement vers +∞ en `float`) ainsi que le débordement de la somme des factures d'une catégorie.
- **DTOs JSON internes séparés du modèle** : `BudjecktJson`, `MonthJson`, `CategoryJson`, `FactureJson` sont des classes internes dédiées à la sérialisation ; le modèle métier (`Budjeckt`, `MonthBudget`, `Facture`) reste indépendant du format de fichier.
- **Invariants vérifiés à la sauvegarde comme au chargement** : `SauvegarderJson` applique le même `ValiderJson` qu'au chargement avant d'écrire — un état mémoire non conforme (moins de 12 mois, un mois sans catégorie, facture orpheline) lève `InvalidDataException` sans rien écrire, plutôt que de produire un fichier que l'application rejetterait au prochain lancement.
- **MVVM avec CommunityToolkit** : le frontend sépare la logique UI (XAML) de la logique métier (`MainViewModel`). La vue charge le modèle `BudjecktBackend`, applique les mutations, sauvegarde et met à jour les observables ; les commandes sont liées via `[RelayCommand]` avec `CanExecute` (formulaire d'ajout désactivé tant que la saisie est invalide).
- **Écriture atomique du fichier** : sauvegarde via fichier temporaire puis `File.Move` (renommage sur le même volume), pour qu'une coupure en plein écriture ne tronque pas le fichier JSON.
- **Un fichier par année** : les données sont découpées en `depenses-<année>.json` dès la version multi-années. L'année est passée aux mois (`MonthBudget.Annee`) et la résolution des dates par défaut, la validation JSON et le sélecteur de date du frontend en tiennent compte. La classe `ArchivesBudjeckt` centralise la détection des années, la création d'une année vierge, la suppression et la migration de l'ancien `depenses.json`.
- **Cagnotte globale à double écriture** : le solde vit dans `cagnotte.json` (global, toutes années confondues) mais chaque opération est aussi enregistrée dans le `MontantCagnotte` du mois affiché ; le reste du mois le compte : `reste = revenue − total des dépenses − MontantCagnotte`. Cette redondance conserve à chaque mois son reste correct (déduit de la cagnotte) même si `cagnotte.json` venait à être perdu, et évite de parcourir toutes les années pour connaître le reste d'un mois. Le plafond d'un dépôt est le reste non négatif du mois (on ne peut pas mettre en déficit le mois pour nourrir la cagnotte), celui d'un retrait le solde global. À chaque opération, `cagnotte.json` est sauvegardé **d'abord**, puis le fichier de l'année : si l'écriture de la cagnotte échoue, celle de l'année est court-circuitée et aucun des deux fichiers n'enregistre le mouvement (une trace partielle « ferait disparaître » de l'argent du pot au prochain lancement). Fenêtre résiduelle inévitable d'une écriture en deux fichiers : si la cagnotte réussit mais que le fichier de l'année échoue ensuite, le pot affichera le mouvement sans qu'il apparaisse dans le mois — l'ordre actuel garde la fenêtre la moins coûteuse (l'argent reste visible plutôt que perdu). Un fichier `cagnotte.json` corrompu est mis de côté (`.corrompu-*.bak`) et la cagnotte repart à zéro — le reste des mois est préservé puisque le montant par mois est aussi dans les fichiers d'année.
- **Un fichier par classe** pour le backend, aligné sur la conception lv0/lv1 (`AnalyseProjetBudjeckt.md`).