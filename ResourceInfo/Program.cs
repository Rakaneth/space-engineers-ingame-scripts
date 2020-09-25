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
        List<IMyTerminalBlock> blox = new List<IMyTerminalBlock>();
        List<IMyTextPanel> panels = new List<IMyTextPanel>();
        StringBuilder sb = new StringBuilder();
        Definitions defs = new Definitions();


        public Program()
        {
            GridTerminalSystem.GetBlocksOfType(blox, block => block.IsSameConstructAs(Me) && block.HasInventory);
            GridTerminalSystem.GetBlocksOfType(panels, panel => MyIni.HasSection(panel.CustomData, "ResourceDisplay"));
            
            if (panels.Count == 0)
                throw new Exception("Remember to add [ResourceDisplay] to the Custom Data of connected displays");

            foreach (var panel in panels)
                panel.Font = "Monospace";

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private string tableFormat(string label, MyFixedPoint ore, MyFixedPoint ingot)
        {
            return $"{label,-10}{(double)ore,-15:F2}{(double)ingot,-15:F2}\n";
        }

        public void Main(string argument, UpdateType updateSource)
        {
            MyFixedPoint Fe = 0;
            MyFixedPoint Si = 0;
            MyFixedPoint Ni = 0;
            MyFixedPoint Au = 0;
            MyFixedPoint Ag = 0;
            MyFixedPoint U = 0;
            MyFixedPoint Mg = 0;
            MyFixedPoint Pt = 0;
            MyFixedPoint Co = 0;
            MyFixedPoint Ice = 0;
            MyFixedPoint FeOre = 0;
            MyFixedPoint SiOre = 0;
            MyFixedPoint NiOre = 0;
            MyFixedPoint AuOre = 0;
            MyFixedPoint AgOre = 0;
            MyFixedPoint UOre = 0;
            MyFixedPoint MgOre = 0;
            MyFixedPoint PtOre = 0;
            MyFixedPoint CoOre = 0;
            MyFixedPoint Stone = 0;
            MyFixedPoint Gravel = 0;

            foreach (var block in blox)
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {

                    var inv = block.GetInventory(i);
                    Fe += inv.GetItemAmount(defs.iron);
                    Si += inv.GetItemAmount(defs.silicon);
                    Ni += inv.GetItemAmount(defs.nickel);
                    Au += inv.GetItemAmount(defs.gold);
                    Ag += inv.GetItemAmount(defs.silver);
                    U += inv.GetItemAmount(defs.uranium);
                    Mg += inv.GetItemAmount(defs.magnesium);
                    Ice += inv.GetItemAmount(defs.ice);
                    Co += inv.GetItemAmount(defs.cobalt);
                    Pt += inv.GetItemAmount(defs.platinum);
                    FeOre += inv.GetItemAmount(defs.ironOre);
                    SiOre += inv.GetItemAmount(defs.siliconOre);
                    NiOre += inv.GetItemAmount(defs.nickelOre);
                    AuOre += inv.GetItemAmount(defs.goldOre);
                    AgOre += inv.GetItemAmount(defs.silverOre);
                    UOre += inv.GetItemAmount(defs.uraniumOre);
                    MgOre += inv.GetItemAmount(defs.magnesiumOre);
                    CoOre += inv.GetItemAmount(defs.cobaltOre);
                    PtOre += inv.GetItemAmount(defs.platinumOre);
                    Stone += inv.GetItemAmount(defs.stone);
                    Gravel += inv.GetItemAmount(defs.gravel);
                }      
            }
            sb.Clear();
            sb.Append($"{Me.CubeGrid.CustomName} Ingot Resources (units in kg)\n");
            sb.Append("------------------------------------\n\n");
            sb.Append($"{"Resource",-10}{"Ore",-15}{"Ingot",-15}\n");
            sb.Append(tableFormat("Iron", FeOre, Fe));
            sb.Append(tableFormat("Silicon", SiOre, Si));
            sb.Append(tableFormat("Nickel", NiOre, Ni));
            sb.Append(tableFormat("Cobalt", CoOre, Co));
            sb.Append(tableFormat("Gold", AuOre, Au));
            sb.Append(tableFormat("Uranium", UOre, U));
            sb.Append(tableFormat("Magnesium", MgOre, Mg));
            sb.Append(tableFormat("Platinum", PtOre, Pt));
            sb.Append(tableFormat("Silver", AgOre, Ag));
            sb.Append($"{"Ice",-10}{(double)Ice,-15:F2}\n");
            sb.Append($"{"Stone",-10}{(double)Stone,-15:F2}\n");
            sb.Append($"{"Gravel",-10}{(double)Gravel,-15:F2}\n");
            
            foreach (var display in panels)
                display.WriteText(sb);
        }
    }
}
