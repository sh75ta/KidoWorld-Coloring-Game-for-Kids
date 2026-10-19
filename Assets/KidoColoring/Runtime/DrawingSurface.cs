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
        public Color32 SelectedColor=new Color32(255,48,59,255);
        public float Radius=28;
        public bool AnimateGlitter=true;
        public PatternData Pattern;
        public event Action Changed;
        Texture2D paintTexture,effectTexture,outline;
        Material material;
        int pointer=int.MinValue,renderedRevision=-1;
        ushort region;Vector2 last;float phase,nextUpload;
        static readonly int SparkleTime=Shader.PropertyToID("_SparkleTime");
        public void Initialize(PaintDocument document,Texture2D source)
        {
            Document=document;outline=source;
            var shader=Resources.Load<Shader>("Kido/Shaders/FairyDust");
            if(!shader)throw new InvalidOperationException("Fairy Dust shader is missing.");
            paintTexture=new Texture2D(document.Map.Width,document.Map.Height,TextureFormat.RGBA32,false);
            effectTexture=new Texture2D(document.Map.Width,document.Map.Height,TextureFormat.Alpha8,false,true);
            paintTexture.filterMode=FilterMode.Bilinear;effectTexture.filterMode=FilterMode.Point;
            paintTexture.wrapMode=effectTexture.wrapMode=TextureWrapMode.Clamp;
            material=new Material(shader);material.SetTexture("_LineTex",source);material.SetTexture("_EffectTex",effectTexture);
            material.SetFloat("_CanvasSize",document.Map.Width);
            var img=GetComponent<RawImage>();img.texture=paintTexture;img.material=material;img.raycastTarget=true;Render();
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
            if(Document==null||pointer!=int.MinValue||e.button!=PointerEventData.InputButton.Left||!Locate(e,out var p))return;
            region=Document.RegionAt((int)p.x,(int)p.y);
            pointer=e.pointerId;last=p;phase=Mathf.Repeat(Time.unscaledTime*.11f,1);Document.BeginAction();
            if(Tool==PaintTool.Fill)Document.Fill(region,SelectedColor);
            else Document.Stroke(p,p,Radius,region,SelectedColor,Tool,phase,Pattern);
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.pointerId!=pointer||Tool==PaintTool.Fill)return;
            if(!Locate(e,out var p)){last=new Vector2(float.NaN,float.NaN);return;}
            if(float.IsNaN(last.x))last=p;
            Document.Stroke(last,p,Radius,region,SelectedColor,Tool,phase,Pattern);last=p;
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)FinishStroke();}
        public void FinishStroke(){pointer=int.MinValue;if(Document!=null&&Document.EndAction())Changed?.Invoke();}
        void LateUpdate()
        {
            if(Document==null)return;
            if(material)material.SetFloat(SparkleTime,AnimateGlitter?Time.unscaledTime:0);
            if(renderedRevision!=Document.Revision&&Time.unscaledTime>=nextUpload){Render();nextUpload=Time.unscaledTime+1f/30f;}
        }
        // No CPU outline compositing or texture readback while drawing. Uploads are bounded to 30 Hz.
        public void Render()
        {
            if(Document==null||renderedRevision==Document.Revision)return;
            paintTexture.SetPixels32(Document.Paint);paintTexture.Apply(false,false);
            effectTexture.LoadRawTextureData(Document.Effects);effectTexture.Apply(false,false);
            renderedRevision=Document.Revision;
        }
        public Texture2D Thumbnail(int size=384)
        {
            Render();var rt=RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Texture2D thumb=null;
            try
            {
                Graphics.Blit(paintTexture,rt,material);RenderTexture.active=rt;
                thumb=new Texture2D(size,size,TextureFormat.RGB24,false);thumb.ReadPixels(new Rect(0,0,size,size),0,0);thumb.Apply(false,false);return thumb;
            }
            catch{if(thumb)Destroy(thumb);throw;}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        void OnDisable(){FinishStroke();}
        void OnDestroy()
        {
            if(paintTexture)Destroy(paintTexture);if(effectTexture)Destroy(effectTexture);if(material)Destroy(material);
            if(outline)Resources.UnloadAsset(outline);
        }
    }
}
