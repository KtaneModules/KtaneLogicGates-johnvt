using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class LogicGates : MonoBehaviour
{
    const int AND = 0;
    const int OR = 1;
    const int XOR = 2;
    const int NAND = 3;
    const int NOR = 4;
    const int XNOR = 5;

    const int None = 0;
    const int One = 1;
    const int Both = 2;

    const int GateA = 0;
    const int GateB = 1;
    const int GateC = 2;
    const int GateD = 3;
    const int GateE = 4;
    const int GateF = 5;
    const int GateG = 6;

    const int Left = -1;
    const int Right = 1;

    public GameObject[] Inputs;
    public GameObject[] Outputs;
    public KMSelectable ButtonLeft;
    public KMSelectable ButtonRight;
    public KMSelectable ButtonCheck;

    public string TwitchHelpMessage = "Cycle with '!{0} left' (or l/previous/prev/p) and '!{0} right' (or r/next/n). Check with !{0} check.";

    private GateType[] _gateTypes = new GateType[]
    {
        new GateType() { Steps = 1, Name = "AND", Type = AND, Eval = (a, b) => a && b },
        new GateType() { Steps = 2, Name = "OR", Type = OR, Eval = (a, b) => a || b },
        new GateType() { Steps = 3, Name = "XOR", Type = XOR, Eval = (a, b) => a ^ b },
        new GateType() { Steps = 4, Name = "NAND", Type = NAND, Eval = (a, b) => !(a && b) },
        new GateType() { Steps = 5, Name = "NOR", Type = NOR, Eval = (a, b) => !(a || b) },
        new GateType() { Steps = 6, Name = "XNOR", Type = XNOR, Eval = (a, b) => !(a ^ b) },
    };

    private List<Gate> _gates = new List<Gate>();
    private List<int> _inputs = new List<int>();
    private int _solution = 0;
    private int _currentInputIndex = 0;
    private int _moduleId;
    private static int _moduleIdCounter = 1;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        ButtonLeft.OnInteract += delegate () { PressArrow(Left); return false; };
        ButtonRight.OnInteract += delegate () { PressArrow(Right); return false; };
        ButtonCheck.OnInteract += delegate () { PressCheck(); return false; };

        // Keep track of possible solutions
        List<int> on = new List<int>();

        // For wrong answers, keep track of the inputs for gates A, B, C and D.
        // We need at least one of each of "neither", "one" and "both" in order to deduct the gate type
        // Index is gate * 3 (A, B, C and D) + type (neither, one and both).
        List<int>[] gateInputs = new List<int>[12];
        for (var i = 0; i < 12; i++) gateInputs[i] = new List<int>();

        // It might take a couple of tries to randomly find a configuration that "works"
        while (true)
        {
            _gates.Clear();
            on.Clear();
            foreach (var gateInput in gateInputs) gateInput.Clear();
            var config = new List<int>();

            // Pick 4 random gates for A, B, C and D
            int[] pool = new int[] { AND, OR, XOR, NAND, NOR, XNOR, Rnd.Range(0, 6) };
            ShuffleInts(pool);

            for (var i = 0; i < 4; i++) config.Add(pool[i]);

            // Determine the other three by manual rules, checking number of duplicates each time
            config.Add((config[GateA] + _gateTypes[config[GateB]].Steps) % 6);
            while (config.Distinct().Count() < 4)
                config[GateE] = (config[GateE] + 1) % 6;
            config.Add((config[GateE] + _gateTypes[config[GateC]].Steps) % 6);
            while (config.Distinct().Count() < 5)
                config[GateF] = (config[GateF] + 1) % 6;
            config.Add((config[GateF] + _gateTypes[config[GateD]].Steps) % 6);
            while (config.Distinct().Count() < 6)
                config[GateG] = (config[GateG] + 1) % 6;

            // We found a valid config for the 7 gates
            foreach (int gateType in config)
                _gates.Add(new Gate { GateType = _gateTypes[gateType] });

            for (int input = 0; input <= 255; input++)
            {
                if (
                    _gates[GateG].GateType.Eval(
                        _gates[GateE].GateType.Eval(
                            _gates[GateA].GateType.Eval(GetBit(input, 0), GetBit(input, 1)),
                            _gates[GateB].GateType.Eval(GetBit(input, 2), GetBit(input, 3))
                        ),
                        _gates[GateF].GateType.Eval(
                            _gates[GateC].GateType.Eval(GetBit(input, 4), GetBit(input, 5)),
                            _gates[GateD].GateType.Eval(GetBit(input, 6), GetBit(input, 7))
                        )
                    )
                )
                {
                    on.Add(input);
                }
                else
                {
                    for (var i = GateA; i <= GateD; i++)
                    {
                        if (!GetBit(input, i * 2) && !GetBit(input, i * 2 + 1))
                            gateInputs[i * 3 + None].Add(input);
                        else if (GetBit(input, i * 2) && GetBit(input, i * 2 + 1))
                            gateInputs[i * 3 + Both].Add(input);
                        else
                            gateInputs[i * 3 + One].Add(input);
                    }
                }
            }

            // We do need a solution
            if (on.Count == 0) continue;

            // If one of these is empty, we miss an input that's required for the defuser to tell the gate types of A, B, C and D
            bool valid = true;
            for (int i = GateA; i <= GateD; i++)
            {
                valid = (gateInputs[i * 3 + None].Count > 0 && gateInputs[i * 3 + One].Count > 0 && gateInputs[i * 3 + Both].Count > 0);
                if (!valid) break;
            }
            if (!valid) continue;

            // Still here? We've found a valid configuration!
            break;
        }

        // Solution
        _solution = on[Rnd.Range(0, on.Count)];
        _inputs.Add(_solution);

        // Gate to replace one wrong answer on
        var gateToReplace = Rnd.Range(0, 4);
        var twoBitsToReplace = 0;
        if (!GetBit(_solution, gateToReplace * 2) && !GetBit(_solution, gateToReplace * 2 + 1))
            twoBitsToReplace = None;
        else if (GetBit(_solution, gateToReplace * 2) && GetBit(_solution, gateToReplace * 2 + 1))
            twoBitsToReplace = Both;
        else
            twoBitsToReplace = One;

        for (var i = GateA; i <= GateD; i++)
        {
            for (var j = None; j <= Both; j++)
            {
                if ((i == gateToReplace) && (j == twoBitsToReplace)) continue;
                _inputs.Add(gateInputs[i * 3 + j][Rnd.Range(0, gateInputs[i * 3 + j].Count)]);
            }
        }

        // @todo: shuffle _inputs
        _currentInputIndex = Rnd.Range(0, 12);
        UpdateLeds();

        Debug.LogFormat("[Logic Gates #{0}] Gates: {1}", _moduleId, String.Join(", ", _gates.ConvertAll(n => n.GateType.Name).ToArray()));

        foreach (var input in _inputs)
        {
            // The input LEDs are numbered top to bottom. But most to least significant bit is bottom to top. So let's just reverse it for logging.
            var inputs = Reverse(Convert.ToString(input, 2).PadLeft(8, '0'));

            var outputs = "";
            for (var i = 0; i < 4; i++)
            {
                outputs += _gates[i].GateType.Eval(GetBit(input, i * 2), GetBit(input, i * 2 + 1)) ? "1" : "0";
            }

            Debug.LogFormat(
                "[Logic Gates #{0}] {1}: In={2} Out={3}",
                _moduleId,
                (input == _solution ? "Solution" : "Wrong answer"),
                inputs,
                outputs
            );
        }
    }

    // Knuth shuffle algorithm :: courtesy of Wikipedia :)
    private void ShuffleInts(int[] ints)
    {
        for (int i = 0; i < ints.Length; i++)
        {
            var tmp = ints[i];
            var r = Rnd.Range(i, ints.Length);
            ints[i] = ints[r];
            ints[r] = tmp;
        }
    }

    public string Reverse(string text)
    {
        if (text == null) return null;

        // this was posted by petebob as well 
        char[] array = text.ToCharArray();
        Array.Reverse(array);
        return new String(array);
    }

    private void UpdateLeds()
    {
        for (var i = 0; i < 8; i++)
        {
            Inputs[i].SetActive(GetBit(_inputs[_currentInputIndex], i));
        }

        for (var i = 0; i < 4; i++)
        {
            Outputs[i].SetActive(
                _gates[i].GateType.Eval(
                    GetBit(_inputs[_currentInputIndex], i * 2),
                    GetBit(_inputs[_currentInputIndex], i * 2 + 1)
                )
            );
        }
    }

    private void PressArrow(int direction)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch(.2f);

        _currentInputIndex = (_currentInputIndex + 12 + direction) % 12;
        UpdateLeds();
    }

    private void PressCheck()
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        GetComponent<KMSelectable>().AddInteractionPunch();

        bool correct = _inputs[_currentInputIndex] == _solution;
        if (correct)
        {
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }

        Debug.LogFormat("[Logic Gates #{0}] Checking {1}: {2}.",
            _moduleId,
            Reverse(Convert.ToString(_inputs[_currentInputIndex], 2).PadLeft(8, '0')),
            (correct ? "correct!" : "wrong!")
        );
    }

    bool GetBit(int number, int bit)
    {
        return (number & (1 << bit)) != 0;
    }

    public KMSelectable[] ProcessTwitchCommand(string command)
    {
        if (
            command.Equals("left", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("l", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("previous", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("prev", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("p", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            return new KMSelectable[] { ButtonLeft };
        }
        else if (
            command.Equals("right", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("r", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("next", StringComparison.InvariantCultureIgnoreCase) ||
            command.Equals("n", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            return new KMSelectable[] { ButtonRight };
        }
        else if (command.Equals("check", StringComparison.InvariantCultureIgnoreCase))
        {
            return new KMSelectable[] { ButtonCheck };
        }

        return null;
    }

    struct Gate
    {
        public GateType GateType { get; set; }
    }

    struct GateType
    {
        public int Type { get; set; }
        public string Name { get; set; }
        public Func<bool, bool, bool> Eval { get; set; }
        public int Steps { get; set; }
    }
}