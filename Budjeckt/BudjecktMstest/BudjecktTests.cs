namespace Budjeckt.Tests;

using System.Globalization;
using Bud = global::Budjeckt.Budjeckt;

[TestClass]
public class BudjecktTests
{
    private static readonly string[] NomsMois =
    {
        "Janvier", "Février", "Mars", "Avril", "Mai", "Juin",
        "Juillet", "Août", "Septembre", "Octobre", "Novembre", "Décembre"
    };

    [TestMethod]
    public void ConstructeurParDéfaut_UtiliseLAnnéeCourante()
    {
        Bud budjeckt = new();

        Assert.AreEqual(DateTime.Now.Year.ToString(), budjeckt.Annee);
    }

    [TestMethod]
    public void ConstructeurParDéfaut_CréeLesDouzeMoisDeLAnnée()
    {
        Bud budjeckt = new();

        Assert.HasCount(12, budjeckt.Months);
        Assert.IsTrue(budjeckt.Months.Select(mois => mois.Nom).SequenceEqual(NomsMois));
    }

    [TestMethod]
    public void ConstructeurAvecAnnée_ConserveLeNomFourni()
    {
        Bud budjeckt = new("2024");

        Assert.AreEqual("2024", budjeckt.Annee);
    }

    [TestMethod]
    public void ConstructeurAvecAnnée_CréeLesMoisAvecCetteAnnée()
    {
        Bud budjeckt = new("2024");

        Assert.IsTrue(budjeckt.Months.All(mois => mois.Annee == 2024));
    }

    [TestMethod]
    public void NomFichierPourAnnee_GénèreLeNomAttendu()
    {
        Assert.AreEqual("depenses-2026.json", Bud.NomFichierPourAnnee(2026));
    }

