namespace Budjeckt;

/// <summary>
/// DTO d'un mois utilisé par la sérialisation JSON. La dépense par catégorie n'est pas stockée :
/// elle est recalculée depuis les factures (source unique de vérité).
/// </summary>
internal class MonthJson
{
    /// <summary>Nom du mois.</summary>
    public string? Nom { get; set; }

    /// <summary>Budget du mois.</summary>
    public float Revenue { get; set; }

    /// <summary>
    /// Montant net que ce mois a mis en cagnotte : positif = mis de côté, négatif = réinjecté
    /// depuis la cagnotte. Alternative 0 pour les fichiers antérieurs à la cagnotte.
    /// </summary>
    public float MontantCagnotte { get; set; }

    /// <summary>Catégories de dépense du mois (id + nom).</summary>
    public List<CategoryJson>? Categories { get; set; }

    /// <summary>Factures du mois.</summary>
    public List<FactureJson>? Factures { get; set; }
}