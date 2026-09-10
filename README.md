# Budjeckt

Application de bureau Windows (WPF, .NET 10) de suivi de dépenses quotidiennes, **100 % locale** : aucune base de données lourde, aucune connexion internet. Les données sont persistées dans un fichier texte JSON (ex. `depenses.json`).

## Objectif

Permettre à l'utilisateur de consigner et suivre ses dépenses quotidiennes :

- **Ajout d'une dépense** (facture) via un formulaire : montant, catégorie, date (par défaut la date du jour).
- **Affichage de l'historique** des dépenses enregistrées.
- **Indicateur financier** : total des dépenses et reste du budget, recalculés dynamiquement à chaque ajout/suppression.
- **Suppression** définitive d'une dépense.
- **Filtrage** par catégorie de dépense.
- **Persistance** automatique dans le fichier JSON au lancement et à la fermeture.

Référence détaillée : `AnalyseProjetBudjeckt.md` (cahier des charges + conception).

## Structure du projet

La solution `Budjeckt.slnx` et les trois projets sont sous `BudjecktBackend/` :

```
BudjecktBackend/
├── Budjeckt.slnx                    # solution (format XML .slnx)
├── Budjeckt/                        # bibliothèque de classes net10.0 (modèle métier)
│   ├── Budjeckt.cs                  #   année (lv0) : charge/sauvegarde du JSON
│   ├── MonthBudget.cs               #   mois (lv1) : catégories, budget, factures, totaux
│   ├── Facture.cs                   #   dépense individuelle (immuable)
│   ├── Validation.cs                #   règles métier (noms, montants, ids)
│   └── *Json.cs                     #   DTOs internes de sérialisation JSON
├── BudjecktTest/                    # tests MSTest net10.0 (74 tests, parallélisés)
└── BudjecktFrontend/                # application WPF net10.0-windows
```

## Prérequis

- **Windows** : le frontend WPF cible `net10.0-windows` et ne se compile/lance que sur Windows.
- **.NET SDK 10** (version `10.0.302` installée sur cette machine).

## Compilation et lancement

Commandes à exécuter depuis la racine du repo (`C:\Users\Terrence\Desktop\Budjeckt`).

**Compilation de la solution :**

```
dotnet build BudjecktBackend/Budjeckt.slnx
```

**Exécution des tests (74 tests, MSTest) :**

```
dotnet test BudjecktBackend/Budjeckt.slnx
dotnet test BudjecktBackend/Budjeckt.slnx --filter "FullyQualifiedName~Budjeckt.Tests.ValidationTests.VerifierMontantPositif_MontantNaN_LèveArgumentException"
```

**Lancement de l'application WPF :**

```
dotnet run --project BudjecktBackend/BudjecktFrontend/BudjecktFrontend.csproj
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
        { "Id": 1, "IdCategorie": 1, "Montant": 750, "Date": "2026-01-03T00:00:00" }
      ]
    }
  ]
}
```

Notes sur le format :

- Le fichier doit contenir **exactement 12 mois**, chacun avec un nom et au moins une catégorie.
- La **dépense faite par catégorie n'est pas stockée** : elle est recalculée depuis les factures au chargement (voir « Décisions d'architecture »).
- Les montants et le revenue doivent être des nombres finis strictement positifs (NaN et ±∞ sont rejetés au chargement) ; chaque facture doit référencer une catégorie existante.
- Si `depenses.json` n'existe pas au lancement, l'application démarre avec les valeurs par défaut (12 mois, 7 catégories par défaut, budget 0).

## Décisions d'architecture

- **Les factures sont la source unique de vérité** : la dépense faite par catégorie, le total et le reste du budget ne sont jamais stockés — ils sont systématiquement recalculés depuis les factures (`MonthBudget.RecalculerTotaux`), y compris après un chargement JSON. La valeur de dépense fournie dans le JSON est ignorée.
- **Ids auto-incrémentés « max + 1 »** : aucun compteur n'est persisté ; le prochain id de catégorie ou de facture est dérivé des ids existants (max + 1). Les ids supprimés ne sont donc jamais réutilisés, même après rechargement.
- **Validation stricte des entrées** (classe `Validation`) : noms non vides et uniques, montants strictement positifs. La vérification couvre explicitement `NaN` et ±∞ (IEEE 754 : `NaN <= 0` est faux, et un montant comme `1e39` déborde silencieusement vers +∞ en `float`).
- **DTOs JSON internes séparés du modèle** : `BudjecktJson`, `MonthJson`, `CategoryJson`, `FactureJson` sont des classes internes dédiées à la sérialisation ; le modèle métier (`Budjeckt`, `MonthBudget`, `Facture`) reste indépendant du format de fichier.
- **Un fichier par classe** pour le backend, aligné sur la conception lv0/lv1 (`AnalyseProjetBudjeckt.md`).