    [TestMethod]
    public void ConstructeurAvecAnnéeVide_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Bud(" "));
    }

    [TestMethod]
    public void Months_SetPublic_RemplaceLeTableau()
    {
        Bud budjeckt = new();
        MonthBudget[] autresMois = { new("Janvier"), new("Février") };

        budjeckt.Months = autresMois;

        Assert.HasCount(2, budjeckt.Months);
    }

    [TestMethod]
    public void Months_SetNull_LèveArgumentNullException()
    {
        Bud budjeckt = new();

        Assert.ThrowsExactly<ArgumentNullException>(() => budjeckt.Months = null!);
    }

    [TestMethod]
    public void SauvegarderJson_CréeLeFichier()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.SauvegarderJson(chemin);

            Assert.IsTrue(File.Exists(chemin));
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
        string chemin = Path.Combine(repertoire, "sous_dossier", "depenses.json");
        try
        {
            new Bud("2026").SauvegarderJson(chemin);

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
    public void SauvegarderJson_MoisSansCatégories_LèveInvalidDataException_SansÉcriture()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            MonthBudget[] mois = CreerDouzeMois();
            mois[0] = new MonthBudget("Janvier", 0f, Array.Empty<Tuple<int, string, float>>(), Enumerable.Empty<Facture>());
            budjeckt.Months = mois;

            Assert.ThrowsExactly<InvalidDataException>(() => budjeckt.SauvegarderJson(chemin));

            Assert.IsFalse(File.Exists(chemin),
                "Un état mémoire non conforme au chargeur ne doit produire aucun fichier");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderJson_MoinsDeDouzeMois_LèveInvalidDataException_SansÉcriture()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.Months = CreerDouzeMois()[..11];

            Assert.ThrowsExactly<InvalidDataException>(() => budjeckt.SauvegarderJson(chemin));

            Assert.IsFalse(File.Exists(chemin),
                "Une année sans exactement 12 mois ne doit produire aucun fichier");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_FichierManquant_GardeLesDonnéesParDéfaut()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new();

            budjeckt.ChargerJson(chemin);

            Assert.AreEqual(DateTime.Now.Year.ToString(), budjeckt.Annee);
            Assert.HasCount(12, budjeckt.Months);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_JsonCorrompu_LèveInvalidDataException()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            File.WriteAllText(chemin, "{ pas un json valide ");

            Bud budjeckt = new();
            Assert.ThrowsExactly<InvalidDataException>(() => budjeckt.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_ContenuNull_LèveInvalidDataException()
    {
        VerifierChargementInvalide("null");
    }

    [TestMethod]
    public void ChargerJson_AnnéeAbsente_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(annee: null));
    }

    [TestMethod]
    public void ChargerJson_AnnéeDEspaces_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(annee: "   "));
    }

    [TestMethod]
    public void ChargerJson_AnnéeNonNumérique_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(annee: "abc"));
    }

    [TestMethod]
    public void ChargerJson_AnnéeHorsPlageBasse_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(annee: "1850"));
    }

    [TestMethod]
    public void ChargerJson_AnnéeHorsPlageHaute_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(annee: "2200"));
    }

    [TestMethod]
    public void ChargerJson_AnnéeMiseEnFormeAvecApostrophes_AcceptéLeFichier()
    {
        string json = CreerJsonAnnée(annee: " 2026 ");
        VerifierChargement(json, budjeckt => Assert.AreEqual(" 2026 ", budjeckt.Annee));
    }

    [TestMethod]
    public void ChargerJson_MoinsDeDouzeMois_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(nomsMois: NomsMois.Take(11).ToArray()));
    }

    [TestMethod]
    public void ChargerJson_PlusDeDouzeMois_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(nomsMois: NomsMois.Concat(new[] { "Treizième" }).ToArray()));
    }

    [TestMethod]
    public void ChargerJson_MoisSansNom_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(nomsMois: new[] { (string?)null }.Concat(NomsMois.Skip(1)).ToArray()));
    }

    [TestMethod]
    public void ChargerJson_MoisSansCatégories_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(categories: _ => null));
    }

    [TestMethod]
    public void ChargerJson_MoisAvecCatégoriesVides_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(categories: _ => "[]"));
    }

    [TestMethod]
    public void ChargerJson_FactureMontantInvalide_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":1,\"Montant\":0,\"Date\":\"2026-01-10T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_FactureCatégorieInconnue_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":99,\"Montant\":50,\"Date\":\"2026-01-10T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_MoisInconnu_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(nomsMois: new[] { "Trece" }.Concat(NomsMois.Skip(1)).ToArray()));
    }

    [TestMethod]
    public void ChargerJson_MoisNomInsensibleÀLaCasse_AcceptéLeFichier()
    {
        string json = CreerJsonAnnée(nomsMois: NomsMois.Select(nom => nom.ToLowerInvariant()).ToArray());

        VerifierChargement(json, budjeckt =>
        {
            Assert.HasCount(12, budjeckt.Months);
            Assert.AreEqual("janvier", budjeckt.Months[0].Nom, "Le nom est chargé tel quel, la validation tolère la casse");
        });
    }

    [TestMethod]
    public void ChargerJson_MoisDoublon_LèveInvalidDataException()
    {
        // Deux « Janvier » : la validation doit rejeter le doublon (la casse est ignorée).
        VerifierChargementInvalide(CreerJsonAnnée(nomsMois: new[] { "Janvier", "janvier" }.Concat(NomsMois.Skip(2)).ToArray()));
    }

    [TestMethod]
    public void ChargerJson_FactureHeureInvalide_LèveInvalidDataException()
    {
        // Une heure de 24 h ou plus sort de [00:00, 24:00) ; elle est rejetée à la validation.
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":1,\"Montant\":50,\"Date\":\"2026-01-10T00:00:00\",\"Heure\":\"24:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_FactureDateExtrême_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":1,\"Montant\":50,\"Date\":\"9999-01-10T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_FactureDateHorsMois_LèveInvalidDataException()
    {
        // Mois « Janvier » avec une facture datée de février.
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":1,\"Montant\":50,\"Date\":\"2026-02-10T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_FactureDUneAutreAnnée_LèveInvalidDataException()
    {
        // Annee 2026 avec une facture datée de 2027 : rejetée à la validation.
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":1,\"Montant\":50,\"Date\":\"2027-01-10T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_FactureIdDupliqué_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ =>
                "{\"Id\":1,\"IdCategorie\":1,\"Montant\":50,\"Date\":\"2026-01-10T00:00:00\"}," +
                "{\"Id\":1,\"IdCategorie\":1,\"Montant\":20,\"Date\":\"2026-01-11T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_CatégorieIdDupliqué_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(
            categories: _ => "[{\"Id\":1,\"Nom\":\"Loyer\"},{\"Id\":1,\"Nom\":\"Eau\"}]"));
    }

    [TestMethod]
    public void ChargerJson_MontantsProvoquantUnDébordement_LèveInvalidDataException()
    {
        // Deux montants proches de float.Max débordent vers +infini à la sommation des catégories.
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ =>
                "{\"Id\":1,\"IdCategorie\":1,\"Montant\":3e38,\"Date\":\"2026-01-10T00:00:00\"}," +
                "{\"Id\":2,\"IdCategorie\":1,\"Montant\":3e38,\"Date\":\"2026-01-11T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_FichierTropVolumineux_LèveInvalidDataException()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            // Création rapide d'un fichier de 11 Mo (sans contenu cochable au préalable);
            // le garde-fou de taille doit rejeter le chargement avant même la lecture JSON.
            using (FileStream flux = File.Create(chemin))
            {
                flux.SetLength(11 * 1024 * 1024);
            }

            Bud budjeckt = new();
            Assert.ThrowsExactly<InvalidDataException>(() => budjeckt.ChargerJson(chemin));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_FactureMontantInfini_LèveInvalidDataException()
    {
        // "1e39" déborde silencieusement vers +infini lors de la désérialisation float ;
        // un tel montant corromprait les totaux et bloquerait toute sauvegarde ultérieure.
        VerifierChargementInvalide(CreerJsonAnnée(
            factures: _ => "{\"Id\":1,\"IdCategorie\":1,\"Montant\":1e39,\"Date\":\"2026-01-10T00:00:00\"}"));
    }

    [TestMethod]
    public void ChargerJson_RevenueInfini_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée().Replace("\"Revenue\":0", "\"Revenue\":1e39"));
    }

    [TestMethod]
    public void ChargerJson_MoisNul_LèveInvalidDataException()
    {
        VerifierChargementInvalide("{\"Annee\":\"2026\",\"Mois\":[" +
            string.Join(",", Enumerable.Repeat("null", 12)) + "]}");
    }

    [TestMethod]
    public void ChargerJson_CatégorieSansNom_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(categories: _ => "[{\"Id\":1,\"Nom\":null}]"));
    }

    [TestMethod]
    public void ChargerJson_FactureNulle_LèveInvalidDataException()
    {
        VerifierChargementInvalide(CreerJsonAnnée(factures: _ => "[null]"));
    }

    [TestMethod]
    public void ChargerJson_MoisSansFactures_ChargeUnMoisVide()
    {
        string json = CreerJsonAnnée().Replace(",\"Factures\":[]", "");

        VerifierChargement(json, budjeckt =>
        {
            Assert.HasCount(12, budjeckt.Months);
            Assert.IsEmpty(budjeckt.Months[0].Factures);
            Assert.AreEqual(0f, budjeckt.Months[0].TotalExpenses);
        });
    }

    [TestMethod]
    public void ChargerJson_RecalculeTotauxDepuisLesFacturesDuFichier()
    {
        string json = CreerJsonAnnée(
            revenue: index => index == 0 ? 500f : 0f,
            factures: index => index == 0
                ? "{\"Id\":1,\"IdCategorie\":1,\"Montant\":100,\"Date\":\"2026-01-10T00:00:00\"}"
                : "");

        VerifierChargement(json, budjeckt =>
        {
            MonthBudget janvier = budjeckt.Months[0];
            Assert.AreEqual(500f, janvier.Revenue);
            Assert.HasCount(1, janvier.Factures);
            Assert.AreEqual(100f, janvier.TotalExpenses, 0.001f);
            Assert.AreEqual(400f, janvier.BudgetRemaining, 0.001f);
            Assert.AreEqual(100f, janvier.ExpenseCategories.First(categorie => categorie.Item1 == 1).Item3, 0.001f);
            Assert.AreEqual(0f, budjeckt.Months[1].TotalExpenses);
        });
    }

    [TestMethod]
    public void SauvegarderPuisCharger_ConserveAnneeMoisFacturesEtDates()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.Months = CreerDouzeMois();
            budjeckt.Months[0].AjouterCategorie("Assurance");
            budjeckt.Months[0].AjouterFacture(4, 25.5f, new DateTime(2026, 1, 10));
            budjeckt.Months[0].AjouterFacture(5, 60f, new DateTime(2026, 1, 5));
            budjeckt.SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            Assert.AreEqual("2026", charge.Annee);
            Assert.HasCount(12, charge.Months);
            Assert.IsTrue(charge.Months.Select(mois => mois.Nom).SequenceEqual(NomsMois));

            MonthBudget janvier = charge.Months[0];
            Assert.HasCount(2, janvier.Factures);

            Facture premiere = janvier.Factures[0];
            Assert.AreEqual(1, premiere.Id);
            Assert.AreEqual(4, premiere.IdCategorie);
            Assert.AreEqual(25.5f, premiere.Montant, 0.001f);
            Assert.AreEqual(new DateTime(2026, 1, 10), premiere.Date);

            Facture seconde = janvier.Factures[1];
            Assert.AreEqual(2, seconde.Id);
            Assert.AreEqual(5, seconde.IdCategorie);
            Assert.AreEqual(60f, seconde.Montant, 0.001f);
            Assert.AreEqual(new DateTime(2026, 1, 5), seconde.Date);

            Assert.AreEqual(85.5f, janvier.TotalExpenses, 0.001f);
            Assert.AreEqual(25.5f, janvier.ExpenseCategories.First(c => c.Item1 == 4).Item3, 0.001f);
            Assert.AreEqual(60f, janvier.ExpenseCategories.First(c => c.Item1 == 5).Item3, 0.001f);
            Assert.IsTrue(janvier.ExpenseCategories.Any(c => c.Item1 == 8 && c.Item2 == "Assurance"));
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderPuisCharger_MoisÀUneSeuleCatégorie_RechargeSansErreur()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.Months = CreerDouzeMois();
            foreach (MonthBudget mois in budjeckt.Months)
            {
                string[] noms = mois.ExpenseCategories.Select(categorie => categorie.Item2).ToArray();
                foreach (string nom in noms.Skip(1))
                {
                    mois.SupprimerCategorie(nom);
                }
            }

            budjeckt.SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            Assert.HasCount(12, charge.Months);
            Assert.IsTrue(charge.Months.All(mois => mois.ExpenseCategories.Length == 1),
                "Un fichier dont chaque mois garde une seule catégorie doit se recharger sans être rejeté");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderPuisCharger_ConserveLeRevenue()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.Months = CreerDouzeMois();
            budjeckt.Months[0] = new MonthBudget(
                "Janvier",
                1200f,
                new[] { new Tuple<int, string, float>(1, "Loyer", 0f) },
                new[] { new Facture(1, 1, 100f, new DateTime(2026, 1, 2)) });
            budjeckt.SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            MonthBudget janvier = charge.Months[0];
            Assert.AreEqual(1200f, janvier.Revenue);
            Assert.HasCount(1, janvier.Factures);
            Assert.AreEqual(100f, janvier.TotalExpenses, 0.001f);
            Assert.AreEqual(1100f, janvier.BudgetRemaining, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderPuisCharger_ConserveLHeureEtLesFacturesSansHeure()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.Months = CreerDouzeMois();
            budjeckt.Months[0].AjouterFacture(4, 25.5f, new DateTime(2026, 1, 10), new TimeSpan(14, 30, 0));
            budjeckt.Months[0].AjouterFacture(5, 60f, new DateTime(2026, 1, 5));
            budjeckt.SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            MonthBudget janvier = charge.Months[0];
            Assert.HasCount(2, janvier.Factures);

            Assert.AreEqual(new TimeSpan(14, 30, 0), janvier.Factures[0].Heure);
            Assert.IsNull(janvier.Factures[1].Heure);
            Assert.AreEqual(85.5f, janvier.TotalExpenses, 0.001f);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void ChargerJson_ReprendLesIdsAuMaxPlusUn()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            Bud budjeckt = new("2026");
            budjeckt.Months = CreerDouzeMois();
            budjeckt.Months[0].AjouterFacture(1, 10f);
            budjeckt.Months[0].AjouterFacture(1, 20f);
            budjeckt.Months[0].SupprimerFacture(2);
            budjeckt.SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            charge.Months[0].AjouterFacture(1, 30f);

            Assert.HasCount(2, charge.Months[0].Factures);
            Assert.AreEqual(2, charge.Months[0].Factures[^1].Id, "L'id ne doit pas réutiliser un id supprimé");
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderPuisCharger_FichierAvecAnnéeDifférente_ConserveLAnnéeDuFichier()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            new Bud("2025").SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            Assert.AreEqual("2025", charge.Annee);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    [TestMethod]
    public void SauvegarderPuisCharger_LesMoisPortentLAnnéeDuFichier_EtLaDateParDefautTombeSurElle()
    {
        string chemin = CreerCheminTemporaire();
        try
        {
            new Bud("2025").SauvegarderJson(chemin);

            Bud charge = new();
            charge.ChargerJson(chemin);

            Assert.IsTrue(charge.Months.All(mois => mois.Annee == 2025), "Tous les mois doivent porter l'année du fichier");

            // Sans date, une facture du mois de janvier 2025 doit retomber sur le 1er
            // janvier 2025 (janvier n'est pas le mois courant en septembre) et non sur 2026.
            charge.Months[0].AjouterFacture(1, 10f);
            Assert.AreEqual(new DateTime(2025, 1, 1), charge.Months[0].Factures[0].Date);
        }
        finally
        {
            SupprimerFichier(chemin);
        }
    }

    private static MonthBudget[] CreerDouzeMois()
    {
        return NomsMois.Select(nom => new MonthBudget(nom)).ToArray();
    }

    /// <summary>
    /// Construit un JSON d'année valide (12 mois) avec possibilité d'altérer une partie
    /// de la structure pour tester chaque branche de validation.
    /// </summary>
    private static string CreerJsonAnnée(
        string? annee = "2026",
        IReadOnlyList<string?>? nomsMois = null,
        Func<int, string?>? categories = null,
        Func<int, string>? factures = null,
        Func<int, float>? revenue = null)
    {
        nomsMois ??= NomsMois;
        categories ??= _ => "[{\"Id\":1,\"Nom\":\"Loyer\"}]";
        factures ??= _ => "";
        revenue ??= _ => 0f;

        var mois = nomsMois.Select((nom, index) =>
            $"{{\"Nom\":{(nom is null ? "null" : $"\"{nom}\"")}," +
            $"\"Revenue\":{revenue(index).ToString(CultureInfo.InvariantCulture)}," +
            $"\"Categories\":{categories(index) ?? "null"}," +
            $"\"Factures\":{(factures(index).Length == 0 ? "[]" : $"[{factures(index)}]")}}}");

        return $"{{\"Annee\":{(annee is null ? "null" : $"\"{annee}\"")},\"Mois\":[{string.Join(",", mois)}]}}";
    }

    /// <summary>
    /// Écrit un JSON dans un fichier temporaire, le charge, exécute les vérifications
    /// puis supprime le fichier temporaire.
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
    /// Écrit un JSON invalide dans un fichier temporaire et vérifie que son chargement
    /// lève une <see cref="InvalidDataException"/>.
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
        return Path.Combine(Path.GetTempPath(), $"budjeckt_{Guid.NewGuid():N}.json");
    }

    private static void SupprimerFichier(string chemin)
    {
        if (File.Exists(chemin))
        {
            File.Delete(chemin);
        }
    }
}