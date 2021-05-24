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
    //comment
    partial class Program : MyGridProgram
    {
        MyIni ini = new MyIni();
        Dictionary<string, int> table;
        List<string> inProduction;
        //IMyInventory cargoInv;
        Definitions defs = new Definitions();
        List<IMyProductionBlock> assemblers = new List<IMyProductionBlock>();
        List<IMyAssembler> disassemblers = new List<IMyAssembler>();
        List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();
        IMyTextSurface myDisplay;
        StringBuilder sb;
        List<MyProductionItem> queue = new List<MyProductionItem>();
        List<IMyTextPanel> displays = new List<IMyTextPanel>();
        MyFixedPoint maxStack;
        const string V = "2.9";
        const string defaultData = @"[Stocks]
BulletproofGlass=0
Canvas=0
Computer=0
ConstructionComp=0
DetectorComp=0
Display=0
Girder=0
GravityComp=0
InteriorPlate=0
LargeTube=0
MedicalComp=0
MetalGrid=0
Missile=0
Motor=0
PowerCell=0
RadioComm=0
ReactorComp=0
SmallTube=0
SolarCell=0
SteelPlate=0
BigNato=0
SmallNato=0
ThrusterComp=0
Superconductor=0
Wolfram40mmMod=0
ConcreteMod=0
HydrogenBottle=0
OxygenBottle=0
EliteWelder=0
EliteGrinder=0
EliteDrill=0

[Config]
MaxStack=5000
---";

        public Program()
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            myDisplay = Me.GetSurface(0);
            sb = new StringBuilder();
            table = new Dictionary<string, int>();
            inProduction = new List<string>();

            UpdateBlocks();

            if (string.IsNullOrEmpty(Storage) || MyIni.HasSection(Me.CustomData, "Reset"))
                Reset();
            else
            {
                Me.CustomData = Storage;
                UpdateStocks();
            }

            myDisplay.ContentType = ContentType.TEXT_AND_IMAGE;

        }

        public void Save()
        {
            Storage = Me.CustomData;
        }

        private void Reset()
        {
            Storage = "";
            Me.CustomData = defaultData;
            UpdateStocks();
        }

        private void UpdateStocks()
        {
            MyIniParseResult result;

            if (!ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());
            else
                Echo("Config file parsed");

            var iniKeys = new List<MyIniKey>();
            ini.GetKeys("Stocks", iniKeys);

            for (int i = 0; i < iniKeys.Count; i++)
            {
                var key = iniKeys[i];
                Echo(key.ToString());
                table[key.Name] = ini.Get(key).ToInt32();
            }
            maxStack = ini.Get("Config", "MaxStack").ToInt32();
        }

        private void UpdateBlocks()
        {
            GridTerminalSystem.GetBlocksOfType(assemblers, assembler => assembler.IsSameConstructAs(Me) && MyIni.HasSection(assembler.CustomData, "Factory"));
            GridTerminalSystem.GetBlocksOfType(inventories, block => block.HasInventory && block.IsSameConstructAs(Me) && !MyIni.HasSection(block.CustomData, "FactoryIgnore"));
            GridTerminalSystem.GetBlocksOfType(displays, display => display.IsSameConstructAs(Me) && MyIni.HasSection(display.CustomData, "FactoryDisplay"));
            GridTerminalSystem.GetBlocksOfType(disassemblers, dis => dis.IsSameConstructAs(Me) && MyIni.HasSection(dis.CustomData, "FactoryDisassembler"));

            if (assemblers.Count == 0)
                Echo("No assemblers found");
            else
            {
                Echo($"Assembler count: {assemblers.Count}");
                foreach (var assembler in assemblers)
                {
                    Echo(assembler.CustomName);
                }
            }

            if (disassemblers.Count == 0)
                Echo("No disassemblers found");
            else
            {
                Echo($"Disassembler count: {disassemblers.Count}");
                foreach (var dis in disassemblers)
                {
                    Echo(dis.CustomName);
                    dis.Mode = MyAssemblerMode.Disassembly;
                    dis.Repeating = true;
                }
            }

            foreach (var display in displays)
                display.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var arg = argument.ToLower();
            sb.AppendLine($"Rakaneth's AutoAssembler v{V}");
            sb.AppendLine("------------------------------");

            if (arg == "setup stocks")
            {
                UpdateStocks();
                return;
            }

            if (arg == "setup blocks")
            {
                UpdateBlocks();
                return;
            }

            if (arg == "reset")
            {
                Reset();
                return;
            }

            if (arg == "clear")
            {
                ClearAllQueues();
                Echo("All assembler queues cleared.");
                return;
            }

            var prodString = inProduction.Count > 0 ? string.Join(", ", inProduction.ToArray()) : "Nothing";
            Echo($"In Production: {prodString}");
            sb.AppendLine($"In Production: {prodString}");
            var items = table.Keys.ToList();
            MyFixedPoint amt = 0;
            //int remainder = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var itemID = items[i];
                MyFixedPoint minimum = table[itemID];
                var thing = defs.GetDefinitionId(itemID);
                var item = defs.GetItemType(itemID);
                foreach (var inventory in inventories)
                {
                    for (int j = 0; j < inventory.InventoryCount; j++)
                    {
                        amt += inventory.GetInventory(j).GetItemAmount(item);
                    }
                }

                Echo($"{item.SubtypeId} Minimum: {minimum}");
                Echo($"{item.SubtypeId} Amt: {amt}");
                var toQueue = minimum - amt;
                Echo($"{thing.SubtypeName} to queue: {toQueue}");
                queue.Clear();
                //int amtPerAssembler = Math.DivRem(toQueue.ToIntSafe(), assemblers.Count, out remainder); 
                if (amt < minimum)
                {
                    if (inProduction.Contains(itemID))
                    {
                        var alreadyQueued = HowManyQueued(thing, assemblers);
                        Echo($"{toQueue} {itemID} to go");
                        sb.AppendLine($"{itemID}: {amt} / {minimum} Queued: {alreadyQueued}");
                        if (amt + alreadyQueued < minimum)
                        {
                            divideAndQueue(thing, minimum - alreadyQueued - amt, assemblers);
                        }
                    }
                    else
                    {
                        Echo($"Queuing up ${toQueue} {itemID}");
                        divideAndQueue(thing, toQueue, assemblers);
                        inProduction.Add(itemID);
                    }
                }
                else
                {
                    Echo($"Quota of {itemID} has been met; removing");
                    var stillQueued = HowManyQueued(thing, assemblers);
                    if (stillQueued > 0)
                        RemoveFromQueue(thing, assemblers, stillQueued);
                    inProduction.Remove(itemID);
                }

                amt = 0;
            }
            myDisplay.WriteText(sb);

            foreach (var display in displays)
                display.WriteText(sb);

            sb.Clear();
        }

        private void divideAndQueue(MyDefinitionId bp, MyFixedPoint amt, List<IMyProductionBlock> assemblers)
        {
            int remainder;
            var amtPerAssembler = Math.DivRem((int)amt, assemblers.Count, out remainder);
            foreach (var assembler in assemblers)
            {
                queueUp(bp, amtPerAssembler, assembler);
                //assembler.AddQueueItem(bp, (MyFixedPoint)amtPerAssembler);
            }


            if (remainder > 0)
            {
                queueUp(bp, remainder, assemblers[0]);
                //assemblers[0].AddQueueItem(bp, (MyFixedPoint)remainder);
            }
        }

        private void queueUp(MyDefinitionId bp, MyFixedPoint amt, IMyProductionBlock assembler)
        {
            if (amt < 1) { return; }
            //queue.Clear();
            //assembler.GetQueue(queue);
            //var idx = queue.FindLastIndex(item => item.BlueprintId == bp);
            assembler.AddQueueItem(bp, amt);
        }

        private void ClearAllQueues()
        {
            foreach (var assembler in assemblers)
                assembler.ClearQueue();
        }

        private MyFixedPoint HowManyQueued(MyDefinitionId bp, List<IMyProductionBlock> assemblers)
        {
            MyFixedPoint acc = 0;
            foreach (var assembler in assemblers)
            {
                queue.Clear();
                assembler.GetQueue(queue);
                foreach (var item in queue)
                    if (item.BlueprintId == bp)
                        acc += item.Amount;
            }

            return acc;
        }

        private void RemoveFromQueue(MyDefinitionId bp, List<IMyProductionBlock> assemblers, MyFixedPoint amt)
        {
            foreach (var assembler in assemblers)
            {
                queue.Clear();
                assembler.GetQueue(queue);
                var idx = queue.FindIndex(q => q.BlueprintId == bp);
                assembler.RemoveQueueItem(idx, amt);
            }
        }
    }
}
