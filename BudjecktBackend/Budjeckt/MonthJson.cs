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

    /// <summary>Catégories de dépense du mois (id + nom).</summary>
    public List<CategoryJson>? Categories { get; set; }

    /// <summary>Factures du mois.</summary>
    public List<FactureJson>? Factures { get; set; }
}