using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KeepCoding;
using UnityEngine;

public class ManualGenerator : MonoBehaviour
{
    private static readonly string[] RANKS = "A 2 3 4 5 6 7 8 9 10 J Q K".Split(' ');
    private static readonly string[] SUITS = "♠ ♥ ♣ ♦".Split(' ');
    private const string SERIAL_CHARS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private Dictionary<char, List<string>> _decks;

    public Dictionary<char, List<string>> Decks
    {
        get
        {
            if (_decks == null)
            {
                GenerateManual();
            }

            return _decks;
        }
    }

    private void GenerateManual()
    {
        var module = GetComponent<NotPokerModule>();
        var ruleSeedComponent = GetComponent<KMRuleSeedable>();
        var rand = ruleSeedComponent.GetRNG();

        module.Log("Generating manual with rule seed {0}:", rand.Seed);

        var deck = (from suit in SUITS from rank in RANKS select rank + suit).ToList();
        _decks = new Dictionary<char, List<string>>();
        foreach (var c in SERIAL_CHARS)
        {
            rand.ShuffleFisherYates(deck);
            Decks[c] = new List<string>(deck);
            module.Log("{0} : {1}", c, deck.Join(" "));
        }
    }
}