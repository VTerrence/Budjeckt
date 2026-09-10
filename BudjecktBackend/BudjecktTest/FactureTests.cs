namespace Budjeckt.Tests;

[TestClass]
public class FactureTests
{
    [TestMethod]
    public void Constructeur_RemplitToutesLesProprietes()
    {
        DateTime date = new(2026, 1, 15);

        var facture = new Facture(3, 4, 25.5f, date);

        Assert.AreEqual(3, facture.Id);
        Assert.AreEqual(4, facture.IdCategorie);
        Assert.AreEqual(25.5f, facture.Montant);
        Assert.AreEqual(date, facture.Date);
    }

    [TestMethod]
    public void Constructeur_MontantNul_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Facture(1, 1, 0f, DateTime.Today));
    }

    [TestMethod]
    public void Constructeur_MontantNégatif_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Facture(1, 1, -3f, DateTime.Today));
    }

    [TestMethod]
    public void Constructeur_MontantNaN_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Facture(1, 1, float.NaN, DateTime.Today));
    }
}