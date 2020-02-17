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
        IMySensorBlock sensor;
        IMyRemoteControl remote;
        MyWaypointInfo lastPos = new MyWaypointInfo();
        bool flying = true;

        List<MyDetectedEntityInfo> detected = new List<MyDetectedEntityInfo>();

        public Program()
        {
            sensor = GridTerminalSystem.GetBlockWithName("Mule Sensor") as IMySensorBlock;
            remote = GridTerminalSystem.GetBlockWithName("Mule Remote Control") as IMyRemoteControl;

            if (sensor == null)
                throw new Exception("No camera named Mule Sensor.");

            if (remote == null)
                throw new Exception("No remote control named Mule Remote Control");

            remote.FlightMode = FlightMode.OneWay;
            remote.SetCollisionAvoidance(true);
            remote.ClearWaypoints();
            remote.SpeedLimit = 10;

            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            sensor.DetectOwner = true;
            sensor.TopExtend = 15;
            sensor.BackExtend = 15;
            sensor.LeftExtend = 15;
            sensor.RightExtend = 15;
            sensor.FrontExtend = 15;
            sensor.BottomExtend = 15;
            
                
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
            sensor.DetectedEntities(detected);
            Echo(lastPos.ToString());
            if (flying) {
                if (detected.Count >= 1)
                {
                    lastPos = new MyWaypointInfo("Owner Position", sensor.LastDetectedEntity.Position);
                    remote.SetAutoPilotEnabled(false);
                    flying = false;
                }
            } else {
                if (!lastPos.IsEmpty() && !remote.IsAutoPilotEnabled && !flying)
                {
                    remote.ClearWaypoints();
                    remote.AddWaypoint(lastPos);
                    remote.SetAutoPilotEnabled(true);
                    flying = true;
                }
            }
                
                
                
        }
    }
}
