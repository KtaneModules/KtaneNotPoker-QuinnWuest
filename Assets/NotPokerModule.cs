using System.Collections.Generic;
using System.Linq;
using KeepCoding;
using KModkit;
using UnityEngine;

public class NotPokerModule : ModuleScript
{
    private static readonly string[] PROHIBITED_STARTING_CARDS = { "A♠", "K♥", "5♦", "2♣" };
    private static readonly string[] BUTTON_NAMES_1 = { "Fold", "Check", "Min Raise", "Max Raise", "All-In" };
    private static readonly string[] BUTTON_NAMES_2 = { "Truth", "Bluff" };

    private static readonly string[] RESPONSES =
    {
        "Terrible play!",
        "Awful play!",
        "Really?",
        "Really, really?",
        "Sure about that?",
        "Are you sure?"
    };

    private static readonly string[] HAND_NAMES =
    {
        "No Hand",
        "Pair",
        "Two Pair",
        "Three of a Kind",
        "Straight",
        "Flush",
        "Full House",
        "Four of a Kind",
        "Straight Flush",
        "Royal Flush",
    };

    private static readonly Dictionary<string, List<string>> _usedCardsPerBomb = new Dictionary<string, List<string>>();

    public KMBombInfo BombInfo;
    public Texture[] CardTextures;
    public Texture[] ChipTextures;
    public MeshRenderer MainCardDisplay;
    public TextMesh ResponseText;
    public MeshRenderer Chip;

    public KMSelectable[] Buttons1;
    public KMSelectable[] Buttons2;

    internal int _correctButton1;
    internal int _correctButton2;
    private bool _pressedButton1 = false;

    private void Start()
    {
        var serialNumber = BombInfo.GetSerialNumber();
        if (!_usedCardsPerBomb.ContainsKey(serialNumber))
            _usedCardsPerBomb[serialNumber] = new List<string>();

        // Generate puzzle
        GeneratePuzzle();

        // Hooks
        var i = 0;
        foreach (var button in Buttons1)
        {
            var j = i++;
            button.Assign(onInteract: () => HandlePressButton1(j));
        }

        i = 0;
        foreach (var button in Buttons2)
        {
            var j = i++;
            button.Assign(onInteract: () => HandlePressButton2(j));
        }

        BombInfo.OnBombSolved = BombInfo.OnBombExploded = delegate
        {
            if (_usedCardsPerBomb.ContainsKey(serialNumber))
                _usedCardsPerBomb.Remove(serialNumber);
        };
    }

    private void HandlePressButton()
    {
        PlaySound(KMSoundOverride.SoundEffect.ButtonPress);
        Get<KMSelectable>().AddInteractionPunch();
    }

    private void HandlePressButton1(int index)
    {
        // Sound and interaction punch
        HandlePressButton();

        // Ignore if module is solved or the lights are not on
        if (IsSolved || !IsActive)
        {
            return;
        }

        // Strike if waiting for button 2
        if (_pressedButton1)
        {
            Strike();
            Log("Strike! Pressed '{0}' when I was expecting '{1}' to be pressed.", BUTTON_NAMES_1[index],
                BUTTON_NAMES_2[_correctButton2]);
            return;
        }

        // Strike if this wasn't the correct button
        if (index != _correctButton1)
        {
            Strike();
            Log("Strike! Pressed '{0}' when I was expecting '{1}' to be pressed.", BUTTON_NAMES_1[index],
                BUTTON_NAMES_1[_correctButton1]);
            return;
        }

        // Success!
        Log("Successfully pressed '{0}'. Now expecting '{1}'.", BUTTON_NAMES_1[index], BUTTON_NAMES_2[_correctButton2]);
        _pressedButton1 = true;

        // Display message
        ResponseText.GetComponent<Renderer>().enabled = true;
        // PlaySound("MessageSound");
        GetComponent<KMAudio>().PlaySoundAtTransform("MessageSound", transform);
    }

