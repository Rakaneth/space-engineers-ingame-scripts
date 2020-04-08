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
        List<IMyPowerProducer> producers = new List<IMyPowerProducer>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();
        List<IMyGasTank> oxyTanks = new List<IMyGasTank>();
        StringBuilder sb = new StringBuilder();
        List<IMyTextPanel> displays = new List<IMyTextPanel>();
        List<IMyTerminalBlock> consumers = new List<IMyTerminalBlock>();
        List<IMyBatteryBlock> batts = new List<IMyBatteryBlock>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyCockpit> cockpits = new List<IMyCockpit>();
        //List<Paginator> paginators = new List<Paginator>();
        Definitions defs = new Definitions();
        MyResourceSinkComponent sink;
        MyCommandLine cmd = new MyCommandLine();
        const char bar = '\u2588';
        const char dash = '-';
        IMyProgrammableBlock displayController;
        int bars;
        MyIni ini;
        

        public Program()
        {
            GridTerminalSystem.GetBlocksOfType(producers, p => p.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(tanks, e => e.IsSameConstructAs(Me) && e.Capacity == 5000000f);
            GridTerminalSystem.GetBlocksOfType(batts, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(displays, display => MyIni.HasSection(display.CustomData, "PowerReadout") && display.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => MyIni.HasSection(cockpit.CustomData, "PowerReadout") && cockpit.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(oxyTanks, o => o.IsSameConstructAs(Me) && o.Capacity == 100000f);
            GridTerminalSystem.GetBlocksOfType(reactors, r => r.IsSameConstructAs(Me));
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            displayController = GridTerminalSystem.GetBlockWithName("Display Controller") as IMyProgrammableBlock;

            ini = new MyIni();
            if (!ini.TryParse(Me.CustomData))
                Echo($"Cannot parse custom data.");

            if (ini.ContainsKey("Config", "Bars"))
                bars = ini.Get("Config", "bars").ToInt32();
            else
                bars = 10;

            if (displays.Count == 0 && cockpits.Count == 0 && displayController == null)
                throw new Exception("Remember to add [PowerReadout] to the Custom Data of any displays or cockpits you want to show this information.");

            foreach (var display in displays)
            {
                display.Font = "Monospace";
                //paginators.Add(new Paginator(this, display));
            }

            foreach (var cockpit in cockpits)
                cockpit.GetSurface(0).Font = "Monospace";
                
            GridTerminalSystem.GetBlocksOfType(consumers, consumer => consumer.Components.TryGet(out sink) && sink.IsPoweredByType(defs.electricity));
            var smReact = reactors.Count(e => e.MaxOutput == 15f);
            var lgReact = reactors.Count(e => e.MaxOutput == 300f);
            Echo($"Batteries: {batts.Count}");
            Echo($"Hydrogen Tanks: {tanks.Count}");
            Echo($"Oxygen Tanks: {oxyTanks.Count}");
            Echo($"Reactors: {reactors.Count}");
            Echo($"--Small Reactors: {smReact}");
            Echo($"--Large Reactors: {lgReact}");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            float generated = 0;
            float maxGenerated = 0;
            float battStored = 0;
            float battMaxStored = 0;
            float used = 0;
            float u = 0;
            double h2Current = 0;
            double o2Current = 0;
            MyFixedPoint uStored = 0;
            bool showAll;
            bool showBatts;

            
            if (cmd.TryParse(argument)) {
                showAll = cmd.Switch("showall");
                showBatts = cmd.Switch("batteries");
            } else {
                showAll = MyIni.HasSection(Me.CustomData, "ShowAll");
                showBatts = MyIni.HasSection(Me.CustomData, "Batteries");
            }

            sb.Append("Power Sources\n");

            foreach (var producer in producers)
            {
                generated += producer.CurrentOutput;
                maxGenerated += producer.MaxOutput;
                if (showAll) sb.Append($"{producer.CustomName}: {producer.CurrentOutput:N2}\n");
            }
            
            sb.Append($"Total Generated: {generated:N2}/{maxGenerated:N2} MW\n\n");
            
            sb.Append("Power Sinks\n");
            
            foreach (var consumer in consumers)
            {
                if (consumer.Components.TryGet(out sink))
                {
                    u = sink.CurrentInputByType(defs.electricity);
                    used += u;
                    if (showAll) sb.Append($"{consumer.CustomName}: {u:N2}\n");
                }
            }

            sb.Append($"Total Consumed: {used:N2} MW");
            sb.Append($"[{Bar(used, maxGenerated, bars, bar, dash)}]");
            sb.Append($" ({used / maxGenerated * 100:N2}% of max power)\n\n");
            sb.Append("Battery Storage\n");

            if (batts.Count > 0)
            {
                foreach (var batt in batts)
                {
                    battStored += batt.CurrentStoredPower;
                    battMaxStored += batt.MaxStoredPower;
                    if (showAll)
                    {
                        sb.Append($"{batt.CustomName}: {batt.CurrentStoredPower:N2} / {batt.MaxStoredPower:N4} MWh ");
                        sb.Append($"[{Bar(batt.CurrentStoredPower, batt.MaxStoredPower, bars, bar, dash)}]\n");
                    }
                }

                sb.Append($"Total Stored: {battStored:N2} / {battMaxStored:N2} MWh");
                sb.Append($"[{Bar(battStored, battMaxStored, bars, bar, dash)}]");
                sb.Append($" {battStored / battMaxStored * 100:N2}% full\n\n");
            }
 
            if (tanks.Count > 0)
            {
                sb.Append("Hydrogen\n");
                foreach (var tank in tanks)
                {    
                    h2Current += tank.FilledRatio;
                    if (showAll)
                    {
                        sb.Append($"{tank.CustomName}: {tank.FilledRatio * 100:N2}%");
                        sb.Append($"[{Bar(tank.FilledRatio * 100, 100, bars, bar, dash)}]\n\n");
                    }
                }
                sb.Append($"Total Hydrogen Reserves: {(h2Current * 100 / tanks.Count):N2}%");
                sb.Append($"[{Bar(h2Current, tanks.Count, bars, bar, dash)}]\n\n");
            }

            if (oxyTanks.Count > 0)
            {
                sb.Append("Oxygen\n");
                foreach (var oxy in oxyTanks)
                {
                    o2Current += oxy.FilledRatio;
                    if (showAll)
                    {
                        sb.Append($"{oxy.CustomName}: {oxy.FilledRatio * 100:N2}%");
                        sb.Append($"[{Bar(oxy.FilledRatio, 1, bars, bar, dash)}]\n\n");
                    }
                }
                sb.Append($"Total Oxygen Reserves: {o2Current * 100 / oxyTanks.Count:N2}%");
                sb.Append($"[{Bar(o2Current, oxyTanks.Count, bars, bar, dash)}]\n\n");
            }

            if (reactors.Count > 0)
            {
                sb.Append($"Reactors\n");
                foreach (var reactor in reactors)
                {
                    uStored += reactor.GetInventory(0).GetItemAmount(defs.uranium);
                }
                sb.Append($"Total Uranium in Reactors: {(float)uStored:N2}\n\n");
            }

            foreach (var display in displays)
            {
                //Echo($"Total lines available for {display.CustomName}: {GetNumLines(display)}");
                display.WriteText(sb);
                //paginator.FromBuilder(sb);
            }

            foreach (var cockpit in cockpits)
                cockpit.GetSurface(0).WriteText(sb);

            displayController?.TryRun(sb.ToString());

            sb.Clear();
        }

        private string Bar(double value, double maxValue, int barWidth, char c, char pad)
        {
            int numBars = (int)(value * barWidth / maxValue);
            return new string(c, numBars).PadRight(barWidth, pad);
        }
    }
}
