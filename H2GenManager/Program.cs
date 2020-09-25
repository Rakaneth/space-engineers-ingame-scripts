using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        IMyTimerBlock onBlock;
        IMyTimerBlock offBlock;
        List<IMyGasTank> h2tanks = new List<IMyGasTank>();
        double lowThreshold;
        double highThreshold;
        bool running;
        MyCommandLine parser = new MyCommandLine();

        public Program()
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            if (string.IsNullOrEmpty(Storage))
            {
                lowThreshold = 0.5;
                highThreshold = 1;
            }
            else
            {
                var vals = Storage.Split(';');
                double.TryParse(vals[0], out lowThreshold);
                double.TryParse(vals[1], out highThreshold);
            }
            Refresh();
        }

        public void Save()
        {
            Storage = $"{lowThreshold};{highThreshold}";
        }

        private void Refresh()
        {
            onBlock = GridTerminalSystem.GetBlockWithName("H2 On Timer") as IMyTimerBlock;
            offBlock = GridTerminalSystem.GetBlockWithName("H2 Off Timer") as IMyTimerBlock;
            GridTerminalSystem.GetBlocksOfType(h2tanks, tank => tank.IsSameConstructAs(Me) && tank.Capacity == 5000000f);
            if (h2tanks.Count == 0)
                Echo("No hydrogen tanks found");
            running = GetH2Percent() < lowThreshold;
        }

        private double GetH2Percent()
        {
            return (h2tanks.Count == 0) ? 0 : h2tanks.Average(t => t.FilledRatio);
        }

        public void Main(string argument)
        {
            if (argument == "refresh")
            {
                Refresh();
                return;
            }
            else if (parser.TryParse(argument)) //low 0.75..high 1
            {
                var cmd = parser.Argument(0).ToLower();
                double val;
                double.TryParse(parser.Argument(1), out val);
                if (val > 1.0 || val < 0)
                {
                    Echo("Value must be between 0 and 1.");
                    return;
                }


                if (cmd == "low")
                {
                    if (val >= highThreshold)
                    {
                        Echo("Low threshold must be less than high threshold.");
                        return;
                    }
                    lowThreshold = val;
                    return;
                }
                else if (cmd == "high")
                {
                    if (val <= lowThreshold)
                    {
                        Echo("High threshold must be greater than low threshold.");
                        return;
                    }
                    highThreshold = val;
                    return;
                }
                else
                    Echo("Command not recognized.");

                return;
            }
            else
            {
                var h2pct = GetH2Percent();
                Echo($"Will run when tanks below {lowThreshold * 100:N2}%.");
                Echo($"Will stop when tanks above {highThreshold * 100:N2}%.");
                Echo($"Current capacity: {h2pct * 100:N2}%.");
                Echo($"Status: {(running ? "Running" : "Not Running")}");
                if (h2tanks.Count > 0)
                {
                    if (h2pct <= lowThreshold && !running)
                    {
                        running = true;
                        onBlock.Trigger();
                    }
                    else if (h2pct >= highThreshold && running)
                    {
                        running = false;    
                        offBlock.Trigger();
                    }
                }
            } 
        }
    }
}
