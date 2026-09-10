namespace Budjeckt;

/// <summary>
/// Dépense individuelle (facture) rattachée à une catégorie de dépense d'un mois.
/// Immuable : identifiant, catégorie, montant et date sont fixés à la création.
/// </summary>
public class Facture
{
    /// <summary>Identifiant unique de la facture (auto-incrémenté au sein du mois).</summary>
    public int Id { get; }

    /// <summary>Identifiant de la catégorie de dépense associée.</summary>
    public int IdCategorie { get; }

    /// <summary>Montant de la dépense, strictement positif.</summary>
    public float Montant { get; }

    /// <summary>Date de la dépense.</summary>
    public DateTime Date { get; }

    /// <summary>
    /// Crée une facture après validation du montant (strictement positif).
    /// </summary>
    /// <param name="id">Identifiant unique de la facture.</param>
    /// <param name="idCategorie">Identifiant de la catégorie de dépense.</param>
    /// <param name="montant">Montant de la dépense (strictement positif).</param>
    /// <param name="date">Date de la dépense.</param>
    /// <exception cref="ArgumentException">Si le montant n'est pas strictement positif.</exception>
    public Facture(int id, int idCategorie, float montant, DateTime date)
    {
        Validation.VerifierMontantPositif(montant);
        Id = id;
        IdCategorie = idCategorie;
        Montant = montant;
        Date = date;
    }
}