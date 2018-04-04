using System;
using System.Collections;
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

        int i, j;
        bool valid;

        ButtonLeft.OnInteract += delegate () { PressArrow(Left); return false; };
        ButtonRight.OnInteract += delegate () { PressArrow(Right); return false; };
        ButtonCheck.OnInteract += delegate () { PressCheck(); return false; };

        // Keep track of possible solutions
        List<int> on = new List<int>();

        // For wrong answers, keep track of the inputs for gates A, B, C and D.
        // We need at least one of each of "neither", "one" and "both" in order to deduct the gate type
        // Index is gate * 3 (A, B, C and D) + type (neither, one and both).
        List<int>[] gateInputs = new List<int>[12];
        for (i = 0; i < 12; i++) gateInputs[i] = new List<int>();

        var config = new List<int>();

        var tries = 0;
        var zeroOn = 0;
        var missingInput = 0;
        var quit = false;

        var configs = new List<List<int>>();

        while (true)
        {
            tries++;
            if (tries > 10000)
            {
                quit = true;
                break;
            }

            _gates.Clear();
            on.Clear();
            foreach (var gateInput in gateInputs) gateInput.Clear();

            // Pick 4 random gates for A, B, C and D
            int[] pool = new int[] { AND, OR, XOR, NAND, NOR, XNOR, Rnd.Range(0, 6) };
            shuffle(pool);

            config.Clear();
            for (i = 0; i < 4; i++) config.Add(pool[i]);

            // Determine the other three by manual rules, check number of duplicates each time
            config.Add((config[GateA] + _gateTypes[config[GateB]].Steps) % 6);
            while (config.Distinct().Count() < 4)
            {
                config[GateE] = (config[GateE] + 1) % 6;
            }
            config.Add((config[GateE] + _gateTypes[config[GateC]].Steps) % 6);
            while (config.Distinct().Count() < 5)
            {
                config[GateF] = (config[GateF] + 1) % 6;
            }
            config.Add((config[GateF] + _gateTypes[config[GateD]].Steps) % 6);
            while (config.Distinct().Count() < 6)
            {
                config[GateG] = (config[GateG] + 1) % 6;
            }

            // Add to tried configs for logging
            configs.Add(config.ToList());

            // We found a valid config for the 7 gates
            foreach (int gateType in config)
            {
                _gates.Add(new Gate { GateType = _gateTypes[gateType] });
            }

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
                    for (i = GateA; i <= GateD; i++)
                    {
                        if (!GetBit(input, i * 2) && !GetBit(input, i * 2 + 1))
                        {
                            gateInputs[i * 3 + None].Add(input);
                        }
                        else if (GetBit(input, i * 2) && GetBit(input, i * 2 + 1))
                        {
                            gateInputs[i * 3 + Both].Add(input);
                        }
                        else
                        {
                            gateInputs[i * 3 + One].Add(input);
                        }
                    }
                }
            }

            // We do need a solution
            if (on.Count == 0)
            {
                zeroOn++;
                continue;
            }

            // If one of these is empty, we miss an input that's required for the defuser to tell the gate types of A, B, C and D
            valid = true;
            for (i = GateA; i <= GateD; i++)
            {
                valid = (gateInputs[i * 3 + None].Count > 0 && gateInputs[i * 3 + One].Count > 0 && gateInputs[i * 3 + Both].Count > 0);
                if (!valid) break;
            }
            if (!valid)
            {
                missingInput++;
                continue;
            }

            // Still here? We have a valid configuration!
            break;
        }

        if (quit)
        {
            Debug.Log("Over 10000 tries! I quit! #zeroOn: " + zeroOn + ", #missingInput: " + missingInput);
        }
        else
        {
            Debug.Log("Found a configuration after " + tries + " tries.");
        }

        Debug.Log(String.Join("\n", configs.ConvertAll(conf => String.Join(" ", conf.ConvertAll(gate => gate.ToString()).ToArray())).ToArray()));

        // Solution
        _solution = on[Rnd.Range(0, on.Count)];
        _inputs.Add(_solution);

        // Gate to replace one wrong answer on
        var gateToReplace = Rnd.Range(0, 4);
        var twoBitsToReplace = 0;
        if (!GetBit(_solution, gateToReplace * 2) && !GetBit(_solution, gateToReplace * 2 + 1))
        {
            twoBitsToReplace = None;
        }
        else if (GetBit(_solution, gateToReplace * 2) && GetBit(_solution, gateToReplace * 2 + 1))
        {
            twoBitsToReplace = Both;
        }
        else
        {
            twoBitsToReplace = One;
        }

        for (i = GateA; i <= GateD; i++)
        {
            for (j = None; j <= Both; j++)
            {
                if ((i == gateToReplace) && (j == twoBitsToReplace))
                {
                    continue;
                }
                _inputs.Add(gateInputs[i * 3 + j][Rnd.Range(0, gateInputs[i * 3 + j].Count)]);
            }
        }

        _currentInputIndex = Rnd.Range(0, 12);
        UpdateLeds();

        Debug.LogFormat("[Logic Gates #{0}] Gates: {1}", _moduleId, String.Join(", ", _gates.ConvertAll(n => n.GateType.Name).ToArray()));
        string msg = "";
        for (i = 0; i < 8; i++)
        {
            msg += GetBit(_solution, i) ? "1" : "0";
        }
        Debug.LogFormat("[Logic Gates #{0}] Solution: {1}", _moduleId, msg);
    }

    // Knuth shuffle algorithm :: courtesy of Wikipedia :)
    private void shuffle(int[] ints)
    {
        for (int i = 0; i < ints.Length; i++)
        {
            var tmp = ints[i];
            var r = Rnd.Range(i, ints.Length);
            ints[i] = ints[r];
            ints[r] = tmp;
        }
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

        if (_inputs[_currentInputIndex] == _solution)
        {
            GetComponent<KMBombModule>().HandlePass();
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
        }

    }

    bool GetBit(int number, int bit)
    {
        return (number & (1 << bit)) != 0;
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

static class Extensions
{
    public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
    {
        return listToClone.Select(item => (T)item.Clone()).ToList();
    }
}