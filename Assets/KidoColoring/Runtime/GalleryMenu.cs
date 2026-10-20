using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Kido.Coloring
{
    // Behaviour/layout from the supplied menu video, using the four original
    // transparent category exports supplied on 20 October.
    public sealed class GalleryMenu:MonoBehaviour
    {
        static readonly string[] Names={"CHRISTMAS","ANIMALS","VEHICLES","TRACING"};
        static readonly Color[] TabColors={KidoUI.Hex("CF111B"),KidoUI.Hex("CC854F"),KidoUI.Hex("325483"),KidoUI.Hex("D9C900")};
        static readonly Color[] PageColors={KidoUI.Hex("FF9FA5"),KidoUI.Hex("F4C69C"),KidoUI.Hex("B9CCD5"),KidoUI.Hex("FFF8A5")};
        readonly List<Texture2D> ownedTextures=new List<Texture2D>();
        readonly RectTransform[] cards=new RectTransform[30];
        readonly int[] worlds=new int[30],slots=new int[30];
        readonly RectTransform[] tabRects=new RectTransform[4];
        readonly RawImage[] mascots=new RawImage[4];
        readonly Text[] labels=new Text[4];
        readonly float[] tabWidths=new float[4];
        RectTransform page,viewport,content,tracing,parents;
        Image pageColor;GalleryScrollRect scroll;
        Action<float,int> changed;
        Font menuFont;
        float width,height,header,stride,maxOffset,targetOffset;
        int selected=-1;bool moving,layingOut;

        public void Initialize(ContentCatalog catalog,Func<CanvasEntry,Texture2D> preview,
            Func<CanvasEntry,float> progress,Func<CanvasEntry,bool> allowed,Action<CanvasEntry> open,
            Action parentGate,Action<float,int> onPosition,float position)
        {
            changed=onPosition;
            menuFont=Resources.Load<Font>("Kido/Fonts/LeagueGothic-Regular");
            if(!menuFont)Debug.LogWarning("League Gothic font is missing from Kido/Fonts/LeagueGothic-Regular.");
            for(int i=0;i<4;i++)
            {
                int index=i;var button=KidoUI.Button("",transform,Color.black,()=>JumpToCategory(index));
                button.name=Names[i];tabRects[i]=(RectTransform)button.transform;
                var face=KidoUI.Panel("Category color",button.transform,TabColors[i]);KidoUI.Place(face,0,0,1,1,3);
                var texture=Resources.Load<Texture2D>("Kido/UI/Figma/Menu/"+Names[i]);
                if(texture)
                {
                    ownedTextures.Add(texture);mascots[i]=KidoUI.Picture(button.transform,texture);
                    mascots[i].name="Original Figma category mascot";
                }
                labels[i]=MenuLabel(Names[i],button.transform,58,Color.white);
            }
            page=KidoUI.Panel("Gallery border",transform,Color.black);
            var fill=KidoUI.Panel("Live category background",page,PageColors[0]);KidoUI.Place(fill,0,0,1,1,3);pageColor=fill.GetComponent<Image>();
            viewport=KidoUI.Panel("Horizontal gallery",page,Color.clear,false);
            viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            scroll=viewport.gameObject.AddComponent<GalleryScrollRect>();scroll.horizontal=true;scroll.vertical=false;
            scroll.viewport=viewport;scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.decelerationRate=.075f;scroll.scrollSensitivity=45;scroll.Interrupted=()=>moving=false;
            content=KidoUI.Rect("Three worlds and tracing",viewport);scroll.content=content;
            content.anchorMin=new Vector2(0,0);content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,.5f);
            var counts=new int[3];
            for(int i=0;i<catalog.canvases.Length;i++)
            {
                var entry=catalog.canvases[i];worlds[i]=entry.category;slots[i]=counts[entry.category]++;
                var button=KidoUI.Button("",content,Color.black,()=>open(entry));button.name=entry.title;cards[i]=(RectTransform)button.transform;
                var face=KidoUI.Panel("White paper",button.transform,Color.white);KidoUI.Place(face,0,0,1,1,3);
                var texture=preview(entry);
                if(!texture){texture=Resources.Load<Texture2D>(entry.thumbPath);if(texture)ownedTextures.Add(texture);}
                if(texture)
                {
                    var art=KidoUI.Picture(face,texture);KidoUI.Place(art.rectTransform,.045f,.045f,.955f,.955f);KidoUI.Square(art.rectTransform);
                }
                else {var title=MenuLabel(entry.title,face,34,KidoUI.Ink);KidoUI.Place(title.rectTransform,.1f,.1f,.9f,.9f);}
                float amount=progress(entry);
                if(amount>0)
                {
                    var track=KidoUI.Panel("Saved progress",face,KidoUI.Hex("E9E5EF"));KidoUI.Place(track,.12f,.035f,.88f,.052f);
                    var bar=KidoUI.Panel("Colored",track,TabColors[entry.category]);KidoUI.Place(bar,0,0,Mathf.Clamp01(amount),1);
                }
                if(!allowed(entry))
                {var text=MenuLabel("GROWN-UPS",face,26,KidoUI.Ink);KidoUI.Place(text.rectTransform,.1f,.04f,.9f,.17f);}
            }
            tracing=KidoUI.Rect("Tracing coming soon",content);
            var soon=MenuLabel("COMING SOON",tracing,132,KidoUI.Ink);KidoUI.Place(soon.rectTransform,.05f,.30f,.95f,.70f);
            soon.resizeTextForBestFit=true;soon.resizeTextMinSize=56;soon.resizeTextMaxSize=132;
            var parentButton=KidoUI.Button("GROWN-UPS",transform,Color.clear,parentGate,24,KidoUI.Ink);
            parents=(RectTransform)parentButton.transform;
            if(menuFont)parentButton.GetComponentInChildren<Text>().font=menuFont;
            var layout=gameObject.AddComponent<FigmaScreenLayout>();layout.Layout=Layout;
            Canvas.ForceUpdateCanvases();layout.Refresh();
            scroll.onValueChanged.AddListener(_=>{if(!layingOut)RefreshPosition();});
            SetOffset(Mathf.Clamp01(position)*maxOffset);RefreshPosition();LayoutTabs(true);
        }
        Text MenuLabel(string value,Transform parent,int size,Color color)
        {
            var text=KidoUI.Label(value,parent,size,color,TextAnchor.MiddleCenter);
            if(menuFont)text.font=menuFont;text.supportRichText=false;return text;
        }
        void Layout(float w,float h,bool tablet)
        {
            layingOut=true;float oldPosition=maxOffset>0?Offset/maxOffset:0;
            width=w;height=h;header=h*(tablet ? .315f : .27f);
            FigmaScreenLayout.Place(page,0,header,w,h-header+24);
            FigmaScreenLayout.Place(viewport,16,25,w-32,h-header-43);
            const float gap=28;
            float cell=Mathf.Max(64,(h-header-43-gap)/2);
            stride=SectionStride(cell,gap);maxOffset=3*stride;
            content.sizeDelta=new Vector2(maxOffset+w-32,0);
            for(int i=0;i<cards.Length;i++)
                FigmaScreenLayout.Place(cards[i],worlds[i]*stride+(slots[i]/2)*(cell+gap),slots[i]%2*(cell+gap),cell,cell);
            FigmaScreenLayout.Place(tracing,3*stride,0,w-32,h-header-43);
            FigmaScreenLayout.Place(parents,w-170,4,155,42);
            scroll.StopMovement();moving=false;SetOffset(oldPosition*maxOffset);
            layingOut=false;RefreshPosition();LayoutTabs(true);
        }
        public static float SectionStride(float cell,float gap)=>5*(cell+gap);
        public static int CategoryAt(float offset,float viewWidth,float sectionStride)
            =>Mathf.Clamp(Mathf.FloorToInt((Mathf.Max(0,offset)+viewWidth*.5f)/sectionStride),0,3);
        public static float Destination(int category,float sectionStride)=>Mathf.Clamp(category,0,3)*sectionStride;
        float Offset=>content?-content.anchoredPosition.x:0;
        void SetOffset(float x){content.anchoredPosition=new Vector2(-Mathf.Clamp(x,0,maxOffset),0);}
        void RefreshPosition()
        {
            if(stride<=0)return;float x=Mathf.Clamp(Offset,0,maxOffset),view=viewport.rect.width;
            selected=CategoryAt(x,view,stride);
            // Blend only while two worlds share the viewport. A world at its tab
            // destination has its own solid background, including the final page.
            int first=Mathf.Clamp(Mathf.FloorToInt(x/stride),0,3),next=Mathf.Min(first+1,3);
            float mix=next==first?0:Mathf.InverseLerp((first+1)*stride-view,(first+1)*stride,x);
            pageColor.color=Color.Lerp(PageColors[first],PageColors[next],Mathf.SmoothStep(0,1,mix));
            changed?.Invoke(maxOffset>0?x/maxOffset:0,selected);
        }
        public void JumpToCategory(int index)
        {
            if(!scroll)return;scroll.StopMovement();targetOffset=Destination(index,stride);moving=true;
            Feedback.Current?.Tap();
        }
        void Update()
        {
            if(!scroll)return;
            if(moving)
            {
                float x=Mathf.Lerp(Offset,targetOffset,1-Mathf.Exp(-Time.unscaledDeltaTime*14));
                if(Mathf.Abs(x-targetOffset)<.35f){x=targetOffset;moving=false;}
                SetOffset(x);RefreshPosition();
            }
            LayoutTabs(false);
        }
        void LayoutTabs(bool immediate)
        {
            if(width<=0)return;const float margin=42,gap=34;
            float available=width-2*margin-3*gap,total=0;
            for(int i=0;i<4;i++)
            {
                float weight=mascots[i]?(i==selected?2.25f:1):1.45f;
                float desired=weight;
                tabWidths[i]=immediate?desired:Mathf.Lerp(tabWidths[i],desired,1-Mathf.Exp(-Time.unscaledDeltaTime*16));
                total+=tabWidths[i];
            }
            float x=margin,tabH=header*.50f,tabY=header-tabH-44;
            for(int i=0;i<4;i++)
            {
                float w=available*tabWidths[i]/total;
                FigmaScreenLayout.Place(tabRects[i],x,tabY,w,tabH);
                var text=labels[i];text.fontSize=Mathf.RoundToInt(Mathf.Min(68,tabH*.44f));
                text.resizeTextForBestFit=true;text.resizeTextMinSize=22;text.resizeTextMaxSize=text.fontSize;
                bool showText=!mascots[i]||i==selected;text.gameObject.SetActive(showText);
                if(mascots[i])
                {
                    float mh=tabH*1.50f,mw=mh*mascots[i].texture.width/mascots[i].texture.height;
                    FigmaScreenLayout.Place(mascots[i].rectTransform,6,tabH-mh,mw,mh);
                    KidoUI.Place(text.rectTransform,Mathf.Min(.6f,(mw+12)/w),.10f,.97f,.91f);
                }
                else
                {
                    KidoUI.Place(text.rectTransform,.04f,.08f,.96f,.92f);
                    tabRects[i].GetComponent<PressBounce>().BaseScale=i==selected?1:.94f;
                }
                x+=w+gap;
            }
        }
        void OnDestroy(){foreach(var texture in ownedTextures)if(texture)Resources.UnloadAsset(texture);if(menuFont)Resources.UnloadAsset(menuFont);}
    }
    public sealed class GalleryScrollRect:ScrollRect,IPointerDownHandler
    {
        public Action Interrupted;
        public void OnPointerDown(PointerEventData e){Interrupted?.Invoke();StopMovement();Feedback.Current?.Tap();}
        public override void OnBeginDrag(PointerEventData e){Interrupted?.Invoke();Feedback.Current?.Tap();base.OnBeginDrag(e);}
        public override void OnScroll(PointerEventData e){Interrupted?.Invoke();base.OnScroll(e);}
    }
}
