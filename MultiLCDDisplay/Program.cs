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
        List<IMyTextPanel> displays = new List<IMyTextPanel>();
        Paginator pg;
        string pattern = "Multi Display (\\d+)";

        public Program()
        {
            GridTerminalSystem.GetBlocksOfType(displays, display => System.Text.RegularExpressions.Regex.IsMatch(display.CustomName, pattern));
            pg = new Paginator(this, displays[0]);
            Runtime.UpdateFrequency = UpdateFrequency.Once;
            foreach (var display in displays)
            {
                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.Font = "Monospace";
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (string.IsNullOrEmpty(argument))
                return;
            pg.FromString(argument);
            pg.Paginate(displays[0]);
            if (displays.Count < pg.TotalPages)
                throw new Exception($"Not enough displays for text; need {pg.TotalPages}");
            for (int p = 0; p<pg.TotalPages; p++)
            {
                var display = displays.Find(d => d.CustomName.Contains($"Multi Display {p+1}"));
                display.WriteText(pg.Page(p));
            }
        }
    }
}
