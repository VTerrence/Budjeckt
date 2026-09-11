namespace Budjeckt;

/// <summary>
/// Classe outils (lv1) : vérifie que les conditions métier sont respectées avant tout traitement.
/// Chaque méthode lève une <see cref="ArgumentException"/> avec un message clair en français
/// lorsque la condition n'est pas satisfaite.
/// </summary>
public static class Validation
{
    /// <summary>
    /// Vérifie que le nom n'est ni nul, ni vide, ni composé uniquement d'espaces.
    /// </summary>
    /// <param name="nom">Nom à vérifier.</param>
    /// <exception cref="ArgumentException">Si le nom est vide.</exception>
    public static void VerifierNomNonVide(string? nom)
    {
        if (string.IsNullOrWhiteSpace(nom))
        {
            throw new ArgumentException("Le nom ne peut pas être vide.", nameof(nom));
        }
    }

    /// <summary>
    /// Vérifie que le nom n'existe pas déjà parmi les noms fournis (comparaison insensible à la casse).
    /// </summary>
    /// <param name="nom">Nom à analyser.</param>
    /// <param name="nomsExistants">Noms déjà enregistrés.</param>
    /// <exception cref="ArgumentException">Si le nom existe déjà.</exception>
    public static void VerifierNomUnique(string nom, IEnumerable<string> nomsExistants)
    {
        if (nomsExistants.Any(existant => string.Equals(existant, nom, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Le nom \"{nom}\" existe déjà.", nameof(nom));
        }
    }

    /// <summary>
    /// Vérifie que le montant est strictement positif, fini et n'est pas NaN.
    /// </summary>
    /// <param name="montant">Montant à vérifier.</param>
    /// <exception cref="ArgumentException">Si le montant est inférieur ou égal à 0, NaN ou infini.</exception>
    public static void VerifierMontantPositif(float montant)
    {
        // Note : NaN <= 0 est false en IEEE 754, d'où la vérification explicite de float.IsNaN.
        // Un montant infini (ex. "1e39" désérialisé en float déborde silencieusement vers +infini,
        // ou "Infinity" saisi par l'utilisateur) corromprait les totaux et bloquerait la sauvegarde JSON.
        if (montant <= 0f || float.IsNaN(montant) || float.IsInfinity(montant))
        {
            throw new ArgumentException("Le montant doit être strictement positif et fini.", nameof(montant));
        }
    }

    /// <summary>
    /// Vérifie que le revenue est un nombre fini (ni NaN, ni infini).
    /// Un revenue NaN ou infini corromprait le reste du budget et bloquerait la sauvegarde JSON.
    /// </summary>
    /// <param name="revenue">Revenue à vérifier.</param>
    /// <exception cref="ArgumentException">Si le revenue est NaN ou infini.</exception>
    public static void VerifierRevenueFini(float revenue)
    {
        if (float.IsNaN(revenue) || float.IsInfinity(revenue))
        {
            throw new ArgumentException("Le revenue doit être un nombre fini.", nameof(revenue));
        }
    }

    /// <summary>
    /// Vérifie que l'heure est soit absente (<c>null</c>), soit comprise dans l'intervalle [00:00, 24:00).
    /// </summary>
    /// <param name="heure">Heure à vérifier.</param>
    /// <exception cref="ArgumentException">Si l'heure est négative ou supérieure ou égale à 24 h.</exception>
    public static void VerifierHeureValide(TimeSpan? heure)
    {
        if (!HeureEstValide(heure))
        {
            throw new ArgumentException("L'heure doit être comprise entre 00:00 et 23:59:59.", nameof(heure));
        }
    }

    /// <summary>
    /// Indique si l'heure (éventuellement absente) est valide : <c>null</c> ou dans [00:00, 24:00).
    /// Fait office de source unique des bornes, réutilisée par <c>Budjeckt.ValiderJson</c>.
    /// </summary>
    /// <param name="heure">Heure à vérifier.</param>
    /// <returns><c>true</c> si valide (ou absente), sinon <c>false</c>.</returns>
    internal static bool HeureEstValide(TimeSpan? heure)
    {
        return heure is null || (heure >= TimeSpan.Zero && heure < TimeSpan.FromDays(1));
    }

    /// <summary>
    /// Vérifie que la date est plausible (année entre 1900 et 2100 inclus).
    /// Les bornes bornent les erreurs de saisie grossières (ex. une date 9999) tout en
    /// couvrant largement les budgets réels.
    /// </summary>
    /// <param name="date">Date à vérifier.</param>
    /// <exception cref="ArgumentException">Si l'année de la date est hors de l'intervalle.</exception>
    public static void VerifierDatePlausible(DateTime date)
    {
        if (!DateEstPlausible(date))
        {
            throw new ArgumentException("La date doit avoir une année comprise entre 1900 et 2100.", nameof(date));
        }
    }

    /// <summary>
    /// Indique si la date est plausible (année entre 1900 et 2100 inclus).
    /// Fait office de source unique des bornes, réutilisée par <c>Budjeckt.ValiderJson</c>.
    /// </summary>
    /// <param name="date">Date à vérifier.</param>
    /// <returns><c>true</c> si plausible, sinon <c>false</c>.</returns>
    internal static bool DateEstPlausible(DateTime date)
    {
        return date.Year is >= 1900 and <= 2100;
    }

    /// <summary>
    /// Vérifie que la date appartient au mois et à l'année dont le nom de mois (ex. « Janvier »)
    /// et l'année (ex. 2026) sont fournis.
    /// </summary>
    /// <param name="date">Date à vérifier.</param>
    /// <param name="nomMois">Nom du mois d'appartenance attendu.</param>
    /// <param name="annee">Année d'appartenance attendue (ex. 2026).</param>
    /// <exception cref="ArgumentException">Si le nom du mois est inconnu ou si la date n'appartient
    /// pas au mois ou à l'année.</exception>
    public static void VerifierDateDansLeMois(DateTime date, string nomMois, int annee)
    {
        int? index = IndexDuMois(nomMois);
        if (index is null)
        {
            throw new ArgumentException($"Le mois \"{nomMois}\" est inconnu.", nameof(nomMois));
        }

        if (date.Month != index.Value || date.Year != annee)
        {
            throw new ArgumentException($"La date doit appartenir au mois de {nomMois} {annee}.", nameof(date));
        }
    }

    /// <summary>
    /// Retourne l'index (1 à 12) du mois français correspondant au nom, ou <c>null</c> si le nom est inconnu.
    /// Utile pour contrôler qu'une date tombe bien sur le mois affiché.
    /// </summary>
    /// <param name="nom">Nom du mois (ex. « Janvier », « Février »).</param>
    /// <returns>Index du mois (1 = Janvier) ou <c>null</c> si inconnu.</returns>
    internal static int? IndexDuMois(string? nom)
    {
        if (nom is null)
        {
            return null;
        }

        return NomsMois.TryGetValue(nom.Trim(), out int index) ? index : null;
    }

    private static readonly Dictionary<string, int> NomsMois = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Janvier"] = 1, ["Février"] = 2, ["Mars"] = 3, ["Avril"] = 4, ["Mai"] = 5, ["Juin"] = 6,
        ["Juillet"] = 7, ["Août"] = 8, ["Septembre"] = 9, ["Octobre"] = 10, ["Novembre"] = 11, ["Décembre"] = 12
    };

