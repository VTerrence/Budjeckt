namespace Budjeckt;

/// <summary>
/// DTO de l'année Budjeckt utilisé par la sérialisation JSON (fichiers <c>depenses-&lt;année&gt;.json</c>).
/// </summary>
internal class BudjecktJson
{
    /// <summary>Nom de l'année (ex. « 2026 »).</summary>
    public string? Annee { get; set; }

    /// <summary>Liste des 12 mois de l'année.</summary>
    public List<MonthJson>? Mois { get; set; }
}