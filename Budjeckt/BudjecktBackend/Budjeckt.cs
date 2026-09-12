using System.Text.Json;

namespace Budjeckt;

/// <summary>
/// Année Budjeckt (lv0) : classe principale regroupant le nom de l'année et les 12 mois
/// (<see cref="MonthBudget"/>). Elle sait charger et sauvegarder ses données dans un fichier
/// JSON (ex. <c>depenses-2026.json</c>) via des sous-méthodes privées.
/// </summary>
public class Budjeckt
{
    private string _annee;
    private MonthBudget[] _mois;

    /// <summary>Nom du fichier hérité, sans année (« depenses.json »).</summary>
    public const string NomFichierHerite = "depenses.json";

    /// <summary>
    /// Noms des 12 mois de l'année dans l'ordre de navigation (source unique, utilisée
    /// pour créer les mois et par l'interface pour ne pas dupliquer la liste).
    /// </summary>
    public static IReadOnlyList<string> NomsDesMois { get; } = new[]
    {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    /// <summary>
    /// Nom du fichier JSON d'une année donnée (ex. « depenses-2026.json »).
    /// </summary>
    /// <param name="annee">Année (ex. 2026).</param>
    public static string NomFichierPourAnnee(int annee) => $"depenses-{annee}.json";

    /// <summary>
    /// Extrait l'année déclarée d'un fichier JSON sans valider les mois (utilisé pour la
    /// migration du fichier hérité). Retourne null si le fichier est absent, trop volumineux,
    /// illisible, si l'année n'est pas un nombre entier ou si elle est hors de la plage
    /// plausible (1900-2100).
    /// </summary>
    /// <param name="chemin">Chemin du fichier JSON.</param>
    internal static int? LireAnneeDuFichier(string chemin)
    {
        if (!File.Exists(chemin))
        {
            return null;
        }

        if (new FileInfo(chemin).Length > TailleMaximaleFichier)
        {
            return null;
        }

        try
        {
            string contenu = File.ReadAllText(chemin);
            BudjecktJson donnees = Deserialiser(contenu);
            int? annee = int.TryParse(donnees?.Annee?.Trim(), out int valeur) ? valeur : null;
            return annee is >= 1900 and <= 2200 ? annee : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Construit une année Budjeckt avec le nom de l'année récupéré par le programme (date système).
    /// </summary>
    public Budjeckt()
        : this(DateTime.Now.Year.ToString())
    {
    }

    /// <summary>
    /// Construit une année Budjeckt avec le nom d'année fourni et crée les 12 mois
    /// <see cref="MonthBudget"/> (un par mois de l'année) avec les catégories par défaut.
    /// </summary>
    /// <param name="annee">Nom de l'année (ex. « 2024 », « 2026 »).</param>
    /// <exception cref="ArgumentException">Si le nom de l'année est vide.</exception>
    public Budjeckt(string annee)
    {
        Validation.VerifierNomNonVide(annee);
        _annee = annee;
        int anneeEntiere = int.Parse(annee, System.Globalization.CultureInfo.InvariantCulture);
        _mois = CreerDouzeMois(anneeEntiere);
    }

    /// <summary>Nom de l'année (ex. « 2026 »).</summary>
    public string Annee => _annee;

    /// <summary>
    /// Tableau des mois de l'année. Le setter remplace intégralement le tableau
    /// (utilisé notamment pour injecter les mois chargés depuis le JSON).
    /// </summary>
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
    /// <exception cref="InvalidDataException">Si le fichier existe mais est corrompu, mal formé,
    /// contient une année non numérique ou hors de la plage plausible (1900-2100), ou une
    /// facture datée d'une année ne correspondant pas à l'année déclarée dans le fichier.</exception>
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
    /// <exception cref="InvalidDataException">Si l'état mémoire ne satisfait pas les mêmes
    /// invariants que le chargement (exactement 12 mois, au moins une catégorie par mois,
    /// factures rattachées à des catégories existantes). Rien n'est écrit dans ce cas.</exception>
    public void SauvegarderJson(string chemin)
    {
        // Le même contrôle structurel qu'au chargement (ValiderJson) est appliqué avant
        // l'écriture : un état mémoire invalide (ex. un mois sans catégorie via le setter
        // Months) ne doit jamais produire un fichier que l'application rejetterait elle-même
        // au prochain lancement — quarantaine et perte de l'année à la clé.
        BudjecktJson donnees = ConstruireJson();
        ValiderJson(donnees);

        string contenu = JsonSerializer.Serialize(donnees);
        EcrireFichier(chemin, contenu);
    }

    /// <summary>
    /// Lit le contenu du fichier ; retourne <c>null</c> si le fichier n'existe pas.
    /// </summary>
    private static string? LireFichier(string chemin)
    {
        if (!File.Exists(chemin))
        {
            return null;
        }

        // Borner la taille lue éviter qu'un fichier corrompu ou gonflé ne provoque un
        // OutOfMemoryException non gérée lors du chargement.
        if (new FileInfo(chemin).Length > TailleMaximaleFichier)
        {
            throw new InvalidDataException($"Le fichier JSON est trop volumineux (limite {TailleMaximaleFichier / (1024 * 1024)} Mo).");
        }

        return File.ReadAllText(chemin);
    }

    private const long TailleMaximaleFichier = 10 * 1024 * 1024;

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
    /// Vérifie que la structure du JSON est correcte : année présente et numérique, exactement 12 mois
    /// dont le nom est connu, chaque mois avec des catégories nommées à ids uniques, aucun élément nul,
    /// montants finis et strictement positifs, heure optionnelle dans [00:00, 24:00), date plausible
    /// appartenant au mois affiché et à l'année déclarée, et chaque facture liée à une catégorie
    /// existante à ids uniques.
    /// </summary>
    private static void ValiderJson(BudjecktJson donnees)
    {
        if (string.IsNullOrWhiteSpace(donnees.Annee))
        {
            throw new InvalidDataException("Le fichier JSON ne contient pas d'année valide.");
        }

        if (!int.TryParse(donnees.Annee.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int annee))
        {
            throw new InvalidDataException($"L'année du fichier JSON n'est pas un nombre valide : \"{donnees.Annee}\".");
        }

        if (annee < 1900 || annee > 2100)
        {
            throw new InvalidDataException($"L'année {annee} du fichier JSON est hors de la plage plausible (1900-2100).");
        }

        if (donnees.Mois is null || donnees.Mois.Count != 12)
        {
            throw new InvalidDataException("Le fichier JSON doit contenir exactement 12 mois.");
        }

        HashSet<string> nomsMoisVus = new(StringComparer.OrdinalIgnoreCase);
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

            // Deux mois portant le même nom (ex. deux « Janvier ») casseraient l'affichage et le tri.
            if (!nomsMoisVus.Add(mois.Nom))
            {
                throw new InvalidDataException($"Le nom de mois \"{mois.Nom}\" est présent plusieurs fois dans le fichier JSON.");
            }

            int? indexMois = Validation.IndexDuMois(mois.Nom);
            if (indexMois is null)
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" du fichier JSON est inconnu.");
            }

            if (mois.Categories is null || mois.Categories.Count == 0)
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" n'a pas de catégories.");
            }

