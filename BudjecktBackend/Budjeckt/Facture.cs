namespace Budjeckt;

/// <summary>
/// Dépense individuelle (facture) rattachée à une catégorie de dépense d'un mois.
/// Immuable : identifiant, catégorie, montant, date et heure (optionnelle) sont fixés à la création.
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

    /// <summary>Heure de la dépense (optionnelle, <c>null</c> si non renseignée).</summary>
    public TimeSpan? Heure { get; }

    /// <summary>
    /// Crée une facture après validation du montant (strictement positif), de la date
    /// (année 1900-2100) et de l'heure (optionnelle).
    /// </summary>
    /// <param name="id">Identifiant unique de la facture.</param>
    /// <param name="idCategorie">Identifiant de la catégorie de dépense.</param>
    /// <param name="montant">Montant de la dépense (strictement positif).</param>
    /// <param name="date">Date de la dépense.</param>
    /// <exception cref="ArgumentException">Si le montant n'est pas strictement positif ou si l'année
    /// de la date est hors de l'intervalle plausible (1900-2100).</exception>
    public Facture(int id, int idCategorie, float montant, DateTime date)
        : this(id, idCategorie, montant, date, null)
    {
    }

    /// <summary>
    /// Crée une facture avec date et heure, après validation du montant (strictement positif),
    /// de la date (année 1900-2100) et de l'heure (optionnelle).
    /// </summary>
    /// <param name="id">Identifiant unique de la facture.</param>
    /// <param name="idCategorie">Identifiant de la catégorie de dépense.</param>
    /// <param name="montant">Montant de la dépense (strictement positif).</param>
    /// <param name="date">Date de la dépense.</param>
    /// <param name="heure">Heure de la dépense (optionnelle).</param>
    /// <exception cref="ArgumentException">Si le montant n'est pas strictement positif, si l'heure est invalide
    /// ou si l'année de la date est hors de l'intervalle plausible (1900-2100).</exception>
    public Facture(int id, int idCategorie, float montant, DateTime date, TimeSpan? heure)
    {
        Validation.VerifierMontantPositif(montant);
        Validation.VerifierDatePlausible(date);
        Validation.VerifierHeureValide(heure);
        Id = id;
        IdCategorie = idCategorie;
        Montant = montant;
        // L'heure vit déjà dans sa propre propriété : la date ne doit jamais en porter,
        // ce qui unifie la normalisation entre saisie, chargement et construction directe.
        Date = date.Date;
        Heure = heure;
    }
}