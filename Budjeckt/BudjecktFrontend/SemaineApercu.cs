using Budjeckt;

namespace BudjecktFrontend;

/// <summary>
/// Ligne affichée dans le panneau « Reste par semaine » : vue immuable d'une
/// <see cref="SemaineBudget"/> avec le libellé de la plage de dates (« Sem. 2 :
/// 08 septembre – 14 septembre ») et le reste disponible — part proportionnelle du restant du
/// mois sur les semaines restantes (poids 1 par semaine pleine, 0,35 pour la 5e), nulle pour les
/// semaines passées — prêt à afficher. La dernière semaine peut recevoir un montant ajusté au
/// centime pour que la somme affichée retrouve le restant.
/// </summary>
public sealed class SemaineApercu
{
    /// <summary>Libellé de la semaine (rang + plage de dates, ex. « Sem. 1 : 01 septembre – 07 septembre »).</summary>
    public string Libelle { get; }

    /// <summary>Reste disponible, formaté (ex. « 17,93 € » pour une part pleine).</summary>
    public string ResteTexte { get; }

    /// <summary><c>true</c> si le reste est positif ou nul (vert), sinon <c>false</c> (rouge).</summary>
    public bool RestePositif { get; }

    /// <summary>
    /// <c>true</c> si cette semaine contient la date de référence fournie au calcul
    /// (mois affiché courant système), sinon <c>false</c>.
    /// </summary>
    public bool EstSemaineCourante { get; }

    /// <summary>
    /// Construit la ligne d'affichage d'une semaine de la répartition hebdomadaire.
    /// </summary>
    /// <param name="semaine">Semaine calculée par <see cref="BudgetHebdomadaire.Calculer"/>.</param>
    /// <param name="montant">
    /// Montant à afficher pour la ligne ; par défaut <see cref="SemaineBudget.Reste"/>. La
    /// dernière semaine doit recevoir le montant ajusté au centime (restant affiché − somme des
    /// autres lignes arrondies) pour que la somme affichée redonne exactement le restant.
    /// </param>
    public SemaineApercu(SemaineBudget semaine, double? montant = null)
    {
        double valeur = montant ?? semaine.Reste;
        Libelle = $"Sem. {semaine.NumeroSemaine} : " +
            $"{semaine.Debut.ToString("dd MMMM", System.Globalization.CultureInfo.CurrentCulture)} – " +
            $"{semaine.Fin.ToString("dd MMMM", System.Globalization.CultureInfo.CurrentCulture)}";
        ResteTexte = Formatage.Montant(valeur);
        RestePositif = valeur >= 0d;
        EstSemaineCourante = semaine.EstSemaineCourante;
    }
}