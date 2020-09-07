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
        IMyTextPanel lcd;
        IMySensorBlock dockSensor;
        List<MyDetectedEntityInfo> detected = new List<MyDetectedEntityInfo>();

        public Program()
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update10;
            Refresh();
        }

        private void Refresh()
        {
            List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<IMyTextPanel> lcds = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(lcds, l => l.IsSameConstructAs(Me) && MyIni.HasSection(l.CustomData, "InfoPanel"));
            GridTerminalSystem.GetBlocksOfType(sensors, s => s.IsSameConstructAs(Me) && MyIni.HasSection(s.CustomData, "DockingSensor"));

            if (lcds.Count == 0)
            {
                Echo("No LCDs found. Configure an LCD with [InfoPanel] in its custom data.");
                return;
            }
                
            if (sensors.Count == 0)
            {
                Echo("No sensors found. Configure a sensor with [DockingSensor] in its custom data.");
                return;
            }

            lcd = lcds[0];
            lcd.ContentType = ContentType.TEXT_AND_IMAGE;
            dockSensor = sensors[0];

            Echo("Sensor and LCD found.");
        }

        public void Main(string argument)
        {
            if (argument.ToLower() == "refresh")
                Refresh();
            else
            {
                dockSensor.DetectedEntities(detected);
                var connector = detected.FirstOrDefault(e => e.Name.Contains("Connector"));

                if (!connector.IsEmpty())
                    lcd.WriteText("Connector below");
            }
            
        }
    }
}
