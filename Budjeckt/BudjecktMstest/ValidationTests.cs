namespace Budjeckt.Tests;

[TestClass]
public class ValidationTests
{
    [TestMethod]
    public void VerifierNomNonVide_NomNull_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierNomNonVide(null));
    }

    [TestMethod]
    public void VerifierNomNonVide_NomVide_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierNomNonVide(string.Empty));
    }

    [TestMethod]
    public void VerifierNomNonVide_NomDEspaces_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierNomNonVide("   "));
    }

    [TestMethod]
    public void VerifierNomNonVide_NomValide_NeLèveRien()
    {
        Validation.VerifierNomNonVide("Alimentation");
    }

    [TestMethod]
    public void VerifierNomUnique_NomDoublon_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Validation.VerifierNomUnique("Loyer", new[] { "loyer", "Eau" }));
    }

    [TestMethod]
    public void VerifierNomUnique_NomUnique_NeLèveRien()
    {
        Validation.VerifierNomUnique("Assurance", new[] { "Loyer", "Eau" });
    }

    [TestMethod]
    public void VerifierNomUnique_ListeVide_NeLèveRien()
    {
        Validation.VerifierNomUnique("Assurance", Enumerable.Empty<string>());
    }

    [TestMethod]
    public void VerifierMontantPositif_MontantNul_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierMontantPositif(0f));
    }

    [TestMethod]
    public void VerifierMontantPositif_MontantNégatif_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierMontantPositif(-12.5f));
    }

    [TestMethod]
    public void VerifierMontantPositif_MontantNaN_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierMontantPositif(float.NaN));
    }

    [TestMethod]
    public void VerifierMontantPositif_MontantInfini_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierMontantPositif(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierMontantPositif(float.NegativeInfinity));
    }

    [TestMethod]
    public void VerifierMontantPositif_MontantPositif_NeLèveRien()
    {
        Validation.VerifierMontantPositif(0.01f);
    }

    [TestMethod]
    public void VerifierRevenueFini_RevenueNaN_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierRevenueFini(float.NaN));
    }

    [TestMethod]
    public void VerifierRevenueFini_RevenueInfini_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierRevenueFini(float.PositiveInfinity));
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierRevenueFini(float.NegativeInfinity));
    }

    [TestMethod]
    public void VerifierRevenueFini_RevenueFini_NeLèveRien()
    {
        Validation.VerifierRevenueFini(0f);
        Validation.VerifierRevenueFini(-100f);
        Validation.VerifierRevenueFini(1200.5f);
    }

    [TestMethod]
    public void VerifierCategorieExiste_IdInconnu_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierCategorieExiste(99, new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void VerifierCategorieExiste_IdConnu_NeLèveRien()
    {
        Validation.VerifierCategorieExiste(2, new[] { 1, 2, 3 });
    }

    [TestMethod]
    public void VerifierCategorieExiste_ListeVide_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierCategorieExiste(1, Enumerable.Empty<int>()));
    }

    [TestMethod]
    public void VerifierFactureExiste_IdInconnu_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierFactureExiste(42, new[] { 1, 2, 3 }));
    }

    [TestMethod]
    public void VerifierFactureExiste_IdConnu_NeLèveRien()
    {
        Validation.VerifierFactureExiste(3, new[] { 1, 2, 3 });
    }

    [TestMethod]
    public void VerifierFactureExiste_ListeVide_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierFactureExiste(1, Enumerable.Empty<int>()));
    }

    [TestMethod]
    public void VerifierHeureValide_Null_NeLèveRien()
    {
        Validation.VerifierHeureValide(null);
    }

    [TestMethod]
    public void VerifierHeureValide_HeureValide_NeLèveRien()
    {
        Validation.VerifierHeureValide(TimeSpan.Zero);
        Validation.VerifierHeureValide(new TimeSpan(23, 59, 59));
    }

    [TestMethod]
    public void VerifierHeureValide_HeureTropGrande_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierHeureValide(new TimeSpan(24, 0, 0)));
    }

    [TestMethod]
    public void VerifierHeureValide_HeureNégative_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierHeureValide(new TimeSpan(-1, 0, 0)));
    }

    [TestMethod]
    public void VerifierDatePlausible_AnnéeExtrême_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierDatePlausible(new DateTime(1899, 1, 1)));
        Assert.ThrowsExactly<ArgumentException>(() => Validation.VerifierDatePlausible(new DateTime(2201, 1, 1)));
    }

    [TestMethod]
    public void VerifierDatePlausible_AnnéeNormale_NeLèveRien()
    {
        Validation.VerifierDatePlausible(new DateTime(2026, 1, 15));
    }

    [TestMethod]
    public void VerifierDatePlausible_AnnéeBorneMinimale_NeLèveRien()
    {
        Validation.VerifierDatePlausible(new DateTime(1900, 1, 1));
    }

    [TestMethod]
    public void VerifierDatePlausible_AnnéeBorneMaximale_NeLèveRien()
    {
        Validation.VerifierDatePlausible(new DateTime(2200, 12, 31));
    }

    [TestMethod]
    public void VerifierDateDansLeMois_DateDansLeMois_NeLèveRien()
    {
        Validation.VerifierDateDansLeMois(new DateTime(2026, 1, 15), "Janvier", 2026);
    }

    [TestMethod]
    public void VerifierDateDansLeMois_NomInsensibleÀLaCasse_NeLèveRien()
    {
        Validation.VerifierDateDansLeMois(new DateTime(2026, 2, 5), "février", 2026);
        Validation.VerifierDateDansLeMois(new DateTime(2026, 1, 5), "JANVIER", 2026);
    }

    [TestMethod]
    public void VerifierDateDansLeMois_NomAvecEspaces_NeLèveRien()
    {
        Validation.VerifierDateDansLeMois(new DateTime(2026, 1, 5), " Janvier ", 2026);
    }

    [TestMethod]
    public void VerifierDateDansLeMois_NomNull_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Validation.VerifierDateDansLeMois(new DateTime(2026, 1, 5), null!, 2026));
    }

    [TestMethod]
    public void VerifierDateDansLeMois_DateHorsMois_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Validation.VerifierDateDansLeMois(new DateTime(2026, 2, 5), "Janvier", 2026));
    }

    [TestMethod]
    public void VerifierDateDansLeMois_MoisInconnu_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Validation.VerifierDateDansLeMois(new DateTime(2026, 1, 5), "Trece", 2026));
    }

    [TestMethod]
    public void VerifierDateDansLeMois_DateHorsAnnée_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            Validation.VerifierDateDansLeMois(new DateTime(2027, 1, 5), "Janvier", 2026));
    }
}