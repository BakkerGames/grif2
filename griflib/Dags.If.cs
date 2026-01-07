using static GrifLib.Common;

namespace GrifLib;

public partial class Dags
{
    /// <summary>
    /// Error message for invalid @if syntax.
    /// </summary>
    private const string _invalidIfSyntax = $"Invalid {IF_TOKEN} syntax";

    /// <summary>
    /// Process an @if ... @then ... [@else if ... @then ...] [@else ...] @endif block.
    /// </summary>
    private static List<GrifMessage> ProcessIf(Grod grod, ScriptObj script)
    {
        // conditions
        bool notFlag;
        string token;
        while (script.Index < script.Tokens.Length)
        {
            notFlag = false;
            while (script.Index < script.Tokens.Length &&
                script.Tokens[script.Index].Equals(NOT_TOKEN, OIC))
            {
                notFlag = !notFlag;
                script.Index++;
            }
            var cond = GetCondition(grod, script);
            if (script.ReturnFlag)
            {
                return [];
            }
            if (script.Index >= script.Tokens.Length)
            {
                throw new SystemException(_invalidIfSyntax);
            }
            if (notFlag)
            {
                cond = !cond;
            }
            token = script.Tokens[script.Index++].ToLower();
            if (token == THEN_TOKEN)
            {
                if (!cond)
                {
                    SkipToElseEndif(script);
                    if (script.ReturnFlag)
                    {
                        return [];
                    }
                    if (script.Tokens[script.Index].Equals(ELSEIF_TOKEN, OIC))
                    {
                        script.Index++;
                        return ProcessIf(grod, script);
                    }
                    if (script.Tokens[script.Index].Equals(ENDIF_TOKEN, OIC))
                    {
                        script.Index++;
                        return [];
                    }
                    // @else
                    script.Index++;
                }
                break;
            }
            if (token == AND_TOKEN)
            {
                if (!cond)
                {
                    SkipOverThen(script);
                    if (script.ReturnFlag)
                    {
                        return [];
                    }
                    SkipToElseEndif(script);
                    if (script.ReturnFlag)
                    {
                        return [];
                    }
                    if (script.Tokens[script.Index].Equals(ELSEIF_TOKEN, OIC))
                    {
                        script.Index++;
                        return ProcessIf(grod, script);
                    }
                    if (script.Tokens[script.Index].Equals(ENDIF_TOKEN, OIC))
                    {
                        script.Index++;
                        return [];
                    }
                    // @else
                    script.Index++;
                    break;
                }
            }
            else if (token == OR_TOKEN)
            {
                if (cond)
                {
                    SkipOverThen(script);
                    if (script.ReturnFlag)
                    {
                        return [];
                    }
                    break;
                }
            }
            else
            {
                throw new SystemException($"Unknown token in {IF_TOKEN}: {token}");
            }
        }
        // process all commands in this section
        List<GrifMessage> result = [];
        while (script.Index < script.Tokens.Length)
        {
            token = script.Tokens[script.Index].ToLower();
            if (token == ELSE_TOKEN || token == ELSEIF_TOKEN || token == ENDIF_TOKEN)
            {
                SkipOverEndif(script);
                return result;
            }
            result.AddRange(ProcessOneCommand(grod, script));
            if (script.ReturnFlag)
            {
                return result;
            }
        }
        throw new SystemException(_invalidIfSyntax);
    }

    /// <summary>
    /// Skip over tokens until the matching @endif is found.
    /// </summary>
    private static void SkipOverEndif(ScriptObj script)
    {
        if (script.ReturnFlag)
        {
            return;
        }
        while (script.Index < script.Tokens.Length)
        {
            var token = script.Tokens[script.Index++];
            if (token.Equals(ENDIF_TOKEN, OIC))
            {
                return;
            }
            if (token.Equals(IF_TOKEN, OIC))
            {
                SkipOverEndif(script);
                if (script.ReturnFlag)
                {
                    return;
                }
            }
        }
        throw new SystemException($"Missing {ENDIF_TOKEN}");
    }

    /// <summary>
    /// Skip over tokens until the matching @then is found.
    /// </summary>
    private static void SkipOverThen(ScriptObj script)
    {
        if (script.ReturnFlag)
        {
            return;
        }
        while (script.Index < script.Tokens.Length)
        {
            var token = script.Tokens[script.Index++].ToLower();
            if (token == THEN_TOKEN)
            {
                return;
            }
        }
        throw new SystemException($"Missing {THEN_TOKEN}");
    }

    /// <summary>
    /// Skip to the next @else, @elseif, or @endif token.
    /// </summary>
    private static void SkipToElseEndif(ScriptObj script)
    {
        if (script.ReturnFlag)
        {
            return;
        }
        while (script.Index < script.Tokens.Length)
        {
            var token = script.Tokens[script.Index].ToLower();
            if (token == ELSE_TOKEN || token == ELSEIF_TOKEN || token == ENDIF_TOKEN)
            {
                return;
            }
            script.Index++;
            if (token.Equals(IF_TOKEN, OIC))
            {
                SkipOverEndif(script);
                if (script.ReturnFlag)
                {
                    return;
                }
            }
        }
        throw new SystemException(_invalidIfSyntax);
    }
}
