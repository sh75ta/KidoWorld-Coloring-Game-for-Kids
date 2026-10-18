using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kido.Coloring
{
    [RequireComponent(typeof(RawImage))]
    public sealed class DrawingSurface:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public PaintDocument Document {get;private set;}
        public PaintTool Tool=PaintTool.Fill;
        public Color32 SelectedColor=new Color32(240,100,116,255);
        public float Radius=14;
        public event Action Changed;
        Texture2D display;
        Color32[] outline,composite;
        int pointer=int.MinValue,renderedRevision=-1;
        ushort region;
        Vector2 last;
        float phase;
        public void Initialize(PaintDocument document,Texture2D source)
        {
            Document=document;outline=source.GetPixels32();composite=new Color32[outline.Length];
            if(source.width!=document.Map.Width || source.height!=document.Map.Height)throw new ArgumentException("Artwork/map dimensions mismatch.");
            display=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);display.filterMode=FilterMode.Bilinear;display.wrapMode=TextureWrapMode.Clamp;
            var img=GetComponent<RawImage>();img.texture=display;img.raycastTarget=true;Render();
        }
        bool Locate(PointerEventData e,out Vector2 p)
        {
            var rt=(RectTransform)transform;var rect=rt.rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,e.position,e.pressEventCamera,out var local);
            p=new Vector2((local.x-rect.x)/rect.width*Document.Map.Width,(local.y-rect.y)/rect.height*Document.Map.Height);
            return rect.Contains(local);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(Document==null || pointer!=int.MinValue || e.button!=PointerEventData.InputButton.Left || !Locate(e,out var p))return;
            region=Document.RegionAt((int)p.x,(int)p.y);if(region==0)return;
            pointer=e.pointerId;last=p;phase=Mathf.Repeat(Time.unscaledTime*.11f,1);Document.BeginAction();
            if(Tool==PaintTool.Fill)Document.Fill(region,SelectedColor);
            else Document.Stroke(p,p,Radius,region,SelectedColor,Tool,phase);
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.pointerId!=pointer || Tool==PaintTool.Fill)return;
            if(!Locate(e,out var p)){last=new Vector2(float.NaN,float.NaN);return;}
            if(float.IsNaN(last.x))last=p;
            Document.Stroke(last,p,Radius,region,SelectedColor,Tool,phase);last=p;
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)FinishStroke();}
        public void FinishStroke(){pointer=int.MinValue;if(Document!=null && Document.EndAction())Changed?.Invoke();}
        void LateUpdate(){if(Document!=null && renderedRevision!=Document.Revision)Render();}
        public void Render(){Document.CompositeInto(outline,composite);display.SetPixels32(composite);display.Apply(false);renderedRevision=Document.Revision;}
        public Texture2D Thumbnail(int size=192)
        {
            Render();var thumb=new Texture2D(size,size,TextureFormat.RGB24,false);var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)pixels[y*size+x]=composite[Mathf.Min(Document.Map.Height-1,y*Document.Map.Height/size)*Document.Map.Width+Mathf.Min(Document.Map.Width-1,x*Document.Map.Width/size)];
            thumb.SetPixels32(pixels);thumb.Apply();return thumb;
        }
        void OnDisable(){FinishStroke();}
        void OnDestroy(){if(display)Destroy(display);}
    }
}
