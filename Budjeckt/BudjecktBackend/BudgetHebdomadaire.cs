namespace Budjeckt;

/// <summary>
/// Découpe un mois en semaines de 7 jours comptées à partir du 1er (1→7, 8→14, 15→21, 22→28,
/// puis les jours 29+ dans une 5e semaine quand le mois les a) et attribue à chacune un reste :
/// le restant du mois (revenue − dépenses) est réparti proportionnellement sur les semaines
/// restantes. Une semaine pleine pèse 1 ; la 5e, quand elle existe, pèse
/// <see cref="FractionSemainePartielle"/> (un mois ≈ 4,35 semaines). Les semaines strictement
/// passées — avant la semaine contenant la date système — affichent 0,00 € : leur argent non
/// dépensé a été reporté et se retrouve dans les semaines restantes.
/// </summary>
public static class BudgetHebdomadaire
{
    /// <summary>
    /// Nombre de semaines moyen contenu dans un mois (une année ≈ 52 semaines,
    /// soit 52/12 ≈ 4,33 ; 4,35 est l'arrondi usuel retenu).
    /// </summary>
    public const double SemainesParMois = 4.35;

    /// <summary>
    /// Poids de répartition de la 5e semaine (jours 29 et plus) : elle porte
    /// <see cref="FractionSemainePartielle"/> de la part d'une semaine pleine, le mois étant
    /// compté à <see cref="SemainesParMois"/> semaines pleines (4,35 − 4 = 0,35).
    /// À conserver en phase avec <see cref="SemainesParMois"/>.
    /// </summary>
    public const double FractionSemainePartielle = 0.35;

    /// <summary>
    /// Part d'une semaine pleine : le restant du mois (<see cref="MonthBudget.BudgetRemaining"/>)
    /// divisé par <see cref="SemainesParMois"/>. Quand toutes les semaines du mois restent à
    /// répartir, les semaines 1 à 4 portent cette part et la 5e, quand elle existe,
    /// <see cref="FractionSemainePartielle"/> fois cette part (la somme vaut alors exactement
    /// le restant du mois).
    /// </summary>
    /// <param name="mois">Mois dont on calcule la part hebdomadaire pleine.</param>
    /// <returns>Part d'une semaine pleine du restant du mois.</returns>
    public static double ResteParSemaine(MonthBudget mois)
    {
        ArgumentNullException.ThrowIfNull(mois);
        return mois.BudgetRemaining / SemainesParMois;
    }

    /// <summary>
    /// Découpe le mois en semaines de 7 jours comptées à partir du 1er, dans l'ordre
    /// chronologique : les quatre premières semaines vont du 1er au 28, la 5e (si le mois a
    /// 29 jours ou plus) porte les jours restants — les mois de 28 jours (février non
    /// bissextil) n'ont donc que 4 semaines et jamais de 5e ligne.
    /// </summary>
    /// <remarks>
    /// <para>Le <b>reste</b> de chaque semaine n'est pas calculé semaine par semaine : le restant
    /// du mois (revenue − dépenses) est réparti sur les semaines considérées, de sorte qu'une
    /// dépense effectuée n'importe où dans le mois réduit la part de toutes les semaines
    /// restantes (répartition proportionnelle). Chaque semaine pleine pèse 1, la 5e — quand elle
    /// existe — pèse <see cref="FractionSemainePartielle"/> ; la somme des parts redonne
    /// exactement le restant du mois.</para>
    /// <para>Quand une <paramref name="reference"/> est fournie (mois affiché = mois courant
    /// système), les semaines strictement passées — avant celle qui contient la référence —
    /// affichent 0,00 € : leur argent non dépensé a été reporté, il est redistribué sur la
    /// semaine courante et les suivantes. Sans référence, aucune semaine n'est écartée : le
    /// restant est réparti sur toutes les semaines du mois.</para>
    /// </remarks>
    /// <param name="mois">Mois à découper.</param>
    /// <param name="reference">Date de référence servant à repérer la semaine courante et à
    /// écarter les semaines passées ; laissez <c>null</c> (mois affiché différent du mois
    /// courant) pour ne marquer aucune semaine et répartir sur tout le mois.</param>
    /// <returns>Semaines du mois, dans l'ordre chronologique (4 ou 5 selon sa durée).</returns>
    public static IReadOnlyList<SemaineBudget> Calculer(MonthBudget mois, DateTime? reference = null)
    {
        ArgumentNullException.ThrowIfNull(mois);

        int? indexMois = Validation.IndexDuMois(mois.Nom);
        if (indexMois is null)
        {
            throw new ArgumentException($"Le mois \"{mois.Nom}\" est inconnu.", nameof(mois));
        }

        if (mois.Annee is < 1900 or > 2200)
        {
            throw new ArgumentException("L'année doit être comprise entre 1900 et 2200.", nameof(mois));
        }

        DateTime premierJour = new(mois.Annee, indexMois.Value, 1);
        DateTime dernierJour = premierJour.AddMonths(1).AddDays(-1);

        var semaines = new List<SemaineBudget>();
        DateTime debut = premierJour;
        int numero = 0;
        while (debut <= dernierJour)
        {
            DateTime fin = debut.AddDays(6);
            if (fin > dernierJour)
            {
                fin = dernierJour;
            }

            numero++;
            // Reste provisoire à 0 : il sera remplacé par la répartition dans RepartirRestant.
            semaines.Add(new SemaineBudget(
                numero,
                debut,
                fin,
                reference is not null && EstDansLaSemaine(reference.Value, debut, fin),
                0d));

            debut = fin.AddDays(1);
        }

        RepartirRestant(semaines, mois.BudgetRemaining, reference);
        return semaines;
    }

