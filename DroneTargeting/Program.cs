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
        IMyBroadcastListener listener;
        IMyRemoteControl executor;
        List<IMyUserControllableGun> guns;
        MyWaypointInfo home;
        MyCommandLine parser;

        public Program()
        {
            listener = IGC.RegisterBroadcastListener(DroneCommands.DRONE_CMD);
            executor = GridTerminalSystem.GetBlockWithName("Command AI") as IMyRemoteControl;
            executor.FlightMode = FlightMode.OneWay;
            executor.SetCollisionAvoidance(true);
            executor.SetDockingMode(false);
            guns = new List<IMyUserControllableGun>();
            GridTerminalSystem.GetBlocksOfType(guns, gun => gun.IsSameConstructAs(Me));
            home = new MyWaypointInfo("Home", executor.GetPosition());
            parser = new MyCommandLine();
            listener.SetMessageCallback();
            Echo("Ready to receive commands");
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

        public void Main()
        {
            var msg = listener.AcceptMessage();
            string data = msg.Data as string;
            if (parser.TryParse(data))
            {
                Echo($"Arg 0: {parser.Argument(0)}");
                Echo($"Arg 1: {parser.Argument(1)}");
                var cmd = parser.Argument(0);
                MyWaypointInfo wp = new MyWaypointInfo();
                Echo(data);
                bool wpParsed = false;
                Echo($"Args passed: {parser.ArgumentCount}");
                if (parser.ArgumentCount > 1)
                    wpParsed = MyWaypointInfo.TryParse(parser.Argument(1), out wp);
                Echo($"WP parsed: {wpParsed}");
                
                switch (cmd)
                {
                    case DroneCommands.ATTACK: //ATTACK (waypoint)
                        executor.ClearWaypoints();
                        Echo($"Current target: {wp.Name}");
                        SetGuns(true);
                        executor.AddWaypoint(wp);
                        executor.SetAutoPilotEnabled(true);
                        break;
                    case DroneCommands.RETURN: //RETURN
                        executor.ClearWaypoints();
                        SetGuns(false);
                        executor.AddWaypoint(home);
                        executor.SetAutoPilotEnabled(true);
                        break;
                    case DroneCommands.SET_HOME: //SET_HOME (waypoint)
                        home = wp;
                        break;
                }
            }

        }

        private void SetGuns(bool enabled)
        {
            foreach (var gun in guns)
                gun.Enabled = enabled;
        }
    }
}
