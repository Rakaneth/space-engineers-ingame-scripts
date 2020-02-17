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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {   
        public int GetNumLines(IMyTextSurface panel) 
        { 
            float fontScale = 1.0f / panel.FontSize;
            float pHeight = PanelSize.LargeLCDPanel.Height / 37f;
            return (int)(pHeight * fontScale);
        }

        public int GetCharsPerLine(IMyTextSurface panel)
        {
            float fontScale = 1.0f / panel.FontSize;
            return (int)(PanelSize.LargeLCDPanel.Width * 0.04f * fontScale) - 2;
        }

        public class PanelSize
        {
            const float _wRes = 18944f / 28.8f; // 512 / (28.8 / 37)
            const float _wResWD = _wRes * 2;
            const float _hRes = _wRes * 0.99375f;
            public readonly float Width, Height;
            public static readonly PanelSize LargeLCDPanel = new PanelSize(_wResWD, _hRes);
            public PanelSize(float width, float height)
            {
                Width = width;
                Height = height;
            }
        }

        public class Paginator
        {
            private List<string> lines = new List<string>();
            private List<string> pages = new List<string>();
            private Program prog;
            private IMyTextPanel display;
            public int TotalPages => pages.Count;
            private int curPage = 0;
            bool displaying;

            public Paginator(Program prog, IMyTextPanel display) 
            { 
                this.prog = prog;
                this.display = display;
            }
            public Paginator(Program prog, IMyTextPanel display, StringBuilder sb) : this(prog, display)
            {
                lines = sb.ToString().Split('\n').ToList();
            }

            public void Clear()
            {
                lines.Clear();
                pages.Clear();
            }
            public void Add(string line) => lines.Add(line);
            public void FromBuilder(StringBuilder sb)
            {
                Clear();
                lines = sb.ToString().Split('\n').ToList();
            }

            public void Paginate(IMyTextPanel display)
            {
                StringBuilder sb = new StringBuilder();
                int linesPerPage = prog.GetNumLines(display);
                for (int i=0; i<lines.Count; i++)
                {
                    sb.AppendLine(lines[i]);
                    if (i > 0 && (i % linesPerPage == linesPerPage - 1))
                    {
                        pages.Add(sb.ToString());
                        sb.Clear();
                    }
                }
                if (sb.Length > 0) pages.Add(sb.ToString());
            }
            
            //This goes in Main blocks
            public void DisplayPages(IMyTextPanel display)
            {
                if (displaying) {
                    display.WriteText(pages[curPage]);
                    displaying = false;
                } else {
                    displaying = true;
                    curPage = (curPage + 1) % pages.Count;
                }
            }
        }
    }
}
