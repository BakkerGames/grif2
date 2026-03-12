using GrifLib;

namespace Tests;

public class UnitTestGrod
{
    [Test]
    public void Test_NullKey()
    {
        Grod g = new("base");
        try
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            g.Set(null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Fail();
        }
        catch (Exception)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void Test_EmptyKey()
    {
        Grod g = new("base");
        try
        {
            g.Set("", "empty");
            Assert.Fail();
        }
        catch (Exception)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void Test_WhitespaceKey()
    {
        Grod g = new("base");
        try
        {
            g.Set("   \t\r\n  ", "whitespace");
            Assert.Fail();
        }
        catch (Exception)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void Test_NotFound()
    {
        Grod g = new("base");
        Assert.That(g.Get("k", false), Is.Null);
    }

    [Test]
    public void Test_SingleValue()
    {
        Grod g = new("base");
        g.Set("k", "v");
        Assert.That(g.Get("k", false), Is.EqualTo("v"));
    }

    [Test]
    public void Test_Add()
    {
        Grod g = new("base");
        g.Set("k", "v");
        Assert.That(g.Get("k", false), Is.EqualTo("v"));
    }

    [Test]
    public void Test_AddTwice()
    {
        Grod g = new("base");
        g.Set("k", "v");
        g.Set("k", "vvv");
        Assert.That(g.Get("k", false), Is.EqualTo("vvv"));
    }

    [Test]
    public void Test_NullValue()
    {
        Grod g = new("base");
        g.Set("k", null);
        Assert.That(g.Get("k", false), Is.Null);
    }

    [Test]
    public void Test_CaseInsensitiveKeys()
    {
        Grod g = new("base");
        g.Set("k", "v");
        Assert.That(g.Get("K", false), Is.EqualTo("v"));
    }

    [Test]
    public void Test_WhitespaceKeysSet()
    {
        try
        {
            Grod g = new("base");
            g.Set("    k   ", "v");
            Assert.Fail();
        }
        catch (Exception)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void Test_WhitespaceKeyGet()
    {
        try
        {
            Grod g = new("base");
            g.Set("k", "v");
            var value = g.Get("   k   ", false);
            Assert.Fail();
        }
        catch (Exception)
        {
            Assert.Pass();
        }
    }

    [Test]
    public void Test_OverlayGetOverlayBaseThru()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("k", "v");
        Assert.That(g1.Get("k", true), Is.EqualTo("v"));
    }

    [Test]
    public void Test_OverlayGetOverlayValue()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("k", "v");
        g1.Set("k", "value");
        Assert.That(g1.Get("k", true), Is.EqualTo("value"));
    }

    [Test]
    public void Test_OverlayGetOverlayBackToBase()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("k", "v");
        g1.Set("k", "value");
        Assert.That(g.Get("k", true), Is.EqualTo("v"));
    }

    [Test]
    public void Test_GetKeys()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        var answer = "";
        foreach (string s in g.Keys(false, false))
        {
            answer += s;
            answer += g.Get(s, false);
        }
        // test separately, order is not guaranteed
        Assert.That(answer.Contains("a1") && answer.Contains("b2") && answer.Contains("c3"), Is.True);
    }

    [Test]
    public void Test_GetKeysBaseAndOverlay()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("a", "1");
        g.Set("b", "2");
        g1.Set("c", "3");
        var answer = "";
        foreach (string s in g1.Keys(true, false))
        {
            answer += s;
            answer += g1.Get(s, true);
        }
        // test separately, order is not guaranteed
        Assert.That(answer.Contains("a1") && answer.Contains("b2") && answer.Contains("c3"), Is.True);
    }

    [Test]
    public void Test_RemoveKey()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        g.Remove("b", false);
        var answer = "";
        foreach (string s in g.Keys(true, false))
        {
            answer += s;
            answer += g.Get(s, true);
        }
        // test separately, order is not guaranteed
        Assert.That(answer.Contains("a1") && !answer.Contains("b2") && answer.Contains("c3"), Is.True);
    }

    [Test]
    public void Test_ContainsKey()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        Assert.That(g.ContainsKey("a", true) && g.ContainsKey("b", true) && g.ContainsKey("c", true), Is.True);
    }

    [Test]
    public void Test_ContainsKeyAfterRemove()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        g.Remove("b", false);
        Assert.That(g.ContainsKey("a", true) && !g.ContainsKey("b", true) && g.ContainsKey("c", true), Is.True);
    }

    [Test]
    public void Test_ContainsKeyAny()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("a", "1");
        g1.Set("b", "2");
        Assert.That(g1.ContainsKey("a", true) && g1.ContainsKey("b", true), Is.True);
    }

    [Test]
    public void Test_MissingKeyNotFound()
    {
        Grod g = new("base");
        Assert.That(g.ContainsKey("a", true), Is.False);
    }

    [Test]
    public void Test_OverlayContainsKeyInBase()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("a", "1");
        Assert.That(g1.ContainsKey("a", true), Is.True);
    }

    [Test]
    public void Test_ModifyValue()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("a", "123456");
        var answer = g.Get("a", true);
        Assert.That(answer, Is.EqualTo("123456"));
    }

    [Test]
    public void Test_Clear()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        g.Clear(true);
        var answer = g.Count(true);
        Assert.That(answer, Is.Zero);
    }

    [Test]
    public void Test_ClearOnlyOverlay()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        g1.Set("aaa", "111");
        g1.Set("bbb", "222");
        g1.Set("ccc", "333");
        var answerBeforeClear = g1.Count(true);
        g1.Clear(false);
        var answerAfterClearOverlay = g1.Count(true);
        var answerAfterClearBase = g.Count(true);
        Assert.That(answerBeforeClear == 6 && answerAfterClearOverlay == 3 & answerAfterClearBase == 3, Is.True);
    }

    [Test]
    public void Test_ClearOnlyBase()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        g1.Set("aaa", "111");
        g1.Set("bbb", "222");
        g1.Set("ccc", "333");
        var answerBeforeClear = g1.Count(true);
        g.Clear(true);
        var answerAfterClearBase = g.Count(true);
        var answerAfterClearOverlay = g1.Count(true);
        Assert.That(answerBeforeClear == 6 && answerAfterClearBase == 0 && answerAfterClearOverlay == 3, Is.True);
    }

    [Test]
    public void Test_Keys()
    {
        Grod g = new("base");
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        var keys = g.Keys(false, false);
        Assert.That(keys.Contains("a") && keys.Contains("b") && keys.Contains("c"), Is.True);
    }

    [Test]
    public void Test_KeysOverlay()
    {
        Grod g = new("base");
        Grod g1 = new("overlay", null, g);
        g.Set("a", "1");
        g.Set("b", "2");
        g.Set("c", "3");
        g1.Set("bbb", "222");
        var keys = g1.Keys(false, false);
        Assert.That(!keys.Contains("a") && !keys.Contains("b") && !keys.Contains("c") && keys.Contains("bbb"), Is.True);
    }
}
