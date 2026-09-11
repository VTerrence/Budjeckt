# Budjeckt

Application de bureau Windows (WPF, .NET 10) de suivi de dépenses quotidiennes, **100 % locale** : aucune base de données lourde, aucune connexion internet. Les données sont persistées dans un fichier texte JSON, situé dans `%APPDATA%\Budjeckt\depenses.json` (créé automatiquement au premier enregistrement).

## Objectif

Permettre à l'utilisateur de consigner et suivre ses dépenses quotidiennes :

- **Ajout d'une dépense** (facture) via un formulaire : montant, catégorie, date et heure optionnelles. Date par défaut : aujourd'hui si la facture tombe dans le mois affiché, sinon le 1er du mois.
- **Affichage de l'historique** des dépenses enregistrées.
- **Indicateur financier** : total des dépenses et reste du budget, recalculés dynamiquement à chaque ajout/suppression.
- **Suppression** définitive d'une dépense.
- **Filtrage** par catégorie de dépense.
- **Persistance** automatique dans le fichier JSON après chaque ajout, suppression ou changement de budget, et chargement au démarrage.

Référence détaillée : `AnalyseProjetBudjeckt.md` (cahier des charges + conception).

## Structure du projet

La solution `Budjeckt.slnx` (format XML `.slnx`) et les trois projets sont regroupés sous le dossier `Budjeckt/` :

```
Budjeckt/
├── Budjeckt.slnx                    # solution (format XML .slnx)
├── BudjecktBackend/                 # bibliothèque de classes net10.0 (modèle métier)
│   ├── Budjeckt.cs                  #   année (lv0) : charge/sauvegarde du JSON
│   ├── MonthBudget.cs               #   mois (lv1) : catégories, budget, factures, totaux
│   ├── Facture.cs                   #   dépense individuelle (immuable)
│   ├── Validation.cs                #   règles métier (noms, montants, ids)
│   └── *Json.cs                     #   DTOs internes de sérialisation JSON
├── BudjecktMstest/                  # tests MSTest net10.0 (129 tests, parallélisés)
└── BudjecktFrontend/                # application WPF net10.0-windows
    ├── MainViewModel.cs             # vue modèle MVVM (CommunityToolkit.Mvvm 8.4.0)
    ├── ApercuFacture.cs             # vue d'une facture pour l'historique
    ├── Formatage.cs                 # formatage d'affichage des montants
    └── MainWindow.xaml(.cs)         # fenêtre principale WPF
```

## Fonctionnalités du frontend WPF

- **Navigation entre les 12 mois** : boutons `‹` / `›` (boucle décembre ↔ janvier) et liste déroulante des mois.
- **Ajout d'une dépense** dans le mois affiché : montant (nombre fini strictement positif), catégorie, date, heure optionnelle au format « HH:mm ». Le bouton reste désactivé tant que la saisie est invalide (montant ≦ 0 ou non numérique, heure hors `[00:00, 24:00)`, date hors du mois).
- **Budget mensuel modifiable** (revenue, nombre fini, négatif admis) recalculé immédiatement avec le reste.
- **Filtre par catégorie**, **tri décroissant** (date, puis heure, puis id) et **suppression** de la dépense sélectionnée.
- **Barre de synthèse** : total affiché (après filtre), total du mois et reste du budget (vert / rouge).
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

**Exécution des tests (129 tests, MSTest) :**

```
dotnet test Budjeckt/Budjeckt.slnx
dotnet test Budjeckt/Budjeckt.slnx --filter "FullyQualifiedName~Budjeckt.Tests.ValidationTests.VerifierMontantPositif_MontantNaN_LèveArgumentException"
```

**Lancement de l'application WPF :**

```
dotnet run --project Budjeckt/BudjecktFrontend/BudjecktFrontend.csproj
```

## Format du fichier depenses.json

Fichier texte UTF-8, désérialisé avec `System.Text.Json`. Structure (les autres mois suivent le même modèle) :

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
- La date d'une facture doit être plausible (année 1900-2100) et **appartenir au mois affiché** ; les ids de catégories et de factures doivent être positifs et uniques au sein d'un mois.
- Un fichier de plus de 10 Mo est rejeté au chargement.
- Si `depenses.json` n'existe pas au lancement, l'application démarre avec les valeurs par défaut (12 mois, 7 catégories par défaut, budget 0).
- **Localisation** : `%APPDATA%\Budjeckt\depenses.json`. Le dossier est créé automatiquement au premier enregistrement ; un fichier corrompu est renommé (`*.corrompu-<horodatage>.bak`) avant d'être remplacé, et l'écriture est **atomique** (fichier temporaire puis déplacement).

## Dépendances externes

- **CommunityToolkit.Mvvm 8.4.0** (projet `BudjecktFrontend` uniquement) — pattern MVVM pour WPF : source-générateurs `[ObservableProperty]` (INotifyPropertyChanged) et `[RelayCommand]` (ICommand), éliminant le boilerplate de binding. Justification : bibliothèque officielle Microsoft (licence MIT), uniquement source-générée (aucun assembly ajouté au-delà du code généré ni dépendance transitive), surface d'attaque nulle (pas de réseau, pas de désérialisation dynamique). Vérifiée avec `dotnet list package --vulnerable` : aucune vulnérabilité connue.

## Décisions d'architecture

- **Les factures sont la source unique de vérité** : la dépense faite par catégorie, le total et le reste du budget ne sont jamais stockés — ils sont systématiquement recalculés depuis les factures (`MonthBudget.RecalculerTotaux`), y compris après un chargement JSON. La valeur de dépense fournie dans le JSON est ignorée.
- **Ids auto-incrémentés « max + 1 »** : aucun compteur n'est persisté ; le prochain id de catégorie ou de facture est dérivé des ids existants (max + 1). Les ids supprimés ne sont donc jamais réutilisés, même après rechargement.
- **Validation stricte des entrées** (classe `Validation`) : noms non vides et uniques, montants strictement positifs, heure dans `[00:00, 24:00)` si renseignée, dates bornées 1900-2100 et appartenant au mois affiché. La vérification couvre explicitement `NaN` et ±∞ (IEEE 754 : `NaN <= 0` est faux, et un montant comme `1e39` déborde silencieusement vers +∞ en `float`) ainsi que le débordement de la somme des factures d'une catégorie.
- **DTOs JSON internes séparés du modèle** : `BudjecktJson`, `MonthJson`, `CategoryJson`, `FactureJson` sont des classes internes dédiées à la sérialisation ; le modèle métier (`Budjeckt`, `MonthBudget`, `Facture`) reste indépendant du format de fichier.
- **MVVM avec CommunityToolkit** : le frontend sépare la logique UI (XAML) de la logique métier (`MainViewModel`). La vue charge le modèle `BudjecktBackend`, applique les mutations, sauvegarde et met à jour les observables ; les commandes sont liées via `[RelayCommand]` avec `CanExecute` (formulaire d'ajout désactivé tant que la saisie est invalide).
- **Écriture atomique du fichier** : sauvegarde via fichier temporaire puis `File.Move` (renommage sur le même volume), pour qu'une coupure en plein écriture ne tronque pas `depenses.json`.
- **Un fichier par classe** pour le backend, aligné sur la conception lv0/lv1 (`AnalyseProjetBudjeckt.md`).