using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LogicGates : MonoBehaviour
{
    private Gate[] _gates = new Gate[3];
    private GateType[] _gateTypes = new GateType[6];

    enum GateOp { AND, OR, XOR, NAND, NOR, XNOR };

    void Start()
    {
        _gateTypes = new GateType[] {
            new GateType() { GateOp = GateOp.AND, Calculate = (a, b) => a && b },
            new GateType() { GateOp = GateOp.OR, Calculate = (a, b) => a || b },
            new GateType() { GateOp = GateOp.XOR, Calculate = (a, b) => a ^ b },
            new GateType() { GateOp = GateOp.NAND, Calculate = (a, b) => !(a && b) },
            new GateType() { GateOp = GateOp.NOR, Calculate = (a, b) => !(a || b) },
            new GateType() { GateOp = GateOp.XNOR, Calculate = (a, b) => !(a ^ b) },
        };

        var lines = new List<string>();

        var configurations = new List<List<int>>();
        for (var i = 12345; i <= 543210; i++)
        {
            var gateConfig = new List<int>
            {
                i / 100000 % 10,
                i / 10000 % 10,
                i / 1000 % 10,
                i / 100 % 10,
                i / 10 % 10,
                i % 10
            };

            foreach (int gateIndex in gateConfig)
            {
                if (gateIndex > 5) goto SKIP;
            }
            if (gateConfig.Count != gateConfig.Distinct().Count()) goto SKIP;
            if (gateConfig[3] > gateConfig[4]) goto SKIP;

            configurations.Add(gateConfig);

            SKIP:;
        }

        foreach (List<int> config in configurations)
        {
            var numOn = 0;
            var numOff = 0;
            for (var input = 0; input < 64; input++)
            {
                BitArray b = new BitArray(new int[] { input });
                bool[] bits = new bool[b.Count];
                b.CopyTo(bits, 0);
                //Array.Reverse(bits);
                if (
                    _gateTypes[config[5]].Calculate(
                        _gateTypes[config[3]].Calculate(
                            _gateTypes[config[0]].Calculate(bits[0], bits[1]),
                            _gateTypes[config[1]].Calculate(bits[2], bits[3])
                        ),
                        _gateTypes[config[4]].Calculate(
                            _gateTypes[config[1]].Calculate(bits[2], bits[3]),
                            _gateTypes[config[2]].Calculate(bits[4], bits[5])
                        )
                    )
                )
                {
                    numOn++;
                }
                else
                {
                    numOff++;
                }
            }

            lines.Add(String.Join(", ", config.ConvertAll(n => _gateTypes[n].GateOp.ToString()).ToArray()) + ", On: " + numOn.ToString() + ", Off: " + numOff.ToString());
        }

        using (StreamWriter w = File.AppendText("log.txt"))
        {
            foreach (var line in lines)
            {
                w.WriteLine(line);
            }
        }

        _gates[0] = new Gate() { GateType = _gateTypes[5] };
        Debug.Log(_gates[0].GateType.Calculate(false, false));
        Debug.Log(_gates[0].GateType.Calculate(false, true));
        Debug.Log(_gates[0].GateType.Calculate(true, false));
        Debug.Log(_gates[0].GateType.Calculate(true, true));
    }

    struct Gate
    {
        public GateType GateType { get; set; }
    }

    struct GateType
    {
        public GateOp GateOp { get; set; }
        public Func<bool, bool, bool> Calculate { get; set; }
    }
}
