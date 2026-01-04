using GrifLib;
using Newtonsoft.Json.Linq;
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
}
