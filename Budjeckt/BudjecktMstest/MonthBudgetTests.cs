namespace Budjeckt.Tests;

[TestClass]
public class MonthBudgetTests
{
    private static readonly string[] NomsParDefaut =
    {
        "Loyer", "Eau", "Electricite", "Chauffage", "Alimentation", "Transports", "Loisirs"
    };

    private static readonly string[] NomsMois =
    {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    [TestMethod]
    public void ConstructeurParDefaut_CréeLesSeptCatégoriesParDéfaut()
    {
        var mois = new MonthBudget("Janvier");

        Assert.AreEqual("Janvier", mois.Nom);
        Assert.AreEqual(0f, mois.Revenue);

        Tuple<int, string, float>[] categories = mois.ExpenseCategories;
        Assert.HasCount(7, categories);
        for (int i = 0; i < categories.Length; i++)
        {
            Assert.AreEqual(i + 1, categories[i].Item1, $"Id de la {i + 1}e catégorie");
            Assert.AreEqual(NomsParDefaut[i], categories[i].Item2, $"Nom de la {i + 1}e catégorie");
            Assert.AreEqual(0f, categories[i].Item3, 0.001f, $"Dépense de la {i + 1}e catégorie");
        }

        Assert.IsEmpty(mois.Factures);
        Assert.AreEqual(0f, mois.TotalExpenses);
        Assert.AreEqual(0f, mois.BudgetRemaining);
    }

    [TestMethod]
    public void ConstructeurParDéfaut_NomVide_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new MonthBudget("   "));
    }

    [TestMethod]
    public void ConstructeurComplet_RecalculeDépenseCategorieTotalEtReste()
    {
        var categories = new[]
        {
            new Tuple<int, string, float>(1, "Loyer", 0f),
            new Tuple<int, string, float>(2, "Eau", 0f)
        };
        var factures = new[]
        {
            new Facture(1, 1, 200f, new DateTime(DateTime.Today.Year, 1, 3)),
            new Facture(2, 1, 50f, new DateTime(DateTime.Today.Year, 1, 10)),
            new Facture(3, 2, 300f, new DateTime(DateTime.Today.Year, 1, 20))
        };

        var mois = new MonthBudget("Janvier", 1000f, categories, factures);

        Assert.AreEqual(1000f, mois.Revenue);
        Assert.HasCount(3, mois.Factures);

        Tuple<int, string, float>[] resultats = mois.ExpenseCategories;
        Assert.AreEqual(250f, resultats[0].Item3, 0.001f, "Dépense de la catégorie Loyer");
        Assert.AreEqual(300f, resultats[1].Item3, 0.001f, "Dépense de la catégorie Eau");
        Assert.AreEqual(550f, mois.TotalExpenses, 0.001f);
        Assert.AreEqual(450f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void AjouterCatégorie_AugmenteLaListeAvecIdAutoEtDépenseNulle()
    {
        var mois = new MonthBudget("Mars");

        mois.AjouterCategorie("Assurance");

        Tuple<int, string, float>[] categories = mois.ExpenseCategories;
        Assert.HasCount(8, categories);
        Assert.AreEqual(8, categories[^1].Item1);
        Assert.AreEqual("Assurance", categories[^1].Item2);
        Assert.AreEqual(0f, categories[^1].Item3, 0.001f);
    }

    [TestMethod]
    public void AjouterCatégorie_DoublonInsensibleÀLaCasse_LèveArgumentException()
    {
        var mois = new MonthBudget("Mars");

        mois.AjouterCategorie("Assurance");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterCategorie("assurance"));
    }

    [TestMethod]
    public void AjouterCatégorie_NomVide_LèveArgumentException()
    {
        var mois = new MonthBudget("Mars");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterCategorie(string.Empty));
    }

    [TestMethod]
    public void AjouterFacture_SansDate_MoisCourant_UtiliseAujourdHui()
    {
        var mois = new MonthBudget(NomsMois[DateTime.Today.Month - 1]);

        mois.AjouterFacture(4, 12.5f);

        Assert.HasCount(1, mois.Factures);
        Facture facture = mois.Factures[0];
        Assert.AreEqual(1, facture.Id);
        Assert.AreEqual(4, facture.IdCategorie);
        Assert.AreEqual(12.5f, facture.Montant);
        Assert.AreEqual(DateTime.Today, facture.Date);
    }

    [TestMethod]
    public void AjouterFacture_SansDate_AutreMois_UtiliseLePremierDuMois()
    {
        int indexAutreMois = (DateTime.Today.Month % 12) + 1;
        var mois = new MonthBudget(NomsMois[indexAutreMois - 1]);

        mois.AjouterFacture(4, 12.5f);

        Assert.HasCount(1, mois.Factures);
        Assert.AreEqual(new DateTime(DateTime.Today.Year, indexAutreMois, 1), mois.Factures[0].Date);
    }

    [TestMethod]
    public void AjouterFacture_AvecDate_GardeLaDateEtIncémenteLId()
    {
        var mois = new MonthBudget("Janvier");
        DateTime date = new(DateTime.Today.Year, 1, 15);

        mois.AjouterFacture(4, 12.5f);
        mois.AjouterFacture(5, 60f, date);

        Assert.HasCount(2, mois.Factures);
        Assert.AreEqual(2, mois.Factures[1].Id);
        Assert.AreEqual(date, mois.Factures[1].Date);
    }

    [TestMethod]
    public void AjouterFacture_AvecHeure_GardeLHeure()
    {
        var mois = new MonthBudget("Janvier");
        var heure = new TimeSpan(14, 30, 0);

        mois.AjouterFacture(4, 12.5f, new DateTime(DateTime.Today.Year, 1, 3), heure);

        Assert.HasCount(1, mois.Factures);
        Assert.AreEqual(heure, mois.Factures[0].Heure);
    }

    [TestMethod]
    public void AjouterFacture_SansHeure_HeureNulle()
    {
        var mois = new MonthBudget("Janvier");

        mois.AjouterFacture(4, 12.5f, new DateTime(DateTime.Today.Year, 1, 3));

        Assert.IsNull(mois.Factures[0].Heure);
    }

    [TestMethod]
    public void AjouterFacture_HeureInvalide_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() =>
            mois.AjouterFacture(4, 10f, new DateTime(2026, 1, 3), new TimeSpan(24, 0, 0)));
        Assert.ThrowsExactly<ArgumentException>(() =>
            mois.AjouterFacture(4, 10f, new DateTime(2026, 1, 3), new TimeSpan(-1, 0, 0)));
    }

    [TestMethod]
    public void AjouterFacture_DateHorsMois_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(4, 10f, new DateTime(2026, 2, 5)));
    }

    [TestMethod]
    public void AjouterFacture_DateImplausible_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(4, 10f, new DateTime(9999, 1, 5)));
    }

    [TestMethod]
    public void AjouterFacture_MoisNomInconnu_SansDate_LèveArgumentException()
    {
        var mois = new MonthBudget("Trece");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(4, 10f));
    }

    [TestMethod]
    public void AjouterFacture_MoisNomInconnu_AvecDate_LèveArgumentException()
    {
        var mois = new MonthBudget("Trece");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(4, 10f, new DateTime(2026, 1, 5)));
    }

    [TestMethod]
    public void AjouterFacture_DateNulleAvecHeure_UtiliseLaDateParDéfautEtGardeLHeure()
    {
        var mois = new MonthBudget(NomsMois[DateTime.Today.Month - 1]);
        var heure = new TimeSpan(14, 0, 0);

        mois.AjouterFacture(4, 10f, null, heure);

        Assert.HasCount(1, mois.Factures);
        Assert.AreEqual(DateTime.Today, mois.Factures[0].Date);
        Assert.AreEqual(heure, mois.Factures[0].Heure);
    }

    [TestMethod]
    public void AjouterFacture_DateAvecComposanteHoraire_EstNormaliséeSansHeureDeJour()
    {
        var mois = new MonthBudget("Janvier");

        mois.AjouterFacture(4, 10f, new DateTime(DateTime.Today.Year, 1, 10, 15, 30, 0));

        Assert.AreEqual(new DateTime(DateTime.Today.Year, 1, 10), mois.Factures[0].Date);
    }

    [TestMethod]
    public void AjouterFacture_RecalculeTotalResteEtDépenseParCatégorie()
    {
        var mois = new MonthBudget("Avril", 500f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>());

        mois.AjouterFacture(1, 100f);
        mois.AjouterFacture(1, 50f);

        Assert.AreEqual(150f, mois.TotalExpenses, 0.001f);
        Assert.AreEqual(350f, mois.BudgetRemaining, 0.001f);
        Assert.AreEqual(150f, mois.ExpenseCategories[0].Item3, 0.001f);
    }

    [TestMethod]
    public void AjouterFacture_CatégorieInconnue_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(99, 10f));
    }

    [TestMethod]
    public void AjouterFacture_MontantNulOuNégatif_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(1, 0f));
        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(1, -5f));
    }

    [TestMethod]
    public void SupprimerFacture_SupprimeLaLigneEtRecalculeLesTotaux()
    {
        var mois = new MonthBudget("Janvier");
        mois.AjouterFacture(1, 100f);
        mois.AjouterFacture(1, 50f);
        mois.AjouterFacture(2, 25f);

        mois.SupprimerFacture(2);

        Assert.HasCount(2, mois.Factures);
        Assert.AreEqual(125f, mois.TotalExpenses, 0.001f);
        Assert.AreEqual(100f, mois.ExpenseCategories[0].Item3, 0.001f, "Dépense Loyer après suppression");
        Assert.AreEqual(25f, mois.ExpenseCategories[1].Item3, 0.001f, "Dépense Eau après suppression");
    }

    [TestMethod]
    public void SupprimerFacture_IdInconnu_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");
        mois.AjouterFacture(1, 100f);

        Assert.ThrowsExactly<ArgumentException>(() => mois.SupprimerFacture(77));
    }

    [TestMethod]
    public void SupprimerFacture_SuppriméeDéfinitivement()
    {
        var mois = new MonthBudget("Janvier");
        mois.AjouterFacture(1, 100f);

        mois.SupprimerFacture(1);

        Assert.IsEmpty(mois.Factures);
        Assert.AreEqual(0f, mois.TotalExpenses);
        Assert.AreEqual(0f, mois.BudgetRemaining);
    }

    [TestMethod]
    public void ConstructeurComplet_CatégoriesNull_LèveArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new MonthBudget("Janvier", 0f, null!, Enumerable.Empty<Facture>()));
    }

    [TestMethod]
    public void ConstructeurComplet_FacturesNull_LèveArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new MonthBudget("Janvier", 0f, Array.Empty<Tuple<int, string, float>>(), null!));
    }

    [TestMethod]
    public void ConstructeurComplet_IgnoreLaDépenseFournieEtRecalculeDepuisLesFactures()
    {
        var categories = new[]
        {
            new Tuple<int, string, float>(1, "Loyer", 5000f),
            new Tuple<int, string, float>(2, "Eau", 999f)
        };
        var factures = new[]
        {
            new Facture(1, 1, 120f, new DateTime(DateTime.Today.Year, 1, 3))
        };

        var mois = new MonthBudget("Janvier", 500f, categories, factures);

        Assert.AreEqual(120f, mois.ExpenseCategories[0].Item3, 0.001f, "La dépense fournie doit être ignorée au profit des factures");
        Assert.AreEqual(0f, mois.ExpenseCategories[1].Item3, 0.001f);
        Assert.AreEqual(120f, mois.TotalExpenses, 0.001f);
        Assert.AreEqual(380f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void ConstructeurComplet_RevenueNaN_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthBudget("Janvier", float.NaN, Array.Empty<Tuple<int, string, float>>(), Enumerable.Empty<Facture>()));
    }

    [TestMethod]
    public void ConstructeurComplet_RevenueInfini_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthBudget("Janvier", float.PositiveInfinity, Array.Empty<Tuple<int, string, float>>(), Enumerable.Empty<Facture>()));
        Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthBudget("Janvier", float.NegativeInfinity, Array.Empty<Tuple<int, string, float>>(), Enumerable.Empty<Facture>()));
    }

    [TestMethod]
    public void ConstructeurComplet_RevenueNégatifAutorisé_EstAccepté()
    {
        var mois = new MonthBudget("Janvier", -50f, Array.Empty<Tuple<int, string, float>>(), Enumerable.Empty<Facture>());

        Assert.AreEqual(-50f, mois.Revenue);
        Assert.AreEqual(-50f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void ChangerRevenue_ModifieLeRevenueEtLeReste()
    {
        var mois = new MonthBudget("Janvier", 100f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>());
        mois.AjouterFacture(1, 25f);

        mois.ChangerRevenue(200f);

        Assert.AreEqual(200f, mois.Revenue);
        Assert.AreEqual(175f, mois.BudgetRemaining, 0.001f);
        Assert.AreEqual(25f, mois.TotalExpenses, 0.001f, "Le total des dépenses ne doit pas changer");
    }

    [TestMethod]
    public void ChangerRevenue_RevenueNaN_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() => mois.ChangerRevenue(float.NaN));
    }

    [TestMethod]
    public void ChangerRevenue_RevenueInfini_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier");

        Assert.ThrowsExactly<ArgumentException>(() => mois.ChangerRevenue(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentException>(() => mois.ChangerRevenue(float.NegativeInfinity));
    }

    [TestMethod]
    public void ChangerRevenue_RevenueNégatif_EstAccepté()
    {
        var mois = new MonthBudget("Janvier", 100f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>());

        mois.ChangerRevenue(-50f);

        Assert.AreEqual(-50f, mois.Revenue);
        Assert.AreEqual(-50f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void ChangerRevenue_RevenueInvalide_LaisseRevenueEtResteInchangés()
    {
        var mois = new MonthBudget("Janvier", 100f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>());
        mois.AjouterFacture(1, 25f);
        Assert.AreEqual(75f, mois.BudgetRemaining, 0.001f);

        Assert.ThrowsExactly<ArgumentException>(() => mois.ChangerRevenue(float.NaN));
        Assert.AreEqual(100f, mois.Revenue, "La validation précède toute mutation");
        Assert.AreEqual(75f, mois.BudgetRemaining, 0.001f, "Le reste ne change pas si le revenue est refusé");
        Assert.AreEqual(25f, mois.TotalExpenses, 0.001f);

        Assert.ThrowsExactly<ArgumentException>(() => mois.ChangerRevenue(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentException>(() => mois.ChangerRevenue(float.NegativeInfinity));
        Assert.AreEqual(100f, mois.Revenue);
        Assert.AreEqual(75f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void ChangerRevenue_SurMoisAvecFactures_NeModifieQueLeRevenueEtLeReste()
    {
        var mois = new MonthBudget("Janvier", 100f,
            new[]
            {
                new Tuple<int, string, float>(1, "Loyer", 0f),
                new Tuple<int, string, float>(2, "Eau", 0f)
            },
            new[]
            {
                new Facture(1, 1, 25f, new DateTime(DateTime.Today.Year, 1, 3)),
                new Facture(2, 1, 50f, new DateTime(DateTime.Today.Year, 1, 10)),
                new Facture(3, 2, 75f, new DateTime(DateTime.Today.Year, 1, 15))
            });
        Assert.AreEqual(-50f, mois.BudgetRemaining, 0.001f);

        mois.ChangerRevenue(300f);

        Assert.AreEqual(300f, mois.Revenue);
        Assert.AreEqual(150f, mois.TotalExpenses, 0.001f, "Le total des dépenses ne doit pas changer");
        Assert.AreEqual(150f, mois.BudgetRemaining, 0.001f);
        Tuple<int, string, float>[] categories = mois.ExpenseCategories;
        Assert.AreEqual(75f, categories[0].Item3, 0.001f, "Dépense Loyer inchangée");
        Assert.AreEqual(75f, categories[1].Item3, 0.001f, "Dépense Eau inchangée");
        Assert.HasCount(3, mois.Factures);
    }

    [TestMethod]
    public void ChangerRevenue_RevenueNul_LeResteDevientLOpposéDuTotal()
    {
        var mois = new MonthBudget("Janvier", 100f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>());
        mois.AjouterFacture(1, 40f);

        mois.ChangerRevenue(0f);

        Assert.AreEqual(0f, mois.Revenue);
        Assert.AreEqual(-40f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void ChangerRevenue_RevenueAuxBornesFloat_EstAccepté()
    {
        var mois = new MonthBudget("Janvier", 0f,
            Array.Empty<Tuple<int, string, float>>(),
            Enumerable.Empty<Facture>());

        mois.ChangerRevenue(float.MaxValue);
        Assert.AreEqual(float.MaxValue, mois.Revenue);
        Assert.AreEqual(float.MaxValue, mois.BudgetRemaining);

        mois.ChangerRevenue(float.MinValue);
        Assert.AreEqual(float.MinValue, mois.Revenue);
        Assert.AreEqual(float.MinValue, mois.BudgetRemaining);
    }

    [TestMethod]
    public void ChangerRevenue_ResteDébordant_LèveInvalidDataException()
    {
        var mois = new MonthBudget("Janvier", 0f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            new[] { new Facture(1, 1, float.MaxValue, new DateTime(DateTime.Today.Year, 1, 3)) });
        Assert.AreEqual(-float.MaxValue, mois.BudgetRemaining, 0.001f);

        Assert.ThrowsExactly<InvalidDataException>(() => mois.ChangerRevenue(-float.MaxValue));

        Assert.AreEqual(0f, mois.Revenue, "Le revenue ne doit pas être modifié si le reste déborde");
        Assert.AreEqual(-float.MaxValue, mois.BudgetRemaining, 0.001f, "Le reste (déjà à la borne) doit rester inchangé");
    }

    [TestMethod]
    public void ConstructeurComplet_FactureRéférençantCatégorieInconnue_LèveArgumentException()
    {
        var categories = new[] { new Tuple<int, string, float>(1, "Loyer", 0f) };
        var factures = new[] { new Facture(1, 99, 10f, new DateTime(2026, 1, 3)) };

        Assert.ThrowsExactly<ArgumentException>(() => new MonthBudget("Janvier", 100f, categories, factures));
    }

    [TestMethod]
    public void AjouterCatégorie_SurMoisSansCatégorie_RepartDeLIdUn()
    {
        var mois = new MonthBudget("Janvier", 0f, Array.Empty<Tuple<int, string, float>>(), Enumerable.Empty<Facture>());

        mois.AjouterCategorie("Assurance");

        Assert.HasCount(1, mois.ExpenseCategories);
        Assert.AreEqual(1, mois.ExpenseCategories[0].Item1);
        Assert.AreEqual("Assurance", mois.ExpenseCategories[0].Item2);
        Assert.AreEqual(0f, mois.ExpenseCategories[0].Item3, 0.001f);
    }

    [TestMethod]
    public void AjouterCatégorie_RetourneUneCopie_LaMutationExterneNImpactePasLeMois()
    {
        var mois = new MonthBudget("Janvier");

        Tuple<int, string, float>[] copie = mois.ExpenseCategories;
        copie[0] = new Tuple<int, string, float>(99, "Faux", 999f);

        mois.AjouterFacture(1, 10f);

        Assert.HasCount(7, mois.ExpenseCategories);
        Assert.IsFalse(mois.ExpenseCategories.Any(categorie => categorie.Item1 == 99));
        Assert.AreEqual(10f, mois.ExpenseCategories[0].Item3, 0.001f);
    }

    [TestMethod]
    public void AjouterFacture_AprèsSuppression_RepartDeLIdMaxPlusUn()
    {
        var mois = new MonthBudget("Janvier");
        mois.AjouterFacture(1, 10f);
        mois.AjouterFacture(1, 20f);
        mois.SupprimerFacture(1);

        mois.AjouterFacture(1, 30f);

        Assert.HasCount(2, mois.Factures);
        Assert.AreEqual(3, mois.Factures[^1].Id, "L'id ne doit pas réutiliser un id supprimé");
    }

    [TestMethod]
    public void AjouterFacture_BudgetDépassé_ResteNégatif()
    {
        var mois = new MonthBudget("Janvier", 100f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>());

        mois.AjouterFacture(1, 150f);

        Assert.AreEqual(150f, mois.TotalExpenses, 0.001f);
        Assert.AreEqual(-50f, mois.BudgetRemaining, 0.001f, "Le reste peut être négatif si le budget est dépassé");
    }

    [TestMethod]
    public void ConstructeurParDéfaut_ExposeLAnnéeCourante()
    {
        var mois = new MonthBudget("Janvier");

        Assert.AreEqual(DateTime.Today.Year, mois.Annee);
    }

    [TestMethod]
    public void ConstructeurAvecAnnée_ExposeLAnnéeFournie()
    {
        var mois = new MonthBudget("Janvier", 0f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>(), 2025);

        Assert.AreEqual(2025, mois.Annee);
    }

    [TestMethod]
    public void ConstructeurComplet_FactureDUneAutreAnnée_LèveArgumentException()
    {
        var categories = new[]
        {
            new Tuple<int, string, float>(1, "Loyer", 0f),
            new Tuple<int, string, float>(2, "Eau", 0f)
        };
        var factures = new[]
        {
            new Facture(1, 1, 200f, new DateTime(2027, 1, 3))
        };

        Assert.ThrowsExactly<ArgumentException>(() =>
            new MonthBudget("Janvier", 1000f, categories, factures, 2026));
    }

    [TestMethod]
    public void AjouterFacture_AvecDateDUneAutreAnnée_LèveArgumentException()
    {
        var mois = new MonthBudget("Janvier", 0f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>(), 2025);

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(1, 10f, new DateTime(2026, 1, 5)));
        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(1, 10f, new DateTime(2025, 2, 5)));
    }

    [TestMethod]
    public void AjouterFacture_SansDate_AutreAnnéeMêmeMoisCourant_UtilisePremierDuMoisDeLAnnée()
    {
        // Le mois correspond au mois courant mais l'année du mois est différente :
        // la date par défaut ne doit PAS être aujourd'hui, mais le 1er du mois dans cette année.
        string nomMoisCourant = NomsMois[DateTime.Today.Month - 1];
        int autreAnnée = DateTime.Today.Year + 1;
        var mois = new MonthBudget(nomMoisCourant, 0f,
            new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
            Enumerable.Empty<Facture>(), autreAnnée);

        mois.AjouterFacture(1, 10f);

        Assert.HasCount(1, mois.Factures);
        Assert.AreEqual(new DateTime(autreAnnée, DateTime.Today.Month, 1), mois.Factures[0].Date);
    }
}