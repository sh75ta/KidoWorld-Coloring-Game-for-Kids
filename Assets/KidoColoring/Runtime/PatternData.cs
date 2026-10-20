using System;
using UnityEngine;
namespace Kido.Coloring
{
    [Serializable] public sealed class PatternEntry { public int id; public string name,path; public bool tint,approved; }
    [Serializable] public sealed class PatternCatalog { public PatternEntry[] patterns; }
    public sealed class PatternData
    {
        readonly Color32[] pixels; readonly int width,height;
        public PatternData(Texture2D texture,bool tint)
        {
            pixels=texture.GetPixels32();width=texture.width;height=texture.height;
        }
        public Color32 Sample(int x,int y,Color32 color)
        {
            return pixels[(y*2%height)*width+x*2%width];
        }
    }
}
