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
}