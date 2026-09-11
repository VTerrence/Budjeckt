using Budjeckt;
using System.Globalization;

namespace BudjecktFrontend;

/// <summary>
/// Ligne affichée dans l'historique des dépenses : vue immuable d'une <see cref="Facture"/>
/// enrichie du nom de sa catégorie et des textes prêts à afficher (date, heure, montant).
/// </summary>
public sealed class ApercuFacture
{
    /// <summary>Facture sous-jacente.</summary>
    public Facture Source { get; }

    /// <summary>Nom de la catégorie de la dépense.</summary>
    public string NomCategorie { get; }

    /// <summary>Date affichée (ex. « 03 janvier 2026 »).</summary>
    public string DateTexte { get; }

    /// <summary>Heure affichée (« HH:mm ») ou « — » si aucune heure.</summary>
    public string HeureTexte { get; }

    /// <summary>Montant affiché (ex. « 12,50 € »).</summary>
    public string MontantTexte { get; }

    /// <summary>
    /// Construit la ligne d'affichage d'une facture.
    /// </summary>
    /// <param name="facture">Facture à afficher.</param>
    /// <param name="nomCategorie">Nom de la catégorie résolue depuis le mois.</param>
    public ApercuFacture(Facture facture, string nomCategorie)
    {
        Source = facture;
        NomCategorie = nomCategorie;
        DateTexte = facture.Date.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture);
        HeureTexte = facture.Heure is { } heure ? heure.ToString(@"hh\:mm") : "—";
        MontantTexte = Formatage.Montant(facture.Montant);
    }
}