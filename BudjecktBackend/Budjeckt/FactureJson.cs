namespace Budjeckt;

/// <summary>
/// DTO d'une facture utilisé par la sérialisation JSON.
/// </summary>
internal class FactureJson
{
    /// <summary>Identifiant unique de la facture.</summary>
    public int Id { get; set; }

    /// <summary>Identifiant de la catégorie de dépense associée.</summary>
    public int IdCategorie { get; set; }

    /// <summary>Montant de la dépense.</summary>
    public float Montant { get; set; }

    /// <summary>Date de la dépense (ISO-8601).</summary>
    public DateTime Date { get; set; }

    /// <summary>Heure de la dépense (optionnelle).</summary>
    public TimeSpan? Heure { get; set; }
}