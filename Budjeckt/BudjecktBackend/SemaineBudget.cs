namespace Budjeckt;

/// <summary>
/// Semaine d'un mois, comptée à partir du 1er (7 jours par semaine, dernière partielle) :
/// elle porte le numéro de semaine dans l'ordre chronologique, ses bornes, le drapeau
/// « semaine courante » et le reste disponible — part proportionnelle du restant du mois sur
/// les semaines restantes (poids 1 par semaine pleine, poids
/// <see cref="BudgetHebdomadaire.FractionSemainePartielle"/> sur la 5e, quand elle existe).
/// </summary>
public sealed class SemaineBudget
{
    /// <summary>Numéro de la semaine dans l'ordre chronologique des semaines du mois (1 à N).</summary>
    public int NumeroSemaine { get; }

    /// <summary>Premier jour de la semaine (1er du mois pour la 1ère, 8, 15 ou 22 ensuite).</summary>
    public DateTime Debut { get; }

    /// <summary>Dernier jour de la semaine (7, 14, 21 ou 28 — ou dernier jour du mois pour la 5e).</summary>
    public DateTime Fin { get; }

    /// <summary>
    /// <c>true</c> si la date de référence fournie à
    /// <see cref="BudgetHebdomadaire.Calculer"/> tombe dans cette semaine.
    /// </summary>
    public bool EstSemaineCourante { get; }

    /// <summary>
    /// Reste disponible pour la semaine : part proportionnelle du restant du mois (budget −
    /// dépenses) réparti sur les semaines restantes par <see cref="BudgetHebdomadaire.Calculer"/> —
    /// poids 1 pour les semaines pleines (1 à 4), <see cref="BudgetHebdomadaire.FractionSemainePartielle"/>
    /// pour la 5e, quand elle existe ; les semaines strictement passées affichent 0 (leur argent
    /// a été reporté). Potentiellement négatif en cas de dépassement du budget.
    /// </summary>
    public double Reste { get; }

    /// <summary>Construit une semaine de la répartition hebdomadaire.</summary>
    /// <param name="numeroSemaine">Rang de la semaine dans le mois (1 à N).</param>
    /// <param name="debut">Premier jour de la semaine.</param>
    /// <param name="fin">Dernier jour de la semaine.</param>
    /// <param name="estSemaineCourante"><c>true</c> si la date de référence y tombe.</param>
    /// <param name="reste">Reste disponible pour la semaine.</param>
    public SemaineBudget(int numeroSemaine, DateTime debut, DateTime fin, bool estSemaineCourante, double reste)
    {
        NumeroSemaine = numeroSemaine;
        Debut = debut;
        Fin = fin;
        EstSemaineCourante = estSemaineCourante;
        Reste = reste;
    }
}