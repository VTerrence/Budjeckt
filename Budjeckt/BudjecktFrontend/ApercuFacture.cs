using Budjeckt;
using System.Globalization;

namespace BudjecktFrontend;

/// <summary>
/// Ligne affichée dans l'historique des dépenses : vue immuable d'une <see cref="Facture"/>
/// enrichie du nom de sa catégorie et des textes prêts à afficher (date, heure, montant),
/// ou ligne « Total » de synthèse (voir <see cref="CreeTotal"/>).
/// </summary>
public sealed class ApercuFacture
{
    /// <summary>Facture sous-jacente ; <c>null</c> pour la ligne « Total ».</summary>
    public Facture? Source { get; }

    /// <summary>Nom de la catégorie de la dépense (ou « Total » pour la ligne de synthèse).</summary>
    public string NomCategorie { get; }

    /// <summary>Date affichée (ex. « 03 janvier 2026 ») ; vide pour la ligne « Total ».</summary>
    public string DateTexte { get; }

    /// <summary>Heure affichée (« HH:mm ») ou « — » si aucune heure ; vide pour la ligne « Total ».</summary>
    public string HeureTexte { get; }

    /// <summary>Montant affiché (ex. « 12,50 € »).</summary>
    public string MontantTexte { get; }

    /// <summary>
    /// <c>true</c> si la ligne est la ligne « Total » de la liste, insensible à la
    /// sélection et à la suppression.
    /// </summary>
    public bool EstTotal { get; }

    /// <summary>
    /// Construit la ligne d'affichage d'une facture.
    /// </summary>
    /// <param name="facture">Facture à afficher.</param>
    /// <param name="nomCategorie">Nom de la catégorie résolue depuis le mois.</param>
    public ApercuFacture(Facture facture, string nomCategorie)
        : this(nomCategorie,
               facture.Date.ToString("dd MMMM yyyy", CultureInfo.CurrentCulture),
               facture.Heure is { } heure ? heure.ToString(@"hh\:mm") : "—",
               Formatage.Montant(facture.Montant),
               estTotal: false)
    {
        Source = facture;
    }

    /// <summary>
    /// Construit la ligne « Total » affichée en dernier dans l'historique.
    /// </summary>
    /// <param name="montant">Total des factures de la liste filtrée.</param>
    public static ApercuFacture CreeTotal(float montant)
    {
        return new ApercuFacture("Total", string.Empty, string.Empty, Formatage.Montant(montant), estTotal: true);
    }

    private ApercuFacture(string nomCategorie, string dateTexte, string heureTexte, string montantTexte, bool estTotal)
    {
        NomCategorie = nomCategorie;
        DateTexte = dateTexte;
        HeureTexte = heureTexte;
        MontantTexte = montantTexte;
        EstTotal = estTotal;
        Source = null;
    }
}