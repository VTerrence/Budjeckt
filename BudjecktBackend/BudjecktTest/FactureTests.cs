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

    [TestMethod]
    public void Constructeur_AvecHeure_GardeLHeure()
    {
        var heure = new TimeSpan(8, 45, 0);

        var facture = new Facture(1, 2, 25.5f, new DateTime(2026, 1, 10), heure);

        Assert.AreEqual(heure, facture.Heure);
    }

    [TestMethod]
    public void Constructeur_SansHeure_HeureNulle()
    {
        var facture = new Facture(1, 2, 25.5f, new DateTime(2026, 1, 10));

        Assert.IsNull(facture.Heure);
    }

    [TestMethod]
    public void Constructeur_HeureInvalide_LèveArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            new Facture(1, 1, 10f, DateTime.Today, new TimeSpan(24, 0, 0)));
        Assert.ThrowsExactly<ArgumentException>(() =>
            new Facture(1, 1, 10f, DateTime.Today, new TimeSpan(-3, 0, 0)));
    }

    [TestMethod]
    public void Constructeur_DateAvecComposanteHoraire_EstNormaliséeÀMinuit()
    {
        var facture = new Facture(1, 1, 10f, new DateTime(2026, 1, 10, 18, 45, 0));

        Assert.AreEqual(new DateTime(2026, 1, 10), facture.Date);
    }
}