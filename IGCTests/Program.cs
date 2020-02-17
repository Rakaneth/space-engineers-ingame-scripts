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
        IMyTextSurface display;

        public Program()
        {
            display = Me.GetSurface(0);
            display.ContentType = ContentType.TEXT_AND_IMAGE;
            listener = IGC.RegisterBroadcastListener("ping");
            
            listener.SetMessageCallback();
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
            while (listener.HasPendingMessage) 
            {
                var msg = listener.AcceptMessage();
                display.WriteText($"Received ping from {msg.Source}\n", true);
                IGC.SendUnicastMessage(msg.Source, "pong", "pong");
            }
                
        }
    }
}