    /// <summary>
    /// Vérifie que l'identifiant d'une catégorie existe.
    /// </summary>
    /// <param name="idCategorie">Identifiant de catégorie à vérifier.</param>
    /// <param name="idsCategories">Identifiants de catégories existants.</param>
    /// <exception cref="ArgumentException">Si l'identifiant ne correspond à aucune catégorie.</exception>
    public static void VerifierCategorieExiste(int idCategorie, IEnumerable<int> idsCategories)
    {
        if (!idsCategories.Contains(idCategorie))
        {
            throw new ArgumentException($"La catégorie d'id {idCategorie} n'existe pas.", nameof(idCategorie));
        }
    }

    /// <summary>
    /// Vérifie que l'identifiant d'une facture existe.
    /// </summary>
    /// <param name="idFacture">Identifiant de facture à vérifier.</param>
    /// <param name="idsFactures">Identifiants de factures existants.</param>
    /// <exception cref="ArgumentException">Si l'identifiant ne correspond à aucune facture.</exception>
    public static void VerifierFactureExiste(int idFacture, IEnumerable<int> idsFactures)
    {
        if (!idsFactures.Contains(idFacture))
        {
            throw new ArgumentException($"La facture d'id {idFacture} n'existe pas.", nameof(idFacture));
        }
    }
}