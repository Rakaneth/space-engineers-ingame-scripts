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

        public Program()
        {
            inventories = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(inventories, i => i.HasInventory && i.IsSameConstructAs(Me));
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            var startSortTime = System.DateTime.Now;
            foreach(var inventory in inventories)
            {
                for (int i=0; i<inventory.InventoryCount; i++)
                {
                    StackInventory(inventory.GetInventory(i));
                }
            }
            var endSortTime = DateTime.Now;
            Echo($"Sort completed in {endSortTime.Subtract(startSortTime).TotalMilliseconds} ms");
        }

        private void StackInventory(IMyInventory inv)
        {
            for (int i=inv.ItemCount-1; i>=0; i--) 
            {
                var item = inv.GetItemAt(i);
                if (item.HasValue)
                {
                    var type = item.Value.Type;
                    for (int j=0; j<i; j++)
                    {
                        var dup = inv.GetItemAt(j);
                        if (dup?.Type == type)
                        {
                            inv.TransferItemTo(inv, i, j, true);
                            break;
                        }
                    }
                }
            }
        }
    }
}