            HashSet<int> idsCategorie = new();
            foreach (CategoryJson categorie in mois.Categories)
            {
                if (categorie is null || string.IsNullOrWhiteSpace(categorie.Nom))
                {
                    throw new InvalidDataException($"Le mois \"{mois.Nom}\" a une catégorie sans nom.");
                }

                // Des ids négatifs, nuls ou dupliqués casseraient l'auto-incrément
                // (max + 1) et la suppression par id (First) au sein du mois.
                if (categorie.Id < 1 || !idsCategorie.Add(categorie.Id))
                {
                    throw new InvalidDataException(
                        $"Le mois \"{mois.Nom}\" a des catégories avec des ids négatifs, nuls ou dupliqués.");
                }
            }

            // Un revenue infini (ex. « 1e39 » désérialisé en float) corromprait le reste du budget
            // et bloquerait toute sauvegarde JSON ultérieure.
            if (float.IsNaN(mois.Revenue) || float.IsInfinity(mois.Revenue))
            {
                throw new InvalidDataException($"Le mois \"{mois.Nom}\" a un revenue invalide (NaN ou infini).");
            }

            HashSet<int> idsFacture = new();
            foreach (FactureJson facture in mois.Factures ?? Enumerable.Empty<FactureJson>())
            {
                if (facture is null)
                {
                    throw new InvalidDataException($"Le mois \"{mois.Nom}\" a une facture nulle.");
                }

                // Comme pour les catégories : un id négatif/nul ou dupliqué casserait
                // l'auto-incrément et la suppression par id au sein du mois.
                if (facture.Id < 1 || !idsFacture.Add(facture.Id))
                {
                    throw new InvalidDataException(
                        $"Le mois \"{mois.Nom}\" a des factures avec des ids négatifs, nuls ou dupliqués.");
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

                // L'heure est optionnelle mais doit rester dans [00:00, 24:00) si renseignée.
                if (!Validation.HeureEstValide(facture.Heure))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une heure invalide.");
                }

                // La date doit rester plausible (armure contre les dates 9999) et tomber sur le mois affiché.
                if (!Validation.DateEstPlausible(facture.Date))
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une date hors de l'intervalle plausible (1900-2100).");
                }

