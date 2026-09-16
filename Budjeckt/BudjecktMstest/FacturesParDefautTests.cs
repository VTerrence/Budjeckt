namespace Budjeckt.Tests;

using Bud = global::Budjeckt.Budjeckt;

/// <summary>
/// Tests des factures par défaut (récurrentes) : enregistrement, désactivation, application
/// automatique dans les mois suivants (sans rétroactivité), idempotence et sérialisation JSON.
/// </summary>
[TestClass]
public class FacturesParDefautTests
{
    private static readonly string[] NomsMois =
    {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    [TestMethod]
    public void AjouterDefaut_EnregistreUnDéfautActif()
    {
        var budjeckt = new Bud("2026");

        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);

        Assert.HasCount(1, budjeckt.FacturesParDefaut);
        Assert.AreEqual("Loyer", budjeckt.FacturesParDefaut[0].NomCategorie);
        Assert.AreEqual(500f, budjeckt.FacturesParDefaut[0].Montant, 0.001f);
        Assert.AreEqual(3, budjeckt.FacturesParDefaut[0].MoisCreation);
        Assert.IsTrue(budjeckt.FacturesParDefaut[0].EstActive);
    }

    [TestMethod]
    public void AjouterDefaut_NomVide_LèveArgumentException()
    {
        var budjeckt = new Bud("2026");

        Assert.ThrowsExactly<ArgumentException>(() => budjeckt.AjouterFactureParDefaut("   ", 10f, 1));
    }

    [TestMethod]
    public void AjouterDefaut_MontantInvalide_LèveArgumentException()
    {
        var budjeckt = new Bud("2026");

        Assert.ThrowsExactly<ArgumentException>(() => budjeckt.AjouterFactureParDefaut("Loyer", 0f, 1));
        Assert.ThrowsExactly<ArgumentException>(() => budjeckt.AjouterFactureParDefaut("Loyer", -5f, 1));
    }

    [TestMethod]
    public void AjouterDefaut_MoisDeCréationHorsPlage_LèveArgumentException()
    {
        var budjeckt = new Bud("2026");

        Assert.ThrowsExactly<ArgumentException>(() => budjeckt.AjouterFactureParDefaut("Loyer", 10f, 0));
        Assert.ThrowsExactly<ArgumentException>(() => budjeckt.AjouterFactureParDefaut("Loyer", 10f, 13));
    }

    [TestMethod]
    public void AjouterDefaut_CatégorieCagnotte_LèveArgumentException()
    {
        var budjeckt = new Bud("2026");

        Assert.ThrowsExactly<ArgumentException>(() => budjeckt.AjouterFactureParDefaut("Cagnotte", 10f, 1));
    }

