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
        public float Radius=56;
        public bool AnimateGlitter=true;
        public PatternData Pattern;
        public event Action Changed;
        public event Action<Vector2,Color32,bool> TouchCompleted;
        Texture2D paintTexture,effectTexture,outline;
        Material material;
        RenderTexture beforeFill;
        Rect artworkRect=new Rect(1f/6,1f/6,2f/3,2f/3);
        float fillStarted=-10;
        float pageAspect=1;
        int pointer=int.MinValue,renderedRevision=-1;
        ushort region;Vector2 last,contact;float phase,nextUpload;
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
            SetArtworkRect(artworkRect);
            var img=GetComponent<RawImage>();img.texture=paintTexture;img.material=material;img.raycastTarget=true;Render();
        }
        bool Locate(PointerEventData e,out Vector2 p)
        {
            var rt=(RectTransform)transform;var rect=rt.rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,e.position,e.pressEventCamera,out var local);
            p=new Vector2(PageAxis((local.x-rect.x)/rect.width,artworkRect.xMin,artworkRect.xMax)*Document.Map.Width,
                PageAxis((local.y-rect.y)/rect.height,artworkRect.yMin,artworkRect.yMax)*Document.Map.Height);
            return rect.Contains(local);
        }
        public static float PageAxis(float value,float start,float end)
        {
            if(value<start)return value/Mathf.Max(.001f,start)/6;
            if(value>end)return 5f/6+(value-end)/Mathf.Max(.001f,1-end)/6;
            return 1f/6+(value-start)/Mathf.Max(.001f,end-start)*2f/3;
        }
        public void SetArtworkRect(Rect rect)
        {
            artworkRect=rect;
            if(material)material.SetVector("_ArtRect",new Vector4(rect.x,rect.y,rect.width,rect.height));
        }
        public void CompleteFillReveal(){fillStarted=-10;if(material)material.SetFloat("_FillRadius",100);}
        public void OnPointerDown(PointerEventData e)
        {
            if(Document==null||pointer!=int.MinValue||e.button!=PointerEventData.InputButton.Left||!Locate(e,out var p))return;
            CompleteFillReveal();
            region=Document.RegionAt((int)p.x,(int)p.y);
            pointer=e.pointerId;last=p;contact=e.position;phase=Mathf.Repeat(Time.unscaledTime*.11f,1);Document.BeginAction();
            if(Tool==PaintTool.Fill)
            {
                Render();
                if(AnimateGlitter)
                {
                    if(!beforeFill){beforeFill=new RenderTexture(Document.Map.Width,Document.Map.Height,0,RenderTextureFormat.ARGB32);beforeFill.Create();}
                    Graphics.Blit(paintTexture,beforeFill);material.SetTexture("_BeforeFill",beforeFill);
                    var bounds=((RectTransform)transform).rect;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out var local);
                    pageAspect=bounds.width/Mathf.Max(1,bounds.height);material.SetFloat("_PageAspect",pageAspect);
                    material.SetVector("_FillOrigin",new Vector4((local.x-bounds.x)/bounds.width,(local.y-bounds.y)/bounds.height,0,0));
                    fillStarted=Time.unscaledTime;material.SetFloat("_FillRadius",0);
                }
                Document.Fill(region,SelectedColor);
            }
            else Document.Stroke(p,p,Radius,region,SelectedColor,Tool,phase,Pattern);
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.pointerId!=pointer||Tool==PaintTool.Fill)return;
            contact=e.position;if(!Locate(e,out var p)){last=new Vector2(float.NaN,float.NaN);return;}
            if(float.IsNaN(last.x))last=p;
            Document.Stroke(last,p,Radius,region,SelectedColor,Tool,phase,Pattern);last=p;
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer){contact=e.position;FinishStroke();}}
        public void FinishStroke()
        {
            if(pointer==int.MinValue)return;
            pointer=int.MinValue;
            if(Document!=null)
            {if(Document.EndAction())Changed?.Invoke();TouchCompleted?.Invoke(contact,SelectedColor,Tool==PaintTool.Fill);}
        }
        void LateUpdate()
        {
            if(Document==null)return;
            if(material)material.SetFloat(SparkleTime,AnimateGlitter?Time.unscaledTime:0);
            float age=Time.unscaledTime-fillStarted;
            if(material)material.SetFloat("_FillRadius",AnimateGlitter&&age<.38f?Mathf.Clamp01(age/.38f)*Mathf.Sqrt(pageAspect*pageAspect+1):100);
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
            Render();material.SetFloat("_FillRadius",100);
            material.SetVector("_ArtRect",new Vector4(1f/6,1f/6,2f/3,2f/3));
            var rt=RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Texture2D thumb=null;
            try
            {
                Graphics.Blit(paintTexture,rt,material);RenderTexture.active=rt;
                thumb=new Texture2D(size,size,TextureFormat.RGB24,false);thumb.ReadPixels(new Rect(0,0,size,size),0,0);thumb.Apply(false,false);return thumb;
            }
            catch{if(thumb)Destroy(thumb);throw;}
            finally{SetArtworkRect(artworkRect);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
        void OnDisable(){FinishStroke();}
        void OnDestroy()
        {
            if(paintTexture)Destroy(paintTexture);if(effectTexture)Destroy(effectTexture);if(material)Destroy(material);
            if(beforeFill){beforeFill.Release();Destroy(beforeFill);}
            if(outline)Resources.UnloadAsset(outline);
        }
    }
}
