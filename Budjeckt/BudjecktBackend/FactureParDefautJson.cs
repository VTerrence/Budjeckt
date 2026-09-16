namespace Budjeckt;

/// <summary>
/// DTO d'une facture par défaut utilisé par la sérialisation JSON de l'année.
/// </summary>
internal class FactureParDefautJson
{
    /// <summary>Nom de la catégorie ciblée.</summary>
    public string? Categorie { get; set; }

    /// <summary>Montant de la facture reproduite.</summary>
    public float Montant { get; set; }

    /// <summary>Rang (1 à 12) du mois de création du défaut (0 si absent des fichiers antérieurs).</summary>
    public int MoisCreation { get; set; }

    /// <summary>État du défaut (<c>true</c> tant qu'il est reproduit).</summary>
    public bool Active { get; set; }
}