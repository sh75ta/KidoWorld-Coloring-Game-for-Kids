using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kido.Coloring
{
    public static class KidoUI
    {
        public static readonly Color Ink=Hex("293450"),Muted=Hex("788198"),Cream=Hex("FFF9EF"),Purple=Hex("7661D5");
        public static readonly Color[] Accents={Hex("D76776"),Hex("40A68C"),Hex("E7A63C")};
        public static readonly Color[] Backgrounds={Hex("FFF3EF"),Hex("EDF8F0"),Hex("EFF5FF")};
        public static readonly string[] HexColors={"F06474","FFB45F","FFD85E","93C96B","41BAA3","65C8E5","6F98E7","9A7DE3","D993D3","B38262","485575","FFFFFF"};
        static Sprite rounded;static Font font;
        public static Color Hex(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
        public static RectTransform Rect(string name,Transform parent)
        {
            var obj=new GameObject(name,typeof(RectTransform));var rect=obj.GetComponent<RectTransform>();rect.SetParent(parent,false);return rect;
        }
        public static void Place(RectTransform rect,float left,float bottom,float right,float top,float inset=0)
        {rect.anchorMin=new Vector2(left,bottom);rect.anchorMax=new Vector2(right,top);rect.offsetMin=new Vector2(inset,inset);rect.offsetMax=new Vector2(-inset,-inset);}
        public static RectTransform Panel(string name,Transform parent,Color color,bool round=true)
        {
            var rect=Rect(name,parent);var img=rect.gameObject.AddComponent<Image>();img.color=color;img.raycastTarget=false;
            if(round){img.sprite=Rounded();img.type=Image.Type.Sliced;}
            return rect;
        }
        public static Text Label(string text,Transform parent,int size,Color color,TextAnchor anchor=TextAnchor.MiddleLeft)
        {
            var rect=Rect(text,parent);var t=rect.gameObject.AddComponent<Text>();
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font=font;t.text=text;t.fontSize=size;t.color=color;t.alignment=anchor;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            return t;
        }
        public static Button Button(string label,Transform parent,Color background,Action click,int size=26,Color? textColor=null)
        {
            var rect=Panel(label,parent,background);var image=rect.GetComponent<Image>();image.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.9f,.9f,.94f);colors.selectedColor=Color.white;button.colors=colors;
            button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(()=>click?.Invoke());
            if(!string.IsNullOrEmpty(label)){var t=Label(label,rect,size,textColor??Ink,TextAnchor.MiddleCenter);Place(t.rectTransform,0,0,1,1,8);}
            rect.gameObject.AddComponent<PressBounce>();return button;
        }
        public static RawImage Picture(Transform parent,Texture texture)
        {
            var rect=Rect("Picture",parent);var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return image;
        }
        public static void Square(RectTransform rect)
        {var fitter=rect.gameObject.AddComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fitter.aspectRatio=1;}
        public static Sprite Rounded()
        {
            if(rounded)return rounded;
            const int n=64;const float radius=18;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);var pixels=new Color32[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++)
            {
                float dx=Mathf.Max(radius-x,0,x-(n-1-radius)),dy=Mathf.Max(radius-y,0,y-(n-1-radius));
                float alpha=Mathf.Clamp01(radius+.5f-Mathf.Sqrt(dx*dx+dy*dy));pixels[y*n+x]=new Color(1,1,1,alpha);
            }
            tex.SetPixels32(pixels);tex.Apply();tex.wrapMode=TextureWrapMode.Clamp;tex.filterMode=FilterMode.Bilinear;
            rounded=Sprite.Create(tex,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(20,20,20,20));return rounded;
        }
    }
    public sealed class PressBounce:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        Vector3 target=Vector3.one;
        public void OnPointerDown(PointerEventData e){target=Vector3.one*.96f;}
        public void OnPointerUp(PointerEventData e){target=Vector3.one;}
        public void OnPointerExit(PointerEventData e){target=Vector3.one;}
        void Update(){transform.localScale=Vector3.Lerp(transform.localScale,target,Time.unscaledDeltaTime*22);}
    }
    public sealed class SafeArea:MonoBehaviour
    {
        Rect last;int width,height;
        void OnEnable(){Apply();} void Update(){if(last!=Screen.safeArea || width!=Screen.width || height!=Screen.height)Apply();}
        void Apply()
        {
            if(Screen.width<1 || Screen.height<1)return;
            last=Screen.safeArea;width=Screen.width;height=Screen.height;
            var rect=(RectTransform)transform;rect.anchorMin=new Vector2(last.x/width,last.y/height);rect.anchorMax=new Vector2(last.xMax/width,last.yMax/height);rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
    public sealed class ToolGlyph:MaskableGraphic
    {
        public PaintTool tool;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch(tool)
            {
                case PaintTool.Fill: Poly(vh,new[]{new Vector2(.26f,.35f),new Vector2(.52f,.18f),new Vector2(.81f,.53f),new Vector2(.55f,.73f)});Line(vh,.33f,.66f,.48f,.86f,.065f);Line(vh,.48f,.86f,.7f,.71f,.065f);break;
                case PaintTool.Brush:Line(vh,.35f,.32f,.72f,.79f,.14f);Disc(vh,.29f,.25f,.14f);break;
                case PaintTool.Eraser:Poly(vh,new[]{new Vector2(.18f,.35f),new Vector2(.52f,.77f),new Vector2(.8f,.55f),new Vector2(.46f,.13f)});break;
                case PaintTool.Glitter: Star(vh,.48f,.53f,.3f);Star(vh,.79f,.8f,.12f);Star(vh,.18f,.19f,.12f);break;
                case PaintTool.Pattern:Disc(vh,.3f,.3f,.13f);Disc(vh,.7f,.3f,.13f);Disc(vh,.3f,.7f,.13f);Disc(vh,.7f,.7f,.13f);break;
                case PaintTool.Rainbow:
                    for(int a=0;a<3;a++){float rad=.4f-a*.11f;for(int s=0;s<16;s++){float t=s*Mathf.PI/16,u=(s+1)*Mathf.PI/16;Line(vh,.5f+Mathf.Cos(t)*rad,.25f+Mathf.Sin(t)*rad,.5f+Mathf.Cos(u)*rad,.25f+Mathf.Sin(u)*rad,.05f);}}break;
            }
        }
        Vector2 Point(Vector2 p){var r=rectTransform.rect;return new Vector2(r.x+p.x*r.width,r.y+p.y*r.height);}
        void Poly(VertexHelper vh,Vector2[] pts){int start=vh.currentVertCount;foreach(var p in pts)vh.AddVert(Point(p),color,Vector2.zero);for(int i=1;i<pts.Length-1;i++)vh.AddTriangle(start,start+i,start+i+1);}
        void Line(VertexHelper vh,float ax,float ay,float bx,float by,float w){var a=new Vector2(ax,ay);var b=new Vector2(bx,by);var delta=b-a;var n=new Vector2(-delta.y,delta.x).normalized*w*.5f;Poly(vh,new[]{a-n,b-n,b+n,a+n});}
        void Disc(VertexHelper vh,float x,float y,float r){var pts=new Vector2[24];for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;pts[i]=new Vector2(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r);}Poly(vh,pts);}
        void Star(VertexHelper vh,float x,float y,float r){for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Poly(vh,new[]{new Vector2(x,y),new Vector2(x+Mathf.Cos(a-.4f)*r*.33f,y+Mathf.Sin(a-.4f)*r*.33f),new Vector2(x+Mathf.Cos(a)*r,y+Mathf.Sin(a)*r),new Vector2(x+Mathf.Cos(a+.4f)*r*.33f,y+Mathf.Sin(a+.4f)*r*.33f)});}}
    }
}
