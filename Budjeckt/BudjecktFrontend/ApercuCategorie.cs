namespace BudjecktFrontend;

/// <summary>
/// Ligne du breakdown « Dépenses par catégorie » : nom d'une catégorie et montant
/// cumulé de ses factures, formaté pour l'affichage (comme <see cref="ApercuFacture"/>).
/// </summary>
public sealed class ApercuCategorie
{
    /// <summary>Nom de la catégorie.</summary>
    public string Nom { get; }

    /// <summary>Montant cumulé des factures de la catégorie (ex. « 1 250,00 € »).</summary>
    public string MontantTexte { get; }

    /// <summary>
    /// Construit la ligne d'affichage d'une catégorie.
    /// </summary>
    /// <param name="nom">Nom de la catégorie.</param>
    /// <param name="montant">Montant dépensé dans la catégorie.</param>
    public ApercuCategorie(string nom, float montant)
    {
        Nom = nom;
        MontantTexte = Formatage.Montant(montant);
    }
}