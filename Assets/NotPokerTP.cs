using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KeepCoding;

public class NotPokerTP : TPScript<NotPokerModule>
{
    private static readonly Regex COMMAND_SYNTAX = new Regex(
        @"(press )?((f|fold|c|check|mn|min|min-raise|mx|max|max-raise|a|allin|b|bluff|t|truth)|(f|fold|c|check|mn|min|mx|max|a|allin) (b|bluff|t|truth))"
        , RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

    public override IEnumerator Process(string command)
    {
        if (Module.IsSolved) yield break;

        if (!COMMAND_SYNTAX.IsMatch(command))
        {
            yield return "sendtochaterror Invalid command";
            yield break;
        }

        yield return null;
        var buttons = command.ToLowerInvariant().Replace("press ", "").Split(" ").ToList();
        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index];
            switch (button)
            {
                case "f":
                case "fold":
                    Module.Buttons1[0].OnInteract();
                    break;
                case "c":
                case "check":
                    Module.Buttons1[1].OnInteract();
                    break;
                case "mn":
                case "min":
                case "min-raise":
                    Module.Buttons1[2].OnInteract();
                    break;
                case "mx":
                case "max":
                case "max-raise":
                    Module.Buttons1[3].OnInteract();
                    break;
                case "a":
                case "allin":
                    Module.Buttons1[4].OnInteract();
                    break;
                case "t":
                case "truth":
                    Module.Buttons2[0].OnInteract();
                    break;
                case "b":
                case "bluff":
                    Module.Buttons2[1].OnInteract();
                    break;
            }

            if (index < buttons.Count - 1)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }
    }

    public override IEnumerator ForceSolve()
    {
        if (Module.IsSolved) yield break;
        
        Module.Buttons1[Module._correctButton1].OnInteract();
        yield return new WaitForSeconds(0.5f);
        Module.Buttons2[Module._correctButton2].OnInteract();
    }
}