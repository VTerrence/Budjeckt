namespace Budjeckt;

/// <summary>
/// Facture par défaut (récurrente) d'une année : modèle transversal aux mois qui, lorsqu'il est
/// actif, est reproduit automatiquement dans chaque mois (de l'année) dont le rang est supérieur
/// ou égal à son mois de création et qui possède la catégorie associée. Il n'est jamais appliqué
/// rétroactivement aux mois antérieurs à sa création. Plusieurs défauts peuvent viser la même
/// catégorie (des montants différents), mais un couple (catégorie, montant) identique reste unique :
/// ré-ajouter le même défaut le réactive au lieu de le dupliquer.
/// </summary>
public class FactureParDefaut
{
    /// <summary>Nom de la catégorie ciblée (résolu par nom dans chaque mois, les ids différant d'un mois à l'autre).</summary>
    public string NomCategorie { get; }

    /// <summary>Montant de la facture reproduite (strictement positif).</summary>
    public float Montant { get; }

    /// <summary>
    /// Rang (1 à 12) du mois de création du défaut : seuls les mois de rang supérieur ou égal
    /// le reproduisent (pas de rétroactivité sur les mois antérieurs).
    /// </summary>
    public int MoisCreation { get; }

    /// <summary>État du défaut : <c>true</c> tant qu'il est reproduit dans les mois suivants.</summary>
    public bool EstActive { get; private set; }

    /// <summary>
    /// Crée un défaut récurrent après validation du nom de catégorie, du montant (strictement
    /// positif) et du mois de création (1 à 12).
    /// </summary>
    /// <param name="nomCategorie">Nom de la catégorie ciblée.</param>
    /// <param name="montant">Montant de la facture à reproduire.</param>
    /// <param name="moisCreation">Rang (1 à 12) du mois où le défaut a été posé.</param>
    /// <param name="estActive">État initial du défaut ; <c>true</c> par défaut.</param>
    /// <exception cref="ArgumentException">Si le nom est vide, si le montant n'est pas strictement
    /// positif ou si le mois de création est hors de la plage [1, 12].</exception>
    public FactureParDefaut(string nomCategorie, float montant, int moisCreation, bool estActive = true)
    {
        Validation.VerifierNomNonVide(nomCategorie);
        Validation.VerifierMontantPositif(montant);

        if (moisCreation < 1 || moisCreation > 12)
        {
            throw new ArgumentException("Le mois de création d'un défaut doit être compris entre 1 et 12.", nameof(moisCreation));
        }

        NomCategorie = nomCategorie.Trim();
        Montant = montant;
        MoisCreation = moisCreation;
        EstActive = estActive;
    }

    /// <summary>
    /// Désactive le défaut : les mois non encore ouverts cessent de le reproduire, sans toucher
    /// aux factures déjà matérialisées (y compris dans le mois courant).
    /// </summary>
    internal void Desactiver()
    {
        EstActive = false;
    }

    /// <summary>
    /// Réactive le défaut (récréation du même couple catégorie/montant après désactivation) :
    /// la reproduction reprend dans les mois suivants.
    /// </summary>
    internal void Reactiver()
    {
        EstActive = true;
    }
}