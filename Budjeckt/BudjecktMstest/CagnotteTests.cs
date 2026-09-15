namespace Budjeckt.Tests;

[TestClass]
public class CagnotteTests
{
    [TestMethod]
    public void ConstructeurParDéfaut_SoldeNul()
    {
        Assert.AreEqual(0f, new Cagnotte().Solde);
    }

    [TestMethod]
    public void Deposer_AugmenteLeSolde()
    {
        var cagnotte = new Cagnotte();

        cagnotte.Deposer(100f);

        Assert.AreEqual(100f, cagnotte.Solde, 0.001f);
    }

    [TestMethod]
    public void Deposer_PlusieursDépôts_Cumulent()
    {
        var cagnotte = new Cagnotte();
        cagnotte.Deposer(50f);
        cagnotte.Deposer(30.5f);
        cagnotte.Deposer(19.5f);

        Assert.AreEqual(100f, cagnotte.Solde, 0.001f);
    }

    [TestMethod]
    public void Deposer_MontantInvalide_LèveArgumentException_SansMutation()
    {
        var cagnotte = new Cagnotte();

        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Deposer(0f));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Deposer(-10f));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Deposer(float.NaN));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Deposer(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Deposer(float.NegativeInfinity));

        Assert.AreEqual(0f, cagnotte.Solde, 0.001f);
    }

    [TestMethod]
    public void Deposer_DébordementFloat_LèveInvalidDataException_SansMutation()
    {
        var cagnotte = new Cagnotte();
        cagnotte.Deposer(float.MaxValue);

        Assert.ThrowsExactly<InvalidDataException>(() => cagnotte.Deposer(float.MaxValue));
        Assert.AreEqual(float.MaxValue, cagnotte.Solde, 0.001f, "Un dépôt refusé ne doit pas modifier le solde");
    }

    [TestMethod]
    public void Retirer_DiminueLeSolde()
    {
        var cagnotte = new Cagnotte();
        cagnotte.Deposer(100f);

        cagnotte.Retirer(30f);

        Assert.AreEqual(70f, cagnotte.Solde, 0.001f);
    }

    [TestMethod]
    public void Retirer_JusquAuSolde_LeRendNul()
    {
        var cagnotte = new Cagnotte();
        cagnotte.Deposer(100f);

        cagnotte.Retirer(100f);

        Assert.AreEqual(0f, cagnotte.Solde, 0.001f);
    }

    [TestMethod]
    public void Retirer_MontantSupérieurAuSolde_LèveArgumentException_SansMutation()
    {
        var cagnotte = new Cagnotte();
        cagnotte.Deposer(50f);

        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(50.01f));
        Assert.AreEqual(50f, cagnotte.Solde, 0.001f, "Un retrait refusé ne doit pas modifier le solde");
    }

    [TestMethod]
    public void Retirer_MontantInvalide_LèveArgumentException()
    {
        var cagnotte = new Cagnotte();

        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(0f));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(-5f));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(float.NaN));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(float.NegativeInfinity));
    }

    [TestMethod]
    public void Retirer_NeJamaiscréerDeSoldeNégatif()
    {
        // La garde sur le plafond suffit : le solde ne peut pas devenir négatif par construction.
        Cagnotte cagnotte = new();
        cagnotte.Deposer(10f);

        Assert.ThrowsExactly<ArgumentException>(() => cagnotte.Retirer(10.01f));
        Assert.IsGreaterThanOrEqualTo(0f, cagnotte.Solde);
    }

    [TestMethod]
    public void ChargerJson_FichierManquant_ConserveLeSoldeNul()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            cagnotte.ChargerJson(chemin);

            Assert.AreEqual(0f, cagnotte.Solde, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_FichierManquant_ConserveLeSoldeCourant()
    {
        Cagnotte cagnotte = new();
        cagnotte.Deposer(123.45f);
        string chemin = CreerCheminTemporaire();
        try
        {
            cagnotte.ChargerJson(chemin);

            Assert.AreEqual(123.45f, cagnotte.Solde, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_RemplaceLeSoldeCourant()
    {
        // Le chargement doit remplacer le solde en mémoire par celui du fichier (pas une
        // addition) : recharger la cagnotte au démarrage ne doit pas cumuler deux fois le solde.
        Cagnotte cagnotte = new();
        cagnotte.Deposer(100f);
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{\"Solde\":250.5}");

            cagnotte.ChargerJson(chemin);

            Assert.AreEqual(250.5f, cagnotte.Solde, 0.001f,
                "Le solde du fichier doit remplacer le solde courant, sans cumul");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_ChargeLeSoldeDuFichier()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{\"Solde\":250.5}");

            cagnotte.ChargerJson(chemin);

            Assert.AreEqual(250.5f, cagnotte.Solde, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_SoldeNégatif_LèveInvalidDataException()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{\"Solde\":-1}");

            Assert.ThrowsExactly<InvalidDataException>(() => cagnotte.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_SoldeInfini_LèveInvalidDataException()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{\"Solde\":1e39}");

            Assert.ThrowsExactly<InvalidDataException>(() => cagnotte.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_JsonCorrompu_LèveInvalidDataException()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{{{solde pas json");

            Assert.ThrowsExactly<InvalidDataException>(() => cagnotte.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_JsonVide_LèveInvalidDataException()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "   ");

            Assert.ThrowsExactly<InvalidDataException>(() => cagnotte.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_FichierTropVolumineux_LèveInvalidDataException()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, new string(' ', (int)Cagnotte.TailleMaximaleFichier + 1));

            Assert.ThrowsExactly<InvalidDataException>(() => cagnotte.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_JsonSansCleSolde_ConserveLeSoldeNul()
    {
        Cagnotte cagnotte = new();
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{\"Autre\":42}");

            cagnotte.ChargerJson(chemin);

            Assert.AreEqual(0f, cagnotte.Solde, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderJson_CréeLeFichierPuisRechargeLeSolde()
    {
        Cagnotte cagnotte = new();
        cagnotte.Deposer(333.33f);
        string chemin = CreerCheminTemporaire();
        try
        {
            cagnotte.SauvegarderJson(chemin);

            Assert.IsTrue(File.Exists(chemin));

            Cagnotte rechargee = new();
            rechargee.ChargerJson(chemin);
            Assert.AreEqual(333.33f, rechargee.Solde, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderJson_CréeLesRépertoiresParents()
    {
        string repertoire = Path.Combine(Path.GetTempPath(), "budjeckt_tests", Guid.NewGuid().ToString("N"));
        string chemin = Path.Combine(repertoire, "sous_dossier", Cagnotte.NomFichier);
        try
        {
            new Cagnotte().SauvegarderJson(chemin);

            Assert.IsTrue(Directory.Exists(Path.GetDirectoryName(chemin)), "Le répertoire parent doit être créé");
            Assert.IsTrue(File.Exists(chemin));
        }
        finally
        {
            if (Directory.Exists(repertoire))
            {
                Directory.Delete(repertoire, recursive: true);
            }
        }
    }

    [TestMethod]
    public void SauvegarderJson_DépôtsEtRetraits_RechargentLeSoldeNet()
    {
        Cagnotte cagnotte = new();
        cagnotte.Deposer(500f);
        cagnotte.Deposer(200f);
        cagnotte.Retirer(50f);
        string chemin = CreerCheminTemporaire();
        try
        {
            cagnotte.SauvegarderJson(chemin);

            Cagnotte rechargee = new();
            rechargee.ChargerJson(chemin);
            Assert.AreEqual(650f, rechargee.Solde, 0.001f, "Le fichier doit porter le solde net après plusieurs opérations");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderJson_NeLaissePasDeFichierTemporaireDerrière()
    {
        Cagnotte cagnotte = new();
        cagnotte.Deposer(10f);
        string chemin = CreerCheminTemporaire();
        try
        {
            cagnotte.SauvegarderJson(chemin);

            Assert.IsFalse(File.Exists(chemin + ".tmp"), "L'écriture atomique ne doit pas laisser de .tmp");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    private static string CreerCheminTemporaire()
    {
        return Path.Combine(Path.GetTempPath(), $"budjeckt_cagnotte_{Guid.NewGuid():N}.json");
    }

    private static void SupprimerFichier(string chemin)
    {
        if (File.Exists(chemin))
        {
            File.Delete(chemin);
        }
    }
}