    [TestMethod]
    public void AjouterDefaut_DéfautIdentique_RéactiveAuLieuDeDupliquer()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
        budjeckt.DesactiverFactureParDefaut("Loyer", 500f);

        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);

        Assert.HasCount(1, budjeckt.FacturesParDefaut);
        Assert.IsTrue(budjeckt.FacturesParDefaut[0].EstActive);
    }

    [TestMethod]
    public void Appliquer_ReproduitLeDéfautDansLeMoisDeCréationEtLesSuivants()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);

        int creeMars = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[2]);

        Assert.AreEqual(1, creeMars);
        Assert.HasCount(1, budjeckt.Months[2].Factures);
        Assert.AreEqual(500f, budjeckt.Months[2].Factures[0].Montant, 0.001f);
        Assert.IsTrue(budjeckt.Months[2].Factures[0].EstParDefaut);
        Assert.AreEqual(3, budjeckt.Months[2].Factures[0].Date.Month, "La date doit appartenir au mois cible");
    }

    [TestMethod]
    public void Appliquer_NeRéproduitPasRétroactivementDansLesMoisAntérieurs()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);

        Assert.AreEqual(0, budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[0]), "Janvier est antérieur à Mars");
        Assert.AreEqual(0, budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[1]), "Février est antérieur à Mars");
        Assert.IsEmpty(budjeckt.Months[0].Factures);
        Assert.IsEmpty(budjeckt.Months[1].Factures);
    }

    [TestMethod]
    public void Appliquer_EstIdempotent()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
        MonthBudget mars = budjeckt.Months[2];

        budjeckt.AppliquerFacturesParDefaut(mars);
        int deuxieme = budjeckt.AppliquerFacturesParDefaut(mars);

        Assert.AreEqual(0, deuxieme);
        Assert.HasCount(1, mars.Factures);
    }

    [TestMethod]
    public void Appliquer_IgnoreLaCatégorieAbsenteDuMois()
    {
        var budjeckt = new Bud("2026");
        // La catégorie « Assurance » n'existe pas dans les mois par défaut.
        budjeckt.AjouterFactureParDefaut("Assurance", 50f, 1);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[5]);

        Assert.AreEqual(0, cree);
        Assert.IsEmpty(budjeckt.Months[5].Factures);
    }

    [TestMethod]
    public void Appliquer_IgnoreLesDéfautsDésactivés()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
        budjeckt.DesactiverFactureParDefaut("Loyer", 500f);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[5]);

        Assert.AreEqual(0, cree);
        Assert.IsEmpty(budjeckt.Months[5].Factures);
    }

    [TestMethod]
    public void Appliquer_PlusieursDéfautsDUneMêmeCatégorie_SontTousReproduits()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Alimentation", 300f, 1);
        budjeckt.AjouterFactureParDefaut("Alimentation", 40f, 1);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[0]);

        Assert.AreEqual(2, cree);
        Assert.HasCount(2, budjeckt.Months[0].Factures);
    }

    [TestMethod]
    public void Appliquer_NeReproduitQueLesDéfautsAppliquésAUneSeuleFoisParCouple()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 1);
        budjeckt.AjouterFactureParDefaut("Loyer", 900f, 1);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[1]);

        Assert.AreEqual(2, cree);
        Assert.HasCount(2, budjeckt.Months[1].Factures);
    }

    [TestMethod]
    public void Appliquer_MoisInconnu_NePlantePasEtNeCréeRien()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 1);

        int cree = budjeckt.AppliquerFacturesParDefaut(new MonthBudget("Brume"));

        Assert.AreEqual(0, cree);
    }

    [TestMethod]
    public void Appliquer_Null_LèveArgumentNullException()
    {
        var budjeckt = new Bud("2026");

        Assert.ThrowsExactly<ArgumentNullException>(() => budjeckt.AppliquerFacturesParDefaut(null!));
    }

    [TestMethod]
    public void Desactiver_ArrêteLaReproductionSansSupprimerLesCopiesExistantes()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
        budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[2]);

        budjeckt.DesactiverFactureParDefaut("Loyer", 500f);

        Assert.HasCount(1, budjeckt.Months[2].Factures, "La copie déjà matérialisée reste en place");
        Assert.IsFalse(budjeckt.FacturesParDefaut[0].EstActive);
        Assert.AreEqual(0, budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[4]));
    }

    [TestMethod]
    public void Desactiver_AucunDéfautCorrespondant_NeModifieRien()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);

        budjeckt.DesactiverFactureParDefaut("Loyer", 900f);

        Assert.IsTrue(budjeckt.FacturesParDefaut[0].EstActive);
    }

    [TestMethod]
    public void DesactiverDeCategorie_DésactiveTousLesDéfautsDeLaCatégorie()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 1);
        budjeckt.AjouterFactureParDefaut("Loyer", 900f, 1);
        budjeckt.AjouterFactureParDefaut("Eau", 30f, 1);

        budjeckt.DesactiverFacturesParDefautDeCategorie("loyer");

        Assert.IsFalse(budjeckt.FacturesParDefaut[0].EstActive);
        Assert.IsFalse(budjeckt.FacturesParDefaut[1].EstActive);
        Assert.IsTrue(budjeckt.FacturesParDefaut[2].EstActive);
    }

    [TestMethod]
    public void SauvegardeEtRechargement_RestaureLesDéfautsEtLesMarques()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
            budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[2]);
            budjeckt.SauvegarderJson(chemin);

            Bud charge = new("2026");
            charge.ChargerJson(chemin);

            Assert.HasCount(1, charge.FacturesParDefaut);
            Assert.AreEqual("Loyer", charge.FacturesParDefaut[0].NomCategorie);
            Assert.AreEqual(500f, charge.FacturesParDefaut[0].Montant, 0.001f);
            Assert.AreEqual(3, charge.FacturesParDefaut[0].MoisCreation);
            Assert.IsTrue(charge.FacturesParDefaut[0].EstActive);
            Assert.HasCount(1, charge.Months[2].Factures);
            Assert.IsTrue(charge.Months[2].Factures[0].EstParDefaut, "Le marquage par défaut doit survivre au rechargement");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegardeEtRechargement_PersisteLÉtatDésactivé()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
            budjeckt.DesactiverFactureParDefaut("Loyer", 500f);
            budjeckt.SauvegarderJson(chemin);

            Bud charge = new("2026");
            charge.ChargerJson(chemin);

            Assert.HasCount(1, charge.FacturesParDefaut);
            Assert.IsFalse(charge.FacturesParDefaut[0].EstActive);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void Chargement_SansListeDeDéfauts_ChargeVideEtSansErreur()
    {
        string json = CreerJsonAnnee();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, json);
            Bud budjeckt = new("2026");
            budjeckt.ChargerJson(chemin);

            Assert.IsEmpty(budjeckt.FacturesParDefaut);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void Chargement_DéfautMontantNonPositif_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[{\"Categorie\":\"Loyer\",\"Montant\":0,\"MoisCreation\":3,\"Active\":true}]"));
    }

    [TestMethod]
    public void Chargement_DéfautMoisHorsPlage_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[{\"Categorie\":\"Loyer\",\"Montant\":10,\"MoisCreation\":13,\"Active\":true}]"));
    }

    [TestMethod]
    public void Chargement_DéfautCatégorieVide_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[{\"Categorie\":\"  \",\"Montant\":10,\"MoisCreation\":3,\"Active\":true}]"));
    }

    [TestMethod]
    public void Chargement_DéfautDoublon_LèveInvalidDataException()
    {
        string defauts = "[{\"Categorie\":\"Loyer\",\"Montant\":10,\"MoisCreation\":3,\"Active\":true}," +
                         "{\"Categorie\":\"loyer\",\"Montant\":10,\"MoisCreation\":4,\"Active\":true}]";
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: defauts));
    }

    [TestMethod]
    public void Chargement_DéfautCatégorieCagnotte_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[{\"Categorie\":\"Cagnotte\",\"Montant\":10,\"MoisCreation\":3,\"Active\":true}]"));
    }

    [TestMethod]
    public void AjouterFacture_EstParDefautSurCatégorieCagnotte_LèveArgumentException_SansMutation()
    {
        var categories = new[]
        {
            new Tuple<int, string, float>(1, "Loyer", 0f),
            new Tuple<int, string, float>(2, "Cagnotte", 0f)
        };
        var mois = new MonthBudget("Janvier", 2000f, categories, Enumerable.Empty<Facture>());

        Assert.ThrowsExactly<ArgumentException>(() => mois.AjouterFacture(2, 100f, null, null, estParDefaut: true));

        Assert.IsEmpty(mois.Factures, "Aucune facture ne doit être créée sur une rejet métier");
        Assert.AreEqual(2000f, mois.BudgetRemaining, 0.001f);
    }

    [TestMethod]
    public void AjouterFacture_EstParDefautSurCatégorieOrdinaire_CréeLaFactureMarquée()
    {
        var mois = new MonthBudget("Janvier");

        mois.AjouterFacture(1, 500f, null, null, estParDefaut: true);

        Assert.HasCount(1, mois.Factures);
        Assert.IsTrue(mois.Factures[0].EstParDefaut);
    }

    [TestMethod]
    public void Chargement_MouvementCagnotteMarquéParDéfaut_LèveInvalidDataException()
    {
        string json = CreerJsonAnneeAvecMois(
            "{\"Nom\":\"Janvier\",\"Revenue\":0,\"MontantCagnotte\":0," +
            "\"Categories\":[{\"Id\":1,\"Nom\":\"Cagnotte\"}]," +
            "\"Factures\":[{\"Id\":1,\"IdCategorie\":1,\"Montant\":50,\"Date\":\"2026-01-05\",\"EstParDefaut\":true}]}");

        VerifierChargementInvalide(json);
    }

    [TestMethod]
    public void Appliquer_UneFactureManuelleDeMêmeMontant_NempêchePasLaReproduction()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 1);
        budjeckt.Months[3].AjouterFacture(1, 500f, new DateTime(2026, 4, 10), null);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[3]);

        Assert.AreEqual(1, cree, "Le défaut est reproduit même si une dépense manuelle identique existe");
        Assert.HasCount(2, budjeckt.Months[3].Factures);
    }

    [TestMethod]
    public void Appliquer_EstInsensibleÀLaCasseDuNomDeCatégorie()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("LOYER", 500f, 1);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[4]);

        Assert.AreEqual(1, cree);
        Assert.IsTrue(budjeckt.Months[4].Factures[0].EstParDefaut);
    }

    [TestMethod]
    public void Appliquer_StrictementAprèsLeMoisDeCréation_MarqueLaFactureReproduite()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);

        int cree = budjeckt.AppliquerFacturesParDefaut(budjeckt.Months[5]);

        Assert.AreEqual(1, cree);
        Assert.HasCount(1, budjeckt.Months[5].Factures);
        Assert.IsTrue(budjeckt.Months[5].Factures[0].EstParDefaut);
        Assert.AreEqual(6, budjeckt.Months[5].Factures[0].Date.Month);
    }

    [TestMethod]
    public void Desactiver_NomBlanc_NeFaitRien()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 1);

        budjeckt.DesactiverFactureParDefaut("   ", 500f);
        budjeckt.DesactiverFacturesParDefautDeCategorie("   ");

        Assert.IsTrue(budjeckt.FacturesParDefaut[0].EstActive);
    }

    [TestMethod]
    public void AjouterDefaut_Réactivation_InsensibleÀLaCasse()
    {
        var budjeckt = new Bud("2026");
        budjeckt.AjouterFactureParDefaut("Loyer", 500f, 3);
        budjeckt.DesactiverFactureParDefaut("LOYER", 500f);

        budjeckt.AjouterFactureParDefaut("loyer", 500f, 3);

        Assert.HasCount(1, budjeckt.FacturesParDefaut);
        Assert.IsTrue(budjeckt.FacturesParDefaut[0].EstActive);
    }

    [TestMethod]
    public void Chargement_ListeDeDéfautsVide_ChargeSansErreur()
    {
        string json = CreerJsonAnnee(DefautsJson: "[]");

        VerifierChargement(json, budjeckt => Assert.IsEmpty(budjeckt.FacturesParDefaut));
    }

    [TestMethod]
    public void Chargement_DéfautDébordantEnInfini_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[{\"Categorie\":\"Loyer\",\"Montant\":1e39,\"MoisCreation\":3,\"Active\":true}]"));
    }

    [TestMethod]
    public void Chargement_DéfautMontantNégatif_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[{\"Categorie\":\"Loyer\",\"Montant\":-5,\"MoisCreation\":3,\"Active\":true}]"));
    }

    [TestMethod]
    public void Chargement_EntréeNullDansLaListeDesDéfauts_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnee(DefautsJson: "[null]"));
    }

    /// <summary>
    /// Construit un JSON d'année valide (12 mois, une catégorie « Loyer » chacun) avec une
    /// liste de factures par défaut optionnelle insérée avant la fermeture de l'objet racine.
    /// </summary>
    private static string CreerJsonAnnee(string? annee = "2026", string? DefautsJson = null)
    {
        string mois = string.Join(",", NomsMois.Select(nom =>
            $"{{\"Nom\":\"{nom}\",\"Revenue\":0,\"MontantCagnotte\":0," +
            "\"Categories\":[{\"Id\":1,\"Nom\":\"Loyer\"}],\"Factures\":[]}"));

        string defauts = DefautsJson is null
            ? ""
            : $",\"FacturesParDefaut\":{DefautsJson}";

        return $"{{\"Annee\":\"{annee}\",\"Mois\":[{mois}]{defauts}}}";
    }

    /// <summary>
    /// Construit un JSON d'année valide dont le premier mois (Janvier) est remplacé par le
    /// contenu JSON fourni (les 11 autres mois restent minimaux), pour tester la validation
    /// des factures par défaut au chargement.
    /// </summary>
    private static string CreerJsonAnneeAvecMois(string premierMoisJson)
    {
        string mois = premierMoisJson + "," + string.Join(",", NomsMois.Skip(1).Select(nom =>
            $"{{\"Nom\":\"{nom}\",\"Revenue\":0,\"MontantCagnotte\":0," +
            "\"Categories\":[{\"Id\":1,\"Nom\":\"Loyer\"}],\"Factures\":[]}"));

        return $"{{\"Annee\":\"2026\",\"Mois\":[{mois}]}}";
    }

    /// <summary>
    /// Écrit un JSON dans un fichier temporaire, le charge, exécute les vérifications puis
    /// supprime le fichier temporaire.
    /// </summary>
    private static void VerifierChargement(string json, Action<Bud> vérifier)
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, json);
            Bud budjeckt = new();
            budjeckt.ChargerJson(chemin);
            vérifier(budjeckt);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    /// <summary>
    /// Écrit un JSON dans un fichier temporaire, le charge, puis vérifie que le chargement
    /// lève une <see cref="InvalidDataException"/>. Le fichier temporaire est supprimé ensuite.
    /// </summary>
    private static void VerifierChargementInvalide(string json)
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, json);
            Bud budjeckt = new();
            Assert.ThrowsExactly<InvalidDataException>(() => budjeckt.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    private static string CreerCheminTemporaire()
    {
        return Path.Combine(Path.GetTempPath(), $"budjeckt_defauts_{Guid.NewGuid():N}.json");
    }

    private static void SupprimerFichier(string chemin)
    {
        if (File.Exists(chemin))
        {
            File.Delete(chemin);
        }
    }
}