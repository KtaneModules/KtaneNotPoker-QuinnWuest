using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using KeepCoding;

public class NotPokerTP : TPScript<NotPokerModule>
{
    public override IEnumerator Process(string command)
    {
        if (Module.IsSolved)
            yield break;
        command = command.Trim().ToLowerInvariant();

        var m = Regex.Match(command, @"^\s*(press\s+)?(?<button1>f|fold|c|check|mn|min|minraise|min\-raise|mx|max|maxraise|max\-raise|a|allin|all\-in)\s*([,;])?\s+(?<button2>bluff|b|truth|t)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        if (!m.Success)
        {
            yield return "sendtochaterror Invalid command";
            yield break;
        }

        yield return null;
        var buttons = new string[] { m.Groups["button1"].Value, m.Groups["button2"].Value };
        var list = new List<KMSelectable>();
        for (var index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            switch (button)
            {
                case "f":
                case "fold":
                    list.Add(Module.Buttons1[0]);
                    break;
                case "c":
                case "check":
                    list.Add(Module.Buttons1[1]);
                    break;
                case "mn":
                case "min":
                case "min-raise":
                    list.Add(Module.Buttons1[2]);
                    break;
                case "mx":
                case "max":
                case "max-raise":
                    list.Add(Module.Buttons1[3]);
                    break;
                case "a":
                case "allin":
                    list.Add(Module.Buttons1[4]);
                    break;
                case "t":
                case "truth":
                    list.Add(Module.Buttons2[0]);
                    break;
                case "b":
                case "bluff":
                    list.Add(Module.Buttons2[1]);
                    break;
                default:
                    yield break;
            }
        }
        yield return null;
        for (int i = 0; i < list.Count; i++)
        {
            list[i].OnInteract();
            if (i != list.Count - 1)
                yield return new WaitForSeconds(0.5f);
        }
    }

    public override IEnumerator ForceSolve()
    {
        if (Module.IsSolved)
            yield break;

        if (!Module._pressedButton1)
        {
            Module.Buttons1[Module._correctButton1].OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
        Module.Buttons2[Module._correctButton2].OnInteract();
    }
}