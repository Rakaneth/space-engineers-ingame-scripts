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
        IMyShipController controller;
        List<IMyThrust> upThrusters = new List<IMyThrust>();
        List<IMyThrust> downThrusters = new List<IMyThrust>();
        List<IMyThrust> leftThrusters = new List<IMyThrust>();
        List<IMyThrust> rightThrusters = new List<IMyThrust>();
        List<IMyThrust> forwardThrusters = new List<IMyThrust>();
        List<IMyThrust> backThrusters = new List<IMyThrust>();
        Dictionary<string, List<IMyThrust>> thrusterDict;
        const double G = 9.81;
        const double moonG = 0.25 * G;
        const double alienG = 1.1 * G;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            thrusterDict = new Dictionary<string, List<IMyThrust>>()
            {
                { "up", upThrusters },
                { "down", downThrusters },
                { "left", leftThrusters },
                { "right", rightThrusters },
                { "forward", forwardThrusters },
                { "back", backThrusters },
            };
            Refresh();
        }

        public void Main(string argument)
        {
            var cmd = argument.ToLower();

            if (cmd == "refresh")
                Refresh();

            else if (thrusterDict.ContainsKey(cmd))
                GetThrustInfo(thrusterDict[cmd], cmd);
            else
                Echo("Unknown command.");
        }

        private void Refresh()
        {
            var controls = new List<IMyShipController>();
            GridTerminalSystem.GetBlocksOfType(controls, control => control.IsSameConstructAs(Me) && MyIni.HasSection(control.CustomData, "MainControl"));
            controller = controls[0];
            InitThrusters(upThrusters, Vector3.Up);
            InitThrusters(downThrusters, Vector3.Down);
            InitThrusters(leftThrusters, Vector3.Left);
            InitThrusters(rightThrusters, Vector3.Right);
            InitThrusters(forwardThrusters, Vector3.Forward);
            InitThrusters(backThrusters, Vector3.Backward);

            Echo($"Main controller: {controller.CustomName}");
            Echo($"Up thrusters: {upThrusters.Count}");
            Echo($"Down thrusters: {downThrusters.Count}");
            Echo($"Left Thrusters: {leftThrusters.Count}");
            Echo($"Right Thrusters: {leftThrusters.Count}");
            Echo($"Forward Thrusters: {forwardThrusters.Count}");
            Echo($"Backward Thrusters: {backThrusters.Count}");
        }

        private void InitThrusters(List<IMyThrust> thrustList, Vector3 direction)
        {
            GridTerminalSystem.GetBlocksOfType(thrustList, t => t.IsSameConstructAs(Me) && t.GridThrustDirection == direction);
        }

        private void GetThrustInfo(List<IMyThrust> thrustList, string dir)
        {
            var masses = controller.CalculateShipMass();
            float thrust = thrustList.Sum(thruster => thruster.MaxEffectiveThrust);
            var accel = thrust / masses.TotalMass;
            var supMassEarth = thrust / G;
            var supMassMoon = thrust / moonG;
            var supMassAlien = thrust / alienG;

            Echo($"Total mass of grid: {masses.TotalMass:N2} kg");
            Echo($"Total power of {dir} thrusters: {thrust / 1000:N2} kN");
            Echo($"Raw acceleration: {accel:N2} m/s/s");
            Echo($"Acceleration against Earth gravity: {accel - G:N2}");
            Echo($"Acceleration against Moon gravity: {accel - moonG:N2}");
            Echo($"Acceleration against Alien gravity: {accel - alienG:N2}");
            Echo($"Weight that can be pushed by these thrusters:");
            Echo($"--Earth: {supMassEarth:N2} (cargo limit: {supMassEarth - masses.TotalMass:N2}");
            Echo($"--Moon: {supMassMoon:N2} (cargo limit: {supMassMoon - masses.TotalMass:N2}");
            Echo($"--Alien: {supMassAlien:N2} (cargo limit: {supMassAlien - masses.TotalMass:N2}");
        }
    }
}
