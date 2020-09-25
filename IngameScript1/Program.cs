﻿using Sandbox.Game.EntityComponents;
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
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();
        IMyTextSurface myDisplay;
        StringBuilder sb;
        List<MyProductionItem> queue = new List<MyProductionItem>();
        List<IMyTextPanel> displays = new List<IMyTextPanel>();      

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            myDisplay = Me.GetSurface(0);
            sb = new StringBuilder();
                
            table = new Dictionary<string, int>();
            inProduction = new List<string>();

            UpdateBlocks();

            Me.CustomData = @"[Stocks]
BulletproofGlass=0
Computer=0
Concrete=0
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
---";
            myDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
            myDisplay.WriteText("Auto Assembler");
        }

        private void UpdateStocks()
        {
            MyIniParseResult result;
            if (!ini.TryParse(Me.CustomData, out result))
                throw new Exception(result.ToString());
            else
                Echo("Config file parsed");

            var iniKeys = new List<MyIniKey>();
            ini.GetKeys(iniKeys);

            for (int i = 0; i < iniKeys.Count; i++)
            {
                var key = iniKeys[i];
                Echo(key.ToString());
                table[key.Name] = ini.Get(key).ToInt32();
            }
        }

        private void UpdateBlocks()
        {
            GridTerminalSystem.GetBlocksOfType(assemblers, assembler => assembler.IsSameConstructAs(Me) && MyIni.HasSection(assembler.CustomData, "Factory"));
            GridTerminalSystem.GetBlocksOfType(inventories, block => block.HasInventory && block.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(displays, display => display.IsSameConstructAs(Me) && MyIni.HasSection(display.CustomData, "FactoryDisplay"));
            if (assemblers.Count == 0)
                Echo("No assemblers found");
            else 
            {
                Echo($"Assembler count: {assemblers.Count}");
                foreach(var assembler in assemblers)
                {
                    Echo(assembler.CustomName);
                }
            }
                
            
            foreach (var display in displays)
                display.ContentType = ContentType.TEXT_AND_IMAGE;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var arg = argument.ToLower();
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
                        sb.AppendLine($"{itemID}: {toQueue}");
                        sb.AppendLine($"Queued: {alreadyQueued}");
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

        private void divideAndQueue(MyDefinitionId bp, MyFixedPoint amt, List<IMyAssembler> assemblers)
        {
            int remainder;
            var amtPerAssembler = Math.DivRem((int)amt, assemblers.Count, out remainder);
            foreach (var assembler in assemblers)
            {
                assembler.AddQueueItem(bp, (MyFixedPoint)amtPerAssembler);
            }
            

            if (remainder > 0)
            {
                assemblers[0].AddQueueItem(bp, (MyFixedPoint)remainder);
            }
        }

        private MyFixedPoint HowManyQueued(MyDefinitionId bp, List<IMyAssembler> assemblers)
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

        private void RemoveFromQueue(MyDefinitionId bp, List<IMyAssembler> assemblers, MyFixedPoint amt)
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
