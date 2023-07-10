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
        List<IMyGasTank> smallTanks = new List<IMyGasTank>();
        List<IMyGasTank> oxyTanks = new List<IMyGasTank>();
        //StringBuilder sb = new StringBuilder();
        List<IMyTextPanel> displays = new List<IMyTextPanel>();
        List<IMyTextPanel> battDisplays = new List<IMyTextPanel>();
        List<IMyTextPanel> h2Displays = new List<IMyTextPanel>();
        List<IMyTextPanel> o2Displays = new List<IMyTextPanel>();
        List<IMyTextPanel> reactorDisplays = new List<IMyTextPanel>();
        List<IMyTextPanel> producerDisplays = new List<IMyTextPanel>();
        List<IMyTextPanel> consumerDisplays = new List<IMyTextPanel>();
        List<IMyTerminalBlock> consumers = new List<IMyTerminalBlock>();
        List<IMyBatteryBlock> batts = new List<IMyBatteryBlock>();
        List<IMyReactor> reactors = new List<IMyReactor>();
        List<IMyCockpit> cockpits = new List<IMyCockpit>();
        Dictionary<long, int> barConfig = new Dictionary<long, int>();
        Dictionary<long, StringBuilder> sbs = new Dictionary<long, StringBuilder>();
        Definitions defs = new Definitions();
        MyResourceSinkComponent sink;
        const char bar = '\u2588';
        const char dash = '-';
        const float LG_H2_MAX = 15000000f;
        const float SM_H2_MAX = 1000000f;
        const float OXY_MAX = 100000f;
        IMyProgrammableBlock displayController;
        MyIni ini;

        public Program()
        {
            int bars;
            GridTerminalSystem.GetBlocksOfType(producers, p => p.IsSameConstructAs(Me) && !(p is IMyBatteryBlock));
            GridTerminalSystem.GetBlocksOfType(tanks, e => e.IsSameConstructAs(Me) && e.Capacity == LG_H2_MAX);
            GridTerminalSystem.GetBlocksOfType(smallTanks, e => e.IsSameConstructAs(Me) && e.Capacity == SM_H2_MAX);
            GridTerminalSystem.GetBlocksOfType(batts, b => b.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(displays, display => MyIni.HasSection(display.CustomData, "PowerReadout") && display.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => MyIni.HasSection(cockpit.CustomData, "PowerReadout") && cockpit.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(oxyTanks, o => o.IsSameConstructAs(Me) && o.Capacity == OXY_MAX);
            GridTerminalSystem.GetBlocksOfType(reactors, r => r.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(battDisplays, bd => MyIni.HasSection(bd.CustomData, "BatteryInfo") && bd.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(h2Displays, h2 => MyIni.HasSection(h2.CustomData, "H2Info") && h2.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(o2Displays, o2 => MyIni.HasSection(o2.CustomData, "O2Info") && o2.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(reactorDisplays, rd => MyIni.HasSection(rd.CustomData, "ReactorInfo") && rd.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(producerDisplays, pd => MyIni.HasSection(pd.CustomData, "ProducerInfo") && pd.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(consumerDisplays, cd => MyIni.HasSection(cd.CustomData, "ConsumerInfo") && cd.IsSameConstructAs(Me));
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            displayController = GridTerminalSystem.GetBlockWithName("Display Controller") as IMyProgrammableBlock;

            ini = new MyIni();

            if (displays.Count == 0 && cockpits.Count == 0 && displayController == null)
                throw new Exception("Remember to add [PowerReadout] to the Custom Data of any displays or cockpits you want to show this information.");

            var displayLists = new Dictionary<string, List<IMyTextPanel>>()
            {
                { "PowerReadout", displays },
                { "BatteryInfo", battDisplays },
                { "H2Info", h2Displays },
                { "O2Info", o2Displays },
                {"ReactorInfo", reactorDisplays },
                {"ProducerInfo", producerDisplays },
                {"ConsumerInfo", consumerDisplays },
            };

            Action<long, string> barConfigF = (eid, section) =>
            {
                //Echo($"Config for {eid} with section {section}");
                bars = ini.ContainsKey(section, "Bars") ? ini.Get(section, "Bars").ToInt32() : 5;
                barConfig.Add(eid, bars);
            };

            foreach (var displayListEntry in displayLists)
            {
                foreach (var display in displayListEntry.Value)
                {
                    if (!ini.TryParse(display.CustomData))
                        Echo($"Cannot parse custom data for {display.CustomName}");
                    barConfigF(display.EntityId, displayListEntry.Key);
                    sbs.Add(display.EntityId, new StringBuilder());
                    display.Font = "Monospace";
                    display.ContentType = ContentType.TEXT_AND_IMAGE;
                    display.FontSize = 0.5f;
                }
            }

            foreach (var cockpit in cockpits)
            {
                cockpit.GetSurface(0).Font = "Monospace";
                cockpit.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
                if (!ini.TryParse(cockpit.CustomData))
                    Echo($"Cannot parse custom data for {cockpit.CustomName}");

                bars = ini.ContainsKey("PowerReadout", "Bars") ? ini.Get("PowerReadout", "Bars").ToInt32() : 5;
                barConfig.Add(cockpit.EntityId, bars);
                sbs.Add(cockpit.EntityId, new StringBuilder());
            }

            GridTerminalSystem.GetBlocksOfType(consumers,
                consumer => consumer.Components.TryGet(out sink)
                && sink.IsPoweredByType(defs.electricity)
                && !(consumer is IMyBatteryBlock)
                && consumer.BlockDefinition.SubtypeName != "LargeHydrogenEngine");
            var smReact = reactors.Count(e => e.MaxOutput == 15f);
            var lgReact = reactors.Count(e => e.MaxOutput == 300f);
            Echo($"Batteries: {batts.Count}");
            Echo($"Hydrogen Tanks: {tanks.Count + smallTanks.Count}");
            Echo($"--Small H2 Tanks: {smallTanks.Count}");
            Echo($"--Large H2 Tanks: {tanks.Count}");
            Echo($"Oxygen Tanks: {oxyTanks.Count}");
            Echo($"Reactors: {reactors.Count}");
            Echo($"--Small Reactors: {smReact}");
            Echo($"--Large Reactors: {lgReact}");

            if (!Me.CustomName.Contains("PowerReadout")) {
                Me.CustomName += " [PowerReadout]";
            }

            var myLCD = Me.GetSurface(0);
            myLCD.ContentType = ContentType.TEXT_AND_IMAGE;
            myLCD.WriteText("Power Readout");
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
            double h2Max = 0;
            MyFixedPoint uStored = 0;

            AddDisplayList("Power Sources\n", displays, producerDisplays);

            foreach (var producer in producers)
            {
                generated += producer.CurrentOutput;
                maxGenerated += producer.MaxOutput;
                if (producer.CurrentOutput >= 0.001f)
                    AddDisplayList($"{producer.CustomName}: {producer.CurrentOutput:N2}\n", producerDisplays);
            }

            AddDisplayList($"Total Generated: {generated:N2}/{maxGenerated:N2} MW\n\n", displays, producerDisplays);
            AddDisplayList("Power Sinks\n", displays, consumerDisplays);

            foreach (var consumer in consumers)
            {
                if (consumer.Components.TryGet(out sink))
                {
                    //var defin = consumer.BlockDefinition;
                    //Echo(defin.SubtypeName);
                    u = sink.CurrentInputByType(defs.electricity);
                    used += u;
                    if (u >= 0.1f)
                        AddDisplayList($"{consumer.CustomName}: {u}\n", consumerDisplays);
                }
            }

            AddDisplayList($"Total Consumed: {used:N2} MW", displays);
            AddBarList(used, maxGenerated, displays);
            AddDisplayList($" ({used / maxGenerated * 100:N2}% of max power)\n\n", displays);
            AddDisplayList("Battery Storage\n", displays, battDisplays);

            if (batts.Count > 0)
            {
                foreach (var batt in batts)
                {
                    battStored += batt.CurrentStoredPower;
                    battMaxStored += batt.MaxStoredPower;

                    AddDisplayList($"{batt.CustomName}\nOutput {batt.CurrentOutput:N2} MW\n", battDisplays);
                    AddDisplayList($"{batt.CurrentStoredPower:N2} / {batt.MaxStoredPower:N2} MWh ", battDisplays);
                    AddBarList(batt.CurrentStoredPower, batt.MaxStoredPower, battDisplays);
                    AddDisplayList("\n\n", battDisplays);
                }

                AddDisplayList($"Total Stored: {battStored:N2} / {battMaxStored:N2} MWh", displays, battDisplays);
                AddBarList(battStored, battMaxStored, displays, battDisplays);
                AddDisplayList($" {battStored / battMaxStored * 100:N2}% full\n\n", displays, battDisplays);
            }

            if (tanks.Count + smallTanks.Count > 0)
            {
                AddDisplayList("Hydrogen\n", displays, h2Displays);

                foreach (var tank in tanks)
                {
                    h2Current += tank.FilledRatio * LG_H2_MAX;
                    h2Max += LG_H2_MAX;
                    AddDisplayList($"{tank.CustomName}: {tank.FilledRatio * 100:N2}%", h2Displays);
                    AddBarList(tank.FilledRatio, 1, h2Displays);
                    AddDisplayList($"\n", h2Displays);
                }

                foreach (var smTank in smallTanks)
                {
                    h2Current += smTank.FilledRatio * SM_H2_MAX;
                    h2Max += SM_H2_MAX;
                    AddDisplayList($"{smTank.CustomName}: {smTank.FilledRatio * 100:N2}%", h2Displays);
                    AddBarList(smTank.FilledRatio, 1, h2Displays);
                    AddDisplayList($"\n", h2Displays);
                }

                AddDisplayList($"Total Hydrogen Reserves: {(h2Current * 100 / h2Max):N2}%", displays, h2Displays);
                AddBarList(h2Current, h2Max, displays, h2Displays);
                AddDisplayList("\n\n", displays, h2Displays);
            }

            if (oxyTanks.Count > 0)
            {
                AddDisplayList("Oxygen\n", displays, o2Displays);

                foreach (var oxy in oxyTanks)
                {
                    o2Current += oxy.FilledRatio;
                    AddDisplayList($"{oxy.CustomName}: {oxy.FilledRatio * 100:N2}%", o2Displays);
                    AddBarList(oxy.FilledRatio, 1, o2Displays);
                    AddDisplayList("\n", o2Displays);
                }

                AddDisplayList($"Total Oxygen Reserves: {o2Current * 100 / oxyTanks.Count:N2}%", displays, o2Displays);
                AddBarList(o2Current, oxyTanks.Count, displays, o2Displays);
                AddDisplayList("\n\n", displays, o2Displays);
            }

            if (reactors.Count > 0)
            {
                AddDisplayList("Reactors\n", displays, reactorDisplays);

                foreach (var reactor in reactors)
                {
                    var thisU = reactor.GetInventory(0).GetItemAmount(defs.uranium);
                    uStored += thisU;
                    AddDisplayList($"{reactor.CustomName}\nOutput: {reactor.CurrentOutput:N2} MW\n", reactorDisplays);
                    AddDisplayList($"Uranium Stored: {(float)thisU:N2} kg\n\n", reactorDisplays);
                }

                AddDisplayList($"Total Uranium in Reactors: {(float)uStored:N2}\n\n", displays, reactorDisplays);
            }

            WriteAll();
            ClearAllSB();
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

        private void AddDisplayList(string text, params List<IMyTextPanel>[] displayLists)
        {
            foreach (var displayList in displayLists)
                foreach (var d in displayList)
                    AddText(text, d);
        }

        private void AddBarList(double value, double maxValue, params List<IMyTextPanel>[] displayLists)
        {
            foreach (var displayList in displayLists)
                foreach (var d in displayList)
                    AddBar(value, maxValue, d);
        }

        private void ClearAllSB()
        {
            foreach (var sb in sbs.Values)
                sb.Clear();
        }


        private void WriteAll()
        {
            var displayLists = new List<List<IMyTextPanel>>() { displays, battDisplays, h2Displays, o2Displays, reactorDisplays, producerDisplays, consumerDisplays };
            foreach (var displayList in displayLists)
            {
                foreach (var display in displayList)
                {
                    var sb = sbs[display.EntityId];
                    display.WriteText(sb);
                }
            }

            foreach (var cockpit in cockpits)
            {
                var sbc = sbs[cockpit.EntityId];
                cockpit.GetSurface(0).WriteText(sbc);
            }
        }
    }
}
