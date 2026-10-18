using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        ScrollRect menuScroll;Button[] tabs;Text menuHeadline,saveLabel,progressLabel;
        Button undoButton;readonly List<Button> tools=new List<Button>(),swatches=new List<Button>();
        int category;float menuPosition,nextSave;bool dirty,completed;
        Coroutine scrollTween;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if(SceneManager.GetActiveScene().name!="KidoGame")return;
            var obj=new GameObject("Kido Coloring");obj.AddComponent<KidoApp>();
        }
        IEnumerator Start()
        {
            Application.targetFrameRate=60;
            Screen.orientation=ScreenOrientation.AutoRotation;
            Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
            Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
            feedback=gameObject.AddComponent<Feedback>();
            if(!FindAnyObjectByType<AudioListener>())gameObject.AddComponent<AudioListener>();
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
            ShowSplash();yield return null;
            Exception failure=null;
            try
            {
                catalog=Content.Load();settings=Content.Settings();access=new AccessPolicy(settings);
                store=new PaintStore(Path.Combine(Application.persistentDataPath,"KidoPaintings"));
            }
            catch(Exception e){failure=e;}
            if(failure!=null){ClearScreen();Message("Let's try again", "The coloring book couldn't open. Please restart the game.");Debug.LogException(failure);yield break;}
            yield return new WaitForSecondsRealtime(.7f);
            foreach(var entry in catalog.canvases)
            {
                LoadPreview(entry);yield return null;
            }
            ShowMenu();
        }
        void ClearScreen()
        {
            CloseModal();if(scrollTween!=null){StopCoroutine(scrollTween);scrollTween=null;}
            surface=null;tools.Clear();swatches.Clear();menuScroll=null;
            if(screen){screen.gameObject.SetActive(false);Destroy(screen.gameObject);}
            screen=KidoUI.Rect("Screen",root);KidoUI.Place(screen,0,0,1,1);
        }
        void Brand(Transform parent)
        {
            var logo=KidoUI.Label("<b><color=#F06474>k</color><color=#E7A63C>i</color><color=#41BAA3>d</color><color=#7661D5>o</color></b>",parent,54,KidoUI.Ink);
            KidoUI.Place(logo.rectTransform,.04f,.88f,.23f,.98f);
            var sub=KidoUI.Label("C O L O R I N G",parent,16,KidoUI.Muted);KidoUI.Place(sub.rectTransform,.043f,.85f,.23f,.89f);
        }
        void ShowSplash()
        {
            ClearScreen();background.color=KidoUI.Cream;
            var title=KidoUI.Label("<b><color=#F06474>k</color><color=#E7A63C>i</color><color=#41BAA3>d</color><color=#7661D5>o</color></b>",screen,112,KidoUI.Ink,TextAnchor.MiddleCenter);KidoUI.Place(title.rectTransform,.2f,.45f,.8f,.67f);
            var subtitle=KidoUI.Label("A little color. A big imagination.",screen,28,KidoUI.Muted,TextAnchor.MiddleCenter);KidoUI.Place(subtitle.rectTransform,.2f,.35f,.8f,.44f);
            var dots=KidoUI.Label("•  •  •",screen,46,KidoUI.Purple,TextAnchor.MiddleCenter);KidoUI.Place(dots.rectTransform,.4f,.24f,.6f,.34f);StartCoroutine(Pulse(dots));
        }
        IEnumerator Pulse(Text dots)
        {while(dots){dots.transform.localScale=Vector3.one*(1+Mathf.Sin(Time.unscaledTime*4)*.08f);yield return null;}}
        void LoadPreview(CanvasEntry entry)
        {
            var loaded=store.Load(entry.id,512,512,entry.artRevision);
            if(loaded.painting==null){if(loaded.filesExist)unavailableSaves.Add(entry.id);return;}
            progress[entry.id]=loaded.painting.coverage;
            var source=Resources.Load<Texture2D>(entry.thumbPath);if(!source)return;
            var sourcePixels=source.GetPixels32();int size=source.width;var output=new Color32[size*size];var paint=loaded.painting.paint;
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var c=paint[(y*512/size)*512+x*512/size];var line=sourcePixels[y*size+x];
                output[y*size+x]=c.a==0?line:new Color32((byte)(line.r*c.r/255),(byte)(line.g*c.g/255),(byte)(line.b*c.b/255),255);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGB24,false);texture.SetPixels32(output);texture.Apply();previews[entry.id]=texture;
        }
        public void ShowMenu()
        {
            ClearScreen();active=null;Brand(screen);background.color=KidoUI.Backgrounds[category];
            var parents=KidoUI.Button("Parents",screen,Color.white,ParentGate,23);KidoUI.Place((RectTransform)parents.transform,.85f,.88f,.96f,.96f);
            var nav=KidoUI.Panel("World tabs",screen,Color.white);KidoUI.Place(nav,.30f,.87f,.80f,.97f,4);tabs=new Button[3];
            for(int i=0;i<3;i++){int c=i;tabs[i]=KidoUI.Button(Content.Categories[i],nav,Color.white,()=>{feedback.Tap();JumpToCategory(c);},26);KidoUI.Place((RectTransform)tabs[i].transform,i/3f,0,(i+1)/3f,1,6);}
            var kicker=KidoUI.Label("YOUR LITTLE COLORING CLUB",screen,18,KidoUI.Accents[category]);KidoUI.Place(kicker.rectTransform,.045f,.765f,.75f,.81f);
            menuHeadline=KidoUI.Label(Content.Headlines[category],screen,46,KidoUI.Ink);menuHeadline.fontStyle=FontStyle.Bold;KidoUI.Place(menuHeadline.rectTransform,.043f,.69f,.85f,.77f);
            var subtitle=KidoUI.Label("Pick a picture and make it yours.",screen,24,KidoUI.Muted);KidoUI.Place(subtitle.rectTransform,.045f,.635f,.78f,.69f);
            var viewport=KidoUI.Panel("Pictures viewport",screen,new Color(1,1,1,.01f),false);KidoUI.Place(viewport,.035f,.095f,.965f,.625f);
            viewport.GetComponent<Image>().raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.decelerationRate=.08f;scroll.viewport=viewport;menuScroll=scroll;
            var content=KidoUI.Rect("All 30 pictures",viewport);scroll.content=content;content.anchorMin=new Vector2(0,0);content.anchorMax=new Vector2(0,1);content.pivot=new Vector2(0,.5f);
            Canvas.ForceUpdateCanvases();float gap=18,cellWidth=Mathf.Clamp(viewport.rect.width/4.25f,220,320),cellHeight=(viewport.rect.height-gap-8)/2;
            content.sizeDelta=new Vector2(15*(cellWidth+gap)-gap,0);content.anchoredPosition=Vector2.zero;
            for(int i=0;i<catalog.canvases.Length;i++)
            {
                var entry=catalog.canvases[i];int local=i%10,col=entry.category*5+local/2,row=local%2;
                var card=KidoUI.Button("",content,Color.white,()=>OpenCanvas(entry));var cr=(RectTransform)card.transform;
                cr.anchorMin=cr.anchorMax=new Vector2(0,1);cr.pivot=new Vector2(0,1);cr.sizeDelta=new Vector2(cellWidth,cellHeight);cr.anchoredPosition=new Vector2(col*(cellWidth+gap),-row*(cellHeight+gap)-4);
                var pictureHolder=KidoUI.Rect("Art",cr);KidoUI.Place(pictureHolder,.07f,.24f,.93f,.97f);
                var pic=KidoUI.Picture(pictureHolder,previews.TryGetValue(entry.id,out var preview)?preview:Resources.Load<Texture2D>(entry.thumbPath));KidoUI.Place(pic.rectTransform,0,0,1,1);KidoUI.Square(pic.rectTransform);
                var label=KidoUI.Label(ShortTitle(entry.title),cr,20,KidoUI.Ink,TextAnchor.MiddleCenter);KidoUI.Place(label.rectTransform,.04f,.065f,.96f,.24f);
                float amount=progress.TryGetValue(entry.id,out var value)?value:0;
                var track=KidoUI.Panel("Progress",cr,KidoUI.Hex("F0EFF5"));KidoUI.Place(track,.10f,.045f,.90f,.064f);
                if(amount>0){var fill=KidoUI.Panel("Colored",track,KidoUI.Accents[entry.category]);KidoUI.Place(fill,0,0,amount,1);}
                if(amount>=settings.completionThreshold){var badge=KidoUI.Label("★",cr,27,KidoUI.Accents[entry.category],TextAnchor.MiddleCenter);KidoUI.Place(badge.rectTransform,.79f,.77f,.98f,.99f);}
                if(!access.CanOpen(entry.id)){var lockLabel=KidoUI.Label("Parents",cr,18,KidoUI.Purple,TextAnchor.MiddleCenter);KidoUI.Place(lockLabel.rectTransform,.60f,.79f,.98f,.98f);}
                if(unavailableSaves.Contains(entry.id)){var warn=KidoUI.Label("Saved picture needs help",cr,15,KidoUI.Accents[0],TextAnchor.MiddleCenter);KidoUI.Place(warn.rectTransform,.05f,.02f,.95f,.10f);}
            }
            var footer=KidoUI.Label("30 pictures. Endless possibilities.",screen,20,KidoUI.Muted);KidoUI.Place(footer.rectTransform,.045f,.025f,.65f,.075f);
            var swipe=KidoUI.Label("Swipe to explore  →",screen,21,KidoUI.Muted,TextAnchor.MiddleRight);KidoUI.Place(swipe.rectTransform,.70f,.025f,.955f,.075f);
            scroll.horizontalNormalizedPosition=menuPosition;
            scroll.onValueChanged.AddListener(_=>
            {
                menuPosition=scroll.horizontalNormalizedPosition;
                float center=-content.anchoredPosition.x+viewport.rect.width*.5f;
                int next=Mathf.Clamp(Mathf.FloorToInt(center/(5*(cellWidth+gap))),0,2);
                SetCategory(next);
            });
            SetCategory(category);
        }
        static string ShortTitle(string title)
        {
            return title.Replace(" with a Fluffy Mane","").Replace(" with a Big Nose","").Replace(" with His Gift Sack","").Replace(" Above the Clouds","").Replace(" to the Rescue","").Replace(" in a Yarn Basket","").Replace(" on a Tropical Branch","").Replace(" Under the Moon","").Replace(" in the Garden","");
        }
        void SetCategory(int c)
        {
            category=c;background.color=KidoUI.Backgrounds[c];if(menuHeadline)menuHeadline.text=Content.Headlines[c];
            if(tabs==null)return;for(int i=0;i<tabs.Length;i++)if(tabs[i]){tabs[i].GetComponent<Image>().color=i==c?KidoUI.Accents[i]:Color.white;tabs[i].GetComponentInChildren<Text>().color=i==c?Color.white:KidoUI.Muted;}
        }
        void JumpToCategory(int c)
        {
            if(!menuScroll)return;if(scrollTween!=null)StopCoroutine(scrollTween);
            float width=menuScroll.content.rect.width,view=menuScroll.viewport.rect.width;
            float destination=Mathf.Clamp01((c*(width+18)/3)/(width-view));
            scrollTween=StartCoroutine(ScrollTo(destination));SetCategory(c);
        }
        IEnumerator ScrollTo(float target)
        {float from=menuScroll.horizontalNormalizedPosition,t=0;menuScroll.StopMovement();while(t<1 && menuScroll){t+=Time.unscaledDeltaTime*4;menuScroll.horizontalNormalizedPosition=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,t));yield return null;}scrollTween=null;}
        void OpenCanvas(CanvasEntry entry)
        {
            if(!access.CanOpen(entry.id)){ParentGate();return;}
            feedback.Tap();PaintDocument document;Texture2D art;LoadResult loaded;
            try
            {
                art=Resources.Load<Texture2D>(entry.artPath);var bytes=Resources.Load<TextAsset>(entry.regionPath);
                if(!art || !bytes)throw new InvalidDataException("Artwork is missing.");
                var map=RegionMap.Decode(bytes.bytes);loaded=store.Load(entry.id,map.Width,map.Height,entry.artRevision);
                if(loaded.filesExist && loaded.painting==null){Message("Your picture is safe", "We couldn't open the saved colors. The saved files have been kept. Please ask a grown-up for help.");return;}
                document=new PaintDocument(map,loaded.painting?.paint);
            }
            catch(Exception e){Debug.LogException(e);Message("Let's try another picture","This picture couldn't open. Your other pictures are still here.");return;}
            ClearScreen();active=entry;dirty=false;completed=document.Coverage>=settings.completionThreshold;background.color=KidoUI.Backgrounds[entry.category];
            var back=KidoUI.Button("‹  Pictures",screen,Color.white,LeaveCanvas,24);KidoUI.Place((RectTransform)back.transform,.025f,.89f,.18f,.975f);
            var name=KidoUI.Label(ShortTitle(entry.title),screen,34,KidoUI.Ink);name.fontStyle=FontStyle.Bold;KidoUI.Place(name.rectTransform,.205f,.90f,.74f,.97f);
            var done=KidoUI.Button("Done  ✓",screen,KidoUI.Accents[entry.category],LeaveCanvas,27,Color.white);KidoUI.Place((RectTransform)done.transform,.785f,.89f,.975f,.975f);
            var board=KidoUI.Panel("Paper",screen,Color.white);KidoUI.Place(board,.025f,.095f,.755f,.865f);
            var artHolder=KidoUI.Rect("Artwork bounds",board);KidoUI.Place(artHolder,.035f,.035f,.965f,.965f);
            var image=KidoUI.Picture(artHolder,art);KidoUI.Place(image.rectTransform,0,0,1,1);KidoUI.Square(image.rectTransform);
            surface=image.gameObject.AddComponent<DrawingSurface>();surface.Initialize(document,art);surface.Changed+=OnPaintChanged;
            var rail=KidoUI.Panel("Creative tools",screen,Color.white);KidoUI.Place(rail,.775f,.095f,.975f,.865f);
            var colorsTitle=KidoUI.Label("YOUR COLORS",rail,17,KidoUI.Muted);KidoUI.Place(colorsTitle.rectTransform,.09f,.935f,.92f,.985f);
            for(int i=0;i<12;i++)
            {
                int index=i;var c=KidoUI.Hex(KidoUI.HexColors[i]);var swatch=KidoUI.Button("",rail,c,()=>SelectColor(index));
                float x=.075f+(i%4)*.22f,y=.87f-(i/4)*.092f;
                KidoUI.Place((RectTransform)swatch.transform,x,y-.065f,x+.19f,y+.013f);swatches.Add(swatch);
                if(i==11){var border=swatch.gameObject.AddComponent<Outline>();border.effectColor=KidoUI.Hex("DDDDEA");border.effectDistance=new Vector2(1,-1);}
            }
            var toolTitle=KidoUI.Label("MAKE YOUR MARK",rail,17,KidoUI.Muted);KidoUI.Place(toolTitle.rectTransform,.09f,.575f,.94f,.63f);
            string[] names={"Tap fill","Brush","Eraser","Glitter","Pattern","Rainbow"};
            for(int i=0;i<6;i++)
            {
                int tool=i;var b=KidoUI.Button("",rail,KidoUI.Hex("F7F5FC"),()=>SelectTool((PaintTool)tool));
                float x=.065f+(i%2)*.46f,y=.545f-(i/2)*.156f;KidoUI.Place((RectTransform)b.transform,x,y-.125f,x+.41f,y+.009f);tools.Add(b);
                var glyphRect=KidoUI.Rect("Tool icon",b.transform);KidoUI.Place(glyphRect,.29f,.34f,.71f,.95f);var glyph=glyphRect.gameObject.AddComponent<ToolGlyph>();glyph.tool=(PaintTool)i;glyph.color=KidoUI.Purple;glyph.raycastTarget=false;
                var label=KidoUI.Label(names[i],b.transform,18,KidoUI.Ink,TextAnchor.MiddleCenter);KidoUI.Place(label.rectTransform,.02f,.02f,.98f,.34f);
            }
            undoButton=KidoUI.Button("↶ Undo",rail,KidoUI.Hex("F0EDF8"),Undo,20);KidoUI.Place((RectTransform)undoButton.transform,.06f,.025f,.48f,.105f);
            var reset=KidoUI.Button("Reset",rail,KidoUI.Hex("FFF0EE"),ConfirmReset,20);KidoUI.Place((RectTransform)reset.transform,.52f,.025f,.94f,.105f);
            saveLabel=KidoUI.Label("Your colors save as you play",screen,20,KidoUI.Muted);KidoUI.Place(saveLabel.rectTransform,.035f,.022f,.70f,.075f);
            progressLabel=KidoUI.Label("",screen,20,KidoUI.Muted,TextAnchor.MiddleRight);KidoUI.Place(progressLabel.rectTransform,.77f,.022f,.97f,.075f);
            SelectColor(0,false);SelectTool(PaintTool.Fill,false);RefreshPaintStatus();
            if(loaded.recovered)Message("Welcome back", "We recovered the last safely saved version of your picture.");
        }
        void SelectColor(int index,bool sound=true)
        {
            if(!surface)return;surface.FinishStroke();surface.SelectedColor=KidoUI.Hex(KidoUI.HexColors[index]);if(sound)feedback.Tap();
            for(int i=0;i<swatches.Count;i++)
            {
                var old=swatches[i].transform.Find("Selected");if(old)Destroy(old.gameObject);
                if(i==index){var label=KidoUI.Label("✓",swatches[i].transform,28,index==11?KidoUI.Ink:Color.white,TextAnchor.MiddleCenter);label.name="Selected";KidoUI.Place(label.rectTransform,0,0,1,1);}
            }
        }
        void SelectTool(PaintTool tool,bool sound=true)
        {
            if(!surface)return;surface.FinishStroke();surface.Tool=tool;if(sound)feedback.Tap();
            for(int i=0;i<tools.Count;i++){tools[i].GetComponent<Image>().color=i==(int)tool?KidoUI.Hex("E8E0FF"):KidoUI.Hex("F7F5FC");}
        }
        void OnPaintChanged(){dirty=true;nextSave=Time.unscaledTime+.65f;feedback.Tap();RefreshPaintStatus();}
        void RefreshPaintStatus()
        {
            if(!surface)return;undoButton.interactable=surface.Document.CanUndo;
            bool nowComplete=surface.Document.Coverage>=settings.completionThreshold;
            if(nowComplete && !completed)feedback.Celebrate(screen);completed=nowComplete;
            if(progressLabel)progressLabel.text=completed?"Beautifully yours!":surface.Document.PaintedPixels==0?"Let's add some color":"Looking lovely!";
        }
        void Undo(){if(!surface)return;surface.FinishStroke();if(surface.Document.Undo())OnPaintChanged();}
        void ConfirmReset()
        {
            if(!surface)return;surface.FinishStroke();
            Confirm("Start fresh?","This clears the colors on this picture. Your other pictures stay just as they are.","Keep coloring","Clear picture",()=>{surface.Document.Clear();OnPaintChanged();});
        }
        bool SavePainting()
        {
            if(!surface || active==null || !dirty)return true;
            surface.FinishStroke();
            if(!store.TrySave(active.id,active.artRevision,surface.Document,out var error))
            {saveLabel.text="Couldn't save yet — please keep this picture open";Debug.LogWarning(error);nextSave=Time.unscaledTime+5;return false;}
            dirty=false;saveLabel.text="Saved with love  ✓";progress[active.id]=surface.Document.Coverage;
            if(previews.TryGetValue(active.id,out var previous))Destroy(previous);previews[active.id]=surface.Thumbnail();return true;
        }
        void LeaveCanvas()
        {
            if(!surface)return;surface.FinishStroke();feedback.Tap();
            if(!SavePainting()){Message("Let's keep your colors","We couldn't save this picture yet. Please check available storage and try again. Your drawing is still open.");return;}
            ShowMenu();
        }
        void Update()
        {
            if(dirty && surface && !surface.Document.ActionOpen && Time.unscaledTime>=nextSave)SavePainting();
            if(Input.GetKeyDown(KeyCode.Escape)) {if(modalRoot)CloseModal();else if(surface)LeaveCanvas();}
        }
        void OnApplicationPause(bool paused){if(paused && surface){surface.FinishStroke();SavePainting();}}
        void OnApplicationFocus(bool focused){if(!focused && surface){surface.FinishStroke();SavePainting();}}
        void OnApplicationQuit(){if(surface){surface.FinishStroke();SavePainting();}}
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
            if(surface)surface.FinishStroke();feedback.Tap();string[] words={"zero","one","two","three","four","five","six","seven","eight","nine"};int a=UnityEngine.Random.Range(1,10),b=UnityEngine.Random.Range(0,10),c=UnityEngine.Random.Range(0,10);string expected=$"{a}{b}{c}";
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
            var motion=KidoUI.Button(feedback.Motion?"Celebrations: on":"Celebrations: off",box,Color.white,()=>{feedback.SetMotion(!feedback.Motion);ShowParents();},21);KidoUI.Place((RectTransform)motion.transform,.53f,.27f,.93f,.40f);
            var close=KidoUI.Button("Back to coloring",box,KidoUI.Purple,CloseModal,24,Color.white);KidoUI.Place((RectTransform)close.transform,.20f,.07f,.80f,.22f);
        }
        void OnDestroy(){foreach(var texture in previews.Values)if(texture)Destroy(texture);}
    }
}
