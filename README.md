# Budjeckt

Application de bureau Windows (WPF, .NET 10) de suivi de dépenses quotidiennes, **100 % locale** : aucune base de données lourde, aucune connexion internet. Les données sont persistées dans des fichiers texte JSON, un par année (`depenses-<année>.json`), situés dans `%APPDATA%\Budjeckt\` (créé automatiquement au premier enregistrement).

## Objectif

Permettre à l'utilisateur de consigner et suivre ses dépenses quotidiennes :

- **Ajout d'une dépense** (facture) via un formulaire : montant, catégorie, date et heure optionnelles. Date par défaut : aujourd'hui si la facture tombe dans le mois affiché, sinon le 1er du mois.
- **Affichage de l'historique** des dépenses enregistrées.
- **Indicateur financier** : total des dépenses et reste du budget, recalculés dynamiquement à chaque ajout/suppression.
- **Suppression** définitive d'une dépense.
- **Filtrage** par catégorie de dépense ; un **Total** en pied de liste reflète le filtre (total de la catégorie sélectionnée, ou du mois si aucune) et est recalculé à chaque mutation.
- **Catégories personnalisables** : une catégorie ajoutée s'applique aux 12 mois de l'année (un mois qui la possède déjà est ignoré) ; une catégorie peut être supprimée définitivement pour toute l'année, **sa suppression emportant toutes les factures qui lui sont rattachées**.
- **Multi-années** : les données sont découpées par année (`depenses-2026.json`, `depenses-2027.json`…). Toutes les années sont navigables, lisibles et modifiables ; des années peuvent être supprimées définitivement via un panneau de sélection multiple. L'année courante est créée automatiquement au lancement et l'année suivante peut être préparée d'avance via le bouton `＋`.
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
│   ├── Facture.cs                   #   dépense individuelle (immuable)
│   ├── Validation.cs                #   règles métier (noms, montants, ids, date mois + année)
│   └── *Json.cs                     #   DTOs internes de sérialisation JSON
├── BudjecktMstest/                  # tests MSTest net10.0 (191 tests, parallélisés)
└── BudjecktFrontend/                # application WPF net10.0-windows
    ├── MainViewModel.cs             # vue modèle MVVM (CommunityToolkit.Mvvm 8.4.0)
    ├── AnneeSelectionnable.cs       # ligne sélectionnable des années à supprimer
    ├── ApercuFacture.cs             # vue d'une facture pour l'historique
    ├── Formatage.cs                 # formatage d'affichage des montants
    └── MainWindow.xaml(.cs)         # fenêtre principale WPF
```

## Fonctionnalités du frontend WPF

