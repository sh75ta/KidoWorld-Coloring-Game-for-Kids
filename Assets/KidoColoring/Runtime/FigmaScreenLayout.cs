using System;
using UnityEngine;

namespace Kido.Coloring
{
    // Uniform design-space scaling: icons remain round on phones and tablets.
    // Source frames: 6:99 (1600x900), 6:62 (1422x1067), splash 6:36 / 6:46.
    public sealed class FigmaScreenLayout:MonoBehaviour
    {
        public Action<float,float,bool> Layout;
        public bool Splash;
        Vector2 lastSize;
        public void Refresh()
        {
            var rect=(RectTransform)transform;var parent=(RectTransform)rect.parent;
            var size=parent.rect.size;if(size.x<=0||size.y<=0)return;
            lastSize=size;bool tablet=size.x/size.y<1.55f;
            float h=tablet?(Splash?1080:1067):900,scale=size.y/h,w=size.x/scale;
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.sizeDelta=new Vector2(w,h);rect.anchoredPosition=Vector2.zero;
            rect.localScale=Vector3.one*scale;Layout?.Invoke(w,h,tablet);
        }
        void LateUpdate(){if(((RectTransform)transform.parent).rect.size!=lastSize)Refresh();}
        public static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }
    }
}
