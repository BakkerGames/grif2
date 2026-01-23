using GrifLib;
using static GrifLib.Common;
using static GrifLib.Dags;

namespace Tests;

public class TestDags
{
    [SetUp]
    public void Setup()
    {
        // This method is called before each test.
        // You can initialize any shared resources here.
    }

    [Test]
    public void TestProcess()
    {
        Grod grod = new("testGrod");
        string script = $"{WRITE_TOKEN}abc)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "abc") }));
    }

    [Test]
    public void TestTwoCommands()
    {
        Grod grod = new("testGrod");
        string script = $"{WRITE_TOKEN}abc){WRITE_TOKEN}def)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "abc"), new(MessageType.Text, "def") }));
    }

    [Test]
    public void TestConcatenate()
    {
        Grod grod = new("testGrod");
        string script = $"{WRITE_TOKEN}abc,def)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage>
            { new(MessageType.Text, "abc"), new(MessageType.Text, "def") }));
    }

    [Test]
    public void TestWriteError()
    {
        Grod grod = new("testGrod");
        string script = $"{WRITE_TOKEN}abc";
        var result = Process(grod, script);
        var expected = "Missing closing parenthesis";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestNoParams()
    {
        Grod grod = new("testGrod");
        string script = $"{WRITE_TOKEN})";
        var result = Process(grod, script);
        var expected = "Expected at least one parameter, but got 0";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestGetAndSet()
    {
        Grod grod = new("testGrod");
        grod.Set("key1", "value1");
        string script = $"{GET_TOKEN}key1)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "value1") }));
    }

    [Test]
    public void TestIfCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition met") }));
    }

    [Test]
    public void TestIfNotCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {NOT_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition met") }));
    }

    [Test]
    public void TestIfWithAndCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {TRUE} {AND_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition met") }));
    }

    [Test]
    public void TestIfWithOrCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {OR_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition met") }));
    }

    [Test]
    public void TestIfWithOrShortCircuitCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {TRUE} {OR_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition met") }));
    }

    [Test]
    public void TestIfWithElseCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ELSE_TOKEN} {WRITE_TOKEN}\"Condition not met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition not met") }));
    }

    [Test]
    public void TestIfWithAndFailsToElseCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {TRUE} {AND_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ELSE_TOKEN} {WRITE_TOKEN}\"Condition not met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition not met") }));
    }

    [Test]
    public void TestIfWithAndShortCircuitToElseCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {AND_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ELSE_TOKEN} {WRITE_TOKEN}\"Condition not met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Condition not met") }));
    }

    [Test]
    public void TestIfWithElseIfCondition()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}\"Condition met\") {ELSEIF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}\"Second condition met\") {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Second condition met") }));
    }

    [Test]
    public void TestIfNestedAnswer1()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {TRUE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer1) {ELSE_TOKEN} {WRITE_TOKEN}Answer2) {ENDIF_TOKEN} {ELSEIF_TOKEN} {FALSE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer3) {ELSE_TOKEN} {WRITE_TOKEN}Answer4) {ENDIF_TOKEN} {ELSE_TOKEN} {WRITE_TOKEN}Answer5) {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Answer1") }));
    }

    [Test]
    public void TestIfNestedAnswer2()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {TRUE} {THEN_TOKEN} {IF_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}Answer1) {ELSE_TOKEN} {WRITE_TOKEN}Answer2) {ENDIF_TOKEN} {ELSEIF_TOKEN} {FALSE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer3) {ELSE_TOKEN} {WRITE_TOKEN}Answer4) {ENDIF_TOKEN} {ELSE_TOKEN} {WRITE_TOKEN}Answer5) {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Answer2") }));
    }

    [Test]
    public void TestIfNestedAnswer3()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer1) {ELSE_TOKEN} {WRITE_TOKEN}Answer2) {ENDIF_TOKEN} {ELSEIF_TOKEN} {TRUE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer3) {ELSE_TOKEN} {WRITE_TOKEN}Answer4) {ENDIF_TOKEN} {ELSE_TOKEN} {WRITE_TOKEN}Answer5) {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Answer3") }));
    }

    [Test]
    public void TestIfNestedAnswer4()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer1) {ELSE_TOKEN} {WRITE_TOKEN}Answer2) {ENDIF_TOKEN} {ELSEIF_TOKEN} {TRUE} {THEN_TOKEN} {IF_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}Answer3) {ELSE_TOKEN} {WRITE_TOKEN}Answer4) {ENDIF_TOKEN} {ELSE_TOKEN} {WRITE_TOKEN}Answer5) {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Answer4") }));
    }

    [Test]
    public void TestIfNestedAnswer5()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {FALSE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer1) {ELSE_TOKEN} {WRITE_TOKEN}Answer2) {ENDIF_TOKEN} {ELSEIF_TOKEN} {FALSE} {THEN_TOKEN} {IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}Answer3) {ELSE_TOKEN} {WRITE_TOKEN}Answer4) {ENDIF_TOKEN} {ELSE_TOKEN} {WRITE_TOKEN}Answer5) {ENDIF_TOKEN}";
        grod.Set("key1", "value1");
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Answer5") }));
    }

    [Test]
    public void TestIfEQ()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {EQ_TOKEN}1,1) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfEQNull()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {EQ_TOKEN}null,null) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfEQString()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {EQ_TOKEN}abc,abc) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfNE()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {NE_TOKEN}1,2) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfNENull()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {NE_TOKEN}null,2) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfNEString()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {NE_TOKEN}abc,xyz) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfGT()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {GT_TOKEN}2,1) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfGTNull()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {GT_TOKEN}2,null) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfGTString()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {GT_TOKEN}xyz,abc) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfGE()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {GE_TOKEN}1,1) {AND_TOKEN} {GE_TOKEN}2,1) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfGENull()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {GE_TOKEN}null,null) {AND_TOKEN} {GE_TOKEN}2,null) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfGEString()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {GE_TOKEN}abc,abc) {AND_TOKEN} {GE_TOKEN}xyz,abc) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfLT()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {LT_TOKEN}1,2) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfLTNull()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {LT_TOKEN}null,2) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfLTString()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {LT_TOKEN}abc,xyz) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfLE()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {LE_TOKEN}1,1) {AND_TOKEN} {LE_TOKEN}1,2) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfLENull()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {LE_TOKEN}null,null) {AND_TOKEN} {LE_TOKEN}null,2) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestIfLEString()
    {
        Grod grod = new("testGrod");
        string script = $"{IF_TOKEN} {LE_TOKEN}abc,abc) {AND_TOKEN} {LE_TOKEN}abc,xyz) {THEN_TOKEN} {WRITE_TOKEN}answer) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "answer") }));
    }

    [Test]
    public void TestMsg()
    {
        Grod grod = new("testGrod");
        grod.Set("Hello", "Hello, World!");
        string script = $"{MSG_TOKEN}Hello)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Hello, World!"), new(MessageType.Text, "\\n") }));
    }

    [Test]
    public void TestParameterWithFunction()
    {
        Grod grod = new("testGrod");
        grod.Set("key1", "value1");
        string script = $"{WRITE_TOKEN}{GET_TOKEN}key1))";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "value1") }));
    }

    [Test]
    public void TestParameterWithNestedFunction()
    {
        Grod grod = new("testGrod");
        grod.Set("key1", "value1");
        grod.Set("key2", "key1");
        string script = $"{WRITE_TOKEN}{GET_TOKEN}{GET_TOKEN}key2)))";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "value1") }));
    }

    [Test]
    public void TestUnknownToken()
    {
        Grod grod = new("testGrod");
        string script = "@unknown()";
        var result = Process(grod, script);
        var expected = "Unknown token: @unknown(";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestUserDefinedScript()
    {
        Grod grod = new("testGrod");
        grod.Set("@myScript", $"{WRITE_TOKEN}\"Hello from user-defined script!\")");
        string script = "@myScript";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Hello from user-defined script!") }));
    }

    [Test]
    public void TestAddTo()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "5");
        string script = $"{ADDTO_TOKEN}counter,3){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "8") }));
    }

    [Test]
    public void TestSubTo()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "5");
        string script = $"{SUBTO_TOKEN}counter,2){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "3") }));
    }

    [Test]
    public void TestMulTo()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "5");
        string script = $"{MULTO_TOKEN}counter,4){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "20") }));
    }

    [Test]
    public void TestDivideBy()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "20");
        string script = $"{DIVTO_TOKEN}counter,4){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "5") }));
    }

    [Test]
    public void TestDivideByZero()
    {
        var counterValue = "20";
        Grod grod = new("testGrod");
        grod.Set("counter", counterValue);
        string script = $"{DIVTO_TOKEN}counter,0){GET_TOKEN}counter)";
        var result = Process(grod, script);
        var expected = "Division by zero is not allowed.";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestAddToNonNumber()
    {
        var counterValue = "five";
        Grod grod = new("testGrod");
        grod.Set("counter", counterValue);
        string script = $"{ADDTO_TOKEN}counter,3) {GET_TOKEN}counter)";
        var result = Process(grod, script);
        var expected0 = "Invalid number: five";
        var expected1 = new GrifMessage(MessageType.Internal, counterValue);
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Value, Does.Contain(expected0));
            Assert.That(result[1], Is.EqualTo(expected1));
        }
    }

    [Test]
    public void TestModTo()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "20");
        string script = $"{MODTO_TOKEN}counter,6){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "2") }));
    }

    [Test]
    public void TestModToNegative()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "-20");
        string script = $"{MODTO_TOKEN}counter,30){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "10") }));
    }

    [Test]
    public void TestAdd()
    {
        Grod grod = new("testGrod");
        string script = $"{ADD_TOKEN}5,3)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "8") }));
    }

    [Test]
    public void TestSub()
    {
        Grod grod = new("testGrod");
        string script = $"{SUB_TOKEN}5,3)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "2") }));
    }

    [Test]
    public void TestMul()
    {
        Grod grod = new("testGrod");
        string script = $"{MUL_TOKEN}5,3)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "15") }));
    }

    [Test]
    public void TestDiv()
    {
        Grod grod = new("testGrod");
        string script = $"{DIV_TOKEN}6,3)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "2") }));
    }

    [Test]
    public void TestMod()
    {
        Grod grod = new("testGrod");
        string script = $"{MOD_TOKEN}20,6)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "2") }));
    }

    [Test]
    public void TestModNegative()
    {
        Grod grod = new("testGrod");
        string script = $"{MOD_TOKEN}-20,30)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "10") }));
    }

    [Test]
    public void TestDivByZero()
    {
        Grod grod = new("testGrod");
        string script = $"{DIV_TOKEN}6,0)";
        var result = Process(grod, script);
        var expected = "Division by zero is not allowed.";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestModByZero()
    {
        Grod grod = new("testGrod");
        string script = $"{MOD_TOKEN}20,0)";
        var result = Process(grod, script);
        var expected = "Attempted to divide by zero.";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestInvalidCommand()
    {
        Grod grod = new("testGrod");
        string script = "@invalidcommand()";
        var result = Process(grod, script);
        var expected = "Unknown token: @invalidcommand(";
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
            Assert.That(result[0].Value, Does.Contain(expected));
        }
    }

    [Test]
    public void TestEmptyScript()
    {
        Grod grod = new("testGrod");
        string script = "";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage>()));
    }

    [Test]
    public void TestWhitespaceScript()
    {
        Grod grod = new("testGrod");
        string script = "   ";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage>() { new(MessageType.Text, script) }));
    }

    [Test]
    public void TestScriptWithOnlyComments()
    {
        Grod grod = new("testGrod");
        string script = $"{COMMENT_TOKEN}\"This is a comment\n\") {COMMENT_TOKEN}\"Another comment\")";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage>()));
    }

    [Test]
    public void TestScriptWithMixedContent()
    {
        Grod grod = new("testGrod");
        string script = $"{COMMENT_TOKEN}\"This is a comment\n\") {WRITE_TOKEN}Hello) {COMMENT_TOKEN}\"Another comment\") {WRITE_TOKEN}World)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Hello"), new(MessageType.Text, "World") }));
    }

    [Test]
    public void TestNeg()
    {
        Grod grod = new("testGrod");
        string script = $"{NEG_TOKEN}5)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "-5") }));
    }

    [Test]
    public void TestNegZero()
    {
        Grod grod = new("testGrod");
        string script = $"{NEG_TOKEN}0)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "0") }));
    }

    [Test]
    public void TestNegTo()
    {
        Grod grod = new("testGrod");
        grod.Set("counter", "5");
        string script = $"{NEGTO_TOKEN}counter){GET_TOKEN}counter)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "-5") }));
    }

    [Test]
    public void TestGetValue()
    {
        Grod grod = new("testGrod");
        grod.Set("key1", "value1");
        grod.Set("key2", $"{GET_TOKEN}key1)");
        string script = $"{GETVALUE_TOKEN}key2)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "value1") }));
    }

    [Test]
    public void TestGetValueNonExistentKey()
    {
        Grod grod = new("testGrod");
        string script = $"{GETVALUE_TOKEN}nonexistent)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "") }));
    }

    [Test]
    public void TestGetValueWithNestedFunction()
    {
        Grod grod = new("testGrod");
        grod.Set("key1", "value1");
        grod.Set("key2", $"{GET_TOKEN}key1)");
        grod.Set("key3", $"{GET_TOKEN}key2)");
        string script = $"{GETVALUE_TOKEN}key3)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Internal, "value1") }));
    }

    [Test]
    public void TestComplexScript()
    {
        Grod grod = new("testGrod");
        grod.Set("a", "10");
        grod.Set("b", "20");
        string script = $"{IF_TOKEN} {GT_TOKEN}{GET_TOKEN}a),5) {AND_TOKEN} {LT_TOKEN}{GET_TOKEN}b),30) {THEN_TOKEN} {WRITE_TOKEN}{ADD_TOKEN}{GET_TOKEN}a),{GET_TOKEN}b))) {ELSE_TOKEN} {WRITE_TOKEN}Out of range) {ENDIF_TOKEN}";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "30") }));
    }

    [Test]
    public void TestCommentCommand()
    {
        Grod grod = new("testGrod");
        string script = $"{COMMENT_TOKEN}\"This is a comment\"){WRITE_TOKEN}Hello)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Hello") }));
    }

    [Test]
    public void TestCommentCommandWithNewline()
    {
        Grod grod = new("testGrod");
        string script = $"{COMMENT_TOKEN}\"This is a comment\nwith a newline\"){WRITE_TOKEN}Hello)";
        var result = Process(grod, script);
        Assert.That(result, Is.EqualTo(new List<GrifMessage> { new(MessageType.Text, "Hello") }));
    }
}
