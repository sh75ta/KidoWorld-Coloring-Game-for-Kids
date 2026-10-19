using System;
using UnityEngine;
namespace Kido.Coloring
{
    [Serializable] public sealed class PatternEntry { public int id; public string name,path; public bool tint,approved; }
    [Serializable] public sealed class PatternCatalog { public PatternEntry[] patterns; }
    public sealed class PatternData
    {
        readonly Color32[] pixels; readonly int width,height; readonly bool tint;
        readonly float minLuma,maxLuma;
        public PatternData(Texture2D texture,bool tint)
        {
            pixels=texture.GetPixels32();width=texture.width;height=texture.height;this.tint=tint;
            minLuma=1;maxLuma=0;
            foreach(var c in pixels){float v=Luma(c);minLuma=Mathf.Min(minLuma,v);maxLuma=Mathf.Max(maxLuma,v);}
        }
        static float Luma(Color32 c)=>(c.r*.2126f+c.g*.7152f+c.b*.0722f)/255f;
        public Color32 Sample(int x,int y,Color32 color)
        {
            Color32 sample=pixels[(y*2%height)*width+x*2%width];
            if(!tint)return sample;
            float light=Mathf.InverseLerp(minLuma,maxLuma,Luma(sample));
            Color c=Color.Lerp(color,Color.white,light*.78f);c.a=1;return c;
        }
    }
}