- **Navigation entre les 12 mois** : boutons `‹` / `›` (boucle décembre ↔ janvier) et liste déroulante des mois.
- **Navigation entre les années** : liste déroulante des années disponibles (la plus récente en premier) et bouton `＋` pour créer l'année suivante ; le sélecteur de date et la date par défaut du formulaire sont bornés à l'année affichée.
- **Suppression d'années** : bouton « Supprimer… » ouvrant un panneau de sélection multiple ; une ou plusieurs années peuvent être cochées puis supprimées définitivement après confirmation, avec les boutons « Tout sélectionner » / « Tout désélectionner » pour la sélection en masse. L'année courante supprimée est automatiquement recréée avec les valeurs par défaut.
- **Ajout d'une dépense** dans le mois affiché : montant (nombre fini strictement positif), catégorie, date, heure optionnelle au format « HH:mm ». Le bouton reste désactivé tant que la saisie est invalide (montant ≦ 0 ou non numérique, heure hors `[00:00, 24:00)`, date hors du mois et de l'année).
- **Budget mensuel modifiable** (revenue, nombre fini, négatif admis) recalculé immédiatement avec le reste.
- **Gestion des catégories** : groupbox « Catégories » avec champ de saisie (désactivé si vide) et bouton « + Ajouter » pour ajouter une catégorie à l'année affichée (message de succès/erreur dans le groupbox). Une seconde ligne (liste déroulante + bouton « Supprimer ») supprime une catégorie pour toute l'année après confirmation — **toutes les catégories sont supprimables, y compris celles par défaut** : elles figurent toutes dans la liste de suppression, et **la MessageBox de confirmation rappelle que les factures de la catégorie sont supprimées avec elle** (le message de succès précise ensuite leur sort : aucune, une, ou N). **Un mois doit toujours garder au moins une catégorie : la suppression est refusée avec un message si elle viderait un mois** (le chargeur JSON rejette sinon l'année entière, risquant sa perte).
- **Total de la liste** : dernière ligne de la grille, grisée, en caractères semi-gras et non sélectionnable — elle contient « Total » dans la colonne Catégorie et le montant dans la colonne Montant ; elle suit le filtre (total de la catégorie sélectionnée) et reste visible même liste vide.
- **Raccourcis ergonomiques** : la touche **Entrée** valide le formulaire d'ajout de dépense (montant, catégorie, date ou heure au focus) ; l'historique supporte la **multi-sélection** (Shift+clic / Ctrl+clic pour étendre la sélection, glisser pour une zone), et la touche **Suppr** (ou le bouton « Supprimer la sélection ») supprime toutes les dépenses sélectionnées après confirmation.
- **Filtre par catégorie**, **tri décroissant** (date, puis heure, puis id) et **suppression de la sélection multi** ; toute suppression de dépense (une ou plusieurs) demande confirmation avec le nombre et le montant total.
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

**Exécution des tests (191 tests, MSTest) :**

```
dotnet test Budjeckt/Budjeckt.slnx
dotnet test Budjeckt/Budjeckt.slnx --filter "FullyQualifiedName~Budjeckt.Tests.ValidationTests.VerifierMontantPositif_MontantNaN_LèveArgumentException"
```

**Lancement de l'application WPF :**

```
dotnet run --project Budjeckt/BudjecktFrontend/BudjecktFrontend.csproj
```

## Format des fichiers JSON

Fichiers texte UTF-8, désérialisés avec `System.Text.Json`. Un fichier par année, nommé `depenses-<année>.json` (ex. `depenses-2026.json`). Structure (les autres mois suivent le même modèle) :

```json
{
  "Annee": "2026",
  "Mois": [
    {
      "Nom": "Janvier",
      "Revenue": 2000,
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
        { "Id": 1, "IdCategorie": 1, "Montant": 750, "Date": "2026-01-03T00:00:00", "Heure": "12:30:00" },
        { "Id": 2, "IdCategorie": 2, "Montant": 45, "Date": "2026-01-05T00:00:00" }
      ]
    }
  ]
}
```

Notes sur le format :

- Le fichier doit contenir **exactement 12 mois**, chacun avec un nom connu (français) et au moins une catégorie.
- La **dépense faite par catégorie n'est pas stockée** : elle est recalculée depuis les factures au chargement (voir « Décisions d'architecture »).
- Les montants et le revenue doivent être des nombres finis strictement positifs (NaN et ±∞ sont rejetés au chargement) ; chaque facture doit référencer une catégorie existante.
- Le champ `Heure` est **optionnel** (format `"HH:mm:ss"`), absent si l'heure n'a pas été renseignée ; une heure hors `[00:00, 24:00)` est rejetée.
- La date d'une facture doit être plausible (année 1900-2200) et **appartenir au mois ET à l'année déclaré(e)s** ; les ids de catégories et de factures doivent être positifs et uniques au sein d'un mois.
- Un fichier de plus de 10 Mo est rejeté au chargement.
- Si `depenses-<année>.json` n'existe pas, l'application démarre avec les valeurs par défaut (12 mois, 7 catégories par défaut, budget 0) pour l'année concernée.
- **Localisation** : `%APPDATA%\Budjeckt\`. Le dossier est créé automatiquement au premier enregistrement ; un fichier corrompu est renommé (`*.corrompu-<horodatage>.bak`) avant d'être remplacé, et l'écriture est **atomique** (fichier temporaire puis déplacement).
- **Migration automatique** : au premier lancement, l'ancien fichier unique `depenses.json` est déplacé vers `depenses-<année>.json` selon son champ `Annee` ; si la cible existe déjà, il est conservé sous `depenses-<année>-legacy.json` au lieu d'être écrasé. Un fichier hérité illisible est mis de côté (`.corrompu-*.bak`) pour ne pas être écrasé.

## Dépendances externes

- **CommunityToolkit.Mvvm 8.4.0** (projet `BudjecktFrontend` uniquement) — pattern MVVM pour WPF : source-générateurs `[ObservableProperty]` (INotifyPropertyChanged) et `[RelayCommand]` (ICommand), éliminant le boilerplate de binding. Justification : bibliothèque officielle Microsoft (licence MIT), uniquement source-générée (aucun assembly ajouté au-delà du code généré ni dépendance transitive), surface d'attaque nulle (pas de réseau, pas de désérialisation dynamique). Vérifiée avec `dotnet list package --vulnerable` : aucune vulnérabilité connue.

## Décisions d'architecture

- **Les factures sont la source unique de vérité** : la dépense faite par catégorie, le total et le reste du budget ne sont jamais stockés — ils sont systématiquement recalculés depuis les factures (`MonthBudget.RecalculerTotaux`), y compris après un chargement JSON. La valeur de dépense fournie dans le JSON est ignorée.
- **Ids auto-incrémentés « max + 1 »** : aucun compteur n'est persisté ; le prochain id de catégorie ou de facture est dérivé des ids existants (max + 1). Les ids supprimés ne sont donc jamais réutilisés, même après rechargement.
- **Suppression de catégorie en cascade** : supprimer une catégorie (`MonthBudget.SupprimerCategorie`, par nom, insensible à la casse) emporte définitivement toutes les factures qui la référencent dans les 12 mois de l'année, et retourne leur nombre (affiché à l'utilisateur). Sans cette cascade, les factures orphelines seraient rejetées au prochain chargement par la validation d'intégrité des références (`IdCategorie`), rendant le fichier invalide ; la cascade garantit qu'un fichier reste valide après toute suppression. Aucune catégorie n'est protégée, y compris les défauts, **sauf la dernière d'un mois : sa suppression est refusée (`ArgumentException`) car un mois sans catégorie rendrait le fichier illisible (voir « Format du fichier »)** ; le même refus est remonté dans l'interface avant la confirmation.
- **Validation stricte des entrées** (classe `Validation`) : noms non vides et uniques, montants strictement positifs, heure dans `[00:00, 24:00)` si renseignée, dates bornées 1900-2200 et appartenant au mois ET à l'année du mois affiché. La vérification couvre explicitement `NaN` et ±∞ (IEEE 754 : `NaN <= 0` est faux, et un montant comme `1e39` déborde silencieusement vers +∞ en `float`) ainsi que le débordement de la somme des factures d'une catégorie.
- **DTOs JSON internes séparés du modèle** : `BudjecktJson`, `MonthJson`, `CategoryJson`, `FactureJson` sont des classes internes dédiées à la sérialisation ; le modèle métier (`Budjeckt`, `MonthBudget`, `Facture`) reste indépendant du format de fichier.
- **Invariants vérifiés à la sauvegarde comme au chargement** : `SauvegarderJson` applique le même `ValiderJson` qu'au chargement avant d'écrire — un état mémoire non conforme (moins de 12 mois, un mois sans catégorie, facture orpheline) lève `InvalidDataException` sans rien écrire, plutôt que de produire un fichier que l'application rejetterait au prochain lancement.
- **MVVM avec CommunityToolkit** : le frontend sépare la logique UI (XAML) de la logique métier (`MainViewModel`). La vue charge le modèle `BudjecktBackend`, applique les mutations, sauvegarde et met à jour les observables ; les commandes sont liées via `[RelayCommand]` avec `CanExecute` (formulaire d'ajout désactivé tant que la saisie est invalide).
- **Écriture atomique du fichier** : sauvegarde via fichier temporaire puis `File.Move` (renommage sur le même volume), pour qu'une coupure en plein écriture ne tronque pas le fichier JSON.
- **Un fichier par année** : les données sont découpées en `depenses-<année>.json` dès la version multi-années. L'année est passée aux mois (`MonthBudget.Annee`) et la résolution des dates par défaut, la validation JSON et le sélecteur de date du frontend en tiennent compte. La classe `ArchivesBudjeckt` centralise la détection des années, la création d'une année vierge, la suppression et la migration de l'ancien `depenses.json`.
- **Un fichier par classe** pour le backend, aligné sur la conception lv0/lv1 (`AnalyseProjetBudjeckt.md`).