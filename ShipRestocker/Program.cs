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
        List<IMyTerminalBlock> inventories;
        Dictionary<long, MyIni> parsers;
        List<IMyCargoContainer> toRestock;
        Definitions defs;

        public Program()
        {
            inventories = new List<IMyTerminalBlock>();
            toRestock = new List<IMyCargoContainer>();
            parsers = new Dictionary<long, MyIni>();
            defs = new Definitions();
            GridTerminalSystem.GetBlocksOfType(toRestock, r => MyIni.HasSection(r.CustomData, "Restock") && r.IsSameConstructAs(Me));
            
            if (toRestock.Count == 0)
                throw new Exception("No destination cargo containers configured");

            foreach (var cargo in toRestock)
            {
                var parser = new MyIni();
                if (parser.TryParse(cargo.CustomData))
                    parsers.Add(cargo.EntityId, parser);
                else
                    Echo($"Could not parse custom data for {cargo.CustomName}; see readme.");
            }
            Echo("Ready to restock");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            foreach (var cargo in toRestock)
            {
                GridTerminalSystem.GetBlocksOfType(inventories, i => i.HasInventory && MyIni.HasSection(i.CustomData, "RestockSource"));
                
                if (inventories.Count == 0)
                {
                    Echo("No source containers found; aborting.");
                    return;
                }

                var startTime = DateTime.Now;
                var keyList = new List<MyIniKey>();
                var parser = parsers[cargo.EntityId];
                parser.GetKeys(keyList);
                foreach (var key in keyList)
                {
                    var item = defs.GetItemType(key.Name);
                    var amt = parser.Get(key).ToInt32() - cargo.GetInventory(0).GetItemAmount(item);
                    if (amt > 0)
                    {
                        Echo($"Restocking {cargo.CustomName} with {amt} {key.Name}");
                        Restock(cargo, item, amt);
                    }
                    else
                        Echo($"{cargo.CustomName} has quota of {key.Name}; skipping");
                        
                }
                var endTime = DateTime.Now;
                Echo($"Restocked {cargo.CustomName} in {(endTime - startTime).TotalMilliseconds} ms");
            }
            
        }

        private void Restock(IMyCargoContainer cargo, MyItemType item, MyFixedPoint amt)
        {
            var remaining = amt;
            var cargoInv = cargo.GetInventory(0);
            foreach (var inventory in inventories)
            {
                if (inventory.EntityId == cargo.EntityId)
                    continue;

                if (remaining <= 0)
                    return;

                if (toRestock.Contains(inventory as IMyCargoContainer))
                    return;

                for (int i = 0; i < inventory.InventoryCount; i++)
                {
                    var inv = inventory.GetInventory(i);
                    var invItem = inv.FindItem(item);
                    if (inv.CanTransferItemTo(cargoInv, item) && invItem.HasValue)
                    {
                        var inStock = inv.GetItemAmount(item);
                        if (inStock < remaining)
                        {
                            inv.TransferItemTo(cargoInv, invItem.Value);
                            remaining -= inStock;
                        }
                        else
                        {
                            inv.TransferItemTo(cargoInv, invItem.Value, remaining);
                            return;
                        }
                    }
                }
            }
        }
    }
}
