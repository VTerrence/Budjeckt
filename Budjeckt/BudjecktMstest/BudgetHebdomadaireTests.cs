namespace Budjeckt.Tests;

[TestClass]
public class BudgetHebdomadaireTests
{
    private static Tuple<int, string, float>[] CategoriesParDefaut() =>
        new[]
        {
            new Tuple<int, string, float>(1, "Loyer", 0f),
            new Tuple<int, string, float>(2, "Eau", 0f)
        };

    private static MonthBudget MoisDeSeptembre2026(float revenue, params Facture[] factures) =>
        new("Septembre", revenue, CategoriesParDefaut(), factures, 2026);

    [TestMethod]
    public void ResteParSemaine_RestantDe435_PartPleineDeCent()
    {
        // La part pleine (restant ÷ 4,35) restitue le restant total du mois.
        var mois = MoisDeSeptembre2026(435f);
        Assert.AreEqual(435f, BudgetHebdomadaire.ResteParSemaine(mois) * BudgetHebdomadaire.SemainesParMois, 0.001d);
        Assert.AreEqual(100d, BudgetHebdomadaire.ResteParSemaine(mois), 0.0001d);
    }

    [TestMethod]
    public void ResteParSemaine_RetrancheLesDépensesDuMois()
    {
        // Budget 1000, dépense 100 : restant 900 → 900/4,35 ≈ 206,90.
        var factures = new[] { new Facture(1, 1, 100f, new DateTime(2026, 9, 5)) };
        var mois = MoisDeSeptembre2026(1000f, factures);

        Assert.AreEqual(900d, (double)mois.BudgetRemaining, 0.0001d);
        Assert.AreEqual(900d / BudgetHebdomadaire.SemainesParMois, BudgetHebdomadaire.ResteParSemaine(mois), 0.0001d);
    }

    [TestMethod]
    public void ResteParSemaine_BudgetNul_ResteNul()
    {
        Assert.AreEqual(0d, BudgetHebdomadaire.ResteParSemaine(MoisDeSeptembre2026(0f)), 0.0001d);
    }

    [TestMethod]
    public void ResteParSemaine_Dépassement_ResteNégatif()
    {
        // Budget 100 dépensé 150 : restant −50 → ≈ −11,49 par semaine.
        var factures = new[] { new Facture(1, 1, 150f, new DateTime(2026, 9, 5)) };
        var mois = MoisDeSeptembre2026(100f, factures);

        Assert.AreEqual(-50d, (double)mois.BudgetRemaining, 0.0001d);
        Assert.AreEqual(-50d / BudgetHebdomadaire.SemainesParMois, BudgetHebdomadaire.ResteParSemaine(mois), 0.0001d);
    }

