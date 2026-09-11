namespace Budjeckt.Tests;

using Bud = global::Budjeckt.Budjeckt;

[TestClass]
public class ArchivesBudjecktTests
{
    private string? _répertoireTemporaire;

    [TestInitialize]
    public void CréerRépertoireTemporaire()
    {
        _répertoireTemporaire = Path.Combine(Path.GetTempPath(), "budjeckt_archives", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_répertoireTemporaire);
    }

    [TestCleanup]
    public void SupprimerRépertoireTemporaire()
    {
        if (_répertoireTemporaire is not null && Directory.Exists(_répertoireTemporaire))
        {
            Directory.Delete(_répertoireTemporaire, recursive: true);
        }
    }

    [TestMethod]
    public void AnneeCourante_RetourneLAnnéeDuJour()
    {
        Assert.AreEqual(DateTime.Today.Year, ArchivesBudjeckt.AnneeCourante());
    }

    [TestMethod]
    public void SupprimerAnnee_SupprimeLeFichierEtRetourneTrue()
    {
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"));
        new Bud("2027").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2027.json"));

        bool supprimé = ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026);

        Assert.IsTrue(supprimé);
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2026.json")), "Le fichier de l'année doit disparaître");
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2027.json")), "Les autres années doivent rester intactes");
    }

    [TestMethod]
    public void SupprimerAnnee_AnnéeInexistante_RetourneFalse()
    {
        bool supprimé = ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026);

        Assert.IsFalse(supprimé);
    }

    [TestMethod]
    public void SupprimerAnnee_AnnéeHorsPlage_RetourneFalse()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-1850.json"), "{}");

        Assert.IsFalse(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 1850));
        Assert.IsFalse(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2200));
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-1850.json")), "Aucun fichier ne doit être supprimé hors plage");
    }

    [TestMethod]
    public void SupprimerAnnee_DossierInexistant_RetourneFalse()
    {
        string dossier = Path.Combine(Path.GetTempPath(), "dossier_inexistant_" + Guid.NewGuid());

        Assert.IsFalse(ArchivesBudjeckt.SupprimerAnnee(dossier, 2026));
    }

    [TestMethod]
    public void SupprimerAnnee_FichierVerrouillé_RetourneFalse()
    {
        string chemin = Path.Combine(_répertoireTemporaire!, "depenses-2026.json");
        new Bud("2026").SauvegarderJson(chemin);

        using (new FileStream(chemin, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.IsFalse(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026));
            Assert.IsTrue(File.Exists(chemin), "Un fichier verrouillé ne doit pas être supprimé");
        }
    }

    [TestMethod]
    public void SupprimerAnnee_FichierEnLectureSeule_RetourneFalse()
    {
        string chemin = Path.Combine(_répertoireTemporaire!, "depenses-2026.json");
        new Bud("2026").SauvegarderJson(chemin);
        File.SetAttributes(chemin, FileAttributes.ReadOnly);

        try
        {
            Assert.IsFalse(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026));
            Assert.IsTrue(File.Exists(chemin), "Un fichier en lecture seule ne doit pas être supprimé");
        }
        finally
        {
            File.SetAttributes(chemin, FileAttributes.Normal);
        }
    }

    [TestMethod]
    public void SupprimerAnnee_AnnéesBornes1900Et2100_Supprime()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-1900.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2100.json"), "{}");

        Assert.IsTrue(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 1900));
        Assert.IsTrue(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2100));
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-1900.json")));
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2100.json")));
    }

    [TestMethod]
    public void SupprimerAnnee_AnneesExistantesRefleteLaSuppression()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2025.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2027.json"), "{}");

        Assert.IsTrue(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026));

        Assert.IsTrue(new[] { 2027, 2025 }.SequenceEqual(ArchivesBudjeckt.AnneesExistantes(_répertoireTemporaire!)));
    }

    [TestMethod]
    public void SupprimerAnnee_DeuxièmeSuppression_RetourneFalse()
    {
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"));

        Assert.IsTrue(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026));
        Assert.IsFalse(ArchivesBudjeckt.SupprimerAnnee(_répertoireTemporaire!, 2026));
    }

    [TestMethod]
    public void AnneesExistantes_RépertoireManquant_ListeVide()
    {
        Assert.IsEmpty(ArchivesBudjeckt.AnneesExistantes(Path.Combine(Path.GetTempPath(), "dossier_inexistant_" + Guid.NewGuid())));
    }

    [TestMethod]
    public void AnneesExistantes_RépertoireVide_ListeVide()
    {
        Assert.IsEmpty(ArchivesBudjeckt.AnneesExistantes(_répertoireTemporaire!));
    }

    [TestMethod]
    public void AnneesExistantes_ListeLesAnnéesTrieesParOrdreDécroissant()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2024.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2025.json"), "{}");

        List<int> annees = ArchivesBudjeckt.AnneesExistantes(_répertoireTemporaire!);

        Assert.IsTrue(new[] { 2026, 2025, 2024 }.SequenceEqual(annees));
    }

    [TestMethod]
    public void AnneesExistantes_IgnoreLesFichiersNonConformes()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-abc.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-1899.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2101.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "autres.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2027.json"), "{}");

        List<int> annees = ArchivesBudjeckt.AnneesExistantes(_répertoireTemporaire!);

        Assert.IsTrue(new[] { 2027 }.SequenceEqual(annees));
    }

    [TestMethod]
    public void AnneesExistantes_AnnéesAuxBornesDeLaPlage_Listées()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-1900.json"), "{}");
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2100.json"), "{}");

        List<int> annees = ArchivesBudjeckt.AnneesExistantes(_répertoireTemporaire!);

        Assert.IsTrue(new[] { 2100, 1900 }.SequenceEqual(annees));
    }

    [TestMethod]
    public void OuvrirOuCreerAnnee_AnnéeAbsente_LaCréeEtLaSauvegarde()
    {
        Bud budjeckt = ArchivesBudjeckt.OuvrirOuCreerAnnee(_répertoireTemporaire!, 2027);

        Assert.AreEqual("2027", budjeckt.Annee);
        Assert.HasCount(12, budjeckt.Months);
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2027.json")), "Le fichier de l'année doit être créé sur disque");
    }

    [TestMethod]
    public void OuvrirOuCreerAnnee_AnnéeExistante_RechargeSesDonnées()
    {
        var original = new Bud("2026");
        original.Months = OuvrirDouzeMoisAvecFacture();
        original.SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"));

        Bud charge = ArchivesBudjeckt.OuvrirOuCreerAnnee(_répertoireTemporaire!, 2026);

        Assert.HasCount(1, charge.Months[0].Factures);
        Assert.AreEqual(100f, charge.Months[0].TotalExpenses, 0.001f);
        Assert.AreEqual("2026", charge.Annee);
    }

    [TestMethod]
    public void OuvrirOuCreerAnnee_FichierCorrompu_LèveInvalidDataException()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"), "{ pas du json ");

        Assert.ThrowsExactly<InvalidDataException>(() => ArchivesBudjeckt.OuvrirOuCreerAnnee(_répertoireTemporaire!, 2026));
    }

    [TestMethod]
    public void OuvrirOuCreerAnnee_AnnéeDéclaréeDifférente_LèveInvalidDataException()
    {
        // Le fichier depenses-2025.json déclare 2026 : le décalage doit être refusé.
        Bud original = new("2026");
        original.SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2025.json"));

        Assert.ThrowsExactly<InvalidDataException>(() => ArchivesBudjeckt.OuvrirOuCreerAnnee(_répertoireTemporaire!, 2025));
    }

    [TestMethod]
    public void OuvrirOuCreerAnnee_AnnéeDéclaréeAvecEspaces_EstAcceptée()
    {
        // L'année « 2026 » tapée avec des espaces dans le JSON est tolérée (validation Trim).
        string chemin = Path.Combine(_répertoireTemporaire!, "depenses-2026.json");
        new Bud("2026").SauvegarderJson(chemin);
        string contenu = File.ReadAllText(chemin).Replace("\"Annee\":\"2026\"", "\"Annee\":\" 2026 \"");
        File.WriteAllText(chemin, contenu);

        Bud charge = ArchivesBudjeckt.OuvrirOuCreerAnnee(_répertoireTemporaire!, 2026);

        Assert.HasCount(12, charge.Months);
    }

    [TestMethod]
    public void MigrerFichierHerite_FichierAbsent_RenvoieFaux()
    {
        Assert.IsFalse(ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!));
    }

    [TestMethod]
    public void MigrerFichierHerite_DéplaceVersDepensesAnnée()
    {
        Bud original = new("2026");
        original.SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses.json"));

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsTrue(migré);
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")), "Le fichier hérité doit être déplacé");
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2026.json")), "Le fichier doit être renommé selon l'année déclarée");
    }

    [TestMethod]
    public void MigrerFichierHerite_CibleExistante_RenommeEnLegacy()
    {
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"));
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses.json"));

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsTrue(migré);
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")));
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2026-legacy.json")), "L'ancien fichier doit être conservé en -legacy");
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2026.json")), "La cible existante ne doit pas être écrasée");
    }

    [TestMethod]
    public void MigrerFichierHerite_Illisible_NeDéplaceRien()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses.json"), "garbage");

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsFalse(migré);
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")), "Un fichier illisible doit être laissé en place");
    }

    [TestMethod]
    public void MigrerFichierHerite_AnnéeHorsPlage_NeDéplaceRien()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses.json"), "{\"Annee\":\"1850\",\"Mois\":[]}");

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsFalse(migré);
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")));
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-1850.json")), "Une année hors plage ne doit pas être déplacée");
    }

    [TestMethod]
    public void MigrerFichierHerite_AnnéeNonNumérique_NeDéplaceRien()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses.json"), "{\"Annee\":\"abc\",\"Mois\":[]}");

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsFalse(migré);
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")));
    }

    [TestMethod]
    public void MigrerFichierHerite_AnnéeAbsente_NeDéplaceRien()
    {
        File.WriteAllText(Path.Combine(_répertoireTemporaire!, "depenses.json"), "{\"Mois\":[]}");

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsFalse(migré);
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")));
    }

    [TestMethod]
    public void MigrerFichierHerite_LegacyDéjàPrésent_RenommeAvecHorodatage()
    {
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2026.json"));
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses-2026-legacy.json"));
        new Bud("2026").SauvegarderJson(Path.Combine(_répertoireTemporaire!, "depenses.json"));

        bool migré = ArchivesBudjeckt.MigrerFichierHerite(_répertoireTemporaire!);

        Assert.IsTrue(migré);
        Assert.IsFalse(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses.json")));
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2026.json")), "La cible originelle ne doit pas être écrasée");
        Assert.IsTrue(File.Exists(Path.Combine(_répertoireTemporaire!, "depenses-2026-legacy.json")), "Le -legacy existant doit être conservé");
        Assert.IsTrue(Directory.EnumerateFiles(_répertoireTemporaire!, "depenses-2026-legacy-*.json").Any(),
            "Le fichier hérité doit être conservé avec un suffixe horodaté");
    }

    private static MonthBudget[] OuvrirDouzeMoisAvecFacture()
    {
        string[] noms = {
            "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
            "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
        };
        MonthBudget[] mois = noms.Select(nom => new MonthBudget(nom)).ToArray();
        mois[0].AjouterFacture(1, 100f, new DateTime(2026, 1, 10));
        return mois;
    }
}