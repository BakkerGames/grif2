using System.Text;
using GrifLib;
using static GrifLib.Common;
using static GrifLib.Dags;

namespace Tests;

public class UnitTestDags
{
    private readonly Grod grod = new("base");
    private List<GrifMessage> result = [];

    private static string Squash(List<GrifMessage> result)
    {
        var sb = new StringBuilder();
        foreach (var item in result)
        {
            if (item.Type == MessageType.Text || item.Type == MessageType.Internal)
            {
                sb.Append(item.Value);
            }
        }
        return sb.ToString();
    }

    [SetUp]
    public void Setup()
    {
        grod.Clear(true);
        result.Clear();
    }

    [Test]
    public void Test_Passing()
    {
        Assert.Pass();
    }

    [Test]
    public void Test_Get()
    {
        var key = "abc";
        var value = "123";
        grod.Set(key, value);
        result = Process(grod, $"{GET_TOKEN}{key})");
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Value, Is.EqualTo(value));
    }

    [Test]
    public void Test_Set()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{SET_TOKEN}{key},{value})");
        result = Process(grod, $"{GET_TOKEN}{key})");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_Set_Script()
    {
        var key = "abc";
        var answer = $"{COMMENT_TOKEN}\"this is a comment\")";
        var value = "\"" + answer.Replace("\"", "\\\"") + "\"";
        Process(grod, $"{SET_TOKEN}{key},{value})");
        result = Process(grod, $"{GET_TOKEN}{key})");
        Assert.That(Squash(result), Is.EqualTo(answer));
    }

    [Test]
    public void Test_SetArray()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{SETARRAY_TOKEN}{key},2,3,{value})");
        result = Process(grod, $"{GETARRAY_TOKEN}{key},2,3)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_SetArray_Null()
    {
        var key = "abc";
        var value = "";
        Process(grod, $"{SETARRAY_TOKEN}{key},2,3,{value})");
        result = Process(grod, $"{GETARRAY_TOKEN}{key},2,3)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_ClearArray()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{SETARRAY_TOKEN}{key},2,3,{value})");
        Process(grod, $"{CLEARARRAY_TOKEN}{key})");
        result = Process(grod, $"{GETARRAY_TOKEN}{key},2,3)");
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_SetList()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{SETLIST_TOKEN}{key},1,{value})");
        result = Process(grod, $"{GETLIST_TOKEN}{key},1)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_SetList_Null()
    {
        var key = "abc";
        var value = "";
        Process(grod, $"{SETLIST_TOKEN}{key},1,{value})");
        result = Process(grod, $"{GETLIST_TOKEN}{key},1)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_SetList_TabCRLF()
    {
        var key = "abc";
        var value = "abc\t\r\n123";
        Process(grod, $"{SETLIST_TOKEN}{key},1,\"{value}\")");
        result = Process(grod, $"{GETLIST_TOKEN}{key},1)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_InsertAtList()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{ADDLIST_TOKEN}{key},0)");
        Process(grod, $"{ADDLIST_TOKEN}{key},1)");
        Process(grod, $"{ADDLIST_TOKEN}{key},2)");
        Process(grod, $"{ADDLIST_TOKEN}{key},3)");
        Process(grod, $"{INSERTATLIST_TOKEN}{key},1,{value})");
        result = Process(grod, $"{GETLIST_TOKEN}{key},1)");
        Assert.That(Squash(result), Is.EqualTo(value));
        result = Process(grod, $"{GETLIST_TOKEN}{key},4)");
        Assert.That(Squash(result), Is.EqualTo("3"));
    }

    [Test]
    public void Test_RemoveAtList()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{SETLIST_TOKEN}{key},3,{value})");
        Process(grod, $"{REMOVEATLIST_TOKEN}{key},0)");
        result = Process(grod, $"{GETLIST_TOKEN}{key},2)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_Function()
    {
        Process(grod, $"{SET_TOKEN}\"@boo\",\"{WRITE_TOKEN}eek!)\")");
        result = Process(grod, "@boo");
        Assert.That(Squash(result), Is.EqualTo("eek!"));
    }

    [Test]
    public void Test_FunctionParameters()
    {
        Process(grod, $"{SET_TOKEN}\"@boo(x)\",\"{WRITE_TOKEN}$x)\")");
        result = Process(grod, "@boo(eek!)");
        Assert.That(Squash(result), Is.EqualTo("eek!"));
    }

    [Test]
    public void Test_Abs()
    {
        result = Process(grod, $"{WRITE_TOKEN}{ABS_TOKEN}1))");
        Assert.That(Squash(result), Is.EqualTo("1"));
        result = Process(grod, $"{WRITE_TOKEN}{ABS_TOKEN}-1))");
        Assert.That(Squash(result), Is.EqualTo("1"));
    }

    [Test]
    public void Test_Add()
    {
        result = Process(grod, $"{WRITE_TOKEN}{ADD_TOKEN}1,3))");
        Assert.That(Squash(result), Is.EqualTo("4"));
    }

    [Test]
    public void Test_AddTo()
    {
        result = Process(grod, $"{SET_TOKEN}value,12) {ADDTO_TOKEN}value,7) {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("19"));
    }

    [Test]
    public void Test_Comment()
    {
        result = Process(grod, $"{COMMENT_TOKEN}\"this is a comment\")");
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_Concat()
    {
        result = Process(grod, $"{WRITE_TOKEN}{CONCAT_TOKEN}abc, def, 123))");
        Assert.That(Squash(result), Is.EqualTo("abcdef123"));
    }

    [Test]
    public void Test_Debug()
    {
        grod.Set(DEBUG_FLAG, TRUE);
        result = Process(grod, $"{DEBUG_TOKEN}\"### this is a comment\")");
        Assert.That(Squash(result), Is.EqualTo("### this is a comment" + NL_CHAR));
        result = Process(grod, $"{DEBUG_TOKEN}{ADD_TOKEN}123,456))");
        Assert.That(Squash(result), Is.EqualTo("579" + NL_CHAR));
        grod.Set(DEBUG_FLAG, FALSE);
        result = Process(grod, $"{DEBUG_TOKEN}\"### this is a comment\")");
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_Div()
    {
        result = Process(grod, $"{WRITE_TOKEN}{DIV_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo("7"));
    }

    [Test]
    public void Test_DivTo()
    {
        result = Process(grod, $"{SET_TOKEN}value,12) {DIVTO_TOKEN}value,3) {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("4"));
    }

    [Test]
    public void Test_EQ()
    {
        result = Process(grod, $"{WRITE_TOKEN}{EQ_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{EQ_TOKEN}42,42))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
    }

    [Test]
    public void Test_Exec()
    {
        result = Process(grod, $"{EXEC_TOKEN}\"{SET_TOKEN}value,23)\") {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("23"));
    }

    [Test]
    public void Test_False()
    {
        result = Process(grod, $"{WRITE_TOKEN}{ISFALSE_TOKEN}\"\"))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{ISFALSE_TOKEN}0))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{ISFALSE_TOKEN}1))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{ISFALSE_TOKEN}abc))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_For()
    {
        result = Process(grod, $"{FOR_TOKEN}x,1,3) {WRITE_TOKEN}$x) {ENDFOR_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("123"));
    }

    [Test]
    public void Test_ForEachKey()
    {
        Process(grod, $"{SET_TOKEN}value.1,100) {SET_TOKEN}value.2,200)");
        result = Process(grod, $"{FOREACHKEY_TOKEN}x,\"value.\") {WRITE_TOKEN}$x) {ENDFOREACHKEY_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("12"));
        result = Process(grod, $"{FOREACHKEY_TOKEN}x,\"value.\") {GET_TOKEN}value.$x) {ENDFOREACHKEY_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("100200"));
    }

    [Test]
    public void Test_ForEachList()
    {
        Process(grod, $"{SETLIST_TOKEN}value,1,10)");
        Process(grod, $"{SETLIST_TOKEN}value,2,20)");
        Process(grod, $"{SETLIST_TOKEN}value,3,30)");
        result = Process(grod, $"{FOREACHLIST_TOKEN}x,value) {WRITE_TOKEN}$x) {ENDFOREACHLIST_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("102030"));
    }

    [Test]
    public void Test_Format()
    {
        result = Process(grod, $"{WRITE_TOKEN}{FORMAT_TOKEN}\"{{0}}-{{1}}-{{2}}\",1,2,3))");
        Assert.That(Squash(result), Is.EqualTo("1-2-3"));
        result = Process(grod, $"{WRITE_TOKEN}{FORMAT_TOKEN}\"{{2}}-{{1}}-{{0}}\",1,2,3))");
        Assert.That(Squash(result), Is.EqualTo("3-2-1"));
        result = Process(grod, $"{WRITE_TOKEN}{FORMAT_TOKEN}\"{{0}}-{{1}}-{{2}}\",1,2))");
        Assert.That(Squash(result), Is.EqualTo("1-2-{2}"));
    }

    [Test]
    public void Test_GE()
    {
        result = Process(grod, $"{WRITE_TOKEN}{GE_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{GE_TOKEN}42,42))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{GE_TOKEN}1,42))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_GetInChannel()
    {
        grod.Set(INCHANNEL, "abc");
        result = Process(grod, $"{WRITE_TOKEN}{GETINCHANNEL_TOKEN})");
        Assert.That(Squash(result), Is.EqualTo("abc"));
        result = Process(grod, $"{WRITE_TOKEN}{GETINCHANNEL_TOKEN})");
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_GetValue()
    {
        Process(grod, $"{SET_TOKEN}v1,\"{GET_TOKEN}v2)\") {SET_TOKEN}v2,123)");
        result = Process(grod, $"{GET_TOKEN}v1)");
        Assert.That(Squash(result), Is.EqualTo($"{GET_TOKEN}v2)"));
        result = Process(grod, $"{WRITE_TOKEN}{GETVALUE_TOKEN}v1))");
        Assert.That(Squash(result), Is.EqualTo("123"));
    }

    [Test]
    public void Test_GoLabel()
    {
        result = Process(grod, $"{WRITE_TOKEN}abc) {GOLABEL_TOKEN}1) {WRITE_TOKEN}def) {LABEL_TOKEN}1) {WRITE_TOKEN}xyz)");
        Assert.That(Squash(result), Is.EqualTo("abcxyz"));
    }

    [Test]
    public void Test_GT()
    {
        result = Process(grod, $"{WRITE_TOKEN}{GT_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{GT_TOKEN}42,42))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{GT_TOKEN}1,42))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_If()
    {
        result = Process(grod, $"{IF_TOKEN} {TRUE} {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("abc"));
        result = Process(grod, $"{IF_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("def"));
        result = Process(grod, $"{IF_TOKEN} {TRUE} {OR_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("abc"));
        result = Process(grod, $"{IF_TOKEN} {TRUE} {AND_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("def"));
        result = Process(grod, $"{IF_TOKEN} null {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("def"));
    }

    [Test]
    public void Test_IsBool()
    {
        result = Process(grod, $"{WRITE_TOKEN}{ISBOOL_TOKEN}0))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{ISBOOL_TOKEN}1))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{ISBOOL_TOKEN}notboolean))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_Null()
    {
        result = Process(grod, $"{WRITE_TOKEN}{ISNULL_TOKEN}null))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{ISNULL_TOKEN}abc))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{ISNULL_TOKEN}{GET_TOKEN}value)))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
    }

    [Test]
    public void Test_Exists()
    {
        result = Process(grod, $"{WRITE_TOKEN}{EXISTS_TOKEN}test.value))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{SET_TOKEN}test.value,null) {WRITE_TOKEN}{EXISTS_TOKEN}test.value))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{SET_TOKEN}test.value,abc) {WRITE_TOKEN}{EXISTS_TOKEN}test.value))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{SET_TOKEN}test.value,\"\") {WRITE_TOKEN}{EXISTS_TOKEN}test.value))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_IsScript()
    {
        result = Process(grod, $"{SET_TOKEN}test.value,abc) {WRITE_TOKEN}{ISSCRIPT_TOKEN}test.value))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{SET_TOKEN}test.value,\"{GET_TOKEN}value)\") {WRITE_TOKEN}{ISSCRIPT_TOKEN}test.value))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
    }

    [Test]
    public void Test_LE()
    {
        result = Process(grod, $"{WRITE_TOKEN}{LE_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{LE_TOKEN}42,42))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{LE_TOKEN}1,42))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
    }

    [Test]
    public void Test_Lower()
    {
        result = Process(grod, $"{LOWER_TOKEN}ABC)");
        Assert.That(Squash(result), Is.EqualTo("abc"));
        result = Process(grod, $"{LOWER_TOKEN}DEF)");
        Assert.That(Squash(result), Is.EqualTo("def"));
    }

    [Test]
    public void Test_LT()
    {
        result = Process(grod, $"{WRITE_TOKEN}{LT_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{LT_TOKEN}42,42))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{LT_TOKEN}1,42))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
    }

    [Test]
    public void Test_Mod()
    {
        result = Process(grod, $"{WRITE_TOKEN}{MOD_TOKEN}13,4))");
        Assert.That(Squash(result), Is.EqualTo("1"));
        result = Process(grod, $"{WRITE_TOKEN}{MOD_TOKEN}12,4))");
        Assert.That(Squash(result), Is.EqualTo("0"));
    }

    [Test]
    public void Test_ModTo()
    {
        result = Process(grod, $"{SET_TOKEN}value,13) {MODTO_TOKEN}value,4) {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("1"));
    }

    [Test]
    public void Test_Msg()
    {
        result = Process(grod, $"{SET_TOKEN}value,abcdef) {MSG_TOKEN}value)");
        Assert.That(Squash(result), Is.EqualTo("abcdef" + NL_CHAR));
    }

    [Test]
    public void Test_Mul()
    {
        result = Process(grod, $"{WRITE_TOKEN}{MUL_TOKEN}3,4))");
        Assert.That(Squash(result), Is.EqualTo("12"));
    }

    [Test]
    public void Test_MulTo()
    {
        result = Process(grod, $"{SET_TOKEN}value,3) {MULTO_TOKEN}value,4) {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("12"));
    }

    [Test]
    public void Test_NE()
    {
        result = Process(grod, $"{WRITE_TOKEN}{NE_TOKEN}42,6))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{NE_TOKEN}42,42))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_Neg()
    {
        result = Process(grod, $"{WRITE_TOKEN}{NEG_TOKEN}3))");
        Assert.That(Squash(result), Is.EqualTo("-3"));
    }

    [Test]
    public void Test_NegTo()
    {
        result = Process(grod, $"{SET_TOKEN}value,3) {NEGTO_TOKEN}value) {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("-3"));
    }

    [Test]
    public void Test_NL()
    {
        result = Process(grod, NL_TOKEN);
        Assert.That(Squash(result), Is.EqualTo(NL_CHAR));
    }

    [Test]
    public void Test_Not()
    {
        result = Process(grod, $"{IF_TOKEN} {NOT_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("abc"));
    }

    [Test]
    public void Test_Or()
    {
        result = Process(grod, $"{IF_TOKEN} {TRUE} {OR_TOKEN} {FALSE} {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {WRITE_TOKEN}def) {ENDIF_TOKEN}");
        Assert.That(Squash(result), Is.EqualTo("abc"));
    }

    [Test]
    public void Test_Rand()
    {
        result = Process(grod, $"{RAND_TOKEN}30)");
        Assert.That(result[0].Value == TRUE || result[0].Value == FALSE);
    }

    [Test]
    public void Test_Replace()
    {
        result = Process(grod, $"{WRITE_TOKEN}{REPLACE_TOKEN}abcdef,d,x))");
        Assert.That(Squash(result), Is.EqualTo("abcxef"));
    }

    [Test]
    public void Test_Rnd()
    {
        result = Process(grod, $"{SET_TOKEN}value,{RND_TOKEN}20))");
        result = Process(grod, $"{GET_TOKEN}value)");
        var r1 = long.Parse(Squash(result));
        Assert.That(r1 >= 0 && r1 < 20);
    }

    [Test]
    public void Test_Script()
    {
        result = Process(grod, $"{SET_TOKEN}script1,\"{WRITE_TOKEN}abc)\")");
        result = Process(grod, $"{SCRIPT_TOKEN}script1)");
        Assert.That(Squash(result), Is.EqualTo("abc"));
    }

    [Test]
    public void Test_SetOutChannel()
    {
        result = Process(grod, $"{SETOUTCHANNEL_TOKEN}abc)");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Type, Is.EqualTo(MessageType.OutChannel));
            Assert.That(result[0].Value, Is.EqualTo("abc"));
        }
    }

    [Test]
    public void Test_Sub()
    {
        result = Process(grod, $"{WRITE_TOKEN}{SUB_TOKEN}1,3))");
        Assert.That(Squash(result), Is.EqualTo("-2"));
    }

    [Test]
    public void Test_Substring()
    {
        result = Process(grod, $"{WRITE_TOKEN}{SUBSTRING_TOKEN}abcdef,1,4))");
        Assert.That(Squash(result), Is.EqualTo("bcde"));
    }

    [Test]
    public void Test_SubTo()
    {
        result = Process(grod, $"{SET_TOKEN}value,12) {SUBTO_TOKEN}value,7) {WRITE_TOKEN}{GET_TOKEN}value))");
        Assert.That(Squash(result), Is.EqualTo("5"));
    }

    [Test]
    public void Test_Swap()
    {
        result = Process(grod, $"{SET_TOKEN}value1,abc) {SET_TOKEN}value2,def) {SWAP_TOKEN}value1,value2) {WRITE_TOKEN}{GET_TOKEN}value1),{GET_TOKEN}value2))");
        Assert.That(Squash(result), Is.EqualTo("defabc"));
    }

    [Test]
    public void Test_Trim()
    {
        result = Process(grod, $"{SET_TOKEN}value,\"   abc   \") {WRITE_TOKEN}{TRIM_TOKEN}{GET_TOKEN}value)))");
        Assert.That(Squash(result), Is.EqualTo("abc"));
    }

    [Test]
    public void Test_True()
    {
        result = Process(grod, $"{WRITE_TOKEN}{ISTRUE_TOKEN}0))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{WRITE_TOKEN}{ISTRUE_TOKEN}1))");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{WRITE_TOKEN}{ISTRUE_TOKEN}abc))");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
    }

    [Test]
    public void Test_Upper()
    {
        result = Process(grod, $"{WRITE_TOKEN}{UPPER_TOKEN}abc))");
        Assert.That(Squash(result), Is.EqualTo("ABC"));
    }

    [Test]
    public void Test_PrettyScript()
    {
        var script = $"{IF_TOKEN} {EQ_TOKEN}{GET_TOKEN}value),0) {THEN_TOKEN} {WRITE_TOKEN}\"zero\") {ELSE_TOKEN} {WRITE_TOKEN}\"not zero\") {ENDIF_TOKEN}";
        var expected = $"{IF_TOKEN} {EQ_TOKEN}{GET_TOKEN}value),0) {THEN_TOKEN}\r\n\t{WRITE_TOKEN}\"zero\")\r\n{ELSE_TOKEN}\r\n\t{WRITE_TOKEN}\"not zero\")\r\n{ENDIF_TOKEN}";
        var actual = PrettyScript(script);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Test_PrettyScript_Min()
    {
        var script = $"{IF_TOKEN}{EQ_TOKEN}{GET_TOKEN}value),0){THEN_TOKEN}{WRITE_TOKEN}\"zero\"){ELSE_TOKEN}{WRITE_TOKEN}\"not zero\"){ENDIF_TOKEN}";
        var expected = $"{IF_TOKEN} {EQ_TOKEN}{GET_TOKEN}value),0) {THEN_TOKEN}\r\n\t{WRITE_TOKEN}\"zero\")\r\n{ELSE_TOKEN}\r\n\t{WRITE_TOKEN}\"not zero\")\r\n{ENDIF_TOKEN}";
        var actual = PrettyScript(script);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Test_PrettyScript_Same()
    {
        var script = $"{WRITE_TOKEN}\"hello \\\"wonderful\\\" world.\")";
        var actual = PrettyScript(script);
        Assert.That(actual, Is.EqualTo(script));
    }

    [Test]
    public void Test_IfThenNoStatements()
    {
        var script = $"{IF_TOKEN} {EQ_TOKEN}1,1) {THEN_TOKEN} {ENDIF_TOKEN}";
        result = Process(grod, script);
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_IfThenElseNoStatements()
    {
        var script = $"{IF_TOKEN} {EQ_TOKEN}1,2) {THEN_TOKEN} {WRITE_TOKEN}abc) {ELSE_TOKEN} {ENDIF_TOKEN}";
        result = Process(grod, script);
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    //[Test]
    //public void Test_Return()
    //{
    //    var key = "abc";
    //    var value1 = "123";
    //    var value2 = "456";
    //    result = Process(grod, $"{SET_TOKEN}{key},{value1}) {RETURN_TOKEN} {SET_TOKEN}{key},{value2})");
    //    Assert.That(result, Is.Empty);
    //    result = Process(grod, $"{GET_TOKEN}{key})");
    //    Assert.That(Squash(result), Is.EqualTo(value1));
    //}

    [Test]
    public void Test_AddList()
    {
        var key = "abc";
        var value1 = "123";
        var value2 = "456";
        Process(grod, $"{ADDLIST_TOKEN}{key},{value1}) {ADDLIST_TOKEN}{key},{value2})");
        result = Process(grod, $"{GET_TOKEN}{key})");
        Assert.That(Squash(result), Is.EqualTo(value1 + ',' + value2));
    }

    [Test]
    public void Test_ClearList()
    {
        var key = "abc";
        var value1 = "123";
        var value2 = "456";
        Process(grod, $"{ADDLIST_TOKEN}{key},{value1}) {ADDLIST_TOKEN}{key},{value2})");
        Process(grod, $"{CLEARLIST_TOKEN}{key})");
        result = Process(grod, $"{GET_TOKEN}{key})");
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_GetArray()
    {
        var key = "abc";
        var value = "123";
        Process(grod, $"{SETARRAY_TOKEN}{key},2,3,{value})");
        result = Process(grod, $"{GETARRAY_TOKEN}{key},2,3)");
        Assert.That(Squash(result), Is.EqualTo(value));
    }

    [Test]
    public void Test_GetList()
    {
        var key = "abc";
        var value1 = "123";
        var value2 = "456";
        Process(grod, $"{ADDLIST_TOKEN}{key},{value1}) {ADDLIST_TOKEN}{key},{value2})");
        result = Process(grod, $"{GETLIST_TOKEN}{key},0)");
        Assert.That(Squash(result), Is.EqualTo(value1));
        result = Process(grod, $"{GETLIST_TOKEN}{key},1)");
        Assert.That(Squash(result), Is.EqualTo(value2));
        result = Process(grod, $"{GETLIST_TOKEN}{key},2)");
        Assert.That(Squash(result), Is.EqualTo(""));
    }

    [Test]
    public void Test_IsNumber()
    {
        var value1 = "123";
        var value2 = "abc";
        var value3 = "";
        result = Process(grod, $"{ISNUMBER_TOKEN}{value1})");
        Assert.That(Squash(result), Is.EqualTo(TRUE));
        result = Process(grod, $"{ISNUMBER_TOKEN}{value2})");
        Assert.That(Squash(result), Is.EqualTo(FALSE));
        result = Process(grod, $"{ISNUMBER_TOKEN}{value3})");
        Assert.That(result[0].Type, Is.EqualTo(MessageType.Error));
    }

    [Test]
    public void Test_ListLength()
    {
        var key = "abc";
        var value1 = "123";
        var value2 = "456";
        Process(grod, $"{ADDLIST_TOKEN}{key},{value1}) {ADDLIST_TOKEN}{key},{value2})");
        result = Process(grod, $"{LISTLENGTH_TOKEN}{key})");
        Assert.That(Squash(result), Is.EqualTo("2"));
    }

    [Test]
    public void Test_Write()
    {
        var value1 = "123";
        result = Process(grod, $"{WRITE_TOKEN}{value1})");
        Assert.That(Squash(result), Is.EqualTo(value1));
    }

    [Test]
    public void Test_WriteLine()
    {
        var value1 = "123";
        result = Process(grod, $"{WRITELINE_TOKEN}{value1})");
        // @writeline result ends with two characters, '\' and 'n'.
        // This is the expected behavior. See Test_NL().
        Assert.That(Squash(result), Is.EqualTo(value1 + NL_CHAR));
    }

    [Test]
    public void Test_GetBit()
    {
        result = Process(grod, $"{GETBIT_TOKEN}4,2)");
        Assert.That(Squash(result), Is.EqualTo("1"));
        result = Process(grod, $"{GETBIT_TOKEN}8,2)");
        Assert.That(Squash(result), Is.EqualTo("0"));
        result = Process(grod, $"{GETBIT_TOKEN}1073741824,30)");
        Assert.That(Squash(result), Is.EqualTo("1"));
    }

    [Test]
    public void Test_SetBit()
    {
        result = Process(grod, $"{SETBIT_TOKEN}0,2)");
        Assert.That(Squash(result), Is.EqualTo("4"));
        result = Process(grod, $"{SETBIT_TOKEN}0,0)");
        Assert.That(Squash(result), Is.EqualTo("1"));
        result = Process(grod, $"{SETBIT_TOKEN}0,30)");
        Assert.That(Squash(result), Is.EqualTo("1073741824"));
    }

    [Test]
    public void Test_ClearBit()
    {
        result = Process(grod, $"{CLEARBIT_TOKEN}7,2)");
        Assert.That(Squash(result), Is.EqualTo("3"));
        result = Process(grod, $"{CLEARBIT_TOKEN}7,0)");
        Assert.That(Squash(result), Is.EqualTo("6"));
        result = Process(grod, $"{CLEARBIT_TOKEN}1073741824,30)");
        Assert.That(Squash(result), Is.EqualTo("0"));
    }

    [Test]
    public void Test_BitwiseAnd()
    {
        result = Process(grod, $"{BITWISEAND_TOKEN}7,2)");
        Assert.That(Squash(result), Is.EqualTo("2"));
        result = Process(grod, $"{BITWISEAND_TOKEN}8,2)");
        Assert.That(Squash(result), Is.EqualTo("0"));
    }

    [Test]
    public void Test_BitwiseOr()
    {
        result = Process(grod, $"{BITWISEOR_TOKEN}7,2)");
        Assert.That(Squash(result), Is.EqualTo("7"));
        result = Process(grod, $"{BITWISEOR_TOKEN}8,2)");
        Assert.That(Squash(result), Is.EqualTo("10"));
    }

    [Test]
    public void Test_BitwiseXor()
    {
        result = Process(grod, $"{BITWISEXOR_TOKEN}7,2)");
        Assert.That(Squash(result), Is.EqualTo("5"));
        result = Process(grod, $"{BITWISEXOR_TOKEN}8,7)");
        Assert.That(Squash(result), Is.EqualTo("15"));
    }

    [Test]
    public void Test_ToBinary()
    {
        result = Process(grod, $"{TOBINARY_TOKEN}7)");
        Assert.That(Squash(result), Is.EqualTo("111"));
    }

    [Test]
    public void Test_ToInteger()
    {
        result = Process(grod, $"{TOINTEGER_TOKEN}111)");
        Assert.That(Squash(result), Is.EqualTo("7"));
    }

    [Test]
    public void Test_ToHex()
    {
        result = Process(grod, $"{TOHEX_TOKEN}255)");
        Assert.That(Squash(result), Is.EqualTo("FF"));
    }

    [Test]
    public void Test_FromHex()
    {
        result = Process(grod, $"{FROMHEX_TOKEN}FF)");
        Assert.That(Squash(result), Is.EqualTo("255"));
    }

    [Test]
    public void Test_FlipBit()
    {
        result = Process(grod, $"{FLIPBIT_TOKEN}7,2)");
        Assert.That(Squash(result), Is.EqualTo("3"));
        result = Process(grod, $"{FLIPBIT_TOKEN}8,2)");
        Assert.That(Squash(result), Is.EqualTo("12"));
    }

    [Test]
    public void Test_Len()
    {
        result = Process(grod, $"{LEN_TOKEN}abcabc)");
        Assert.That(Squash(result), Is.EqualTo("6"));
        result = Process(grod, $"{LEN_TOKEN}\"\")");
        Assert.That(Squash(result), Is.EqualTo("0"));
        result = Process(grod, $"{LEN_TOKEN}null)");
        Assert.That(Squash(result), Is.EqualTo("4"));
    }

    [Test]
    public void Test_ValidateScript()
    {
        var script1 = $"{SET_TOKEN}value,123) {WRITE_TOKEN}{GET_TOKEN}value))";
        bool isValid = ValidateScript(script1);
        Assert.That(isValid, Is.True);
        var script2 = $"{SET_TOKEN}value,123 {WRITE_TOKEN}{GET_TOKEN}value))";
        isValid = ValidateScript(script2);
        Assert.That(isValid, Is.False);
    }
}