    [TestMethod]
    public void ResteParSemaine_MoisNull_LèveArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BudgetHebdomadaire.ResteParSemaine(null!));
    }

    [TestMethod]
    public void ResteParSemaine_ValeurDécimale_ArrondiAuCentime()
    {
        // 78/4,35 ≈ 17,9310 → affichage « 17,93 ».
        double partPleine = BudgetHebdomadaire.ResteParSemaine(MoisDeSeptembre2026(78f));
        Assert.AreEqual(78d / BudgetHebdomadaire.SemainesParMois, partPleine, 0.0001d);
        Assert.AreEqual(17.93d, partPleine, 0.005d);
    }

    [TestMethod]
    public void Calculer_MoisDeTrenteJours_CinqSemainesÀPartirDuPremier()
    {
        // Septembre 2026 (30 jours) : semaines 1→7, 8→14, 15→21, 22→28, puis 29→30.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 9, 13));

        Assert.HasCount(5, semaines);
        Assert.AreEqual(new DateTime(2026, 9, 1), semaines[0].Debut);
        Assert.AreEqual(new DateTime(2026, 9, 7), semaines[0].Fin);
        Assert.AreEqual(new DateTime(2026, 9, 8), semaines[1].Debut);
        Assert.AreEqual(new DateTime(2026, 9, 22), semaines[3].Debut);
        Assert.AreEqual(new DateTime(2026, 9, 28), semaines[3].Fin);
        Assert.AreEqual(new DateTime(2026, 9, 29), semaines[4].Debut);
        Assert.AreEqual(new DateTime(2026, 9, 30), semaines[4].Fin);
    }

    [TestMethod]
    public void Calculer_MoisDeTrenteEtUnJours_CinquièmeSemaineDeTroisJours()
    {
        // Octobre 2026 (31 jours) : la 5e semaine couvre les jours 29, 30 et 31.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Octobre", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 2026));

        Assert.HasCount(5, semaines);
        Assert.AreEqual(new DateTime(2026, 10, 29), semaines[4].Debut);
        Assert.AreEqual(new DateTime(2026, 10, 31), semaines[4].Fin);
    }

    [TestMethod]
    public void Calculer_FévrierVingtHuitJours_QuatreSemainesSansCinquième()
    {
        // Février 2027 (28 jours) : exactement 4 semaines pleines, pas de 5e ligne.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Février", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 2027));

        Assert.HasCount(4, semaines);
        Assert.AreEqual(new DateTime(2027, 2, 1), semaines[0].Debut);
        Assert.AreEqual(new DateTime(2027, 2, 7), semaines[0].Fin);
        Assert.AreEqual(new DateTime(2027, 2, 22), semaines[3].Debut);
        Assert.AreEqual(new DateTime(2027, 2, 28), semaines[3].Fin);
    }

    [TestMethod]
    public void Calculer_FévrierBissextil2028_CinquièmeSemaineDUnJour()
    {
        // Février 2028 (29 jours) : la 5e semaine ne couvre que le 29.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Février", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 2028));

        Assert.HasCount(5, semaines);
        Assert.AreEqual(new DateTime(2028, 2, 29), semaines[4].Debut);
        Assert.AreEqual(new DateTime(2028, 2, 29), semaines[4].Fin);
    }

    [TestMethod]
    public void Calculer_QuatrePremièresSemaines_PartPleine_EtCinquième_Part0_35()
    {
        // Budget 435, dépenses 35 : restant 400. Part pleine = 400/4,35 ≈ 91,95 vue 4 fois,
        // 5e semaine = part des 0,35 semaine restants ≈ 32,18. La somme des 5 lignes brutes
        // redonne le restant du mois (4 × pleine + 0,35 × pleine = 4,35 × pleine = 400).
        var factures = new[]
        {
            new Facture(1, 1, 10f, new DateTime(2026, 9, 5)),
            new Facture(2, 1, 20f, new DateTime(2026, 9, 10)),
            new Facture(3, 2, 5f, new DateTime(2026, 9, 30))
        };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(435f, factures), new DateTime(2026, 9, 3));

        Assert.HasCount(5, semaines);
        double partPleine = 400d / BudgetHebdomadaire.SemainesParMois;
        foreach (SemaineBudget semaine in semaines.Take(4))
        {
            Assert.AreEqual(partPleine, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(
            partPleine * BudgetHebdomadaire.FractionSemainePartielle,
            semaines[4].Reste,
            0.0001d,
            "Semaine 5");

        Assert.AreEqual(400d, semaines.Sum(s => s.Reste), 0.01d, "La somme des lignes doit restituer le restant du mois");
    }

    [TestMethod]
    public void Calculer_MoisBudgetNul_ToutesLesLignesNulles()
    {
        // Budget 0 : part pleine et part 0,35 sont toutes deux nulles, aucune ligne ne déraille.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(MoisDeSeptembre2026(0f));
        foreach (SemaineBudget semaine in semaines)
        {
            Assert.AreEqual(0d, semaine.Reste);
        }
    }

    [TestMethod]
    public void Calculer_DépassementBudget_LaPart0_35EstNégative()
    {
        // Budget 100, dépenses 150 : restant −50. Part pleine ≈ −11,49, 5e semaine ≈ −4,02 ;
        // la somme des 5 lignes = −50 (signe conservé sur la part partielle).
        var factures = new[] { new Facture(1, 1, 150f, new DateTime(2026, 9, 5)) };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(100f, factures));

        Assert.HasCount(5, semaines);
        double partPleine = -50d / BudgetHebdomadaire.SemainesParMois;
        foreach (SemaineBudget semaine in semaines.Take(4))
        {
            Assert.AreEqual(partPleine, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(partPleine * BudgetHebdomadaire.FractionSemainePartielle, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(-50d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_SemainesPassées_ReportéesSurLesRestantes()
    {
        // Budget 435, dépenses 35 (semaine 1) : restant 400. Le 13 septembre tombe dans la
        // semaine 2 → la semaine 1 (passée) affiche 0 (sa part non dépensée est reportée et
        // redistribuée) ; le restant est réparti sur les semaines 2 à 5 (poids 3,35).
        var factures = new[]
        {
            new Facture(1, 1, 10f, new DateTime(2026, 9, 3)),
            new Facture(2, 1, 25f, new DateTime(2026, 9, 6))
        };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(435f, factures), new DateTime(2026, 9, 13));

        Assert.HasCount(5, semaines);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d, "Semaine passée : part reportée");

        double partRestante = 400d / 3.35d;
        foreach (SemaineBudget semaine in semaines.Skip(1).Take(3))
        {
            Assert.AreEqual(partRestante, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(400d * BudgetHebdomadaire.FractionSemainePartielle / 3.35d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(400d, semaines.Sum(s => s.Reste), 0.01d,
            "La somme des restes doit restituer le restant du mois");
    }

    [TestMethod]
    public void Calculer_DépenseFuture_RéduitProportionnellementLesSemainesRestantes()
    {
        // Budget 435, dépense 35 au 24 septembre (semaine 4, future) : le restant 400 est
        // réparti sur les semaines 2 à 5 comme si la dépense avait réduit tout le reliquat.
        // Sans dépense (restant 435), chaque semaine pleine restante porterait 435/3,35 : la
        // dépense diminue donc la part de toutes les semaines restantes, y compris passées en date.
        var factures = new[] { new Facture(1, 1, 35f, new DateTime(2026, 9, 24)) };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(435f, factures), new DateTime(2026, 9, 13));

        double partRestante = 400d / 3.35d;
        Assert.AreEqual(partRestante, semaines[1].Reste, 0.0001d);
        Assert.AreEqual(partRestante, semaines[3].Reste, 0.0001d);
        Assert.AreEqual(400d * BudgetHebdomadaire.FractionSemainePartielle / 3.35d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
    }

    [TestMethod]
    public void Calculer_Février_QuatreSemaines_SemaineCouranteEnSemaine2()
    {
        // Février 2027 (28 jours, 4 semaines) : budget 300. Référence au 10 → semaine 2 courante,
        // poids restants 3 (semaines 2, 3, 4) → 100,00 € chacune ; la semaine 1 affiche 0.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Février", 300f, CategoriesParDefaut(), Array.Empty<Facture>(), 2027),
            new DateTime(2027, 2, 10));

        Assert.HasCount(4, semaines);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
        foreach (SemaineBudget semaine in semaines.Skip(1))
        {
            Assert.AreEqual(100d, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(300d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_DernièreSemaineCourante_ConcentreToutLeRestant()
    {
        // Le 29 septembre tombe dans la 5e semaine (29→30) : c'est la dernière, il ne reste
        // qu'elle à répartir → elle porte intégralement le restant du mois, les quatre autres
        // semaines (passées) affichent 0.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(200f), new DateTime(2026, 9, 29));

        Assert.IsTrue(semaines[4].EstSemaineCourante);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
        Assert.AreEqual(0d, semaines[3].Reste, 0.0001d);
        Assert.AreEqual(200d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(200d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_Dépassement_SemainesPasséesNulles_RestantesNégatives()
    {
        // Budget 100, dépense 150 en semaine 1 : restant −50. Semaine 1 passée affiche 0 ;
        // les semaines 2 à 5 se partagent le restant négatif (poids 3,35), la somme redonne −50.
        var factures = new[] { new Facture(1, 1, 150f, new DateTime(2026, 9, 5)) };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(100f, factures), new DateTime(2026, 9, 13));

        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
        double partRestante = -50d / 3.35d;
        Assert.AreEqual(partRestante, semaines[1].Reste, 0.0001d);
        Assert.AreEqual(-50d * BudgetHebdomadaire.FractionSemainePartielle / 3.35d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(-50d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_DateDeRéférence_DonneLaSemaineCourante()
    {
        // Le 13 septembre tombe dans la semaine 2 (8→14 septembre).
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 9, 13));

        Assert.IsFalse(semaines[0].EstSemaineCourante);
        Assert.IsTrue(semaines[1].EstSemaineCourante);
        Assert.IsFalse(semaines[2].EstSemaineCourante);
        Assert.IsFalse(semaines[4].EstSemaineCourante);
    }

    [TestMethod]
    public void Calculer_RéférenceSurLesBornes_MarqueLesSemaines()
    {
        // 7 septembre = dernier jour de la semaine 1, 8 septembre = premier jour de la semaine 2.
        IReadOnlyList<SemaineBudget> finSemaine1 = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 9, 7));
        Assert.IsTrue(finSemaine1[0].EstSemaineCourante);
        Assert.IsFalse(finSemaine1[1].EstSemaineCourante);

        IReadOnlyList<SemaineBudget> debutSemaine2 = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 9, 8));
        Assert.IsTrue(debutSemaine2[1].EstSemaineCourante);
        Assert.IsFalse(debutSemaine2[0].EstSemaineCourante);
    }

    [TestMethod]
    public void Calculer_RéférenceSurLaFrontièreDeLaCinquièmeSemaine_MarqueLaBonneSemaine()
    {
        // Septembre 2026 (30 jours) : semaine 4 = 22→28 septembre, semaine 5 = 29→30.
        // Le 28 marque la semaine 4 ; le 29 bascule sur la semaine partielle (borne haute).
        IReadOnlyList<SemaineBudget> dernierJourSemaine4 = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 9, 28));
        Assert.IsTrue(dernierJourSemaine4[3].EstSemaineCourante);
        Assert.IsFalse(dernierJourSemaine4[4].EstSemaineCourante);

        IReadOnlyList<SemaineBudget> premierJourSemaine5 = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 9, 29));
        Assert.IsTrue(premierJourSemaine5[4].EstSemaineCourante);
        Assert.IsFalse(premierJourSemaine5[3].EstSemaineCourante);
    }

    [TestMethod]
    public void Calculer_RéférenceHorsDuMois_AucuneSemaineCourante()
    {
        // Les semaines restent dans le mois (aucun débordement) : une date du mois précédent
        // (31 août) ou du mois suivant (1er octobre) n'appartient à aucune ligne de septembre.
        IReadOnlyList<SemaineBudget> avant = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 8, 31));
        foreach (SemaineBudget semaine in avant)
        {
            Assert.IsFalse(semaine.EstSemaineCourante);
        }

        IReadOnlyList<SemaineBudget> apres = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(0f), new DateTime(2026, 10, 1));
        foreach (SemaineBudget semaine in apres)
        {
            Assert.IsFalse(semaine.EstSemaineCourante);
        }
    }

    [TestMethod]
    public void Calculer_SansRéférence_AucuneSemaineCourante()
    {
        // Mois affiché différent du mois courant système : aucune semaine marquée en gras.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Octobre", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 2026));
        foreach (SemaineBudget semaine in semaines)
        {
            Assert.IsFalse(semaine.EstSemaineCourante);
        }
    }

    [TestMethod]
    public void Calculer_MoisNull_LèveArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BudgetHebdomadaire.Calculer(null!, new DateTime(2026, 9, 3)));
    }

    [TestMethod]
    public void Calculer_MoisInconnu_LèveArgumentException()
    {
        var frimaire = new MonthBudget("Frimaire", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 2026);

        ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(
            () => BudgetHebdomadaire.Calculer(frimaire, new DateTime(2026, 9, 3)));

        Assert.AreEqual("mois", exception.ParamName);
    }

    [TestMethod]
    public void Calculer_AnnéeHorsBornes_LèveArgumentException()
    {
        var moisAnnee0 = new MonthBudget("Septembre", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 0);
        var moisAnnee9999 = new MonthBudget("Septembre", 0f, CategoriesParDefaut(), Array.Empty<Facture>(), 9999);

        ArgumentException basse = Assert.ThrowsExactly<ArgumentException>(
            () => BudgetHebdomadaire.Calculer(moisAnnee0));
        ArgumentException haute = Assert.ThrowsExactly<ArgumentException>(
            () => BudgetHebdomadaire.Calculer(moisAnnee9999));

        Assert.AreEqual("mois", basse.ParamName);
        Assert.AreEqual("mois", haute.ParamName);
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_ÉcartDE_PlusieursCentimes()
    {
        // Restant 78, 4 lignes pleines à 17,9310… : les lignes affichées font 17,93 × 4 = 71,72,
        // la dernière porte donc 78 − 71,72 = 6,28.
        double partPleine = 78d / BudgetHebdomadaire.SemainesParMois;
        IReadOnlyList<double> lignes = new[] { partPleine, partPleine, partPleine, partPleine };

        Assert.AreEqual(6.28d, BudgetHebdomadaire.ResteDerniereSemaineAffiche(78d, lignes), 0.0001d);
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_Février_QuatreSemainesSansPartPartielle()
    {
        // Février (28 jours) : 4 lignes pleines à 180,46 affichées ; la dernière = restant − 3 × 180,46.
        double partPleine = 785d / BudgetHebdomadaire.SemainesParMois;
        IReadOnlyList<double> lignes = new[] { partPleine, partPleine, partPleine };

        Assert.AreEqual(243.62d, BudgetHebdomadaire.ResteDerniereSemaineAffiche(785d, lignes), 0.0001d);
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_SommeAffichée_RedonneLeRestant()
    {
        // Petit restant : 0,50 réparti sur 5 semaines → 0,11 × 4 + 0,06 = 0,50 exact.
        double partPleine = 0.50d / BudgetHebdomadaire.SemainesParMois;
        double derniere = BudgetHebdomadaire.ResteDerniereSemaineAffiche(0.5d, new[] { partPleine, partPleine, partPleine, partPleine });

        Assert.AreEqual(0.06d, derniere, 0.0001d);
        Assert.AreEqual(0.5d, 4 * Math.Round(partPleine, 2) + derniere, 0.0001d);
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_ListeNull_LèveArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(
            () => BudgetHebdomadaire.ResteDerniereSemaineAffiche(78d, null!));
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_ListeVide_RetourneLeRestantArrondi()
    {
        // Aucune ligne précédente : la dernière ligne porte tout le restant, arrondi N2.
        Assert.AreEqual(78d, BudgetHebdomadaire.ResteDerniereSemaineAffiche(78d, Array.Empty<double>()), 0.0001d);
        Assert.AreEqual(6.28d, BudgetHebdomadaire.ResteDerniereSemaineAffiche(6.283d, Array.Empty<double>()), 0.0001d);
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_DépassementNégatif_LaSommeRedonneLeRestantAffiché()
    {
        // Restant −50 : lignes pleines affichées à −11,49 (−50/4,35 ≈ −11,4943 arrondi N2).
        // La dernière porte la différence : −50 − 3 × (−11,49) = −15,53, et la somme affichée
        // (3 × −11,49 + −15,53) redonne exactement le restant affiché −50.
        double partPleine = -50d / BudgetHebdomadaire.SemainesParMois;
        double derniere = BudgetHebdomadaire.ResteDerniereSemaineAffiche(-50d, new[] { partPleine, partPleine, partPleine });

        Assert.AreEqual(-15.53d, derniere, 0.0001d);
        Assert.AreEqual(Math.Round(-50d, 2), 3 * Math.Round(partPleine, 2) + derniere, 0.0001d);
    }

    [TestMethod]
    public void ResteDerniereSemaineAffiche_ÉcartSousLeCentime_RetourneZéroSansSigneNégatif()
    {
        // Un restant de −0,003 (dépassement infime) produirait −0,0 → « −0,00 » : le signe
        // parasite doit être écrasé en zéro tout en conservant la garantie de somme au centime.
        double partPleine = -0.003d / BudgetHebdomadaire.SemainesParMois;
        double derniere = BudgetHebdomadaire.ResteDerniereSemaineAffiche(-0.003d, new[] { partPleine, partPleine, partPleine, partPleine });

        Assert.AreEqual(0d, derniere);
        Assert.IsTrue(double.IsPositive(derniere), "Le signe parasite « −0 » doit être écrasé à +0 avant formatage.");
    }

    [TestMethod]
    public void Calculer_RéférencePremierJourDuMois_AucuneSemaineÀZéro()
    {
        // Borne basse de la référence : le 1er septembre tombe dans la semaine 1, aucune
        // semaine n'est encore passée → le restant (435) est réparti sur les 5 semaines
        // (poids 4,35) et aucune ligne n'affiche 0,00 €.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(435f), new DateTime(2026, 9, 1));

        Assert.HasCount(5, semaines);
        Assert.IsTrue(semaines[0].EstSemaineCourante, "Le 1er du mois appartient à la semaine 1.");
        foreach (SemaineBudget semaine in semaines)
        {
            Assert.IsGreaterThan(0d, semaine.Reste, $"Semaine {semaine.NumeroSemaine} : aucune semaine ne doit afficher 0.");
        }

        Assert.AreEqual(100d, semaines[0].Reste, 0.0001d);
        Assert.AreEqual(35d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(435d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_RéférenceDernierJourDuMois_CinquièmeSemainePorteTout()
    {
        // Borne haute de la référence : le 30 septembre (dernier jour du mois) tombe dans la
        // 5e semaine, seule restante → elle porte intégralement le restant (200), les quatre
        // autres semaines (passées) affichent 0.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(200f), new DateTime(2026, 9, 30));

        Assert.HasCount(5, semaines);
        Assert.IsTrue(semaines[4].EstSemaineCourante);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
        Assert.AreEqual(0d, semaines[3].Reste, 0.0001d);
        Assert.AreEqual(200d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(200d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_FévrierQuatreSemaines_AvecDépenses_SansCinquièmeLigne()
    {
        // Février 2027 (28 jours) n'a pas de 5e semaine : avec une dépense de 50 sur 300, le
        // restant 250 est réparti sur les 4 semaines pleines (poids 4) → 62,50 chacune.
        var factures = new[] { new Facture(1, 1, 50f, new DateTime(2027, 2, 5)) };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Février", 300f, CategoriesParDefaut(), factures, 2027));

        Assert.HasCount(4, semaines);
        foreach (SemaineBudget semaine in semaines)
        {
            Assert.AreEqual(62.5d, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(250d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_Février_SemainesPasséesÉcartées_SansPartPartielle()
    {
        // Février 2027, budget 300, dépense 50 en semaine 1 : restant 250. Référence au
        // 10 février → semaine 2 courante, 3 semaines pleines restantes (poids 3) →
        // 83,33 chacune ; la semaine 1 affiche 0 (son argent non dépensé a été reporté).
        var factures = new[] { new Facture(1, 1, 50f, new DateTime(2027, 2, 5)) };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Février", 300f, CategoriesParDefaut(), factures, 2027),
            new DateTime(2027, 2, 10));

        Assert.HasCount(4, semaines);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
        double partRestante = 250d / 3d;
        foreach (SemaineBudget semaine in semaines.Skip(1))
        {
            Assert.AreEqual(partRestante, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(250d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_Février_RéférenceLeDernierJour_DernièreSemainePleinePorteTout()
    {
        // Février 2027, référence au 28 (dernier jour, semaine 4 pleine) : il ne reste que la
        // semaine 4 à répartir — poids total 1 — elle porte tout le restant (300), les trois
        // autres semaines passées affichent 0. Cas distinct de la 5e semaine partielle : le
        // dernier jour d'un mois à 4 semaines pèse 1, pas 0,35.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            new MonthBudget("Février", 300f, CategoriesParDefaut(), Array.Empty<Facture>(), 2027),
            new DateTime(2027, 2, 28));

        Assert.HasCount(4, semaines);
        Assert.IsTrue(semaines[3].EstSemaineCourante);
        Assert.AreEqual(0d, semaines[0].Reste, 0.0001d);
        Assert.AreEqual(0d, semaines[2].Reste, 0.0001d);
        Assert.AreEqual(300d, semaines[3].Reste, 0.0001d);
        Assert.AreEqual(300d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_RestantNul_AvecSemainesPassées_ToutesLesLignesÀZéro()
    {
        // Budget 200 entièrement dépensé (restant 0) avec référence en semaine 2 : les
        // semaines passées affichent 0 et les semaines restantes reçoivent 0 × poids /
        // poidsTotal = 0. Un restant nul ne doit pas dérailler sur la division par le poids
        // total (3,35) : toutes les lignes sont exactement à 0,00 €.
        var factures = new[]
        {
            new Facture(1, 1, 100f, new DateTime(2026, 9, 3)),
            new Facture(2, 1, 100f, new DateTime(2026, 9, 10))
        };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(200f, factures), new DateTime(2026, 9, 13));

        Assert.HasCount(5, semaines);
        foreach (SemaineBudget semaine in semaines)
        {
            Assert.AreEqual(0d, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(0d, semaines.Sum(s => s.Reste), 0.0001d);
    }

    [TestMethod]
    public void Calculer_BudgetNégatif_RépartitionNégative_SansDépenses()
    {
        // Revenue négatif (budget en déficit, autorisé par MonthBudget) : −200 réparti sur
        // les 5 semaines sans référence — part pleine ≈ −45,98, 5e semaine ≈ −16,09, somme −200.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(-200f));

        Assert.HasCount(5, semaines);
        double partPleine = -200d / BudgetHebdomadaire.SemainesParMois;
        foreach (SemaineBudget semaine in semaines.Take(4))
        {
            Assert.AreEqual(partPleine, semaine.Reste, 0.0001d, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(partPleine * BudgetHebdomadaire.FractionSemainePartielle, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(-200d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_RéférenceHorsDuMois_RépartitionÉtendueÀToutesLesSemaines()
    {
        // Une référence hors du mois (1er octobre pour un mois de septembre) ne marque aucune
        // semaine courante et, comme sans référence, le restant est réparti sur toutes les
        // semaines : aucune ligne à 0,00 €, part pleine 100 × 4 et 5e semaine à 35.
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(435f), new DateTime(2026, 10, 1));

        Assert.HasCount(5, semaines);
        Assert.AreEqual(0, semaines.Count(s => s.EstSemaineCourante));
        foreach (SemaineBudget semaine in semaines)
        {
            Assert.IsGreaterThan(0d, semaine.Reste, $"Semaine {semaine.NumeroSemaine}");
        }

        Assert.AreEqual(100d, semaines[0].Reste, 0.0001d);
        Assert.AreEqual(35d, semaines[4].Reste, 0.0001d);
        Assert.AreEqual(435d, semaines.Sum(s => s.Reste), 0.01d);
    }

    [TestMethod]
    public void Calculer_Invariant_ToutMoisAuMoinsQuatreSemaines_SommeÉgaleAuRestant()
    {
        // Un mois a toujours au moins 4 semaines (28 jours en février non bissextil), donc le
        // poids total de la répartition est toujours strictement positif : aucune division par
        // zéro possible. On vérifie l'invariant sur tous les mois de plusieurs années (28, 29,
        // 30 et 31 jours) avec et sans référence (1er, 15, dernier jour) : au moins 4 lignes,
        // au plus une semaine courante marquée (aucune sans référence) et somme des restes qui
        // redonne le restant du mois.
        int[] annees = { 2026, 2027, 2028, 2100 };
        string[] nomsMois =
        {
            "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
            "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
        };

        foreach (int annee in annees)
        {
            foreach (string nom in nomsMois)
            {
                var mois = new MonthBudget(nom, 87f, CategoriesParDefaut(), Array.Empty<Facture>(), annee);
                DateTime premier = new(annee, Array.IndexOf(nomsMois, nom) + 1, 1);
                DateTime dernier = premier.AddMonths(1).AddDays(-1);
                DateTime?[] references = { null, premier, premier.AddDays(14), dernier };

                foreach (DateTime? reference in references)
                {
                    IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(mois, reference);

                    Assert.IsGreaterThanOrEqualTo(4, semaines.Count, $"{nom} {annee} : au moins 4 semaines attendues.");
                    int semainesCourantes = semaines.Count(s => s.EstSemaineCourante);
                    Assert.AreEqual(reference is null ? 0 : 1, semainesCourantes,
                        $"{nom} {annee} : la référence doit marquer exactement une semaine courante.");
                    Assert.AreEqual(87d, semaines.Sum(s => s.Reste), 0.01d,
                        $"{nom} {annee} : la somme des restes doit restituer le restant du mois.");
                }
            }
        }
    }

    [TestMethod]
    public void Calculer_ChaîneAffichage_SommeDesMontantsAffichesRedonneLeRestant()
    {
        // Reproduit ActualiserSemainesBudget : budget 100, dépense 22 → restant 78, référence
        // en semaine 2 (poids restants 3,35). Les 3 lignes pleines restantes sont arrondies à
        // 23,28 (N2) ; la dernière ligne porte l'ajustement au centime via
        // ResteDerniereSemaineAffiche : 78 − 3 × 23,28 = 8,16. La somme des montants affichés
        // redonne exactement le restant arrondi du mois.
        var factures = new[] { new Facture(1, 1, 22f, new DateTime(2026, 9, 5)) };
        IReadOnlyList<SemaineBudget> semaines = BudgetHebdomadaire.Calculer(
            MoisDeSeptembre2026(100f, factures), new DateTime(2026, 9, 13));

        var montants = semaines.Select(s => s.Reste).ToArray();
        montants[^1] = BudgetHebdomadaire.ResteDerniereSemaineAffiche(78d, montants[..^1]);

        Assert.AreEqual(8.16d, montants[^1], 0.0001d, "La dernière ligne porte l'ajustement au centime.");
        double sommeAffichee = montants.Sum(m => Math.Round(m, 2));
        Assert.AreEqual(78d, sommeAffichee, 0.001d,
            "La somme des montants affichés doit redonner exactement le restant arrondi.");
    }
}