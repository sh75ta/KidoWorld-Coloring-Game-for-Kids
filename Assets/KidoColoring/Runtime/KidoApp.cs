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
        ContentCatalog catalog;GameSettings settings;AccessPolicy access;PaintStore store;
        readonly Dictionary<string,Texture2D> previews=new Dictionary<string,Texture2D>();
        readonly Dictionary<string,float> progress=new Dictionary<string,float>();
        readonly HashSet<string> unavailableSaves=new HashSet<string>();
        DrawingSurface surface;CanvasEntry active;
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
        sealed class SaveOutcome {public bool ok;public string error;}
        const int PreviewRevision=3;
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
                catalog=Content.Load();settings=Content.Settings();access=new AccessPolicy(settings);
                store=new PaintStore(Path.Combine(Application.persistentDataPath,"KidoPaintings-v2"));
                patternCatalog=JsonUtility.FromJson<PatternCatalog>(Resources.Load<TextAsset>("Kido/patterns").text);
            }
            catch(Exception e){failure=e;}
            if(failure!=null){ClearScreen();Message("Let's try again", "The coloring book couldn't open. Please restart the game.");Debug.LogException(failure);yield break;}
            yield return new WaitForSecondsRealtime(.7f);
            foreach(var entry in catalog.canvases)
            {
                LoadPreview(entry);SetLoadingProgress((Array.IndexOf(catalog.canvases,entry)+1f)/catalog.canvases.Length);yield return null;
            }
            float introLength=Mathf.Max(feedback.Motion?SplashLogoAnimator.Duration:0,feedback.IntroVoiceLength);
            while(Time.unscaledTime-introStarted<introLength)yield return null;
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
            patternTextures.Clear();patternSamples.Clear();colorPicker=null;patternPicker=null;colorLoop=null;patternLoop=null;
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
            if(!File.Exists(meta)||!File.Exists(png))return;
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
        public void ShowMenu()
        {
            ClearScreen();active=null;background.color=KidoUI.Hex("FFF2F6");
            screen.gameObject.AddComponent<RectMask2D>();
            var design=KidoUI.Rect("Menu prototype layout",screen);
            var gallery=design.gameObject.AddComponent<GalleryMenu>();
            gallery.Initialize(catalog,
                entry=>previews.TryGetValue(entry.id,out var preview)?preview:null,
                entry=>progress.TryGetValue(entry.id,out var amount)?amount:0,
                entry=>access.CanOpen(entry.id),
                entry=>{if(!transitioning&&!leaving)StartCoroutine(FadeTo(()=>OpenCanvas(entry)));},
                ParentGate,(position,world)=>{menuPosition=position;category=world;},menuPosition);
        }
        void OpenCanvas(CanvasEntry entry)
        {
            if(!access.CanOpen(entry.id)){ParentGate();return;}
            PaintDocument document;Texture2D art=null;TextAsset bytes=null;LoadResult loaded;
            try
            {
                art=Resources.Load<Texture2D>(entry.artPath);bytes=Resources.Load<TextAsset>(entry.regionPath);
                if(!art || !bytes)throw new InvalidDataException("Artwork is missing.");
                var originalMap=RegionMap.Decode(bytes.bytes);var map=originalMap.WithPageMargins();
                loaded=store.Load(entry.id+"_page",map.Width,map.Height,entry.artRevision);
                if(!loaded.filesExist)
                {
                    loaded=store.Load(entry.id,originalMap.Width,originalMap.Height,entry.artRevision);
                    if(loaded.painting!=null)loaded.painting=PaintStore.ExpandToPage(loaded.painting);
                }
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
            surface=image.gameObject.AddComponent<DrawingSurface>();surface.Initialize(document,art);surface.AnimateGlitter=feedback.Motion;surface.Changed+=OnPaintChanged;
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
                var mark=KidoUI.Label("✓",swatch.transform,32,Color.white,TextAnchor.MiddleCenter);mark.name="Selected";KidoUI.Place(mark.rectTransform,0,0,1,1);mark.gameObject.SetActive(false);
            }
            patternPicker=PickerContent(palette,"15 patterns",15);
            for(int i=0;i<patternCatalog.patterns.Length;i++)
            {
                int index=i;var entryPattern=patternCatalog.patterns[i];
                var texture=Resources.Load<Texture2D>(entryPattern.path);patternTextures.Add(texture);
                var button=KidoUI.Button("",patternPicker,Color.white,()=>SelectPattern(index));PlacePickerCell((RectTransform)button.transform,i);
                button.GetComponent<Image>().sprite=null;
                var pic=KidoUI.Picture(button.transform,texture);KidoUI.Place(pic.rectTransform,.08f,.08f,.92f,.92f);patternButtons.Add(button);
            }
            colorLoop=colorPicker.parent.GetComponent<LoopingPalette>();colorLoop.Configure(colorPicker,index=>SelectColor(index,false,false));
            patternLoop=patternPicker.parent.GetComponent<LoopingPalette>();patternLoop.Configure(patternPicker,index=>SelectPattern(index,false,false));
            patternPicker.parent.gameObject.SetActive(false);
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
                surface.SetArtworkRect(new Rect((paletteX-artSize)/2/paletteX,(h-artY-artSize)/h,artSize/paletteX,artSize/h));
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
                surface.SetCursor(tools[(int)surface.Tool].GetComponentInChildren<RawImage>());
                pickerCellHeight=h/8;
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
            for(int i=0;i<patternButtons.Count;i++)patternButtons[i].GetComponent<Image>().color=i==index?KidoUI.Purple:Color.white;
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
            surface.SetCursor(tools[(int)tool].GetComponentInChildren<RawImage>());
            for(int i=0;i<tools.Count;i++)
            {
                bool selected=i==(int)tool;
                var edge=tools[i].transform.Find("Circle border").GetComponent<Image>();edge.color=selected?KidoUI.Hex("8B42EA"):KidoUI.Hex("766779");
                edge.transform.Find("Tool face").GetComponent<Image>().color=selected?KidoUI.Hex("EBD8FF"):Color.white;
                tools[i].GetComponent<PressBounce>().BaseScale=selected?1.10f:.88f;
            }
        }
        void OnPaintChanged(){dirty=true;nextSave=Time.unscaledTime+1.5f;RefreshPaintStatus();}
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
        void BeginBackgroundSave()
        {
            if(pendingSave!=null||!surface||active==null||!dirty)return;
            savingRevision=surface.Document.Revision;
            var document=surface.Document;
            if(saveBuffer==null)saveBuffer=new SavedPainting{paint=new Color32[document.Paint.Length],effects=new byte[document.Effects.Length]};
            saveBuffer.id=active.id+"_page";saveBuffer.artRevision=active.artRevision;saveBuffer.width=document.Map.Width;saveBuffer.height=document.Map.Height;saveBuffer.coverage=document.Coverage;
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
            if(surface && savingRevision==surface.Document.Revision){dirty=false;UpdateSavedPreview();}
            return true;
        }
        void UpdateSavedPreview()
        {
            if(!surface||active==null)return;
            if(saveLabel)saveLabel.text="Saved ✓";progress[active.id]=surface.Document.Coverage;
            if(backgrounded){refreshPreview=true;return;}
            refreshPreview=false;Texture2D thumbnail=null;
            try
            {
                thumbnail=surface.Thumbnail();
                if(previews.TryGetValue(active.id,out var previous))Destroy(previous);
                previews[active.id]=thumbnail;
                File.WriteAllBytes(Path.Combine(store.Root,active.id+".preview.png"),thumbnail.EncodeToPNG());
                File.WriteAllText(Path.Combine(store.Root,active.id+".preview.json"),JsonUtility.ToJson(new PreviewInfo{artRevision=active.artRevision,previewRevision=PreviewRevision,coverage=surface.Document.Coverage}));
            }
            catch(Exception e){Debug.LogWarning("Preview cache: "+e.Message);}
            finally{if(thumbnail)thumbnail.Apply(false,true);}
        }
        bool SavePainting()
        {
            if(!surface||active==null)return true;surface.FinishStroke();
            if(!CompleteBackgroundSave(true))return false;
            if(!dirty)
            {
                if(refreshPreview||!previews.ContainsKey(active.id)||!progress.TryGetValue(active.id,out var amount)||Mathf.Abs(amount-surface.Document.Coverage)>.0001f)UpdateSavedPreview();
                return true;
            }
            BeginBackgroundSave();return CompleteBackgroundSave(true);
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
            var group=screen.GetComponent<CanvasGroup>()??screen.gameObject.AddComponent<CanvasGroup>();group.blocksRaycasts=false;
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
                if(refreshPreview||!previews.ContainsKey(active.id)||!progress.ContainsKey(active.id))UpdateSavedPreview();
                feedback.Celebrate(screen);yield return new WaitForSecondsRealtime(feedback.Motion?1.25f:.2f);
                yield return FadeToMenu();
            }
            finally{if(group&&screen==leavingScreen)group.blocksRaycasts=true;leaving=false;}
        }
        void Update()
        {
            if(resetHint&&resetHint.gameObject.activeSelf&&Time.unscaledTime-lastResetTap>3)resetHint.gameObject.SetActive(false);
            if(refreshPreview&&!backgrounded&&surface&&!surface.Document.ActionOpen)UpdateSavedPreview();
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
            foreach(var texture in previews.Values)if(texture)Destroy(texture);previews.Clear();
            // Active paint and undo are never discarded. Gallery previews are disposable.
        }
        RectTransform Modal(string title,string body)
        {
            CloseModal();modalRoot=KidoUI.Panel("Dialog shade",root,new Color(.1f,.14f,.24f,.46f),false);KidoUI.Place(modalRoot,0,0,1,1);modalRoot.GetComponent<Image>().raycastTarget=true;
            var box=KidoUI.Panel("Dialog",modalRoot,KidoUI.Cream);KidoUI.Place(box,.22f,.16f,.78f,.84f);
            var heading=KidoUI.Label(title,box,38,KidoUI.Ink,TextAnchor.MiddleCenter);heading.fontStyle=FontStyle.Bold;KidoUI.Place(heading.rectTransform,.06f,.73f,.94f,.92f);
            var message=KidoUI.Label(body,box,24,KidoUI.Muted,TextAnchor.MiddleCenter);KidoUI.Place(message.rectTransform,.08f,.39f,.92f,.72f);return box;
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
            var cancel=KidoUI.Button("Back",box,Color.white,CloseModal,23);KidoUI.Place((RectTransform)cancel.transform,.08f,.09f,.46f,.25f);
            var go=KidoUI.Button("Continue",box,KidoUI.Purple,()=>{if(input.text==expected)ShowParents();else{input.text="";text.text="";}},23,Color.white);KidoUI.Place((RectTransform)go.transform,.54f,.09f,.92f,.25f);
        }
        void ShowParents()
        {
            var box=Modal("A little space for parents","This first-play version opens all 30 pictures. Purchases and advertising are not available yet.");
            var sound=KidoUI.Button(feedback.Sound?"Sound: on":"Sound: off",box,Color.white,()=>{feedback.SetSound(!feedback.Sound);ShowParents();},22);KidoUI.Place((RectTransform)sound.transform,.07f,.27f,.47f,.40f);
            var motion=KidoUI.Button(feedback.Motion?"Motion: on":"Motion: off",box,Color.white,()=>{feedback.SetMotion(!feedback.Motion);if(surface)surface.AnimateGlitter=feedback.Motion;ShowParents();},21);KidoUI.Place((RectTransform)motion.transform,.53f,.27f,.93f,.40f);
            var close=KidoUI.Button("Back to coloring",box,KidoUI.Purple,CloseModal,24,Color.white);KidoUI.Place((RectTransform)close.transform,.20f,.07f,.80f,.22f);
        }
        void OnDestroy(){Application.lowMemory-=OnLowMemory;backgrounded=true;if(pendingSave!=null)CompleteBackgroundSave(true);foreach(var texture in previews.Values)if(texture)Destroy(texture);foreach(var texture in uiTextures)if(texture)Resources.UnloadAsset(texture);}
    }
}
