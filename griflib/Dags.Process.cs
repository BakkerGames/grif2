using System.Text;
using static GrifLib.Common;

namespace GrifLib;

public partial class Dags
{
    /// <summary>
    /// Process one command from the token list.
    /// </summary>
    private static List<GrifMessage> ProcessOneCommand(Grod grod, ScriptObj script)
    {
        List<GrifMessage> result = [];
        string? value;
        long long1, long2;
        int int1;
        long longAnswer;
        bool boolAnswer;
        bool isNull0;
        bool isNull1;
        string[] list1;
        string[] list2;
        try
        {
            if (script.Index >= script.Tokens.Length)
            {
                return result;
            }
            if (script.ReturnFlag)
            {
                script.Index = script.Tokens.Length;
                return result;
            }
            var token = script.Tokens[script.Index++];
            // static value
            if (!token.StartsWith(SCRIPT_CHAR))
            {
                result.Add(new GrifMessage(MessageType.Internal, token));
                return result;
            }
            // script.Tokens without parameters
            if (!token.EndsWith('('))
            {
                switch (token.ToLower())
                {
                    case IF_TOKEN:
                        result.AddRange(ProcessIf(grod, script));
                        break;
                    case GETINCHANNEL_TOKEN:
                        result.Add(new GrifMessage(MessageType.Internal, grod.Get(INCHANNEL, true) ?? ""));
                        grod.Remove(INCHANNEL, false); // Clear after reading
                        break;
                    case NL_TOKEN:
                        result.Add(new GrifMessage(MessageType.Text, NL_CHAR));
                        break;
                    case RETURN_TOKEN:
                        script.Index = script.Tokens.Length;
                        script.ReturnFlag = true; // End processing
                        break;
                    case WHILE_TOKEN:
                        result.AddRange(ProcessWhile(grod, script));
                        break;
                    case AND_TOKEN:
                    case DO_TOKEN:
                    case ELSEIF_TOKEN:
                    case ELSE_TOKEN:
                    case ENDFOREACHKEY_TOKEN:
                    case ENDFOREACHLIST_TOKEN:
                    case ENDFOR_TOKEN:
                    case ENDIF_TOKEN:
                    case ENDWHILE_TOKEN:
                    case NOT_TOKEN:
                    case OR_TOKEN:
                    case THEN_TOKEN:
                        throw new SystemException($"Token found out of context: {token}");
                    default:
                        value = GetGlobalOrLocal(grod, script, token, true);
                        if (value != null)
                        {
                            var userResult = Process(grod, value);
                            result.AddRange(userResult);
                            break;
                        }
                        throw new SystemException($"Unknown token: {token}");
                }
                return result;
            }
            var p = GetParameters(grod, script);
            if (p.Any(x => x.Type == MessageType.Error))
            {
                script.Index = script.Tokens.Length;
                result.AddRange(p.Where(x => x.Type == MessageType.Error));
                return result;
            }
            switch (token.ToLower())
            {
                case ABS_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(p[0].Value);
                    if (long1 < 0)
                    {
                        long1 = -long1;
                    }
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case ADD_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    longAnswer = long1 + long2;
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case ADDLIST_TOKEN:
                    CheckParameterCount(p, 2);
                    AddListItem(grod, script, p[0].Value, p[1].Value);
                    break;
                case ADDTO_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(GetGlobalOrLocal(grod, script, p[0].Value, true));
                    long2 = GetNumberValue(p[1].Value);
                    longAnswer = long1 + long2;
                    SetGlobalOrLocal(grod, script, p[0].Value, longAnswer.ToString());
                    break;
                case BITWISEAND_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 & long2;
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case BITWISEOR_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 | long2;
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case BITWISEXOR_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 ^ long2;
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case CLEARARRAY_TOKEN:
                    CheckParameterCount(p, 1);
                    ClearArray(grod, script, p[0].Value);
                    break;
                case CLEARBIT_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0 || long2 > 30)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 ^ (long)Math.Pow(2, long2);
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case CLEARLIST_TOKEN:
                    CheckParameterCount(p, 1);
                    if (string.IsNullOrWhiteSpace(p[0].Value))
                    {
                        throw new SystemException($"{token}): List name cannot be blank");
                    }
                    SetGlobalOrLocal(grod, script, p[0].Value, null);
                    break;
                case COMMENT_TOKEN:
                    // Ignore comments
                    break;
                case CONCAT_TOKEN:
                    CheckParameterAtLeastOne(p);
                    StringBuilder sb = new();
                    foreach (var item in p)
                    {
                        sb.Append(item.Value);
                    }
                    result.Add(new GrifMessage(MessageType.Internal, sb.ToString()));
                    break;
                case CONTAINS_TOKEN:
                    CheckParameterCount(p, 2);
                    list1 = SplitList(p[0].Value);
                    if (list1.Contains(p[1].Value))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                        break;
                    }
                    result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    break;
                case CONTAINSLIST_TOKEN:
                    CheckParameterCount(p, 2);
                    list1 = SplitList(p[0].Value);
                    list2 = SplitList(p[1].Value);
                    if (list1.Length == 0 || list2.Length == 0)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                        break;
                    }
                    foreach (var listItem in list2)
                    {
                        if (!list1.Contains(listItem))
                        {
                            result.Add(new GrifMessage(MessageType.Internal, FALSE));
                            break;
                        }
                    }
                    result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    break;
                case DEBUG_TOKEN:
                    CheckParameterCount(p, 1);
                    if (IsTrue(grod.Get(DEBUG_FLAG, true)))
                    {
                        result.Add(new GrifMessage(MessageType.Text, p[0].Value));
                        result.Add(new GrifMessage(MessageType.Text, NL_CHAR));
                    }
                    break;
                case DIV_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long2 == 0)
                    {
                        throw new SystemException("Division by zero is not allowed.");
                    }
                    long1 /= long2;
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case DIVTO_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(GetGlobalOrLocal(grod, script, p[0].Value, true));
                    long2 = GetNumberValue(p[1].Value);
                    if (long2 == 0)
                    {
                        throw new SystemException("Division by zero is not allowed.");
                    }
                    long1 /= long2;
                    SetGlobalOrLocal(grod, script, p[0].Value, long1.ToString());
                    break;
                case EQ_TOKEN:
                    CheckParameterCount(p, 2);
                    isNull0 = IsNull(p[0].Value);
                    isNull1 = IsNull(p[1].Value);
                    if (isNull0 && isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (isNull0 || isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (string.Compare(p[0].Value, p[1].Value, OIC) == 0)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (long.TryParse(p[0].Value, out long1) && long.TryParse(p[1].Value, out long2))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(long1 == long2)));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    break;
                case EXEC_TOKEN:
                    CheckParameterCount(p, 1);
                    value = p[0].Value;
                    result.AddRange(Process(grod, value));
                    break;
                case EXISTS_TOKEN:
                    CheckParameterCount(p, 1);
                    value = GetGlobalOrLocal(grod, script, p[0].Value, true);
                    result.Add(new GrifMessage(MessageType.Internal, TrueFalse(!IsNullOrEmpty(value))));
                    break;
                case ISFALSE_TOKEN:
                case ISFALSE2_TOKEN:
                    CheckParameterCount(p, 1);
                    try
                    {
                        boolAnswer = IsTrue(p[0].Value);
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(!boolAnswer)));
                    }
                    catch (Exception)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(false)));
                    }
                    break;
                case FLIPBIT_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0 || long2 > 30)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 ^ (long)Math.Pow(2, long2);
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case FOR_TOKEN:
                    CheckParameterCount(p, 3);
                    HandleFor(grod, script, p, result);
                    break;
                case FOREACHKEY_TOKEN:
                    if (p.Count != 2 && p.Count != 3)
                    {
                        CheckParameterCount(p, 3);
                    }
                    HandleForEachKey(grod, script, p, result);
                    break;
                case FOREACHLIST_TOKEN:
                    CheckParameterCount(p, 2);
                    HandleForEachList(grod, script, p, result);
                    break;
                case FORMAT_TOKEN:
                    CheckParameterAtLeastOne(p);
                    value = p[0].Value;
                    for (int i = 1; i < p.Count; i++)
                    {
                        value = value.Replace($"{{{i - 1}}}", p[i].Value); // {0} = p[1]
                    }
                    result.Add(new GrifMessage(MessageType.Internal, value));
                    break;
                case FROMHEX_TOKEN:
                    CheckParameterCount(p, 1);
                    try
                    {
                        long1 = Convert.ToInt32(p[0].Value, 16);
                        value = long1.ToString();
                        result.Add(new GrifMessage(MessageType.Internal, value));
                    }
                    catch (Exception)
                    {
                        throw new SystemException($"{token}{p[0].Value}): Invalid hex string");
                    }
                    break;
                case GE_TOKEN:
                    CheckParameterCount(p, 2);
                    isNull0 = IsNull(p[0].Value);
                    isNull1 = IsNull(p[1].Value);
                    if (isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (isNull0)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (long.TryParse(p[0].Value, out long1) && long.TryParse(p[1].Value, out long2))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(long1 >= long2)));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal,
                            TrueFalse(string.Compare(p[0].Value, p[1].Value, OIC) >= 0)));
                    }
                    break;
                case GET_TOKEN:
                    CheckParameterCount(p, 1);
                    value = GetGlobalOrLocal(grod, script, p[0].Value, true) ?? "";
                    result.Add(new GrifMessage(MessageType.Internal, value));
                    break;
                case GETARRAY_TOKEN:
                    CheckParameterCount(p, 3);
                    if (string.IsNullOrWhiteSpace(p[0].Value))
                    {
                        throw new SystemException("Array name cannot be blank");
                    }
                    long1 = GetNumberValue(p[1].Value);
                    long2 = GetNumberValue(p[2].Value);
                    value = GetArrayItem(grod, script, p[0].Value, long1, long2);
                    result.Add(new GrifMessage(MessageType.Internal, value ?? ""));
                    break;
                case GETBIT_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0 || long2 > 30)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 & (long)Math.Pow(2, long2);
                    if (longAnswer != 0)
                    {
                        longAnswer = 1;
                    }
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case GETCHAR_TOKEN:
                    CheckParameterCount(p, 2);
                    int1 = (int)GetNumberValue(p[1].Value);
                    if (int1 < 0)
                    {
                        throw new SystemException("Index out of range");
                    }
                    if (int1 >= p[0].Value.Length)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, " "));
                    }
                    else
                    {
                        value = p[0].Value.Substring(int1, 1);
                        result.Add(new GrifMessage(MessageType.Internal, value));
                    }
                    break;
                case GETLIST_TOKEN:
                    CheckParameterCount(p, 2);
                    if (string.IsNullOrWhiteSpace(p[0].Value))
                    {
                        throw new SystemException("List name cannot be blank");
                    }
                    long1 = GetNumberValue(p[1].Value);
                    value = GetListItem(grod, script, p[0].Value, long1);
                    result.Add(new GrifMessage(MessageType.Internal, value ?? ""));
                    break;
                case GETVALUE_TOKEN:
                    CheckParameterCount(p, 1);
                    value = GetValue(grod, GetGlobalOrLocal(grod, script, p[0].Value, true));
                    result.Add(new GrifMessage(MessageType.Internal, value));
                    break;
                case GOLABEL_TOKEN:
                    CheckParameterCount(p, 1);
                    for (int i = 0; i < script.Tokens.Length - 1; i++)
                    {
                        if (script.Tokens[i] == LABEL_TOKEN && script.Tokens[i + 1] == p[0].Value && script.Tokens[i + 2] == ")")
                        {
                            script.Index = i + 3;
                        }
                    }
                    break;
                case GT_TOKEN:
                    CheckParameterCount(p, 2);
                    isNull0 = IsNull(p[0].Value);
                    isNull1 = IsNull(p[1].Value);
                    if (!isNull0 && isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (isNull0)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (long.TryParse(p[0].Value, out long1) && long.TryParse(p[1].Value, out long2))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(long1 > long2)));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal,
                            TrueFalse(string.Compare(p[0].Value, p[1].Value, OIC) > 0)));
                    }
                    break;
                case INLIST_TOKEN:
                    CheckParameterCount(p, 2);
                    if (InList(grod, script, p[0].Value, p[1].Value))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    break;
                case INSERTATLIST_TOKEN:
                    CheckParameterCount(p, 3);
                    long1 = GetNumberValue(p[1].Value);
                    InsertAtListItem(grod, script, p[0].Value, long1, p[2].Value);
                    break;
                case ISBOOL_TOKEN:
                    CheckParameterCount(p, 1);
                    try
                    {
                        boolAnswer = IsTrue(p[0].Value);
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    catch (Exception)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    break;
                case ISNUMBER_TOKEN:
                    CheckParameterCount(p, 1);
                    if (long.TryParse(p[0].Value, out _))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    break;
                case ISSCRIPT_TOKEN:
                    CheckParameterCount(p, 1);
                    value = GetGlobalOrLocal(grod, script, p[0].Value, true);
                    if (IsNull(value))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(IsScript(value))));
                    }
                    break;
                case LABEL_TOKEN:
                    CheckParameterCount(p, 1);
                    break;
                case LE_TOKEN:
                    CheckParameterCount(p, 2);
                    isNull0 = IsNull(p[0].Value);
                    isNull1 = IsNull(p[1].Value);
                    if (isNull0)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (long.TryParse(p[0].Value, out long1) && long.TryParse(p[1].Value, out long2))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(long1 <= long2)));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal,
                            TrueFalse(string.Compare(p[0].Value, p[1].Value, OIC) <= 0)));
                    }
                    break;
                case LEN_TOKEN:
                    CheckParameterCount(p, 1);
                    result.Add(new GrifMessage(MessageType.Internal, p[0].Value.Length.ToString()));
                    break;
                case LISTLENGTH_TOKEN:
                    CheckParameterCount(p, 1);
                    if (string.IsNullOrWhiteSpace(p[0].Value))
                    {
                        throw new SystemException($"{token}): List name cannot be blank");
                    }
                    value = GetGlobalOrLocal(grod, script, p[0].Value, true);
                    if (IsNullOrEmpty(value))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, "0"));
                    }
                    else
                    {
                        list1 = SplitList(value);
                        result.Add(new GrifMessage(MessageType.Internal, list1.Length.ToString()));
                    }
                    break;
                case LOWER_TOKEN:
                    CheckParameterCount(p, 1);
                    result.Add(new GrifMessage(MessageType.Internal, p[0].Value.ToLower()));
                    break;
                case LT_TOKEN:
                    CheckParameterCount(p, 2);
                    isNull0 = IsNull(p[0].Value);
                    isNull1 = IsNull(p[1].Value);
                    if (isNull0 && !isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (long.TryParse(p[0].Value, out long1) && long.TryParse(p[1].Value, out long2))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(long1 < long2)));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal,
                            TrueFalse(string.Compare(p[0].Value, p[1].Value, OIC) < 0)));
                    }
                    break;
                case MAX_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    long1 = Math.Max(long1, long2);
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case MIN_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    long1 = Math.Min(long1, long2);
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case MOD_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    long1 %= long2;
                    if (long1 < 0) // make positive
                    {
                        long1 += long2;
                    }
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case MODTO_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(GetGlobalOrLocal(grod, script, p[0].Value, true));
                    long2 = GetNumberValue(p[1].Value);
                    long1 %= long2;
                    if (long1 < 0) // make positive
                    {
                        long1 += long2;
                    }
                    SetGlobalOrLocal(grod, script, p[0].Value, long1.ToString());
                    break;
                case MSG_TOKEN:
                    CheckParameterCount(p, 1);
                    var tempResult = Process(grod, GetGlobalOrLocal(grod, script, p[0].Value, true));
                    foreach (var msgItem in tempResult)
                    {
                        if (msgItem.Type == MessageType.Text || msgItem.Type == MessageType.Internal)
                        {
                            value = msgItem.Value;
                            if (!string.IsNullOrEmpty(value)) // whitespace is allowed
                            {
                                result.Add(new GrifMessage(MessageType.Text, value));
                            }
                        }
                    }
                    result.Add(new GrifMessage(MessageType.Text, NL_CHAR));
                    break;
                case MUL_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    long1 *= long2;
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case MULTO_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(GetGlobalOrLocal(grod, script, p[0].Value, true));
                    long2 = GetNumberValue(p[1].Value);
                    long1 *= long2;
                    SetGlobalOrLocal(grod, script, p[0].Value, long1.ToString());
                    break;
                case NE_TOKEN:
                    CheckParameterCount(p, 2);
                    isNull0 = IsNull(p[0].Value);
                    isNull1 = IsNull(p[1].Value);
                    if (isNull0 && isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (isNull0 || isNull1)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    else if (string.Compare(p[0].Value, p[1].Value, OIC) == 0)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, FALSE));
                    }
                    else if (long.TryParse(p[0].Value, out long1) && long.TryParse(p[1].Value, out long2))
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(long1 != long2)));
                    }
                    else
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TRUE));
                    }
                    break;
                case NEG_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(p[0].Value);
                    long1 = -long1;
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case NEGTO_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(GetGlobalOrLocal(grod, script, p[0].Value, true));
                    long1 = -long1;
                    SetGlobalOrLocal(grod, script, p[0].Value, long1.ToString());
                    break;
                case ISNULL_TOKEN:
                case ISNULL2_TOKEN:
                    CheckParameterCount(p, 1);
                    result.Add(new GrifMessage(MessageType.Internal, TrueFalse(IsNullOrEmpty(p[0].Value))));
                    break;
                case RAND_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = _random.Next(100);
                    boolAnswer = long2 < long1;
                    result.Add(new GrifMessage(MessageType.Internal, TrueFalse(boolAnswer)));
                    break;
                case REMOVEATLIST_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[1].Value);
                    RemoveAtListItem(grod, script, p[0].Value, long1);
                    break;
                case REPLACE_TOKEN:
                    CheckParameterCount(p, 3);
                    result.Add(new GrifMessage(MessageType.Internal, p[0].Value.Replace(p[1].Value, p[2].Value, OIC)));
                    break;
                case RND_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(p[0].Value);
                    result.Add(new GrifMessage(MessageType.Internal, _random.Next((int)long1).ToString()));
                    break;
                case SCRIPT_TOKEN:
                    CheckParameterCount(p, 1);
                    value = GetGlobalOrLocal(grod, script, p[0].Value, true);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        var scriptResult = Process(grod, value);
                        result.AddRange(scriptResult);
                    }
                    break;
                case SET_TOKEN:
                    CheckParameterCount(p, 2);
                    SetGlobalOrLocal(grod, script, p[0].Value, p[1].Value);
                    break;
                case SETARRAY_TOKEN:
                    CheckParameterCount(p, 4);
                    if (string.IsNullOrWhiteSpace(p[0].Value))
                    {
                        throw new SystemException("Array name cannot be blank");
                    }
                    long1 = GetNumberValue(p[1].Value); // y
                    long2 = GetNumberValue(p[2].Value); // x
                    SetArrayItem(grod, script, p[0].Value, long1, long2, p[3].Value);
                    break;
                case SETBIT_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    if (long1 < 0 || long2 < 0 || long2 > 30)
                    {
                        throw new SystemException($"{token}{long1},{long2}): Invalid parameters");
                    }
                    longAnswer = long1 | (long)Math.Pow(2, long2);
                    result.Add(new GrifMessage(MessageType.Internal, longAnswer.ToString()));
                    break;
                case SETCHAR_TOKEN:
                    CheckParameterCount(p, 3);
                    int1 = (int)GetNumberValue(p[1].Value);
                    if (int1 < 0)
                    {
                        throw new SystemException("Index out of range");
                    }
                    if (p[2].Value == null || p[2].Value == NULL || p[2].Value.Length < 1)
                    {
                        throw new SystemException("Character not supplied");
                    }
                    value = p[0].Value;
                    if (int1 == value.Length)
                    {
                        value += p[2].Value[0];
                    }
                    else if (int1 > value.Length)
                    {
                        value += new string(' ', int1 - value.Length - 1) + p[2].Value[0];
                    }
                    else
                    {
                        value = value[..int1] + p[2].Value[0] + value[(int1 + 1)..];
                    }
                    result.Add(new GrifMessage(MessageType.Internal, value));
                    break;
                case SETEXTRA_TOKEN:
                    // Set a value in a parent grod by name.
                    // Good for configuration settings or notes or global variables across all saves.
                    // Use OUTCHANNEL_ADD_EXTRA_GROD first to create a parent grod with a name.
                    CheckParameterCount(p, 3);
                    result.Add(new GrifMessage(MessageType.OutChannel, OUTCHANNEL_SET_EXTRA_VALUE,
                        p[0].Value + '\t' + p[1].Value + '\t' + p[2].Value));
                    break;
                case SETLIST_TOKEN:
                    CheckParameterCount(p, 3);
                    long1 = GetNumberValue(p[1].Value);
                    SetListItem(grod, script, p[0].Value, long1, p[2].Value);
                    break;
                case SETOUTCHANNEL_TOKEN:
                    if (p.Count == 1)
                    {
                        result.Add(new GrifMessage(MessageType.OutChannel, p[0].Value));
                    }
                    else if (p.Count == 2)
                    {
                        result.Add(new GrifMessage(MessageType.OutChannel, p[0].Value, p[1].Value));
                    }
                    break;
                case SUB_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(p[0].Value);
                    long2 = GetNumberValue(p[1].Value);
                    long1 -= long2;
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case SUBSTRING_TOKEN:
                    CheckParmeterCountBetween(p, 2, 3);
                    long1 = GetNumberValue(p[1].Value);
                    if (p.Count == 2)
                    {
                        long2 = p[0].Value.Length - long1;
                    }
                    else
                    {
                        long2 = GetNumberValue(p[2].Value);
                    }
                    if (long1 < 0 || long2 < 0 || long1 + long2 > p[0].Value.Length)
                    {
                        throw new SystemException($"{token}{p[0].Value},{long1},{long2}): Invalid parameters");
                    }
                    result.Add(new GrifMessage(MessageType.Internal, p[0].Value.Substring((int)long1, (int)long2)));
                    break;
                case SUBTO_TOKEN:
                    CheckParameterCount(p, 2);
                    long1 = GetNumberValue(GetGlobalOrLocal(grod, script, p[0].Value, true));
                    long2 = GetNumberValue(p[1].Value);
                    long1 -= long2;
                    SetGlobalOrLocal(grod, script, p[0].Value, long1.ToString());
                    break;
                case SWAP_TOKEN:
                    CheckParameterCount(p, 2);
                    value = GetGlobalOrLocal(grod, script, p[0].Value, true);
                    SetGlobalOrLocal(grod, script, p[0].Value, GetGlobalOrLocal(grod, script, p[1].Value, true));
                    SetGlobalOrLocal(grod, script, p[1].Value, value);
                    break;
                case TOBINARY_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(p[0].Value);
                    result.Add(new GrifMessage(MessageType.Internal, Convert.ToString(long1, 2)));
                    break;
                case TOHEX_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = GetNumberValue(p[0].Value);
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString("X")));
                    break;
                case TOINTEGER_TOKEN:
                    CheckParameterCount(p, 1);
                    long1 = Convert.ToInt32(p[0].Value, 2);
                    result.Add(new GrifMessage(MessageType.Internal, long1.ToString()));
                    break;
                case TRIM_TOKEN:
                    CheckParameterCount(p, 1);
                    result.Add(new GrifMessage(MessageType.Internal, p[0].Value.Trim()));
                    break;
                case ISTRUE_TOKEN:
                case ISTRUE2_TOKEN:
                    CheckParameterCount(p, 1);
                    try
                    {
                        boolAnswer = IsTrue(p[0].Value);
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(boolAnswer)));
                    }
                    catch (Exception)
                    {
                        result.Add(new GrifMessage(MessageType.Internal, TrueFalse(false)));
                    }
                    break;
                case UPPER_TOKEN:
                    CheckParameterCount(p, 1);
                    result.Add(new GrifMessage(MessageType.Internal, p[0].Value.ToUpper()));
                    break;
                case WRITE_TOKEN:
                    CheckParameterAtLeastOne(p);
                    foreach (var item in p) // concatenate all parameters
                    {
                        value = GetValue(grod, item.Value);
                        result.Add(new GrifMessage(MessageType.Text, value));
                    }
                    break;
                case WRITELINE_TOKEN:
                    CheckParameterAtLeastOne(p);
                    foreach (var item in p) // concatenate all parameters
                    {
                        value = GetValue(grod, item.Value);
                        result.Add(new GrifMessage(MessageType.Text, value));
                    }
                    result.Add(new GrifMessage(MessageType.Text, NL_CHAR));
                    break;
                default:
                    var userResult = GetUserDefinedFunctionValues(grod, script, token, p);
                    result.AddRange(userResult);
                    break;
            }
            return result;
        }
        catch (Exception ex)
        {
            // Handle exceptions and return error item
            var error = $"Error processing command at index {script.Index}: {ex.Message}";
            var extraInfo = new StringBuilder();
            for (int i = 0; i < script.Tokens.Length; i++)
            {
                var token = script.Tokens[i];
                if (token.Length > 50)
                {
                    token = string.Concat(token.AsSpan(0, 47), "...");
                }
                extraInfo.AppendLine($"{i}: {token}");
            }
            result.Add(new GrifMessage(MessageType.Error, error, extraInfo.ToString()));
            return result;
        }
    }
}
