namespace Budjeckt;

/// <summary>
/// DTO d'une catégorie de dépense utilisé par la sérialisation JSON (id + nom uniquement).
/// </summary>
internal class CategoryJson
{
    /// <summary>Identifiant unique de la catégorie.</summary>
    public int Id { get; set; }

    /// <summary>Nom de la catégorie.</summary>
    public string? Nom { get; set; }
}