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