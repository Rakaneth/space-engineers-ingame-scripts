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
        double lastOutput = 0;
        double curOutput = 0;
        IMyMotorStator eleRotor;
        IMyMotorStator aziRotor;
        List<IMySolarPanel> panels = new List<IMySolarPanel>();
        double tolerance = 0.01;
        bool eleRotate;
        bool aziRotate;
        const double POWER_TARGET = 0.14f;
        const float startAzi = 0f;
        const float startEle = MathHelper.PiOver2;
        bool setup = true;

        public enum Tolerance
        {
            UNDER,
            OVER,
            WITHIN
        }

        public Program()
        {
            eleRotor = GridTerminalSystem.GetBlockWithName("Solar Array Rotor - Elevation") as IMyMotorStator;
            aziRotor = GridTerminalSystem.GetBlockWithName("Solar Array Rotor - Azimuth") as IMyMotorStator;
            aziRotor.TargetVelocityRPM = 0.1f;
            eleRotor.TargetVelocityRPM = 0.1f;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            GridTerminalSystem.GetBlocksOfType(panels, panel => panel.IsSameConstructAs(Me));
            eleRotor.Enabled = true;
            aziRotor.Enabled = true;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            /*
            if (setup)
            {
                eleRotor.RotorLock = false;
                aziRotor.RotorLock = false;
                Echo("Setting up");
                Echo($"Azimuth Angle: {aziRotor.Angle} | {startAzi}");
                Echo($"Elevation Angle: {eleRotor.Angle} | {startEle}");
                if (!eleRotor.RotorLock) RotorAdjust(eleRotor, startEle);
                if (!aziRotor.RotorLock) RotorAdjust(aziRotor, startAzi);
                if (eleRotor.RotorLock && aziRotor.RotorLock)
                {
                    setup = false;
                    Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
                }  
                else
                    return;
            }
            */
            var panel = panels[0];
            bool eleDone;
            if (getTolerance(panel.MaxOutput, POWER_TARGET, tolerance) != Tolerance.WITHIN)
            {
                
                Echo($"Not at max power, {panel.MaxOutput} / {POWER_TARGET}, adjusting");
                if (!(eleRotate || aziRotate))
                {
                    eleRotate = true;
                    aziRotate = false;
                }
                else if (eleRotate)
                {
                    Echo("Adjusting elevation");
                    eleDone = RotorRotation(eleRotor);
                    eleRotate = !eleDone;
                    aziRotate = eleDone;

                }
                else if (aziRotate)
                {
                    Echo("Adjusting azimuth");
                    aziRotate = !RotorRotation(aziRotor);
                }
            }
            else
            {
                eleRotate = false;
                aziRotate = false;
                Echo($"At optimal current position");
            }
        }


        private bool RotorRotation(IMyMotorStator rotor)
        {
            var tol = getTolerance(curOutput, lastOutput, 0.01);
            if (tol != Tolerance.WITHIN)
            {
                if (!rotor.Enabled)
                    rotor.Enabled = true;
                if (rotor.RotorLock)
                    rotor.RotorLock = false;
                lastOutput = curOutput;
                return false;
            }
            else
            {
                rotor.RotorLock = true;
                return true;
            }
        }

        private Tolerance getTolerance(float value, float baseline, float tolerance) 
        {
            if (value * (1f - tolerance) < baseline)
                return Tolerance.UNDER;
            else if (value * (1f + tolerance) > baseline)
                return Tolerance.OVER;
            else
                return Tolerance.WITHIN;
        }

        private Tolerance getTolerance(double value, double baseline, double tolerance)
        {
            if (value * (1.0 - tolerance) < baseline)
                return Tolerance.UNDER;
            else if (value * (1.0 + tolerance) > baseline)
                return Tolerance.OVER;
            else
                return Tolerance.WITHIN;
        }

        private void RotorAdjust(IMyMotorStator rotor, float desiredAngle)
        {
            var tol = getTolerance(rotor.Angle, desiredAngle, 0.01f);
            switch(tol)
            {
                case Tolerance.UNDER:
                    rotor.TargetVelocityRPM = 0.1f;
                    break;
                case Tolerance.OVER:
                    rotor.TargetVelocityRPM = -0.1f;
                    break;
                case Tolerance.WITHIN:
                    rotor.RotorLock = true;
                    break;
            }
        }
    }
}
