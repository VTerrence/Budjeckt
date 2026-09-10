using System.Text.Json;

namespace Budjeckt;

/// <summary>
/// Année Budjeckt (lv0) : classe principale regroupant le nom de l'année et les 12 mois
/// (<see cref="MonthBudget"/>). Elle sait charger et sauvegarder ses données dans un fichier
/// JSON (ex. <c>depenses.json</c>) via des sous-méthodes privées.
/// </summary>
public class Budjeckt
{
    private static readonly string[] NomsMois =
    {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    private string _annee;
    private MonthBudget[] _mois;

    /// <summary>
    /// Construit une année Budjeckt avec le nom de l'année récupéré par le programme (date système).
    /// </summary>
    public Budjeckt()
        : this(DateTime.Now.Year.ToString())
    {
    }

    /// <summary>
    /// Construit une année Budjeckt avec le nom d'année fourni et crée les 12 mois
    /// <see cref="MonthBudget"/> (un par mois de l'année).
    /// </summary>
    /// <param name="annee">Nom de l'année (ex. « 2024 », « 2026 »).</param>
    /// <exception cref="ArgumentException">Si le nom de l'année est vide.</exception>
    public Budjeckt(string annee)
    {
        Validation.VerifierNomNonVide(annee);
        _annee = annee;
        _mois = CreerDouzeMois();
    }

    /// <summary>Nom de l'année (ex. « 2026 »).</summary>
    public string Annee => _annee;

    /// <summary>Tableau des 12 mois de l'année.</summary>
    public MonthBudget[] Months
    {
        get => _mois;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _mois = value;
        }
    }

    /// <summary>
    /// Charge les données depuis un fichier JSON. Décomposée en sous-méthodes :
    /// lecture du fichier, désérialisation, validation de la structure puis application au modèle.
    /// Si le fichier n'existe pas, les données par défaut sont conservées (premier lancement).
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    /// <exception cref="InvalidDataException">Si le fichier existe mais est corrompu ou mal formé.</exception>
    public void ChargerJson(string chemin)
    {
        string? contenu = LireFichier(chemin);
        if (contenu is null)
        {
            return;
        }

        BudjecktJson donnees = Deserialiser(contenu);
        ValiderJson(donnees);
        AppliquerModele(donnees);
    }

    /// <summary>
    /// Sauvegarde les données de l'année dans un fichier JSON. Décomposée en sous-méthodes :
    /// construction du DTO, sérialisation puis écriture du fichier.
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    public void SauvegarderJson(string chemin)
    {
        string contenu = Serialiser();
        EcrireFichier(chemin, contenu);
    }

    /// <summary>
    /// Lit le contenu du fichier ; retourne <c>null</c> si le fichier n'existe pas.
    /// </summary>
    private static string? LireFichier(string chemin)
    {
        return File.Exists(chemin) ? File.ReadAllText(chemin) : null;
    }

    /// <summary>
    /// Désérialise le contenu JSON vers le DTO de l'année.
    /// </summary>
    private static BudjecktJson Deserialiser(string contenu)
    {
        try
        {
            return JsonSerializer.Deserialize<BudjecktJson>(contenu)
                   ?? throw new JsonException("Le JSON ne contient aucune donnée.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Le fichier JSON est corrompu ou mal formé.", exception);
        }
    }

    /// <summary>
    /// Vérifie que la structure du JSON est correcte : année présente, exactement 12 mois,
    /// chaque mois nommé avec des catégories nommées, aucun élément nul, montants finis
    /// et strictement positifs, et chaque facture liée à une catégorie existante.
    /// </summary>
    private static void ValiderJson(BudjecktJson donnees)
    {
        if (string.IsNullOrWhiteSpace(donnees.Annee))
        {
            throw new InvalidDataException("Le fichier JSON ne contient pas d'année valide.");
        }

        if (donnees.Mois is null || donnees.Mois.Count != 12)
        {
            throw new InvalidDataException("Le fichier JSON doit contenir exactement 12 mois.");
        }

        foreach (MonthJson mois in donnees.Mois)
        {
            if (mois is null)
            {
                throw new InvalidDataException("Un mois du fichier JSON est nul.");
            }

            if (string.IsNullOrWhiteSpace(mois.Nom))
            {
                throw new InvalidDataException("Un mois du fichier JSON n'a pas de nom.");
            }

            if (mois.Categories is null || mois.Categories.Count == 0)
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" n'a pas de catégories.");
            }

