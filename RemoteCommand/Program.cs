using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
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
    partial class Program : MyGridProgram
    {
        const string SEND_MODE = "send";
        const string RCV_MODE = "receive";
        string cur_mode = null;
        IMyBroadcastListener listener;

        public Program()
        {
            cur_mode = Storage;
            if (cur_mode == RCV_MODE) 
            {
                listener = IGC.RegisterBroadcastListener("#rcvcmd#");
                listener.SetMessageCallback();
            }
        }

        public void Save()
        {
            Storage = cur_mode;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (string.IsNullOrEmpty(cur_mode))
            {
                Echo("No mode selected. Rerun this script with SEND to set up sender mode or RECEIVE to set up receiver mode.");
                return;
            }


        }
    }
}
