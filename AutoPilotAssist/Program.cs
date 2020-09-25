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
using Sandbox;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        List<IMyRemoteControl> remotes = new List<IMyRemoteControl>();
        MyCommandLine cmd = new MyCommandLine();

        public Program()
        {
            Refresh();
        }


        public void Main(string argument)
        {
            var a = argument.ToLower();

            if (a == "refresh")
            {
                Refresh();
                return;
            }

            if (cmd.TryParse(a))
            {
                var command = cmd.Argument(0);
                switch (command)
                {
                    case "speed":
                        float spd;
                        if (float.TryParse(cmd.Argument(1), out spd))
                            foreach (var remote in remotes)
                                remote.SpeedLimit = spd;
                        else
                            Echo("Error setting speed limit.");
                        break;
                    default:
                        Echo($"Command {command} not recognized.");
                        break;
                }
            }
        }

        private void Refresh()
        {
            GridTerminalSystem.GetBlocksOfType(remotes, r => r.IsSameConstructAs(Me) && MyIni.HasSection(r.CustomData, "AutoPilotAssist"));
            Echo($"{remotes.Count} remote controllers requesting assistance.");
        }

        
    }
}
