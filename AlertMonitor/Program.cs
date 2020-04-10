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
        List<IMyTextPanel> alertDisplays;
        List<IMyTextPanel> currentAlertDisplays;
        MyCommandLine cmd;
        List<string> alerts;

        public Program()
        {
            alertDisplays = new List<IMyTextPanel>();
            currentAlertDisplays = new List<IMyTextPanel>();
            cmd = new MyCommandLine();
            GridTerminalSystem.GetBlocksOfType(alertDisplays, display => display.IsSameConstructAs(Me) && MyIni.HasSection(display.CustomData, "AlertDisplay"));
            GridTerminalSystem.GetBlocksOfType(currentAlertDisplays, display => display.IsSameConstructAs(Me) && MyIni.HasSection(display.CustomData, "CurrentAlertDisplay"));


            foreach (var display in currentAlertDisplays)
            {
                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.WriteText("");
            }
              
            foreach (var display in alertDisplays)
            {
                display.ContentType = ContentType.TEXT_AND_IMAGE;
                display.WriteText("");
            }
            
            if (string.IsNullOrEmpty(Storage))
                alerts = new List<string>();
            else
            {
                alerts = new List<string>(Storage.Split(';'));
                foreach (var display in alertDisplays)
                    DisplayAlerts(display);
                Echo($"{alerts.Count} saved alerts loaded.");
            }

            Echo("Ready to receive alerts");
        }

        public void Save()
        {
            Storage = string.Join(";", alerts);
        }

        public void Main(string argument)
        {
            Color displayColor = Color.White;
            if (cmd.TryParse(argument))
            {
                alerts.Add($"[{DateTime.Now}] {argument}");
                string severity = cmd.Argument(0);
                switch (severity)
                {
                    case "CRITICAL":
                        displayColor = Color.Red;
                        break;
                    case "WARNING":
                        displayColor = Color.Yellow;
                        break;
                    case "INFO":
                        displayColor = Color.Blue;
                        break;
                    case "CLEAR":
                        ClearAlerts();
                        return;
                }

                DisplayCurrentAlert(cmd.Argument(1), displayColor);
                foreach (var display in alertDisplays)
                    DisplayAlerts(display);
            }
        }

        private void ClearAlerts()
        {
            alerts.Clear();
            foreach (var display in alertDisplays)
                display.WriteText("");
            Echo("Alerts cleared.");
        }

        private void DisplayCurrentAlert(string alert, Color color)
        {
            foreach (var display in currentAlertDisplays)
            {
                display.FontColor = color;
                display.WriteText(alert);
            }
            Echo(alert);
        }

        private void DisplayAlerts(IMyTextPanel display)
        {
            var copyAlerts = new List<string>(alerts);
            copyAlerts.Reverse();
            display.WriteText(string.Join("\n", copyAlerts));
        }
    }
}