    /// <summary>
    /// Répartit le restant du mois sur les semaines : part du poids dans la somme des poids des
    /// semaines restantes (poids 1 par semaine pleine, <see cref="FractionSemainePartielle"/> sur
    /// la 5e). Avec une date de référence, les semaines strictement passées affichent 0,00 €
    /// — leur argent non dépensé a été reporté et se retrouve dans les semaines restantes ;
    /// sans référence (mois affiché différent du mois courant, ou référence hors du mois),
    /// la répartition couvre toutes les semaines.
    /// </summary>
    /// <param name="semaines">Semaines calculées, dans l'ordre chronologique.</param>
    /// <param name="restant">Restant du mois (budget − dépenses) à répartir.</param>
    /// <param name="reference">Date de référence marquant la semaine courante ; <c>null</c> pour
    /// n'écarter aucune semaine.</param>
    private static void RepartirRestant(List<SemaineBudget> semaines, double restant, DateTime? reference)
    {
        if (semaines.Count == 0)
        {
            return;
        }

        int courante = 0;
        if (reference is not null)
        {
            courante = semaines.FindIndex(semaine => semaine.EstSemaineCourante);
            if (courante < 0)
            {
                // Référence hors du mois : aucune semaine courante ⇒ même comportement que sans
                // référence (répartition sur toutes les semaines, aucune ligne à 0).
                courante = 0;
            }
        }

        double poidsTotal = 0d;
        for (int i = courante; i < semaines.Count; i++)
        {
            poidsTotal += PoidsSemaine(semaines[i]);
        }

        for (int i = 0; i < semaines.Count; i++)
        {
            double reste = i < courante
                ? 0d
                : restant * PoidsSemaine(semaines[i]) / poidsTotal;
            semaines[i] = AvecReste(semaines[i], reste);
        }
    }

    /// <summary>
    /// Poids de répartition d'une semaine : 1 pour les semaines pleines (1 à 4),
    /// <see cref="FractionSemainePartielle"/> pour la 5e (partielle, jours 29 et plus).
    /// </summary>
    /// <param name="semaine">Semaine dont on veut le poids.</param>
    /// <returns>Poids (1 ou <see cref="FractionSemainePartielle"/>).</returns>
    private static double PoidsSemaine(SemaineBudget semaine) =>
        semaine.NumeroSemaine <= 4 ? 1d : FractionSemainePartielle;

    /// <summary>
    /// Renvoie une copie de la semaine avec le reste remplacé (les bornes et la marque
    /// « semaine courante » sont inchangées).
    /// </summary>
    private static SemaineBudget AvecReste(SemaineBudget semaine, double reste) =>
        new(semaine.NumeroSemaine, semaine.Debut, semaine.Fin, semaine.EstSemaineCourante, reste);

    /// <summary>
    /// Montant d'affichage de la dernière semaine : restant arrondi au centime moins la somme
    /// des lignes précédentes aussi arrondies au centime. Les lignes étant arrondies une à une
    /// (format N2), la différence portée par la dernière ligne garantit que la somme des montants
    /// affichés redonne exactement le restant affiché.
    /// </summary>
    /// <param name="restant">Restant du mois (budget − dépenses).</param>
    /// <param name="lignesPrecedentes">Montants bruts des semaines précédentes.</param>
    /// <returns>Montant à afficher sur la dernière ligne.</returns>
    public static double ResteDerniereSemaineAffiche(double restant, IReadOnlyList<double> lignesPrecedentes)
    {
        ArgumentNullException.ThrowIfNull(lignesPrecedentes);

        double sommeAffichee = 0d;
        foreach (double ligne in lignesPrecedentes)
        {
            sommeAffichee += Math.Round(ligne, 2);
        }

        double valeur = Math.Round(restant, 2) - sommeAffichee;
        // Un arrondi au centime inférieur à la limite de 0,005 deviendrait « −0,00 » (signe
        // parasite du −0,0) : un écart inférieur au centime est ramené à zéro.
        return Math.Abs(valeur) < 0.005d ? 0d : valeur;
    }

    /// <summary>Détermine si une date tombe dans la plage [début, fin] d'une semaine.</summary>
    private static bool EstDansLaSemaine(DateTime date, DateTime debut, DateTime fin)
    {
        return date >= debut && date <= fin;
    }
}