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
        // Painting footprints are radii in page pixels. The pattern tool is deliberately twice the
        // others: its footprint (not the toolbar control) is what was enlarged.
        public const float BrushRadius=56,PatternRadius=BrushRadius*2;
        public bool AnimateGlitter=true;
        public PatternData Pattern;
        public event Action Changed;
        public event Action<Vector2,Color32,bool> TouchCompleted;
        Texture2D paintTexture,effectTexture,outline;
        Material material;
        RenderTexture beforeFill;
        Vector2Int artSize=new Vector2Int(1024,1024),artOffset;
        Vector2 displaySize=new Vector2(1,1);Rect artOnDisplay=new Rect(0,0,1,1);
        float fillStarted=-10;
        float pageAspect=1;
        int pointer=int.MinValue,renderedRevision=-1;
        ushort region;Vector2 last,contact;float phase,nextUpload;
        static readonly int SparkleTime=Shader.PropertyToID("_SparkleTime");
        static readonly int PageMap=Shader.PropertyToID("_PageMap"),ArtMap=Shader.PropertyToID("_ArtMap");
        public static float RadiusFor(PaintTool tool)=>tool==PaintTool.Pattern?PatternRadius:BrushRadius;
        public void Initialize(PaintDocument document,Texture2D source,int artWidth=1024,int artHeight=1024)
        {
            Document=document;outline=source;
            artSize=new Vector2Int(artWidth,artHeight);
            artOffset=new Vector2Int((document.Map.Width-artWidth)/2,(document.Map.Height-artHeight)/2);
            var shader=Resources.Load<Shader>("Kido/Shaders/FairyDust");
            if(!shader)throw new InvalidOperationException("Fairy Dust shader is missing.");
            paintTexture=new Texture2D(document.Map.Width,document.Map.Height,TextureFormat.RGBA32,false);
            effectTexture=new Texture2D(document.Map.Width,document.Map.Height,TextureFormat.Alpha8,false,true);
            paintTexture.filterMode=FilterMode.Bilinear;effectTexture.filterMode=FilterMode.Point;
            paintTexture.wrapMode=effectTexture.wrapMode=TextureWrapMode.Clamp;
            material=new Material(shader);material.SetTexture("_LineTex",source);material.SetTexture("_EffectTex",effectTexture);
            material.SetVector("_PageSize",new Vector4(document.Map.Width,document.Map.Height,0,0));
            SetArtworkLayout(displaySize,artOnDisplay);
            var img=GetComponent<RawImage>();img.texture=paintTexture;img.material=material;img.raycastTarget=true;Render();
        }
        // One uniform scale maps page pixels to screen units on both axes, so a brush stays round
        // everywhere. Touch and shader use the same affine maps. Rect is in display units, origin bottom-left.
        public void SetArtworkLayout(Vector2 display,Rect artRect)
        {
            displaySize=new Vector2(Mathf.Max(1,display.x),Mathf.Max(1,display.y));artOnDisplay=artRect;
            if(Document==null)return;
            float scale=Mathf.Max(.001f,artRect.width/artSize.x);
            float pw=Document.Map.Width,ph=Document.Map.Height;
            // pagePx = artOffset + (displayUV*display - art.min)/scale
            material.SetVector(PageMap,new Vector4(displaySize.x/scale/pw,displaySize.y/scale/ph,(artOffset.x-artRect.x/scale)/pw,(artOffset.y-artRect.y/scale)/ph));
            material.SetVector(ArtMap,new Vector4(displaySize.x/scale/artSize.x,displaySize.y/scale/artSize.y,-artRect.x/scale/artSize.x,-artRect.y/scale/artSize.y));
        }
        bool Locate(PointerEventData e,out Vector2 p)
        {
            var rt=(RectTransform)transform;var rect=rt.rect;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt,e.position,e.pressEventCamera,out var local);
            p=ToPage(new Vector2((local.x-rect.x)/Mathf.Max(1,rect.width)*displaySize.x,(local.y-rect.y)/Mathf.Max(1,rect.height)*displaySize.y));
            return rect.Contains(local);
        }
        // Display units (bottom-left origin) to page pixels, clamped to the page for very wide screens.
        public Vector2 ToPage(Vector2 display)
        {
            float scale=Mathf.Max(.001f,artOnDisplay.width/artSize.x);
            return new Vector2(Mathf.Clamp(artOffset.x+(display.x-artOnDisplay.x)/scale,0,Document.Map.Width-1),
                Mathf.Clamp(artOffset.y+(display.y-artOnDisplay.y)/scale,0,Document.Map.Height-1));
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
            else Document.Stroke(p,p,RadiusFor(Tool),region,SelectedColor,Tool,phase,Pattern);
        }
        public void OnDrag(PointerEventData e)
        {
            if(e.pointerId!=pointer||Tool==PaintTool.Fill)return;
            contact=e.position;bool inside=Locate(e,out var p);
            if(!inside){last=new Vector2(float.NaN,float.NaN);return;}
            if(float.IsNaN(last.x))last=p;
            Document.Stroke(last,p,RadiusFor(Tool),region,SelectedColor,Tool,phase,Pattern);last=p;
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer){contact=e.position;FinishStroke(true);}}
        public void FinishStroke(bool released=false)
        {
            if(pointer==int.MinValue)return;
            pointer=int.MinValue;
            if(Document!=null)
            {if(Document.EndAction())Changed?.Invoke();if(released)TouchCompleted?.Invoke(contact,SelectedColor,Tool==PaintTool.Fill);}
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
        // Art region only, so blank and painted thumbnails frame the artwork identically.
        public Texture2D Thumbnail(int size=384)
        {
            Render();
            return PaintThumbnail.Render(paintTexture,effectTexture,outline,new Vector2Int(Document.Map.Width,Document.Map.Height),artSize,size);
        }
        void OnDisable(){FinishStroke();}
        void OnDestroy()
        {
            if(paintTexture)Destroy(paintTexture);if(effectTexture)Destroy(effectTexture);if(material)Destroy(material);
            if(beforeFill){beforeFill.Release();Destroy(beforeFill);}
            if(outline)Resources.UnloadAsset(outline);
        }
    }
    // Renders the artwork square of a page (paint + glitter + outline) to a small opaque texture.
    public static class PaintThumbnail
    {
        public static Texture2D Render(Texture paint,Texture effects,Texture outline,Vector2Int page,Vector2Int art,int size)
        {
            var shader=Resources.Load<Shader>("Kido/Shaders/FairyDust");
            if(!shader)throw new InvalidOperationException("Fairy Dust shader is missing.");
            var material=new Material(shader);
            var rt=RenderTexture.GetTemporary(size,size,0,RenderTextureFormat.ARGB32);
            var previous=RenderTexture.active;Texture2D thumb=null;
            try
            {
                material.SetTexture("_LineTex",outline);material.SetTexture("_EffectTex",effects);
                material.SetVector("_PageSize",new Vector4(page.x,page.y,0,0));
                float offX=(page.x-art.x)*.5f,offY=(page.y-art.y)*.5f;
                material.SetVector("_PageMap",new Vector4((float)art.x/page.x,(float)art.y/page.y,offX/page.x,offY/page.y));
                material.SetVector("_ArtMap",new Vector4(1,1,0,0));
                material.SetFloat("_FillRadius",100);material.SetFloat("_Thumb",1);material.SetFloat("_SparkleTime",0);
                // Temporary targets are not cleared; start from white so no stale pixels can show through.
                RenderTexture.active=rt;GL.Clear(false,true,Color.white);
                Graphics.Blit(paint,rt,material);RenderTexture.active=rt;
                thumb=new Texture2D(size,size,TextureFormat.RGB24,false);
                thumb.ReadPixels(new Rect(0,0,size,size),0,0);thumb.Apply(false,false);return thumb;
            }
            catch{if(thumb)UnityEngine.Object.Destroy(thumb);throw;}
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(material);}
        }
        // Builds a thumbnail from a saved painting without opening the canvas (startup / stale cache repair).
        public static Texture2D FromSaved(SavedPainting saved,Texture outline,Vector2Int art,int size)
        {
            Texture2D paint=null,effects=null;
            try
            {
                paint=new Texture2D(saved.width,saved.height,TextureFormat.RGBA32,false);paint.wrapMode=TextureWrapMode.Clamp;
                effects=new Texture2D(saved.width,saved.height,TextureFormat.Alpha8,false,true);effects.wrapMode=TextureWrapMode.Clamp;effects.filterMode=FilterMode.Point;
                paint.SetPixels32(saved.paint);paint.Apply(false,false);
                effects.LoadRawTextureData(saved.effects);effects.Apply(false,false);
                return Render(paint,effects,outline,new Vector2Int(saved.width,saved.height),art,size);
            }
            finally{if(paint)UnityEngine.Object.Destroy(paint);if(effects)UnityEngine.Object.Destroy(effects);}
        }
    }
}
