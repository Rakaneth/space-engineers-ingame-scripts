using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class CommonFunctions
        {
            private List<MyInventoryItem> workingItems = new List<MyInventoryItem>();

            public void TagBlock(string tag, IMyTerminalBlock block)
            {
                if (!block.CustomName.Contains(tag))
                {
                    block.CustomName = $"{block.CustomName} [{tag}]";
                }
            }

            public void PBDisplay(string text, IMyProgrammableBlock pb)
            {
                IMyTextSurface panel = pb.GetSurface(0);
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.WriteText(text);
            }
            /// <summary>
            /// Transfers items from `src` to `dest`. 
            /// </summary>
            /// <param name="src">The source inventory.</param>
            /// <param name="dest">The destination inventory.</param>
            /// <returns>`true` if all items transferred successfully, `false` otherwise.</returns>
            public bool TransferInventory(IMyInventory src, IMyInventory dest)
            {
                bool sux = true;
                workingItems.Clear();
                src.GetItems(workingItems);
                foreach(var item in workingItems)
                {
                    if (!src.TransferItemTo(dest, item)) sux = false;
                }
                return sux;
            }
        }
    }
}