            foreach (CategoryJson categorie in mois.Categories)
            {
                if (categorie is null || string.IsNullOrWhiteSpace(categorie.Nom))
                {
                    throw new InvalidDataException($"Le mois \"{mois.Nom}\" a une catégorie sans nom.");
                }
            }

            if (float.IsNaN(mois.Revenue) || float.IsInfinity(mois.Revenue))
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" a un revenue invalide (NaN ou infini).");
            }

            foreach (FactureJson facture in mois.Factures ?? Enumerable.Empty<FactureJson>())
            {
                if (facture is null)
                {
                    throw new InvalidDataException($"Le mois \"{mois.Nom}\" a une facture nulle.");
                }

                // NaN et infini échappent à la comparaison "<= 0" (IEEE 754) ; un montant infini
                // corromprait les totaux et empêcherait toute sauvegarde JSON ultérieure.
                if (facture.Montant <= 0f || float.IsNaN(facture.Montant) || float.IsInfinity(facture.Montant))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a un montant invalide (inférieur ou égal à 0, NaN ou infini).");
                }

                if (!mois.Categories.Any(categorie => categorie.Id == facture.IdCategorie))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" référence une catégorie inconnue.");
                }
            }
        }
    }

    /// <summary>
    /// Applique les données du DTO validé au modèle de l'année.
    /// </summary>
    private void AppliquerModele(BudjecktJson donnees)
    {
        _annee = donnees.Annee!;
        _mois = donnees.Mois!
            .Select(mois => new MonthBudget(
                mois.Nom!,
                mois.Revenue,
                mois.Categories!.Select(categorie => new Tuple<int, string, float>(categorie.Id, categorie.Nom!, 0f)).ToArray(),
                (mois.Factures ?? Enumerable.Empty<FactureJson>())
                    .Select(facture => new Facture(facture.Id, facture.IdCategorie, facture.Montant, facture.Date))))
            .ToArray();
    }

    /// <summary>
    /// Sérialise le modèle de l'année sous forme de chaîne JSON.
    /// </summary>
    private string Serialiser()
    {
        return JsonSerializer.Serialize(ConstruireJson());
    }

    /// <summary>
    /// Construit le DTO de l'année à partir du modèle.
    /// </summary>
    private BudjecktJson ConstruireJson()
    {
        return new BudjecktJson
        {
            Annee = _annee,
            Mois = _mois
                .Select(mois => new MonthJson
                {
                    Nom = mois.Nom,
                    Revenue = mois.Revenue,
                    Categories = mois.ExpenseCategories
                        .Select(categorie => new CategoryJson { Id = categorie.Item1, Nom = categorie.Item2 })
                        .ToList(),
                    Factures = mois.Factures
                        .Select(facture => new FactureJson
                        {
                            Id = facture.Id,
                            IdCategorie = facture.IdCategorie,
                            Montant = facture.Montant,
                            Date = facture.Date
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    /// <summary>
    /// Écrit le contenu JSON dans le fichier, en créant le répertoire parent si nécessaire.
    /// </summary>
    private static void EcrireFichier(string chemin, string contenu)
    {
        string? repertoire = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrEmpty(repertoire))
        {
            Directory.CreateDirectory(repertoire);
        }

        File.WriteAllText(chemin, contenu);
    }

    /// <summary>
    /// Crée les 12 <see cref="MonthBudget"/> de l'année à partir des noms de mois.
    /// </summary>
    private static MonthBudget[] CreerDouzeMois()
    {
        return NomsMois.Select(nom => new MonthBudget(nom)).ToArray();
    }
}