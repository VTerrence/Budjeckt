namespace Budjeckt;

/// <summary>
/// DTO de la cagnotte utilisé par la sérialisation JSON (fichier <c>cagnotte.json</c>).
/// </summary>
internal class CagnotteJson
{
    /// <summary>Solde actuel de la cagnotte (somme des dépôts moins les retraits).</summary>
    public float Solde { get; set; }
}