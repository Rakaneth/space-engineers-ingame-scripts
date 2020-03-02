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
        public class DoorPair
        {
            public IMyDoor Inner { get; set; }
            public IMyDoor Outer { get; set; }

        }

        Dictionary<string, DoorPair> doorPairs = new Dictionary<string, DoorPair>();
        MyCommandLine cmd = new MyCommandLine();
        List<IMyDoor> doors = new List<IMyDoor>();

        public Program()
        {
            Echo("Setting up door pairs");
            GridTerminalSystem.GetBlocksOfType(doors, door => door.IsSameConstructAs(Me) && !string.IsNullOrEmpty(door.CustomData));
            foreach (var door in doors)
            {
                door.Enabled = true;
                door.CloseDoor();
                
                if (!cmd.TryParse(door.CustomData)) continue;
                bool inner = cmd.Switch("inner");
                bool outer = cmd.Switch("outer");
                string id = cmd.Argument(0);
                //DoorPair curPair;
                if (cmd.Items.Count == 2 && (inner || outer))
                {
                    if (!doorPairs.ContainsKey(id))
                        doorPairs.Add(id, new DoorPair());

                    if (inner)
                    {
                        if (doorPairs[id].Inner == null)
                            doorPairs[id].Inner = door;
                        else
                            Echo($"{door.CustomName} - Inner door for door pair {id} taken");
                    }
                        
                    else if (outer)
                    {
                        if (doorPairs[id].Outer == null)
                            doorPairs[id].Outer = door;
                        else
                            Echo($"{door.CustomName} - Outer door for door pair {id} taken");
                    }
                }
            }
            Echo($"Setup complete, {doorPairs.Count} door pairs found");
            foreach(var doorPair in doorPairs)
            {
                if (doorPair.Value.Inner == null)
                    Echo($"Door pair ID {doorPair.Key} has no inner door marked");
                else if (doorPair.Value.Outer == null)
                    Echo($"Door pair ID {doorPair.Key} has no outer door marked");      
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }


        public void Main(string argument, UpdateType updateSource)
        {
            foreach (var doorPair in doorPairs)
                CheckDoorPair(doorPair.Value);
        }

        public void CheckDoorPair(DoorPair pair)
        {
            if (pair.Outer == null || pair.Inner == null)
                return;

            if (pair.Outer.Status == DoorStatus.Open)
            {
                pair.Inner.CloseDoor();
                pair.Inner.Enabled = false;
            }
            else if (pair.Inner.Status == DoorStatus.Open)
            {
                pair.Outer.CloseDoor();
                pair.Outer.Enabled = false;
            }
            else
            {
                pair.Outer.Enabled = true;
                pair.Inner.Enabled = true;
            }
        }
    }
}
