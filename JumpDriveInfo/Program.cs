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
//using Sandbox.ModAPI;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        readonly List<IMyJumpDrive> jumpDrives = new List<IMyJumpDrive>();
        readonly List<IMyTextSurface> cockpitDisplays = new List<IMyTextSurface>();
        readonly List<IMyTextPanel> lcds = new List<IMyTextPanel>();
        readonly MyIni ini = new MyIni();
        const string TAG = "JumpDriveInfo";
        readonly StringBuilder sb = new StringBuilder();


        public Program()
        {
            List<IMyCockpit> cockpits = new List<IMyCockpit>();
            GridTerminalSystem.GetBlocksOfType(cockpits, cockpit => cockpit.IsSameConstructAs(Me) && MyIni.HasSection(cockpit.CustomData, TAG));
            GridTerminalSystem.GetBlocksOfType(lcds, lcd => lcd.IsSameConstructAs(Me) && MyIni.HasSection(lcd.CustomData, TAG));
            GridTerminalSystem.GetBlocksOfType(jumpDrives, jumpDrive => jumpDrive.IsSameConstructAs(Me));
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            foreach (var cockpit in cockpits)
            {
                if (ini.TryParse(cockpit.CustomData))
                {
                    int displayIdx = 0;
                    var displayVal = ini.Get(TAG, "Display");
                    
                    if (!displayVal.IsEmpty)
                        displayIdx = displayVal.ToInt32();

                    var surface = cockpit.GetSurface(displayIdx);
                    surface.ContentType = ContentType.TEXT_AND_IMAGE;
                    cockpitDisplays.Add(surface);
                }
            }

            foreach (var lcd in lcds)
                lcd.ContentType = ContentType.TEXT_AND_IMAGE;

            if (jumpDrives.Count == 0)
                Echo("No jumpdrives found.");
            else
                Echo($"Initialization complete. {jumpDrives.Count} jumpdrives found.");
        }


        public void Main(string argument, UpdateType updateSource)
        {
            sb.Clear();
            foreach (var jd in jumpDrives)
            {
                sb.AppendLine($"{jd.CustomName}");
                sb.AppendLine($"Power: {jd.CurrentStoredPower:N2} / {jd.MaxStoredPower:N2} MWh ({jd.CurrentStoredPower / jd.MaxStoredPower * 100:N2}%)");
                sb.AppendLine($"Status: {jd.Status}");
                sb.AppendLine();
            }

            foreach (var cd in cockpitDisplays)
                cd.WriteText(sb);

            foreach (var lcd in lcds)
                lcd.WriteText(sb);
        }
    }
}
