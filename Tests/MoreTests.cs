using GrifLib;
using static GrifLib.Dags;

namespace Tests;

public class MoreTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestReturn()
    {
        Grod grod = new("testGrod");
        var value = "1";
        string script = $"@set(abc,{value}) @write(@get(abc)) @return @write(xyz)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, value) }));
    }

    [Test]
    public void TestReturnIf()
    {
        Grod grod = new("testGrod");
        var value = "1";
        string script = $"@if true @then @set(abc,{value}) @write(@get(abc)) @return @write(xyz) @endif";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, value) }));
    }

    [Test]
    public void TestReturnFor()
    {
        Grod grod = new("testGrod");
        var answer = "10";
        string script = @"
            @set(value,0)
            @for(i,1,10)
                @addto(value,$i)
                @if @ge(@get(value),10) @then
                    @write(@get(value))
                    @return
                @endif
            @endfor
            @write(@get(value))
            ";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, answer) }));
    }

    [Test]
    public void TestLocalStorage()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(__x,1)
            @write(@get(__x))
            ";
        var answer = "1";
        var result = Process(grod, script);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, answer) }));
            Assert.That(grod.Count(true), Is.Zero);
        }
    }

    [Test]
    public void TestLocalStorageAfter()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(__x,1)
            @write(@get(__x))
            ";
        var answer = "1";
        var result = Process(grod, script); // __x gone afterwards
        var script2 = @"
            @write(@get(__x))
            ";
        var result2 = Process(grod, script2); // no __x, empty value
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, answer) }));
            Assert.That(result2, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "") }));
            Assert.That(grod.Count(true), Is.Zero);
        }
    }

    [Test]
    public void TestWhile()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(__a,0)
            @while @lt(@get(__a),3) @do
                @addto(__a,1)
                @write(@get(__a))
            @endwhile
            @write(""xyz"")
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, "1"),
            new(MessageType.Text, "2"),
            new(MessageType.Text, "3"),
            new(MessageType.Text, "xyz")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestWhileReturn()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(__a,0)
            @while @lt(@get(__a),3) @do
                @addto(__a,1)
                @write(@get(__a))
                @if @eq(@get(__a),2) @then
                    @return
                @endif
            @endwhile
            @write(""xyz"")
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, "1"),
            new(MessageType.Text, "2")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestNestedReturn()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(__a,0)
            @for(i,1,5)
                @addto(__a,1)
                @write(@get(__a))
                @if @eq(@get(__a),3) @then
                    @while true @do
                        @return
                    @endwhile
                @endif
            @endfor
            @write(""xyz"")
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, "1"),
            new(MessageType.Text, "2"),
            new(MessageType.Text, "3")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestGetChar()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,Hello)
            @write(@getchar(@get(text),2))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, "l")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestGetCharOutOfBounds()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,Hi)
            @write(@getchar(@get(text),5))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, " ")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestGetCharInvalid()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,Hello)
            @write(@getchar(@get(text),abc))
            ";
        var answer = "Invalid number: abc";
        var result = Process(grod, script);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(answer));
        }
    }

    [Test]
    public void TestGetCharNegativeIndex()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,Hello)
            @write(@getchar(@get(text),-1))
            ";
        var answer = "Index out of range";
        var result = Process(grod, script);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(answer));
        }
    }

    [Test]
    public void TestGetCharZeroIndex()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,Hello)
            @write(@getchar(@get(text),0))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, "H")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestGetCharEmptyString()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,"""")
            @write(@getchar(@get(text),1))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, " ")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestGetCharUnicode()
    {
        Grod grod = new("testGrod");
        var testValue = "😊🌟🚀";
        var script = @$"
            @set(text,{testValue})
            @write(@getchar(@get(text),1))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, testValue[1].ToString())
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestGetCharNull()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,null)
            @write(@getchar(@get(text),1))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, " ")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }

    [Test]
    public void TestSetChar()
    {
        Grod grod = new("testGrod");
        var script = @"
            @set(text,Hello)
            @set(text,@setchar(@get(text),1,a))
            @write(@get(text))
            ";
        var answer = new List<GrifMessage> {
            new(MessageType.Text, "Hallo")
        };
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(answer));
    }
}