    private void HandlePressButton2(int index)
    {
        // Sound and interaction punch
        HandlePressButton();

        // Ignore if module is solved of the lights are not on
        if (IsSolved || !IsActive)
        {
            return;
        }

        // Strike if waiting for button 1
        if (!_pressedButton1)
        {
            Strike();
            Log("Strike! Pressed '{0}' when I was expecting '{1}' to be pressed.", BUTTON_NAMES_2[index],
                BUTTON_NAMES_1[_correctButton1]);
            return;
        }

        // Strike if this wasn't the correct button
        if (index != _correctButton2)
        {
            Strike();
            Log("Strike! Pressed '{0}' when I was expecting '{1}' to be pressed.", BUTTON_NAMES_2[index],
                BUTTON_NAMES_2[_correctButton2]);
            return;
        }

        // Success!
        Log("Successfully pressed '{0}'. Module solved!", BUTTON_NAMES_2[index]);
        Solve();

        // Display chip
        Chip.enabled = true;
        PlaySound("ChipSound");
    }

    private void GeneratePuzzle()
    {
        // Compute serial number & hops
        var serialNumber = BombInfo.GetSerialNumber();
        var deck = Get<ManualGenerator>().Decks[serialNumber[0]];
        var hops = serialNumber.Skip(1).Select(Extensions.ToIntViaA1Z26).ToList();

        // Log serial number stats
        Log("Serial number is {0}", serialNumber);
        Log("Deck to use is '{0}'", serialNumber[0]);
        Log("Corresponding hops are {0}", hops.Join(", "));

        // Adjust hops
        hops = hops.Take(1).Concat(hops.Skip(1).Select(hop => hop + 1)).ToList();

        // We want to prevent the same card from appearing on multiple Not Poker modules on the same bomb,
        // but if there are more than 48 Not Poker modules, this is not possible.
        var preventDuplicates = BombInfo.GetSolvableModuleIDs().Count(m => m == "NotPokerModule") <= 48;

        // Determine possible hands
        // doing it in this manner allows us to get a better distribution of random hands as we will pick randomly from
        // the set of possible hand CLASSIFICATIONS rather than the set of possible HANDS (royal flushes are still rare)
        var rankedHands = new Dictionary<int, Tuple<string, List<string>>>();
        for (int i = 0, deckIndex = Random.Range(0, deck.Count);
            i < 52;
            i++, deckIndex = (deckIndex + 1) % deck.Count())
        {
            // Choose starting card
            var startingCard = deck[deckIndex];
            if (PROHIBITED_STARTING_CARDS.Contains(startingCard))
                continue;

            if (preventDuplicates && _usedCardsPerBomb[serialNumber].Contains(startingCard))
                continue;

            // Determine hand
            var index = deckIndex;
            var hand = new List<string>();
            hops.ForEach(hop =>
            {
                while (hop > 0)
                {
                    index = (index + 1) % deck.Count;

                    if (!hand.Contains(deck[index]))
                    {
                        --hop;
                    }
                }

                hand.Add(deck[index]);
            });

            // Determine hand rank
            var handRank = DetermineHandRank(hand);
            if (!rankedHands.ContainsKey(handRank))
            {
                rankedHands[handRank] = new Tuple<string, List<string>>(startingCard, hand);
            }
        }

        // Pick a random hand, but make higher-ranked hands more likely
        var ranksToChooseFrom = rankedHands.Keys.SelectMany(rank => Enumerable.Repeat(rank, rank + 1)).ToList();
        var finalHandRank = ranksToChooseFrom.PickRandom();
        var pickedHand = rankedHands[finalHandRank];
        var finalStartingCard = pickedHand.Item1;
        var finalHand = pickedHand.Item2;

        if (preventDuplicates)
            _usedCardsPerBomb[serialNumber].Add(finalStartingCard);

        // Record solution
        var solution = DetermineHandOutput(finalHandRank);
        _correctButton1 = solution.Item1;
        _correctButton2 = solution.Item2;

        // Log puzzle
        Log("Display card is {0}", finalStartingCard);
        Log("Hand is {0}", finalHand.Join(", "));
        Log("This hand is a '{0}'", HAND_NAMES[finalHandRank]);
        Log("Solution is '{0}' - '{1}'", BUTTON_NAMES_1[_correctButton1], BUTTON_NAMES_2[_correctButton2]);

        // Display starting card
        var RANKS = "2 3 4 5 6 7 8 9 10 A J K Q".Split(' ');
        var SUITS = "♣ ♦ ♥ ♠".Split(' ');
        var imageDeck = (from rank in RANKS from suit in SUITS select rank + suit).ToList();
        Assign(onActivate: () =>
            MainCardDisplay.material.mainTexture = CardTextures[imageDeck.IndexOf(finalStartingCard)]);

        // Choose and setup response
        var response = RESPONSES.PickRandom();
        ResponseText.text = response;
        ResponseText.color = Color.green;
        Log("    The randomly chosen response is \"{0}\"", response);

        // Choose and setup chip
        var CHIP_VALUES = "25 50 100 500".Split(" ");
        var chipIndex = Random.Range(0, 4);
        Chip.material.mainTexture = ChipTextures[chipIndex];
        Log("    The randomly chosen chip is \"{0}\"", CHIP_VALUES[chipIndex]);
    }