                if (facture.Date.Month != indexMois.Value)
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une date qui n'appartient pas à ce mois.");
                }

                if (facture.Date.Year != annee)
                {
                    throw new InvalidDataException(
                        $"La facture d'id {facture.Id} du mois \"{mois.Nom}\" a une date dont l'année {facture.Date.Year} ne correspond pas à l'année déclarée {annee}.");
                }
            }
        }
    }

    /// <summary>
    /// Applique les données du DTO validé au modèle de l'année. L'année est parsée depuis le DTO
    /// et passée à chaque mois. Les deux champs sont assignés après construction complète des mois :
    /// un échec (ex. débordement float) laisse alors l'objet dans son état précédent.
    /// </summary>
    private void AppliquerModele(BudjecktJson donnees)
    {
        int annee = int.Parse(donnees.Annee!, System.Globalization.CultureInfo.InvariantCulture);

        MonthBudget[] nouveauxMois = donnees.Mois!
            .Select(mois => new MonthBudget(
                mois.Nom!,
                mois.Revenue,
                mois.Categories!.Select(categorie => new Tuple<int, string, float>(categorie.Id, categorie.Nom!, 0f)).ToArray(),
                (mois.Factures ?? Enumerable.Empty<FactureJson>())
                    .Select(facture => new Facture(facture.Id, facture.IdCategorie, facture.Montant, facture.Date.Date, facture.Heure)),
                annee))
            .ToArray();

        _annee = donnees.Annee!;
        _mois = nouveauxMois;
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
                            Date = facture.Date,
                            Heure = facture.Heure
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    /// <summary>
    /// Écrit le contenu JSON dans le fichier en créant le répertoire parent si nécessaire.
    /// L'écriture est atomique : le contenu est d'abord écrit dans un fichier temporaire puis
    /// déplacé par-dessus la cible, afin qu'une coupure en plein écriture ne tronque pas le fichier.
    /// </summary>
    private static void EcrireFichier(string chemin, string contenu)
    {
        string? repertoire = Path.GetDirectoryName(chemin);
        if (!string.IsNullOrEmpty(repertoire))
        {
            Directory.CreateDirectory(repertoire);
        }

        string cheminTemporaire = chemin + ".tmp";
        File.WriteAllText(cheminTemporaire, contenu);
        File.Move(cheminTemporaire, chemin, true);
    }

    /// <summary>
    /// Crée les 12 <see cref="MonthBudget"/> de l'année avec les catégories par défaut.
    /// </summary>
    private static MonthBudget[] CreerDouzeMois(int annee)
    {
        return NomsDesMois
            .Select(nom => new MonthBudget(nom, 0f, MonthBudget.CreerCategoriesParDefaut(), Enumerable.Empty<Facture>(), annee))
            .ToArray();
    }
}