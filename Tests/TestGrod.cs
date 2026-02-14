using GrifLib;

namespace Tests;

public class TestGrod
{
    [SetUp]
    public void Setup()
    {
        // This method is called before each test.
        // You can initialize any shared resources here.
    }

    [Test]
    public void TestGrodSetAndGet()
    {
        var grod = new Grod("grod1");
        grod.Set("key1", "value1");
        Assert.That(grod.Get("key1", false), Is.EqualTo("value1"));
    }

    [Test]
    public void TestGrodSetAndGetRecursive()
    {
        var parentGrod = new Grod("parent");
        parentGrod.Set("key2", "value2");
        var childGrod = new Grod("child", null, parentGrod);
        Assert.That(childGrod.Get("key2", true), Is.EqualTo("value2"));
    }

    [Test]
    public void TestGrodRemove()
    {
        var grod = new Grod("grod3");
        grod.Set("key3", "value3");
        grod.Remove("key3", false);
        Assert.That(grod.Get("key3", false), Is.Null);
    }

    [Test]
    public void TestGrodClear()
    {
        var grod = new Grod("grod4");
        grod.Set("key4", "value4");
        grod.Clear(false);
        Assert.That(grod.Get("key4", false), Is.Null);
    }

    [Test]
    public void TestGrodCount()
    {
        var grod = new Grod("grod5");
        grod.Set("key5", "value5");
        grod.Set("key6", "value6");
        Assert.That(grod.Count(true), Is.EqualTo(2));
    }

    [Test]
    public void TestGrodCountRecursive()
    {
        var parentGrod = new Grod("parent2");
        parentGrod.Set("key7", "value7");
        parentGrod.Set("key8", "value8");
        var childGrod = new Grod("child2", null, parentGrod);
        childGrod.Set("key8", "value8duplicate");
        childGrod.Set("key9", "value9");
        Assert.That(childGrod.Count(true), Is.EqualTo(3));
    }

    [Test]
    public void TestGrodInvalidKey()
    {
        var grod = new Grod("grod6");
        Assert.Throws<ArgumentException>(() => grod.Set("", "value"));
        Assert.Throws<ArgumentException>(() => grod.Get("", false));
    }

    [Test]
    public void TestGrodSetNullValue()
    {
        var grod = new Grod("grod7");
        grod.Set("key9", null);
        Assert.That(grod.Get("key9", false), Is.Null);
    }

    [Test]
    public void TestGrodSetGrodItem()
    {
        var grod = new Grod("grod8");
        var item = new GrodItem("key10", "value10");
        grod.Set(item);
        Assert.That(grod.Get("key10", false), Is.EqualTo("value10"));
    }

    [Test]
    public void TestGrodSetGrodItemNull()
    {
        var grod = new Grod("grod9");
        Assert.Throws<ArgumentNullException>(() => grod.Set(null!));
    }

    [Test]
    public void TestGrodParentNull()
    {
        var grod = new Grod("grod10", null);
        grod.Set("key11", "value11");
        Assert.That(grod.Get("key11", true), Is.EqualTo("value11"));
    }

    [Test]
    public void TestGrodGetNonExistentKey()
    {
        var grod = new Grod("grod11");
        Assert.That(grod.Get("nonExistentKey", false), Is.Null);
    }

    [Test]
    public void TestGrodClearRecursive()
    {
        var parentGrod = new Grod("parent3");
        parentGrod.Set("key13", "value13");
        var childGrod = new Grod("child3", null, parentGrod);
        childGrod.Set("key14", "value14");
        childGrod.Clear(true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(childGrod.Get("key14", false), Is.Null);
            Assert.That(childGrod.Get("key13", true), Is.Null);
        }
    }
}
