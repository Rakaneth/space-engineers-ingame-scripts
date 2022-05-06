using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        //From Whiplash's tutorial
        public class PID
        {
            readonly double _kP = 0;
            readonly double _kI = 0;
            readonly double _kD = 0;

            double _timeStep = 0;
            double _invTimeStep = 0;
            double _errorSum = 0;
            double _lastError = 0;
            bool _firstRun = true;

            public double Value { get; private set; }

            public PID(double kP, double kI, double kD, double timeStep)
            {
                _kP = kP;
                _kI = kI;
                _kD = kD;
                _timeStep = timeStep;
                if (_timeStep != 0) _invTimeStep = 1 / _timeStep;
            }

            protected virtual double getIntegral(double curErr, double errSum, double timeStep)
            {
                return errSum + curErr * timeStep;
            }

            public double Control(double err)
            {
                var errDrv = (err - _lastError) * (_invTimeStep);

                if (_firstRun)
                {
                    errDrv = 0;
                    _firstRun = false;
                }

                _errorSum = getIntegral(err, _errorSum, _timeStep);
                _lastError = err;
                this.Value = _kP * err + _kI * _errorSum + _kD * errDrv;
                return this.Value;
            }

            public double Control(double err, double timeStep)
            {
                if (timeStep != _timeStep)
                {
                    _timeStep = timeStep;
                    if (_timeStep != 0) _invTimeStep = 1 / _timeStep;
                }

                return Control(err);
            }

            public void Reset()
            {
                _errorSum = 0;
                _lastError = 0;
                _firstRun = true;
            }
        }
    }
}
