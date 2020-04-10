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
        //StringBuilder sb = new StringBuilder();
        List<IMyTextPanel> displays = new List<IMyTextPanel>();
        List<IMyTerminalBlock> consumers = new List<IMyTerminalBlock>();
        List<IMyBatteryBlock> batts = new List<IMyBatteryBlock>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyCockpit> cockpits = new List<IMyCockpit>();
        Dictionary<long, int> barConfig = new Dictionary<long, int>();
        Dictionary<long, StringBuilder> sbs = new Dictionary<long, StringBuilder>();
        Definitions defs = new Definitions();
        MyResourceSinkComponent sink;
        MyCommandLine cmd = new MyCommandLine();
        const char bar = '\u2588';
        const char dash = '-';
        IMyProgrammableBlock displayController;
        MyIni ini;
        

        public Program()
        {
            int bars;
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

            if (displays.Count == 0 && cockpits.Count == 0 && displayController == null)
                throw new Exception("Remember to add [PowerReadout] to the Custom Data of any displays or cockpits you want to show this information.");

            foreach (var display in displays)
            {
                display.Font = "Monospace";
                if (!ini.TryParse(display.CustomData))
                    Echo($"Cannot parse custom data for {display.CustomName}");

                
                bars = ini.ContainsKey("PowerReadout", "Bars") ? ini.Get("PowerReadout", "Bars").ToInt32() : 10;
                barConfig.Add(display.EntityId, bars);
                sbs.Add(display.EntityId, new StringBuilder());
                display.ContentType = ContentType.TEXT_AND_IMAGE;
            }

            foreach (var cockpit in cockpits)
            {
                cockpit.GetSurface(0).Font = "Monospace";
                cockpit.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
                if (!ini.TryParse(cockpit.CustomData))
                    Echo($"Cannot parse custom data for {cockpit.CustomName}");

                bars = ini.ContainsKey("PowerReadout", "Bars") ? ini.Get("PowerReadout", "Bars").ToInt32() : 10;
                barConfig.Add(cockpit.EntityId, bars);
                sbs.Add(cockpit.EntityId, new StringBuilder());

            }
                       
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
            float u;
            float used = 0;
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


            AddTextAll("Power Sources\n");
            //sb.Append("Power Sources\n");

            foreach (var producer in producers)
            {
                generated += producer.CurrentOutput;
                maxGenerated += producer.MaxOutput;
                if (showAll)
                    AddTextAll($"{producer.CustomName}: {producer.CurrentOutput:N2}\n");
                //sb.Append($"{producer.CustomName}: {producer.CurrentOutput:N2}\n");
            }

            AddTextAll($"Total Generated: {generated:N2}/{maxGenerated:N2} MW\n\n");
            AddTextAll("Power Sinks\n");
            //sb.Append($"Total Generated: {generated:N2}/{maxGenerated:N2} MW\n\n");
            //sb.Append("Power Sinks\n");
            
            foreach (var consumer in consumers)
            {
                if (consumer.Components.TryGet(out sink))
                {
                    u = sink.CurrentInputByType(defs.electricity);
                    used += u;
                    if (showAll)
                        AddTextAll($"{consumer.CustomName}: {u:N2}\n");
                    //sb.Append($"{consumer.CustomName}: {u:N2}\n");
                }
            }

            AddTextAll($"Total Consumed: {used:N2} MW");
            AddBarAll(used, maxGenerated);
            AddTextAll($" ({used / maxGenerated * 100:N2}% of max power)\n\n");
            AddTextAll("Battery Storage\n");
            //sb.Append($"[{Bar(used, maxGenerated, bars, bar, dash)}]");
            //sb.Append($" ({used / maxGenerated * 100:N2}% of max power)\n\n");
            //sb.Append("Battery Storage\n");

            if (batts.Count > 0)
            {
                foreach (var batt in batts)
                {
                    battStored += batt.CurrentStoredPower;
                    battMaxStored += batt.MaxStoredPower;
                    if (showAll)
                    {
                        AddTextAll($"{batt.CustomName}: {batt.CurrentStoredPower:N2} / {batt.MaxStoredPower:N4} MWh ");
                        AddBarAll(batt.CurrentStoredPower, batt.MaxStoredPower);
                        //sb.Append($"{batt.CustomName}: {batt.CurrentStoredPower:N2} / {batt.MaxStoredPower:N4} MWh ");
                        //sb.Append($"[{Bar(batt.CurrentStoredPower, batt.MaxStoredPower, bars, bar, dash)}]\n");
                    }
                }

                AddTextAll($"Total Stored: {battStored:N2} / {battMaxStored:N2} MWh");
                AddBarAll(battStored, battMaxStored);
                AddTextAll($" {battStored / battMaxStored * 100:N2}% full\n\n");
                //sb.Append($"Total Stored: {battStored:N2} / {battMaxStored:N2} MWh");
                //sb.Append($"[{Bar(battStored, battMaxStored, bars, bar, dash)}]");
                //sb.Append($" {battStored / battMaxStored * 100:N2}% full\n\n");
            }
 
            if (tanks.Count > 0)
            {
                AddTextAll("Hydrogen\n");
                //sb.Append("Hydrogen\n");
                foreach (var tank in tanks)
                {    
                    h2Current += tank.FilledRatio;
                    if (showAll)
                    {
                        AddTextAll($"{tank.CustomName}: {tank.FilledRatio * 100:N2}%");
                        AddBarAll(tank.FilledRatio, 1);
                        AddTextAll("\n\n");
                        //sb.Append($"{tank.CustomName}: {tank.FilledRatio * 100:N2}%");
                        //sb.Append($"[{Bar(tank.FilledRatio * 100, 100, bars, bar, dash)}]\n\n");
                    }
                }
                AddTextAll($"Total Hydrogen Reserves: {(h2Current * 100 / tanks.Count):N2}%");
                AddBarAll(h2Current, tanks.Count);
                AddTextAll("\n\n");
                //sb.Append($"Total Hydrogen Reserves: {(h2Current * 100 / tanks.Count):N2}%");
                //sb.Append($"[{Bar(h2Current, tanks.Count, bars, bar, dash)}]\n\n");
            }

            if (oxyTanks.Count > 0)
            {
                AddTextAll("Oxygen\n");
                //sb.Append("Oxygen\n");
                foreach (var oxy in oxyTanks)
                {
                    o2Current += oxy.FilledRatio;
                    if (showAll)
                    {
                        AddTextAll($"{oxy.CustomName}: {oxy.FilledRatio * 100:N2}%");
                        AddBarAll(oxy.FilledRatio, 1);
                        AddTextAll("\n\n");
                        //sb.Append($"{oxy.CustomName}: {oxy.FilledRatio * 100:N2}%");
                        //sb.Append($"[{Bar(oxy.FilledRatio, 1, bars, bar, dash)}]\n\n");
                    }
                }
                AddTextAll($"Total Oxygen Reserves: {o2Current * 100 / oxyTanks.Count:N2}%");
                AddBarAll(o2Current, oxyTanks.Count);
                AddTextAll("\n\n");
                //sb.Append($"Total Oxygen Reserves: {o2Current * 100 / oxyTanks.Count:N2}%");
                //sb.Append($"[{Bar(o2Current, oxyTanks.Count, bars, bar, dash)}]\n\n");
            }

            if (reactors.Count > 0)
            {
                AddTextAll($"Reactors\n");
                //sb.Append($"Reactors\n");
                foreach (var reactor in reactors)
                {
                    uStored += reactor.GetInventory(0).GetItemAmount(defs.uranium);
                }
                AddTextAll($"Total Uranium in Reactors: {(float)uStored:N2}\n\n");
                //sb.Append($"Total Uranium in Reactors: {(float)uStored:N2}\n\n");
            }

            WriteAll();
            ClearAllSB();

            //foreach (var cockpit in cockpits)
            //    cockpit.GetSurface(0).WriteText(sb);

            //displayController?.TryRun(sb.ToString());

            //sb.Clear();
        }

        private string Bar(double value, double maxValue, int barWidth, char c, char pad)
        {
            int numBars = (int)(value * barWidth / maxValue);
            return $"[{new string(c, numBars).PadRight(barWidth, pad)}]";
        }

        private void AddText(string text, IMyEntity entity)
        {      
            StringBuilder sb = sbs[entity.EntityId];
            sb.Append(text);
        }

        private void AddBar(double value, double maxValue, IMyEntity entity)
        {
            int bars = barConfig[entity.EntityId];
            AddText(Bar(value, maxValue, bars, bar, dash), entity);
        }

        private void AddTextAll(string text)
        {
            foreach (var display in displays)
                AddText(text, display);

            foreach (var cockpit in cockpits)
                AddText(text, cockpit);
        }

        private void AddBarAll(double value, double maxValue)
        {
            foreach (var display in displays)
                AddBar(value, maxValue, display);

            foreach (var cockpit in cockpits)
                AddBar(value, maxValue, cockpit);
        }

        private void ClearAllSB()
        {
            foreach (var sb in sbs.Values)
                sb.Clear();
        }

        private void WriteAll()
        {
            foreach (var display in displays)
            {
                var sb = sbs[display.EntityId];
                display.WriteText(sb);
            }

            foreach (var cockpit in cockpits)
            {
                var sbc = sbs[cockpit.EntityId];
                cockpit.GetSurface(0).WriteText(sbc);
            }
        }
    }
}
