using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kido.Coloring
{
    public enum PaintTool { Fill, Brush, Eraser, Glitter, Pattern, Rainbow }
    public sealed class PaintDocument
    {
        public readonly RegionMap Map;
        public readonly Color32[] Paint;
        public int PaintedPixels { get; private set; }
        public float Coverage => (float)PaintedPixels / Map.PaintablePixels;
        public bool CanUndo => history.Count > 0;
        public bool ActionOpen => action != null;
        public int Revision { get; private set; }
        struct Change { public int index; public Color32 before; }
        readonly List<List<Change>> history = new List<List<Change>>();
        readonly int[] marks;
        List<Change> action;
        int stamp, historyPixels;
        const int MaxUndoPixels = 2000000;
        public PaintDocument(RegionMap map, Color32[] saved = null)
        {
            Map = map; Paint = new Color32[map.Labels.Length]; marks = new int[Paint.Length];
            if (saved != null)
            {
                if (saved.Length != Paint.Length) throw new ArgumentException("Saved dimensions mismatch.");
                for (int i = 0; i < Paint.Length; i++)
                {
                    if (map.Labels[i] == 0) continue;
                    Paint[i] = saved[i]; if (Paint[i].a > 0) PaintedPixels++;
                }
            }
        }
        public void BeginAction()
        {
            EndAction();
            if (++stamp == int.MaxValue) { Array.Clear(marks, 0, marks.Length); stamp = 1; }
            action = new List<Change>();
        }
        void SetPixel(int i, Color32 c)
        {
            var old = Paint[i];
            if (old.r == c.r && old.g == c.g && old.b == c.b && old.a == c.a) return;
            if (action == null) BeginAction();
            if (marks[i] != stamp) { marks[i] = stamp; action.Add(new Change { index = i, before = old }); }
            if (old.a == 0 && c.a > 0) PaintedPixels++;
            if (old.a > 0 && c.a == 0) PaintedPixels--;
            Paint[i] = c; Revision++;
        }
        public bool EndAction()
        {
            if (action == null) return false;
            bool changed = action.Count > 0;
            if (changed)
            {
                history.Add(action); historyPixels += action.Count;
                while (history.Count > 1 && (history.Count > 24 || historyPixels > MaxUndoPixels))
                { historyPixels -= history[0].Count; history.RemoveAt(0); }
            }
            action = null; return changed;
        }
        public ushort RegionAt(int x, int y) => x < 0 || y < 0 || x >= Map.Width || y >= Map.Height ? (ushort)0 : Map.Labels[y * Map.Width + x];
        public void Fill(ushort region, Color32 color)
        {
            if (region == 0) return;
            color.a = 255;
            for (int i = 0; i < Paint.Length; i++) if (Map.Labels[i] == region) SetPixel(i, color);
        }
        public void Stroke(Vector2 from, Vector2 to, float radius, ushort region, Color32 color, PaintTool tool, float rainbowPhase)
        {
            if (region == 0) return;
            radius = Mathf.Clamp(radius, 2, 80);
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / Mathf.Max(1, radius * .3f)));
            for (int s = 0; s <= steps; s++)
            {
                var point = Vector2.Lerp(from, to, (float)s / steps);
                int x0 = Mathf.Max(0, Mathf.FloorToInt(point.x - radius)), x1 = Mathf.Min(Map.Width - 1, Mathf.CeilToInt(point.x + radius));
                int y0 = Mathf.Max(0, Mathf.FloorToInt(point.y - radius)), y1 = Mathf.Min(Map.Height - 1, Mathf.CeilToInt(point.y + radius));
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    int i = y * Map.Width + x;
                    if (Map.Labels[i] != region || (x-point.x)*(x-point.x)+(y-point.y)*(y-point.y) > radius*radius) continue;
                    Color32 c = color; c.a = 255;
                    if (tool == PaintTool.Eraser) c = new Color32(0,0,0,0);
                    else if (tool == PaintTool.Pattern && ((x+y)/12)%2 == 0) c = Color.Lerp(color, Color.white, .65f);
                    else if (tool == PaintTool.Glitter)
                    {
                        uint hash = unchecked((uint)(x * 73856093) ^ (uint)(y * 19349663));
                        if (hash % 17 < 3) c = Color.Lerp(color, Color.white, .78f);
                    }
                    else if (tool == PaintTool.Rainbow) c = Color.HSVToRGB(Mathf.Repeat(rainbowPhase + (x+y)*.002f,1),.65f,1);
                    SetPixel(i,c);
                }
            }
        }
        public void Clear()
        {
            BeginAction();
            for (int i = 0; i < Paint.Length; i++) if (Paint[i].a != 0) SetPixel(i,new Color32(0,0,0,0));
            EndAction();
        }
        public bool Undo()
        {
            EndAction(); if (history.Count == 0) return false;
            var changes = history[history.Count-1]; history.RemoveAt(history.Count-1); historyPixels -= changes.Count;
            for (int k = changes.Count-1; k >= 0; k--)
            {
                var change = changes[k]; var current = Paint[change.index];
                if (current.a > 0 && change.before.a == 0) PaintedPixels--;
                if (current.a == 0 && change.before.a > 0) PaintedPixels++;
                Paint[change.index] = change.before;
            }
            Revision++; return true;
        }
        public Color32[] Composite(Color32[] outlines)
        {
            if (outlines.Length != Paint.Length) throw new ArgumentException("Outline dimensions mismatch.");
            var output = new Color32[Paint.Length]; CompositeInto(outlines,output); return output;
        }
        public void CompositeInto(Color32[] outlines, Color32[] output)
        {
            for (int i = 0; i < Paint.Length; i++)
            {
                var line=outlines[i]; var c=Paint[i];
                output[i] = c.a == 0 ? line : new Color32((byte)(line.r*c.r/255),(byte)(line.g*c.g/255),(byte)(line.b*c.b/255),255);
            }
        }
    }
}
