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
        List<IMyBatteryBlock> batts;
        List<IMyTextPanel> panels = new List<IMyTextPanel>();
        StringBuilder sb = new StringBuilder();
        List<IMyPowerProducer> producers = new List<IMyPowerProducer>();
        

        public Program()
        {
            batts = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(batts);
            GridTerminalSystem.GetBlocksOfType(panels, panel => MyIni.HasSection(panel.CustomData, "PowerDisplay"));
            GridTerminalSystem.GetBlocksOfType(producers);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            
            
            if (panels.Count == 0)
                throw new Exception("Remember to add [PowerDisplay] to the Custom Data of connected displays");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            GridTerminalSystem.GetBlocksOfType(batts);
            float curPwr = 0.0f;
            float maxPwr = 0.0f;
            float allPwr = 0f;
            sb.Append("Battery Health of Attached Batteries\n");
            sb.Append("-------\n\n");
            foreach(var batt in batts) 
            {
                float c = batt.CurrentStoredPower;
                float m = batt.MaxStoredPower;
                sb.Append($"{batt.CubeGrid.CustomName} - {batt.CustomName}: ");
                sb.Append($"{c} MWh / {m} MWh\n");
                curPwr += c;
                maxPwr += m;
            }

            foreach (var producer in producers)
            {
                allPwr += producer.CurrentOutput;
                Echo(producer.CustomName);
            }
                

            sb.Append($"Total Power: {curPwr} MWh / {maxPwr} MWh\n");
            sb.Append($"Power of all inputs: {allPwr} MW\n");
            
            foreach(var panel in panels)
                panel.WriteText(sb);
            
            sb.Clear();
        }
    }
}
