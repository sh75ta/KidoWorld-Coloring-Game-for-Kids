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
        public readonly byte[] Effects;
        public int PaintedPixels { get; private set; }
        public float Coverage => (float)PaintedPixels / Map.PaintablePixels;
        public bool CanUndo => history.Count > 0;
        public bool ActionOpen => action != null;
        public int Revision { get; private set; }
        public int UndoBytes => historyBytes;
        public const int UndoBudgetBytes = 16 * 1024 * 1024;
        const int TileSize = 64;
        public const float MaxRadius = 224;
        sealed class Tile { public int x,y,w,h; public Color32[] paint; public byte[] effects; public int Bytes => paint.Length * 5; }
        sealed class Edit { public readonly List<Tile> tiles = new List<Tile>(); public int bytes; }
        readonly List<Edit> history = new List<Edit>();
        readonly bool[] captured;
        readonly int tileColumns;
        Edit action;
        int historyBytes;
        public PaintDocument(RegionMap map, Color32[] saved = null, byte[] effects = null)
        {
            Map = map; Paint = new Color32[map.Labels.Length]; Effects = new byte[Paint.Length];
            tileColumns = (map.Width + TileSize - 1) / TileSize;
            captured = new bool[tileColumns * ((map.Height + TileSize - 1) / TileSize)];
            if (saved != null && saved.Length != Paint.Length) throw new ArgumentException("Saved dimensions mismatch.");
            if (effects != null && effects.Length != Paint.Length) throw new ArgumentException("Saved effects mismatch.");
            for (int i = 0; saved != null && i < Paint.Length; i++)
            {
                if (saved[i].a == 0) continue;
                Paint[i] = saved[i]; Effects[i] = effects == null ? (byte)0 : effects[i]; PaintedPixels++;
            }
        }
        public void BeginAction() { EndAction(); Array.Clear(captured,0,captured.Length); action = new Edit(); }
        void Capture(int index)
        {
            int tx=index%Map.Width/TileSize,ty=index/Map.Width/TileSize,key=ty*tileColumns+tx;
            if(captured[key])return;captured[key]=true;
            var tile=new Tile{x=tx*TileSize,y=ty*TileSize};
            tile.w=Math.Min(TileSize,Map.Width-tile.x);tile.h=Math.Min(TileSize,Map.Height-tile.y);
            tile.paint=new Color32[tile.w*tile.h];tile.effects=new byte[tile.paint.Length];
            for(int y=0;y<tile.h;y++)
            {
                int start=(tile.y+y)*Map.Width+tile.x;
                Array.Copy(Paint,start,tile.paint,y*tile.w,tile.w);Array.Copy(Effects,start,tile.effects,y*tile.w,tile.w);
            }
            action.tiles.Add(tile);action.bytes+=tile.Bytes;
            while(history.Count>0 && historyBytes+action.bytes>UndoBudgetBytes)
            {historyBytes-=history[0].bytes;history.RemoveAt(0);}
        }
        void SetPixel(int i,Color32 c,byte effect=0)
        {
            var old=Paint[i];if(c.a==0)effect=0;
            if(old.r==c.r&&old.g==c.g&&old.b==c.b&&old.a==c.a&&Effects[i]==effect)return;
            if(action==null)BeginAction();Capture(i);
            if(old.a==0&&c.a>0)PaintedPixels++;if(old.a>0&&c.a==0)PaintedPixels--;
            Paint[i]=c;Effects[i]=effect;Revision++;
        }
        public bool EndAction()
        {
            if(action==null)return false;bool changed=action.tiles.Count>0;
            if(changed)
            {
                history.Add(action);historyBytes+=action.bytes;
                while(history.Count>1&&(history.Count>24||historyBytes>UndoBudgetBytes))
                {historyBytes-=history[0].bytes;history.RemoveAt(0);}
            }
            action=null;return changed;
        }
        public ushort RegionAt(int x,int y)=>x<0||y<0||x>=Map.Width||y>=Map.Height?(ushort)0:Map.Labels[y*Map.Width+x];
        public void Fill(ushort region,Color32 color)
        {
            if(region>Map.Count)return;color.a=255;
            if(region==0)
            {
                for(int i=0;i<Map.Labels.Length;i++)if(Map.Labels[i]==0)SetPixel(i,color);
                return;
            }
            var b=Map.Bounds[region];
            for(int y=b.yMin;y<b.yMax;y++)for(int x=b.xMin;x<b.xMax;x++)
            {int i=y*Map.Width+x;if(Map.Labels[i]==region)SetPixel(i,color);}
        }
        public void Stroke(Vector2 from,Vector2 to,float radius,ushort region,Color32 color,PaintTool tool,float rainbowPhase,PatternData pattern=null)
        {
            radius=Mathf.Clamp(radius,2,MaxRadius);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/Mathf.Max(1,radius*.3f)));
            for(int s=0;s<=steps;s++)
            {
                var p=Vector2.Lerp(from,to,(float)s/steps);
                int x0=Mathf.Max(0,Mathf.FloorToInt(p.x-radius)),x1=Mathf.Min(Map.Width-1,Mathf.CeilToInt(p.x+radius));
                int y0=Mathf.Max(0,Mathf.FloorToInt(p.y-radius)),y1=Mathf.Min(Map.Height-1,Mathf.CeilToInt(p.y+radius));
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    int i=y*Map.Width+x;if(Map.Labels[i]!=region||(x-p.x)*(x-p.x)+(y-p.y)*(y-p.y)>radius*radius)continue;
                    Color32 c=color;c.a=255;byte effect=0;
                    if(tool==PaintTool.Eraser)c=new Color32(0,0,0,0);
                    else if(tool==PaintTool.Pattern&&pattern!=null)c=pattern.Sample(x,y,color);
                    else if(tool==PaintTool.Glitter)effect=255;
                    else if(tool==PaintTool.Rainbow)c=Color.HSVToRGB(Mathf.Repeat(rainbowPhase+(x+y)*.002f,1),.82f,1);
                    SetPixel(i,c,effect);
                }
            }
        }
        public void Clear(){BeginAction();for(int i=0;i<Paint.Length;i++)if(Paint[i].a!=0)SetPixel(i,new Color32(0,0,0,0));EndAction();}
        public bool Undo()
        {
            EndAction();if(history.Count==0)return false;
            var edit=history[history.Count-1];history.RemoveAt(history.Count-1);historyBytes-=edit.bytes;
            foreach(var tile in edit.tiles)for(int y=0;y<tile.h;y++)for(int x=0;x<tile.w;x++)
            {
                int i=(tile.y+y)*Map.Width+tile.x+x,k=y*tile.w+x;
                if(Paint[i].a>0&&tile.paint[k].a==0)PaintedPixels--;if(Paint[i].a==0&&tile.paint[k].a>0)PaintedPixels++;
                Paint[i]=tile.paint[k];Effects[i]=tile.effects[k];
            }
            Revision++;return true;
        }
        public static Color32 CompositePixel(Color32 paint,Color32 line)
        {
            float ink=line.a/255f;Color baseColor=paint.a==0?Color.white:(Color)paint;
            var result=Color.Lerp(baseColor,(Color)line,ink);result.a=1;return result;
        }
    }
}
