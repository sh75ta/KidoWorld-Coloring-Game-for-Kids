using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Kido.Coloring
{
    public sealed class KidoApp:MonoBehaviour
    {
        RectTransform root,screen,modalRoot;
        Image background;
        Feedback feedback;
        ContentCatalog catalog;GameSettings settings;AccessPolicy access;PurchaseService purchases;PaintStore store;
        readonly Dictionary<string,Texture2D> previews=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,float> progress=new Dictionary<string,float>();
        readonly HashSet<string> unavailableSaves=new HashSet<string>();
        DrawingSurface surface;CanvasEntry active,requestedLockedCanvas;
        Text saveLabel,progressLabel;
        Button undoButton,resetButton;Text resetHint;readonly List<Button> tools=new List<Button>(),swatches=new List<Button>();
        CanvasGroup paletteGroup;
        int category;float menuPosition,nextSave,lastResetTap=-10;bool dirty,completed,leaving,transitioning;
        PatternCatalog patternCatalog;
        RectTransform colorPicker,patternPicker;
        readonly List<Texture2D> patternTextures=new List<Texture2D>();
        readonly Dictionary<int,PatternData> patternSamples=new Dictionary<int,PatternData>();
        LoopingPalette colorLoop,patternLoop;
        readonly List<Button> patternButtons=new List<Button>();
        Task<SaveOutcome> pendingSave;int savingRevision;
        SavedPainting saveBuffer;
        bool backgrounded,refreshPreview;
        readonly List<Texture2D> uiTextures=new List<Texture2D>();
        RectTransform loadingFill;
        float pickerCellHeight=112.5f;
        readonly List<RectTransform> selectionFrames=new List<RectTransform>();
        sealed class SaveOutcome {public bool ok;public string error;}
        // 4: thumbnails frame the artwork square only (3 captured the whole expanded page).
        const int PreviewRevision=4;
        bool previewsPurged;
        [Serializable] sealed class PreviewInfo {public int artRevision,previewRevision;public float coverage;}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(SceneManager.GetActiveScene().name!="KidoGame")return;
            var obj=new GameObject("Kido Coloring");obj.AddComponent<KidoApp>();
        }
        IEnumerator Start()
        {
            Application.lowMemory+=OnLowMemory;
            Application.targetFrameRate=60;
            Screen.orientation=ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
            Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
            if(!FindAnyObjectByType<AudioListener>())gameObject.AddComponent<AudioListener>();
            feedback=gameObject.AddComponent<Feedback>();
            if(!EventSystem.current)
            {
                var es=new GameObject("Event System",typeof(EventSystem),typeof(StandaloneInputModule));
                es.GetComponent<EventSystem>().sendNavigationEvents=false;
            }
            var canvasObject=new GameObject("Kido UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            var bg=KidoUI.Panel("Background",canvasObject.transform,KidoUI.Cream,false);KidoUI.Place(bg,0,0,1,1);background=bg.GetComponent<Image>();
            root=KidoUI.Rect("Safe area",canvasObject.transform);root.gameObject.AddComponent<SafeArea>();
            float introStarted=Time.unscaledTime;
            ShowSplash();feedback.PlayIntroVoice();yield return null;
            Exception failure=null;
            try
            {
                catalog=Content.Load();settings=Content.Settings();purchases=PurchaseService.Create(settings);access=new AccessPolicy(settings,purchases);
                store=new PaintStore(Path.Combine(Application.persistentDataPath,"KidoPaintings-v2"));
                patternCatalog=JsonUtility.FromJson<PatternCatalog>(Resources.Load<TextAsset>("Kido/patterns").text);
            }
            catch(Exception e){failure=e;}
            if(failure!=null){ClearScreen();Message("Let's try again", "The coloring book couldn't open. Please restart the game.");Debug.LogException(failure);yield break;}
            yield return new WaitForSecondsRealtime(.7f);
            foreach(var entry in catalog.canvases)
            {
                LoadPreview(entry);
                if(NeedsPreviewRebuild(entry))RebuildPreview(entry);
                SetLoadingProgress((Array.IndexOf(catalog.canvases,entry)+1f)/catalog.canvases.Length);yield return null;
            }
            float introLength=Mathf.Max(feedback.Motion?SplashLogoAnimator.Duration:0,feedback.IntroVoiceLength);
            while(Time.unscaledTime-introStarted<introLength)yield return null;
            feedback.StartMusic();
            yield return FadeToMenu();
        }
        IEnumerator FadeToMenu()=>FadeTo(ShowMenu);
        IEnumerator FadeTo(Action nextScreen)
        {
            if(transitioning)yield break;transitioning=true;
            var fade=KidoUI.Panel("Scene fade",root,Color.clear,false);KidoUI.Place(fade,0,0,1,1);
            var graphic=fade.GetComponent<Image>();graphic.raycastTarget=true;
            try
            {
                for(float t=0;t<.16f;t+=Time.unscaledDeltaTime){graphic.color=new Color(1,.96f,.98f,t/.16f);yield return null;}
                graphic.color=new Color(1,.96f,.98f,1);nextScreen();fade.SetAsLastSibling();
                for(float t=0;t<.20f;t+=Time.unscaledDeltaTime){graphic.color=new Color(1,.96f,.98f,1-t/.20f);yield return null;}
            }
            finally{if(fade)Destroy(fade.gameObject);transitioning=false;}
        }
        void ClearScreen()
        {
            CloseModal();
            if(surface)surface.FinishStroke();
            surface=null;saveBuffer=null;refreshPreview=false;saveLabel=null;progressLabel=null;loadingFill=null;tools.Clear();swatches.Clear();patternButtons.Clear();
            foreach(var texture in patternTextures)if(texture)Resources.UnloadAsset(texture);
            patternTextures.Clear();patternSamples.Clear();colorPicker=null;patternPicker=null;colorLoop=null;patternLoop=null;selectionFrames.Clear();
            if(screen){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}
            foreach(var texture in uiTextures)if(texture)Resources.UnloadAsset(texture);
            uiTextures.Clear();
            screen=KidoUI.Rect("Screen",root);KidoUI.Place(screen,0,0,1,1);
        }
        void Brand(Transform parent)
        {
            var logo=KidoUI.Label("<b><color=#F06474>k</color><color=#E7A63C>i</color><color=#41BAA3>d</color><color=#7661D5>o</color></b>",parent,54,KidoUI.Ink);
            KidoUI.Place(logo.rectTransform,.04f,.88f,.23f,.98f);
            var sub=KidoUI.Label("C O L O R I N G",parent,16,KidoUI.Muted);KidoUI.Place(sub.rectTransform,.043f,.85f,.23f,.89f);
        }
        Texture2D UITexture(string name)
        {
            var texture=Resources.Load<Texture2D>("Kido/UI/Figma/"+name);
            if(texture&&!uiTextures.Contains(texture))uiTextures.Add(texture);
            return texture;
        }
        RawImage FigmaPicture(string name,Transform parent)
        {var image=KidoUI.Picture(parent,UITexture(name));image.name=name;return image;}
        Button FigmaButton(string asset,Transform parent,Action click)
        {
            var button=KidoUI.Button("",parent,Color.clear,click);button.name=asset;
            var artwork=FigmaPicture(asset,button.transform);KidoUI.Place(artwork.rectTransform,0,0,1,1);
            artwork.raycastTarget=true;button.targetGraphic=artwork;return button;
        }
        void SetLoadingProgress(float amount)
        {if(loadingFill)KidoUI.Place(loadingFill,0,0,Mathf.Clamp01(amount),1);}
        void ShowSplash()
        {
            ClearScreen();background.color=KidoUI.Hex("FFF2F6");
            screen.gameObject.AddComponent<RectMask2D>();
            var design=KidoUI.Rect("Figma splash layout",screen);
            var layout=design.gameObject.AddComponent<FigmaScreenLayout>();layout.Splash=true;
            var pink=FigmaPicture("SplashPink",design);var yellow=FigmaPicture("SplashYellow",design);
            var pinkMotion=pink.gameObject.AddComponent<GentleFloat>();
            var yellowMotion=yellow.gameObject.AddComponent<GentleFloat>();yellowMotion.Phase=2;
            var logoBounds=KidoUI.Rect("Logo bounds",design);
            logoBounds.gameObject.AddComponent<SplashLogoAnimator>().Configure(UITexture("Logo"));
            var dots=new RawImage[5];for(int i=0;i<dots.Length;i++){dots[i]=FigmaPicture("Dot",design);dots[i].gameObject.AddComponent<GentleFloat>().Phase=i;}
            var border=KidoUI.Panel("Loading border",design,Color.black);
            var track=KidoUI.Panel("Loading track",border,KidoUI.Hex("DBDBDB"));KidoUI.Place(track,0,0,1,1,3);
            loadingFill=KidoUI.Panel("Actual loading progress",track,KidoUI.Hex("C393DF"));SetLoadingProgress(.02f);
            layout.Layout=(w,h,tablet)=>
            {
                // Supplied paint splashes are already clipped Figma node exports.
                FigmaScreenLayout.Place(pink.rectTransform,0,0,327,403);
                FigmaScreenLayout.Place(yellow.rectTransform,w-336,h-284,336,284);
                FigmaScreenLayout.Place(logoBounds,(w-577)/2,h/2-293,577,457);
                FigmaScreenLayout.Place(border,(w-679)/2,tablet?836:717,679,35);
                float[,] positions=tablet?new float[,]{{1449,518},{522,625},{1159,152},{797,1003},{305,507}}:new float[,]{{1449,518},{522,625},{1159,152},{797,1003},{0,0}};
                pinkMotion.Capture();yellowMotion.Capture();
                for(int i=0;i<dots.Length;i++){dots[i].gameObject.SetActive(tablet||i<4);FigmaScreenLayout.Place(dots[i].rectTransform,positions[i,0],positions[i,1],21,22);dots[i].GetComponent<GentleFloat>().Capture();}
            };
            Canvas.ForceUpdateCanvases();layout.Refresh();
        }
        void LoadPreview(CanvasEntry entry)
        {
            // Thumbnails are a disposable cache. Never deserialize 30 full paintings on startup.
            string meta=Path.Combine(store.Root,entry.id+".preview.json"),png=Path.Combine(store.Root,entry.id+".preview.png");
            if(!File.Exists(meta)||!File.Exists(png)||previews.ContainsKey(entry.id))return;
            Texture2D texture=null;
            try
            {
                var info=JsonUtility.FromJson<PreviewInfo>(File.ReadAllText(meta));
                if(info.artRevision!=entry.artRevision||info.previewRevision!=PreviewRevision)return;
                if(new FileInfo(png).Length>2000000)return;
                texture=new Texture2D(2,2,TextureFormat.RGB24,false);
                if(!texture.LoadImage(File.ReadAllBytes(png),true)){Destroy(texture);return;}
                previews[entry.id]=texture;progress[entry.id]=Mathf.Clamp01(info.coverage);
            }
            catch(Exception e)when(e is IOException||e is ArgumentException){if(texture)Destroy(texture);Debug.LogWarning("Preview cache ignored: "+e.Message);}
        }
        DateTime LatestSave(string id)
        {
            var latest=DateTime.MinValue;
            foreach(var suffix in new[]{"_page2","_page",""})foreach(var slot in new[]{"a","b"})
            {
                string path=Path.Combine(store.Root,id+suffix+"."+slot+".kido");
                if(File.Exists(path)){var time=File.GetLastWriteTimeUtc(path);if(time>latest)latest=time;}
            }
            return latest;
        }
        // A painting exists but its thumbnail is missing, from an older framing, or older than the save
        // (for example the app was paused or closed before the thumbnail could be rendered).
        bool NeedsPreviewRebuild(CanvasEntry entry)
        {
            var saved=LatestSave(entry.id);if(saved==DateTime.MinValue||unavailableSaves.Contains(entry.id))return false;
            if(!previews.ContainsKey(entry.id))return true;
            string png=Path.Combine(store.Root,entry.id+".preview.png");
            return !File.Exists(png)||File.GetLastWriteTimeUtc(png)<saved.AddSeconds(-1);
        }
        void RebuildPreview(CanvasEntry entry)
        {
            Texture2D art=null,thumbnail=null;
            try
            {
                var loaded=store.LoadPage(entry.id,entry.artRevision,entry.paintWidth,entry.paintHeight);
                if(loaded.painting==null){unavailableSaves.Add(entry.id);return;}
                art=Resources.Load<Texture2D>(entry.artPath);if(!art)return;
                thumbnail=PaintThumbnail.FromSaved(loaded.painting,art,new Vector2Int(entry.paintWidth,entry.paintHeight),384);
                StorePreview(entry,thumbnail,loaded.painting.coverage);thumbnail=null;
            }
            catch(Exception e){Debug.LogWarning("Preview rebuild skipped: "+e.Message);}
            finally{if(thumbnail)Destroy(thumbnail);if(art)Resources.UnloadAsset(art);}
        }
        void StorePreview(CanvasEntry entry,Texture2D thumbnail,float coverage)
        {
            if(previews.TryGetValue(entry.id,out var previous)&&previous)Destroy(previous);
            previews[entry.id]=thumbnail;progress[entry.id]=coverage;
            try
            {
                File.WriteAllBytes(Path.Combine(store.Root,entry.id+".preview.png"),thumbnail.EncodeToPNG());
                File.WriteAllText(Path.Combine(store.Root,entry.id+".preview.json"),JsonUtility.ToJson(new PreviewInfo{artRevision=entry.artRevision,previewRevision=PreviewRevision,coverage=coverage}));
            }
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("Preview cache: "+e.Message);}
            finally{thumbnail.Apply(false,true);}
        }
        public void ShowMenu()
        {
            ClearScreen();active=null;background.color=KidoUI.Hex("FFF2F6");
            if(previewsPurged){foreach(var entry in catalog.canvases)LoadPreview(entry);previewsPurged=false;}
            screen.gameObject.AddComponent<RectMask2D>();
            var design=KidoUI.Rect("Menu prototype layout",screen);
            var gallery=design.gameObject.AddComponent<GalleryMenu>();
            gallery.Initialize(catalog,
                entry=>previews.TryGetValue(entry.id,out var preview)?preview:null,
                entry=>access.CanOpen(entry.id),
                entry=>{if(!transitioning&&!leaving)StartCoroutine(FadeTo(()=>OpenCanvas(entry)));},
                ParentGate,(position,world)=>{menuPosition=position;category=world;},menuPosition);
        }
        void OpenCanvas(CanvasEntry entry)
        {
            if(!access.CanOpen(entry.id)){requestedLockedCanvas=entry;ParentGate();return;}
            PaintDocument document;Texture2D art=null;TextAsset bytes=null;LoadResult loaded;int artWidth,artHeight;
            try
            {
                art=Resources.Load<Texture2D>(entry.artPath);bytes=Resources.Load<TextAsset>(entry.regionPath);
                if(!art || !bytes)throw new InvalidDataException("Artwork is missing.");
                var originalMap=RegionMap.Decode(bytes.bytes);artWidth=originalMap.Width;artHeight=originalMap.Height;var map=originalMap.WithPage();
                loaded=store.LoadPage(entry.id,entry.artRevision,originalMap.Width,originalMap.Height);
                if(loaded.filesExist && loaded.painting==null){Resources.UnloadAsset(art);Message("Your picture is safe", "We couldn't open the saved colors. The saved files have been kept. Please ask a grown-up for help.");return;}
                document=new PaintDocument(map,loaded.painting?.paint,loaded.painting?.effects);
            }
            catch(Exception e){if(art)Resources.UnloadAsset(art);Debug.LogException(e);Message("Let's try another picture","This picture couldn't open. Your other pictures are still here.");return;}
            finally{if(bytes)Resources.UnloadAsset(bytes);}
            ClearScreen();active=entry;dirty=false;completed=document.Coverage>=settings.completionThreshold;background.color=KidoUI.Backgrounds[entry.category];

            background.color=Color.white;
            var design=KidoUI.Rect("Figma coloring layout",screen);
            var layout=design.gameObject.AddComponent<FigmaScreenLayout>();
            var artHolder=KidoUI.Rect("Artwork bounds",design);
            var image=KidoUI.Picture(artHolder,art);KidoUI.Place(image.rectTransform,0,0,1,1);
            surface=image.gameObject.AddComponent<DrawingSurface>();surface.Initialize(document,art,artWidth,artHeight);surface.AnimateGlitter=feedback.Motion;surface.Changed+=OnPaintChanged;
            surface.TouchCompleted+=(position,color,fill)=>feedback.PaintBurst(screen,position,color);
            var rail=KidoUI.Panel("Brushes",design,KidoUI.Hex("FEDCE7"),false);
            var palette=KidoUI.Panel("Colors and patterns",design,Color.white,false);
            paletteGroup=palette.gameObject.AddComponent<CanvasGroup>();
            colorPicker=PickerContent(palette,"30 colors",30);
            for(int i=0;i<KidoUI.HexColors.Length;i++)
            {
                int index=i;var swatch=KidoUI.Button("",colorPicker,KidoUI.Hex(KidoUI.HexColors[i]),()=>SelectColor(index));
                swatch.name="Color "+(i+1);swatch.GetComponent<Image>().sprite=null;
                AddSwatchBorder((RectTransform)swatch.transform);PlacePickerCell((RectTransform)swatch.transform,i);swatches.Add(swatch);
                AddSelectionMark(swatch.transform);
            }
            patternPicker=PickerContent(palette,"15 patterns",15);
            for(int i=0;i<patternCatalog.patterns.Length;i++)
            {
                int index=i;var entryPattern=patternCatalog.patterns[i];
                var texture=Resources.Load<Texture2D>(entryPattern.path);patternTextures.Add(texture);
                var button=KidoUI.Button("",patternPicker,Color.white,()=>SelectPattern(index));PlacePickerCell((RectTransform)button.transform,i);
                button.name="Pattern "+(i+1);button.GetComponent<Image>().sprite=null;
                // Same cell anatomy as a colour swatch: full-bleed fill, 3px black border, selection mark.
                var pic=KidoUI.Picture(button.transform,texture);KidoUI.Place(pic.rectTransform,0,0,1,1);
                AddSwatchBorder((RectTransform)button.transform);AddSelectionMark(button.transform);patternButtons.Add(button);
            }
            colorLoop=colorPicker.parent.GetComponent<LoopingPalette>();colorLoop.Configure(colorPicker,index=>SelectColor(index,false,false));
            patternLoop=patternPicker.parent.GetComponent<LoopingPalette>();patternLoop.Configure(patternPicker,index=>SelectPattern(index,false,false));
            patternPicker.parent.gameObject.SetActive(false);
            selectionFrames.Clear();
            selectionFrames.Add(AddSelectionFrame(palette));
            string[] names={"Fill","Brush","Eraser","Glitter","Pattern","Rainbow"};
            for(int i=0;i<6;i++)
            {
                int tool=i;var button=KidoUI.Button("",design,Color.white,()=>SelectTool((PaintTool)tool));
                button.name=names[i];button.GetComponent<Image>().sprite=KidoUI.Circle();tools.Add(button);
                var edge=KidoUI.Panel("Circle border",button.transform,Color.black,false);edge.GetComponent<Image>().sprite=KidoUI.Circle();KidoUI.Place(edge,0,0,1,1);
                var face=KidoUI.Panel("Tool face",edge,Color.white,false);face.GetComponent<Image>().sprite=KidoUI.Circle();KidoUI.Place(face,0,0,1,1,3);
                var icon=FigmaPicture(names[i],button.transform);
                // Match the transforms applied to the artwork in the supplied Figma screens.
                float rotation=i==0?90:-90;float flip=i==2?1:-1;
                var ir=icon.rectTransform;ir.anchorMin=ir.anchorMax=ir.pivot=new Vector2(.5f,.5f);ir.anchoredPosition=Vector2.zero;
                ir.localRotation=Quaternion.Euler(0,0,rotation);ir.localScale=new Vector3(1,flip,1);
            }
            var done=FigmaButton("Done",design,LeaveCanvas);
            undoButton=FigmaButton("Undo",design,Undo);
            var reset=FigmaButton("Reset",design,ConfirmReset);
            resetButton=reset;lastResetTap=-10;
            resetHint=KidoUI.Label("Tap again to reset",design,21,KidoUI.Ink,TextAnchor.MiddleRight);resetHint.gameObject.SetActive(false);
            var undoHit=KidoUI.Panel("Touch target",undoButton.transform,Color.clear,false);undoHit.GetComponent<Image>().raycastTarget=true;
            KidoUI.Place(undoHit,0,0,1,1);undoHit.offsetMin=new Vector2(-7.5f,-7.5f);undoHit.offsetMax=new Vector2(7.5f,7.5f);
            var resetHit=KidoUI.Panel("Touch target",reset.transform,Color.clear,false);resetHit.GetComponent<Image>().raycastTarget=true;
            KidoUI.Place(resetHit,0,0,1,1);resetHit.offsetMin=new Vector2(-4,-6.5f);resetHit.offsetMax=new Vector2(4,6.5f);
            layout.Layout=(w,h,tablet)=>
            {
                if(surface)surface.FinishStroke();
                float railX=w-252,paletteX=railX-(tablet?105:112),paletteWidth=tablet?111:112;
                float artSize=Mathf.Min(tablet?882:720,paletteX-100),artY=tablet?120:152;
                FigmaScreenLayout.Place(artHolder,0,0,paletteX,h);
                surface.SetArtworkLayout(new Vector2(paletteX,h),new Rect((paletteX-artSize)/2,h-artY-artSize,artSize,artSize));
                FigmaScreenLayout.Place(rail,railX,0,252,h);rail.GetComponent<Image>().color=KidoUI.Hex(tablet?"FFF2F6":"FEDCE7");
                FigmaScreenLayout.Place(palette,paletteX,0,paletteWidth,h);
                FigmaScreenLayout.Place((RectTransform)done.transform,28,30,229,98);
                FigmaScreenLayout.Place((RectTransform)undoButton.transform,paletteX-(tablet?240:234),30,95,97);
                FigmaScreenLayout.Place((RectTransform)reset.transform,paletteX-(tablet?132:126),29,102,99);
                FigmaScreenLayout.Place(resetHint.rectTransform,paletteX-330,130,300,40);
                float diameter=tablet?148:132,step=tablet?174:144,top=tablet?21:24;
                for(int i=0;i<tools.Count;i++)
                {
                    int slot=i==0?1:i==1?2:i==2?0:i;
                    FigmaScreenLayout.Place((RectTransform)tools[i].transform,railX+(252-diameter)/2,top+slot*step,diameter,diameter);
                    var icon=tools[i].GetComponentInChildren<RawImage>();float max=diameter*.75f;
                    float factor=max/Mathf.Max(icon.texture.width,icon.texture.height);
                    icon.rectTransform.sizeDelta=new Vector2(icon.texture.width*factor,icon.texture.height*factor);
                }
                pickerCellHeight=h/8;
                foreach(var frame in selectionFrames)if(frame)frame.sizeDelta=new Vector2(0,pickerCellHeight);
                ResizePicker(colorPicker,swatches);ResizePicker(patternPicker,patternButtons);
            };
            Canvas.ForceUpdateCanvases();layout.Refresh();
            SelectColor(0,false);SelectPattern(0,false);SelectTool(PaintTool.Fill,false);RefreshPaintStatus();
            if(loaded.recovered)Message("Welcome back", "We recovered the last safely saved version of your picture.");
        }
        RectTransform PickerContent(RectTransform parent,string label,int rows)
        {
            var viewport=KidoUI.Panel(label,parent,Color.white,false);KidoUI.Place(viewport,0,0,1,1);
            viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<LoopingPalette>();
            var content=KidoUI.Rect(label+" content",viewport);KidoUI.Place(content,0,0,1,1);return content;
        }
        void PlacePickerCell(RectTransform cell,int index)
        {
            cell.anchorMin=new Vector2(0,1);cell.anchorMax=new Vector2(1,1);cell.pivot=new Vector2(.5f,1);
            cell.offsetMin=new Vector2(0,-(index+1)*pickerCellHeight);cell.offsetMax=new Vector2(0,-index*pickerCellHeight);
        }
        void ResizePicker(RectTransform content,List<Button> buttons)
        {
            content.parent.GetComponent<LoopingPalette>().Resize(pickerCellHeight);
        }
        // Check mark with a dark outline so it reads on any colour or pattern.
        static void AddSelectionMark(Transform cell)
        {
            var mark=KidoUI.Label("✓",cell,40,Color.white,TextAnchor.MiddleCenter);mark.name="Selected";mark.fontStyle=FontStyle.Bold;
            var outline=mark.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.1f,.1f,.2f,.75f);outline.effectDistance=new Vector2(2,-2);
            KidoUI.Place(mark.rectTransform,0,0,1,1);mark.gameObject.SetActive(false);
        }
        // Fixed ring at the centre of the strip: the entry inside it is the selected one.
        static RectTransform AddSelectionFrame(RectTransform palette)
        {
            var frame=KidoUI.Rect("Selection frame",palette);
            frame.anchorMin=new Vector2(0,.5f);frame.anchorMax=new Vector2(1,.5f);frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
            AddRing(frame,KidoUI.Purple,0,7);AddRing(frame,Color.white,7,4);
            return frame;
        }
        static void AddRing(RectTransform parent,Color color,float inset,float thickness)
        {
            for(int side=0;side<4;side++)
            {
                var bar=KidoUI.Panel("Ring",parent,color,false);
                switch(side)
                {
                    case 0:KidoUI.Place(bar,0,1,1,1);bar.offsetMin=new Vector2(inset,-inset-thickness);bar.offsetMax=new Vector2(-inset,-inset);break;
                    case 1:KidoUI.Place(bar,0,0,1,0);bar.offsetMin=new Vector2(inset,inset);bar.offsetMax=new Vector2(-inset,inset+thickness);break;
                    case 2:KidoUI.Place(bar,0,0,0,1);bar.offsetMin=new Vector2(inset,inset);bar.offsetMax=new Vector2(inset+thickness,-inset);break;
                    default:KidoUI.Place(bar,1,0,1,1);bar.offsetMin=new Vector2(-inset-thickness,inset);bar.offsetMax=new Vector2(-inset,-inset);break;
                }
            }
        }
        static void AddSwatchBorder(RectTransform cell)
        {
            var top=KidoUI.Panel("Top border",cell,Color.black,false);KidoUI.Place(top,0,1,1,1);top.sizeDelta=new Vector2(0,3);
            var left=KidoUI.Panel("Left border",cell,Color.black,false);KidoUI.Place(left,0,0,0,1);left.sizeDelta=new Vector2(3,0);
            var right=KidoUI.Panel("Right border",cell,Color.black,false);KidoUI.Place(right,1,0,1,1);right.sizeDelta=new Vector2(3,0);
            var bottom=KidoUI.Panel("Bottom border",cell,Color.black,false);KidoUI.Place(bottom,0,0,1,0);bottom.sizeDelta=new Vector2(0,3);
        }
        void SelectPattern(int index,bool sound=true,bool center=true)
        {
            if(!surface)return;surface.FinishStroke();
            if(!patternSamples.TryGetValue(index,out var sample))
            {sample=new PatternData(patternTextures[index],patternCatalog.patterns[index].tint);patternSamples[index]=sample;}
            surface.Pattern=sample;
            for(int i=0;i<patternButtons.Count;i++)patternButtons[i].transform.Find("Selected").gameObject.SetActive(i==index);
            if(center&&patternLoop)patternLoop.Select(index,sound);
        }
        void SelectColor(int index,bool sound=true,bool center=true)
        {
            if(!surface)return;surface.FinishStroke();surface.SelectedColor=KidoUI.Hex(KidoUI.HexColors[index]);
            for(int i=0;i<swatches.Count;i++)
            {
                var mark=swatches[i].transform.Find("Selected");mark.gameObject.SetActive(i==index);
                if(i==index)mark.GetComponent<Text>().color=surface.SelectedColor.r*.299f+surface.SelectedColor.g*.587f+surface.SelectedColor.b*.114f>145?KidoUI.Ink:Color.white;
            }
            if(center&&colorLoop)colorLoop.Select(index,sound);
        }
        void SelectTool(PaintTool tool,bool sound=true)
        {
            if(!surface)return;surface.FinishStroke();surface.Tool=tool;
            colorPicker.parent.gameObject.SetActive(tool!=PaintTool.Pattern);patternPicker.parent.gameObject.SetActive(tool==PaintTool.Pattern);
            bool usesPalette=tool!=PaintTool.Eraser&&tool!=PaintTool.Rainbow;
            colorLoop.SetInputEnabled(usesPalette&&tool!=PaintTool.Pattern);
            patternLoop.SetInputEnabled(tool==PaintTool.Pattern);
            paletteGroup.alpha=usesPalette?1:.3f;paletteGroup.interactable=usesPalette;paletteGroup.blocksRaycasts=usesPalette;
            for(int i=0;i<tools.Count;i++)
            {
                bool selected=i==(int)tool;
                var edge=tools[i].transform.Find("Circle border").GetComponent<Image>();edge.color=selected?KidoUI.Hex("8B42EA"):KidoUI.Hex("766779");
                edge.transform.Find("Tool face").GetComponent<Image>().color=selected?KidoUI.Hex("EBD8FF"):Color.white;
                tools[i].GetComponent<PressBounce>().BaseScale=selected?1.10f:.88f;
            }
        }
        void OnPaintChanged(){dirty=true;refreshPreview=true;nextSave=Time.unscaledTime+1.5f;RefreshPaintStatus();}
        void RefreshPaintStatus()
        {
            if(!surface)return;undoButton.interactable=surface.Document.CanUndo;
            bool nowComplete=surface.Document.Coverage>=settings.completionThreshold;
            if(nowComplete && !completed)feedback.Celebrate(screen);completed=nowComplete;
            if(progressLabel)progressLabel.text=completed?"Beautifully yours!":surface.Document.PaintedPixels==0?"Let's add some color":"Looking lovely!";
        }
        void Undo(){if(!surface)return;surface.FinishStroke();surface.CompleteFillReveal();if(surface.Document.Undo())OnPaintChanged();}
        void ConfirmReset()
        {
            if(!surface)return;surface.FinishStroke();surface.CompleteFillReveal();
            if(Time.unscaledTime-lastResetTap<=3)
            {lastResetTap=-10;resetHint.gameObject.SetActive(false);surface.Document.Clear();OnPaintChanged();}
            else {lastResetTap=Time.unscaledTime;resetHint.gameObject.SetActive(true);}
        }
        bool BeginBackgroundSaveNow(){BeginBackgroundSave();return pendingSave!=null;}
        void BeginBackgroundSave()
        {
            if(pendingSave!=null||!surface||active==null||!dirty)return;
            savingRevision=surface.Document.Revision;
            var document=surface.Document;
            if(saveBuffer==null)saveBuffer=new SavedPainting{paint=new Color32[document.Paint.Length],effects=new byte[document.Effects.Length]};
            saveBuffer.id=active.id+"_page2";saveBuffer.artRevision=active.artRevision;saveBuffer.width=document.Map.Width;saveBuffer.height=document.Map.Height;saveBuffer.coverage=document.Coverage;
            Array.Copy(document.Paint,saveBuffer.paint,document.Paint.Length);Array.Copy(document.Effects,saveBuffer.effects,document.Effects.Length);
            var snapshot=saveBuffer;
            pendingSave=Task.Run(()=>{bool ok=store.TrySaveSnapshot(snapshot,out var error);return new SaveOutcome{ok=ok,error=error};});
        }
        bool CompleteBackgroundSave(bool wait)
        {
            if(pendingSave==null)return true;if(!wait&&!pendingSave.IsCompleted)return false;
            SaveOutcome result;
            try{result=pendingSave.GetAwaiter().GetResult();}
            catch(Exception e){result=new SaveOutcome{ok=false,error=e.Message};}
            pendingSave=null;
            if(!result.ok){if(saveLabel)saveLabel.text="Couldn't save yet";Debug.LogWarning(result.error);nextSave=Time.unscaledTime+5;return false;}
            if(surface && savingRevision==surface.Document.Revision){dirty=false;if(saveLabel)saveLabel.text="Saved ✓";progress[active.id]=surface.Document.Coverage;}
            return true;
        }
        // Thumbnail of the saved paint. Rendering is deferred while backgrounded (no GPU work then);
        // a stale or missing thumbnail is repaired from the save at the next launch.
        void UpdateSavedPreview()
        {
            if(!surface||active==null||backgrounded)return;
            Texture2D thumbnail=null;
            try
            {
                thumbnail=surface.Thumbnail();
                StorePreview(active,thumbnail,surface.Document.Coverage);thumbnail=null;refreshPreview=false;
            }
            catch(Exception e){Debug.LogWarning("Preview cache: "+e.Message);}
            finally{if(thumbnail)Destroy(thumbnail);}
        }
        bool SavePainting()
        {
            if(!surface||active==null)return true;surface.FinishStroke();
            if(!CompleteBackgroundSave(true))return false;
            if(dirty&&(!(BeginBackgroundSaveNow())||!CompleteBackgroundSave(true)))return false;
            if(refreshPreview)UpdateSavedPreview();
            return true;
        }
        void LeaveCanvas()
        {
            if(!surface||leaving||transitioning)return;surface.FinishStroke();
            StartCoroutine(DoneCelebration());
        }
        IEnumerator DoneCelebration()
        {
            leaving=true;
            var leavingScreen=screen;
            // TryGetComponent: "??" on a Unity object bypasses its null check (and throws in the Editor).
            if(!screen.TryGetComponent<CanvasGroup>(out var group))group=screen.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts=false;
            try
            {
                // Await the existing writer without blocking UI/audio. Input is
                // locked so the saved revision remains stable until navigation.
                do
                {
                    if(pendingSave==null&&dirty)BeginBackgroundSave();
                    while(pendingSave!=null&&!pendingSave.IsCompleted)yield return null;
                    if(!CompleteBackgroundSave(false))
                    {Message("Let's keep your colors","We couldn't save this picture yet. Please check available storage and try again. Your drawing is still open.");yield break;}
                }while(dirty);
                if(refreshPreview)UpdateSavedPreview();
                feedback.Celebrate(screen);yield return new WaitForSecondsRealtime(feedback.Motion?KidsEffects.ShowerSeconds+.15f:.2f);
                yield return FadeToMenu();
            }
            finally{if(group&&screen==leavingScreen)group.blocksRaycasts=true;leaving=false;}
        }
        void Update()
        {
            if(resetHint&&resetHint.gameObject.activeSelf&&Time.unscaledTime-lastResetTap>3)resetHint.gameObject.SetActive(false);
            if(refreshPreview&&!backgrounded&&surface&&!leaving&&!dirty&&pendingSave==null&&!surface.Document.ActionOpen)UpdateSavedPreview();
            if(!leaving&&pendingSave!=null&&pendingSave.IsCompleted)CompleteBackgroundSave(false);
            if(!leaving&&dirty&&surface&&!surface.Document.ActionOpen&&Time.unscaledTime>=nextSave)BeginBackgroundSave();
            if(!transitioning&&!leaving&&Input.GetKeyDown(KeyCode.Escape)) {if(modalRoot)CloseModal();else if(surface)LeaveCanvas();}
        }
        void OnApplicationPause(bool paused){backgrounded=paused;if(paused&&surface){surface.FinishStroke();SavePainting();}}
        void OnApplicationFocus(bool focused){backgrounded=!focused;if(!focused&&surface){surface.FinishStroke();SavePainting();}}
        void OnApplicationQuit(){backgrounded=true;if(surface){surface.FinishStroke();SavePainting();}}
        void OnLowMemory()
        {
            if(!surface)return;
            foreach(var texture in previews.Values)if(texture)Destroy(texture);previews.Clear();previewsPurged=true;
            // Active paint and undo are never discarded. Gallery previews are disposable.
        }
        RectTransform Modal(string title,string body)
        {
            CloseModal();modalRoot=KidoUI.Panel("Dialog shade",root,new Color(.1f,.14f,.24f,.46f),false);KidoUI.Place(modalRoot,0,0,1,1);modalRoot.GetComponent<Image>().raycastTarget=true;
            var box=KidoUI.Panel("Dialog",modalRoot,KidoUI.Cream);KidoUI.Place(box,.22f,.16f,.78f,.84f);
            var heading=KidoUI.Label(title,box,38,KidoUI.Ink,TextAnchor.MiddleCenter);heading.name="Dialog heading";heading.fontStyle=FontStyle.Bold;KidoUI.Place(heading.rectTransform,.06f,.73f,.94f,.92f);
            var message=KidoUI.Label(body,box,24,KidoUI.Muted,TextAnchor.MiddleCenter);message.name="Dialog body";KidoUI.Place(message.rectTransform,.08f,.39f,.92f,.72f);return box;
        }
        void CloseModal(){if(modalRoot){modalRoot.gameObject.SetActive(false);Destroy(modalRoot.gameObject);modalRoot=null;}}
        void Message(string title,string body)
        {var box=Modal(title,body);var ok=KidoUI.Button("Okay",box,KidoUI.Purple,CloseModal,26,Color.white);KidoUI.Place((RectTransform)ok.transform,.27f,.12f,.73f,.28f);}
        void Confirm(string title,string body,string no,string yes,Action action)
        {
            var box=Modal(title,body);var cancel=KidoUI.Button(no,box,Color.white,CloseModal,23);KidoUI.Place((RectTransform)cancel.transform,.06f,.10f,.48f,.29f);
            var confirm=KidoUI.Button(yes,box,KidoUI.Accents[0],()=>{CloseModal();action();},23,Color.white);KidoUI.Place((RectTransform)confirm.transform,.52f,.10f,.94f,.29f);
        }
        void ParentGate()
        {
            if(surface)surface.FinishStroke();string[] words={"zero","one","two","three","four","five","six","seven","eight","nine"};int a=UnityEngine.Random.Range(1,10),b=UnityEngine.Random.Range(0,10),c=UnityEngine.Random.Range(0,10);string expected=$"{a}{b}{c}";
            var box=Modal("For grown-ups",$"Type these numbers in order:\n{words[a]} • {words[b]} • {words[c]}");
            var fieldRect=KidoUI.Panel("Answer",box,Color.white);KidoUI.Place(fieldRect,.33f,.30f,.67f,.43f);fieldRect.GetComponent<Image>().raycastTarget=true;
            var input=fieldRect.gameObject.AddComponent<InputField>();var text=KidoUI.Label("",fieldRect,30,KidoUI.Ink,TextAnchor.MiddleCenter);KidoUI.Place(text.rectTransform,0,0,1,1);input.textComponent=text;input.targetGraphic=fieldRect.GetComponent<Image>();input.contentType=InputField.ContentType.IntegerNumber;input.characterLimit=3;
            var cancel=KidoUI.Button("Back",box,Color.white,CancelParentGate,23);KidoUI.Place((RectTransform)cancel.transform,.08f,.09f,.46f,.25f);
            var go=KidoUI.Button("Continue",box,KidoUI.Purple,()=>{if(input.text==expected)ShowParents();else{input.text="";text.text="";}},23,Color.white);KidoUI.Place((RectTransform)go.transform,.54f,.09f,.92f,.25f);
        }
        void CancelParentGate(){requestedLockedCanvas=null;CloseModal();}
        void ShowParents()
        {
            string body=access.HasFullAccess?"All 30 coloring pictures are unlocked on this device.":"Nine pictures are free. A one-time purchase will unlock all 30 pictures. Version one contains no advertising.";
            var box=Modal("A little space for parents",body);
            var bodyLabel=box.Find("Dialog body") as RectTransform;if(bodyLabel)KidoUI.Place(bodyLabel,.08f,.59f,.92f,.72f);
            string buyLabel=access.HasFullAccess?"Everything unlocked ✓":purchases.PurchasesAvailable?"Unlock all • "+purchases.LocalizedPrice:"Store setup required";
            var buy=KidoUI.Button(buyLabel,box,KidoUI.Hex("56C870"),()=>BeginStoreAction(false),20,Color.white);buy.interactable=!access.HasFullAccess&&purchases.PurchasesAvailable;KidoUI.Place((RectTransform)buy.transform,.07f,.42f,.55f,.56f);
            var restore=KidoUI.Button("Restore purchase",box,Color.white,()=>BeginStoreAction(true),19);restore.interactable=!access.HasFullAccess&&purchases.PurchasesAvailable;KidoUI.Place((RectTransform)restore.transform,.59f,.42f,.93f,.56f);
            var sound=KidoUI.Button(feedback.Sound?"Sound: on":"Sound: off",box,Color.white,()=>{feedback.SetSound(!feedback.Sound);ShowParents();},20);KidoUI.Place((RectTransform)sound.transform,.07f,.24f,.47f,.37f);
            var motion=KidoUI.Button(feedback.Motion?"Motion: on":"Motion: off",box,Color.white,()=>{feedback.SetMotion(!feedback.Motion);if(surface)surface.AnimateGlitter=feedback.Motion;ShowParents();},20);KidoUI.Place((RectTransform)motion.transform,.53f,.24f,.93f,.37f);
            var privacy=KidoUI.Button("Privacy",box,Color.white,OpenPrivacy,20);privacy.interactable=Uri.TryCreate(settings.privacyPolicyUrl,UriKind.Absolute,out _);KidoUI.Place((RectTransform)privacy.transform,.07f,.07f,.47f,.20f);
            var close=KidoUI.Button("Back",box,KidoUI.Purple,CancelParentGate,21,Color.white);KidoUI.Place((RectTransform)close.transform,.53f,.07f,.93f,.20f);
        }
        void OpenPrivacy(){if(Uri.TryCreate(settings.privacyPolicyUrl,UriKind.Absolute,out var uri)&&(uri.Scheme==Uri.UriSchemeHttps))Application.OpenURL(uri.AbsoluteUri);}
        void BeginStoreAction(bool restore)
        {
            if(!purchases.PurchasesAvailable){Message("Store not ready","The store connection has not been configured yet.");return;}
            Action<StoreOutcome> finished=FinishStoreAction;
            if(restore)purchases.Restore(finished);else purchases.Purchase(finished);
        }
        void FinishStoreAction(StoreOutcome outcome)
        {
            if(outcome!=null&&outcome.GrantsAccess)
            {
                var requested=requestedLockedCanvas;requestedLockedCanvas=null;CloseModal();
                if(requested!=null&&access.CanOpen(requested.id)){StartCoroutine(FadeTo(()=>OpenCanvas(requested)));return;}
                Message("Everything is unlocked","All 30 coloring pictures are ready to enjoy.");return;
            }
            if(outcome!=null&&outcome.kind==StoreOutcomeKind.Cancelled){ShowParents();return;}
            if(outcome!=null&&outcome.kind==StoreOutcomeKind.Pending){Message("Purchase pending","Google Play is still processing this purchase. Access will unlock after the store confirms payment.");return;}
            Message("Store unavailable",string.IsNullOrWhiteSpace(outcome?.message)?"Please check the connection and try again.":outcome.message);
        }
        void OnDestroy(){Application.lowMemory-=OnLowMemory;backgrounded=true;if(pendingSave!=null)CompleteBackgroundSave(true);foreach(var texture in previews.Values)if(texture)Destroy(texture);foreach(var texture in uiTextures)if(texture)Resources.UnloadAsset(texture);}
    }
}
