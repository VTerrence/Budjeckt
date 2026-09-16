namespace Budjeckt;

/// <summary>
/// Dépense individuelle (facture) rattachée à une catégorie de dépense d'un mois.
/// Immuable : identifiant, catégorie, montant, date et heure (optionnelle) sont fixés à la création.
/// Une dépense ordinaire a un montant strictement positif ; un mouvement de cagnotte (voir
/// <see cref="EstMouvementCagnotte"/>) porte au contraire un montant signé, non nul.
/// </summary>
public class Facture
{
    /// <summary>Identifiant unique de la facture (auto-incrémenté au sein du mois).</summary>
    public int Id { get; }

    /// <summary>Identifiant de la catégorie de dépense associée.</summary>
    public int IdCategorie { get; }

    /// <summary>
    /// Montant de la facture : strictement positif pour une dépense ordinaire ; signé (non nul)
    /// pour un mouvement de cagnotte (positif = dépôt, négatif = retrait).
    /// </summary>
    public float Montant { get; }

    /// <summary>Date de la dépense.</summary>
    public DateTime Date { get; }

    /// <summary>Heure de la dépense (optionnelle, <c>null</c> si non renseignée).</summary>
    public TimeSpan? Heure { get; }

    /// <summary>
    /// <c>true</c> pour un mouvement de cagnotte : ligne générée par
    /// <see cref="MonthBudget.MettreDeCote"/> / <see cref="MonthBudget.RecupererDeCagnotte"/>,
    /// rattachée à la catégorie réservée « Cagnotte », au montant signé et non supprimable
    /// depuis l'interface (l'annulation se fait par l'opération inverse sur la cagnotte).
    /// </summary>
    public bool EstMouvementCagnotte { get; }

    /// <summary>
    /// <c>true</c> si la dépense matérialise une facture par défaut (récurrente) : marquée
    /// à la création d'un défaut ou par la reproduction automatique dans un mois suivant
    /// (<see cref="Budjeckt.AppliquerFacturesParDefaut"/>). Permet de distinguer la
    /// reproduction d'un modèle d'une dépense ordinaire saisie manuellement, pour l'idempotence
    /// et la désactivation du récurrent lors de la suppression.
    /// </summary>
    public bool EstParDefaut { get; }

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
    /// <param name="estParDefaut">Marque la dépense comme reproduction d'une facture par défaut (récurrente).</param>
    /// <exception cref="ArgumentException">Si le montant n'est pas strictement positif, si l'heure est invalide
    /// ou si l'année de la date est hors de l'intervalle plausible (1900-2100).</exception>
    public Facture(int id, int idCategorie, float montant, DateTime date, TimeSpan? heure, bool estParDefaut = false)
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
        EstMouvementCagnotte = false;
        EstParDefaut = estParDefaut;
    }

    /// <summary>
    /// Crée un mouvement de cagnotte (montant signé, non nul) rattaché à la catégorie réservée.
    /// Interne : seule la logique métier de la cagnotte (<see cref="MonthBudget"/>) construit
    /// de telles lignes, jamais la saisie d'une dépense ordinaire.
    /// </summary>
    /// <param name="id">Identifiant unique du mouvement.</param>
    /// <param name="idCategorie">Identifiant de la catégorie réservée « Cagnotte ».</param>
    /// <param name="montant">Montant signé, non nul et fini (positif = dépôt, négatif = retrait).</param>
    /// <param name="date">Date du mouvement.</param>
    /// <exception cref="ArgumentException">Si le montant est nul, NaN, infini ou si la date est
    /// hors de l'intervalle plausible (1900-2100).</exception>
    internal Facture(int id, int idCategorie, float montant, DateTime date, bool mouvementCagnotte)
    {
        _ = mouvementCagnotte;

        if (montant == 0f || float.IsNaN(montant) || float.IsInfinity(montant))
        {
            throw new ArgumentException("Le montant d'un mouvement de cagnotte doit être non nul et fini.", nameof(montant));
        }

        Validation.VerifierDatePlausible(date);
        Id = id;
        IdCategorie = idCategorie;
        Montant = montant;
        Date = date.Date;
        Heure = null;
        EstMouvementCagnotte = true;
    }
}