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
        IMyCameraBlock camera;
        MyCommandLine cmd;
        List<IMyTextPanel> displays;
        StringBuilder sb;


        public Program()
        {
            camera = GridTerminalSystem.GetBlockWithName("Painter Camera") as IMyCameraBlock;
            camera.EnableRaycast = true;
            cmd = new MyCommandLine();
            displays = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(displays, 
                display => display.IsSameConstructAs(Me) && MyIni.HasSection(display.CustomData, "DroneMonitor"));
            sb = new StringBuilder();
            Echo("Ready to give commands");
        }


        public void Main(string argument)
        {
            if (argument.Contains("RETURN"))
            {
                IGC.SendBroadcastMessage(DroneCommands.DRONE_CMD, "RETURN");
                return;
            }
            MyDetectedEntityInfo target;
            sb.Clear();
            sb.AppendLine("Last Detected Entity");
            if (camera.CanScan(1000))
            {
                target = camera.Raycast(1000, 0, 0);
                if (!target.IsEmpty())
                {
                    if (target.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                    {
                        var cmd = $"{DroneCommands.ATTACK} \"{new MyWaypointInfo(target.Name, target.Position)}\"";
                        Echo($"Command sent: ${cmd}");
                        IGC.SendBroadcastMessage(DroneCommands.DRONE_CMD, cmd);
                    }
                    sb.AppendLine(target.Name);
                    sb.AppendLine(target.Position.ToString());
                }
                else
                    sb.Append("Nothing");
                
                Echo(sb.ToString());
                foreach (var display in displays)
                    display.WriteText(sb);
            }                     
        }
    }
}