    private static Tuple<int, int> DetermineHandOutput(int handRank)
    {
        // Determine correct buttons
        return new Tuple<int, int>(
            handRank / 2, // Button 1
            handRank % 2 // Button 2
        );
    }

    private int DetermineHandRank(List<string> rawHand)
    {
        // Transform hand into more convenient format
        var hand = rawHand.Select(card =>
        {
            var rankStr = card.Substring(0, card.Length - 1);
            int rankInt;
            var isNumeric = int.TryParse(rankStr, out rankInt);

            return new Tuple<int, char>(
                // Rank (ace is high)
                isNumeric ? rankInt : ("JQKA".IndexOf(rankStr[0]) + 11),
                // Suit
                card.Last()
            );
        }).OrderBy(card => card.Item1).ToList();

        // General stats 
        var sameSuit = hand.Select(card => card.Item2).Distinct().Count() == 1;
        var numDistinctRanks = hand.Select(card => card.Item1).Distinct().Count();
        var numInBiggestGroup = hand.GroupBy(card => card.Item1).Select(group => group.Count()).Max();
        var isSuccessive = hand.Select(card => card.Item1).IsSuccessive() ||
                           hand.Select(card => card.Item1 == 14 ? 1 : card.Item1).IsSuccessive();

        // Determine output
        int handRank;

        // Royal Flush / Straight Flush
        if (sameSuit && isSuccessive)
        {
            // If ace is high, then royal flush; otherwise, straight flush
            handRank = hand.Last().Item1 == 14 ? 9 : 8;
        }

        // Four of a Kind / Full House
        else if (numDistinctRanks == 2)
        {
            // Check if 4 of a kind or full house
            handRank = numInBiggestGroup == 4 ? 7 : 6;
        }

        // Flush
        else if (sameSuit)
        {
            handRank = 5;
        }

        // Straight
        else if (isSuccessive)
        {
            handRank = 4;
        }

        // Three of a Kind
        else if (numInBiggestGroup == 3)
        {
            handRank = 3;
        }

        // Two Pair
        else if (numDistinctRanks == 3 && numInBiggestGroup == 2)
        {
            handRank = 2;
        }

        // Pair
        else if (numDistinctRanks == 4 && numInBiggestGroup == 2)
        {
            handRank = 1;
        }

        // No Hand
        else
        {
            handRank = 0;
        }

        return handRank;
    }
}