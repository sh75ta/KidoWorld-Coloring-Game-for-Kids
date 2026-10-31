using NUnit.Framework;
using UnityEngine;
using Kido.Coloring;

namespace Kido.Tests
{
    public sealed class InteractionTests
    {
        sealed class FakeEntitlement:IEntitlementSource
        {public bool HasFullAccess{get;set;}public bool PurchasesAvailable{get;set;}}
        // Behaviours do not run in EditMode; drive the palette's Update directly.
        static void Tick(LoopingPalette loop)=>typeof(LoopingPalette).GetMethod("Update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(loop,null);
        [Test] public void DisablingPaletteFinishesTappedSelectionAndRestoresCenteredCell()
        {
            var root=new GameObject("Palette regression",typeof(RectTransform));
            try
            {
                var viewport=(RectTransform)root.transform;viewport.sizeDelta=new Vector2(100,800);
                var content=KidoUI.Rect("content",viewport);KidoUI.Place(content,0,0,1,1);
                for(int i=0;i<30;i++)KidoUI.Rect("cell",content);
                int selected=-1;var loop=root.AddComponent<LoopingPalette>();loop.Configure(content,i=>selected=i);
                loop.Select(29,false);Assert.AreEqual(29,selected);
                loop.Select(4,true);loop.SetInputEnabled(false);
                Tick(loop);Assert.AreEqual(29,selected);
                root.SetActive(false);root.SetActive(true);loop.SetInputEnabled(true);
                Assert.AreEqual(0,((RectTransform)content.GetChild(4)).anchoredPosition.y,.001f);
                Assert.AreEqual(4,loop.SelectedIndex);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void TappedEntryIsNotReplacedByEntriesPassingTheCentre()
        {
            var root=new GameObject("Palette tap",typeof(RectTransform));
            try
            {
                var viewport=(RectTransform)root.transform;viewport.sizeDelta=new Vector2(100,800);
                var content=KidoUI.Rect("content",viewport);KidoUI.Place(content,0,0,1,1);
                for(int i=0;i<30;i++)KidoUI.Rect("cell",content);
                var reported=new System.Collections.Generic.List<int>();
                var loop=root.AddComponent<LoopingPalette>();loop.Configure(content,i=>reported.Add(i));
                loop.Select(0,false);reported.Clear();
                loop.Select(12,true);
                for(int i=0;i<5;i++)Tick(loop);
                Assert.IsEmpty(reported);Assert.AreEqual(12,loop.SelectedIndex);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(1600,900)] [TestCase(1422,1067)] [TestCase(2000,900)]
        public void EveryTabDestinationReachesItsOwnWorld(float width,float height)
        {
            bool tablet=width/height<1.55f;
            float header=height*(tablet ? .315f : .27f);
            float cell=(height-header-43-28)/2;
            float stride=GalleryMenu.SectionStride(cell,28);
            for(int i=0;i<4;i++)Assert.AreEqual(i,GalleryMenu.CategoryAt(GalleryMenu.Destination(i,stride),width-32,stride));
            Assert.AreEqual(3,GalleryMenu.CategoryAt(3*stride,width-32,stride));
        }
        [Test] public void PaletteWrapUsesShortestPathAndNeverAnInvalidIndex()
        {
            Assert.AreEqual(-1,LoopingPalette.WrappedDelta(29,30));
            Assert.AreEqual(1,LoopingPalette.WrappedDelta(-29,30));
            Assert.AreEqual(29,LoopingPalette.WrappedIndex(-1,30));
            Assert.AreEqual(0,LoopingPalette.WrappedIndex(30,30));
            Assert.AreEqual(14,LoopingPalette.WrappedIndex(-31,15));
        }
        [Test] public void PageMappingIsUniformSoGutterStrokesStayRound()
        {
            var map=new RegionMap(4,4,1,new ushort[]{1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1}).WithPage(8,6);
            var host=new GameObject("Surface",typeof(RectTransform),typeof(UnityEngine.UI.RawImage));
            try
            {
                var surface=host.AddComponent<DrawingSurface>();
                surface.Initialize(new PaintDocument(map),null,4,4);
                surface.SetArtworkLayout(new Vector2(100,80),new Rect(30,20,40,40));
                Assert.AreEqual(new Vector2(2,1),surface.ToPage(new Vector2(30,20)));
                Assert.AreEqual(new Vector2(6,5),surface.ToPage(new Vector2(70,60)));
                var step=surface.ToPage(new Vector2(40,30))-surface.ToPage(new Vector2(30,20));
                Assert.AreEqual(1,step.x,.0001f);Assert.AreEqual(1,step.y,.0001f);
                var corner=surface.ToPage(new Vector2(-500,-500));
                Assert.AreEqual(0,corner.x);Assert.AreEqual(0,corner.y);
            }
            finally{Object.DestroyImmediate(host);}
        }
        [Test] public void PatternToolFootprintIsDoubleTheBrush()
        {
            Assert.AreEqual(DrawingSurface.BrushRadius*2,DrawingSurface.RadiusFor(PaintTool.Pattern));
            Assert.AreEqual(DrawingSurface.BrushRadius,DrawingSurface.RadiusFor(PaintTool.Brush));
            Assert.AreEqual(DrawingSurface.BrushRadius,DrawingSurface.RadiusFor(PaintTool.Glitter));
            Assert.LessOrEqual(DrawingSurface.PatternRadius,PaintDocument.MaxRadius);
        }
        [Test] public void LegacyPageSaveIsReframedIntoTheWiderPageWithoutTouchingTheFile()
        {
            string dir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"kido-test-"+System.Guid.NewGuid().ToString("N"));
            try
            {
                var store=new PaintStore(dir);int legacy=2+512;
                var item=new SavedPainting{id="art_page",width=legacy,height=legacy,artRevision=2,coverage=.5f,
                    paint=new Color32[legacy*legacy],effects=new byte[legacy*legacy]};
                item.paint[0]=Color.red;item.effects[0]=255;
                Assert.IsTrue(store.TrySaveSnapshot(item,out var error),error);
                var loaded=store.LoadPage("art",2,2,2);
                Assert.IsNotNull(loaded.painting);Assert.AreEqual(RegionMap.PageWidth,loaded.painting.width);
                Assert.AreEqual("art_page2",loaded.painting.id);
                int index=((RegionMap.PageHeight-legacy)/2)*RegionMap.PageWidth+(RegionMap.PageWidth-legacy)/2;
                Assert.AreEqual((Color32)Color.red,loaded.painting.paint[index]);Assert.AreEqual(255,loaded.painting.effects[index]);
                Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(dir,"art_page.a.kido")));
            }
            finally{if(System.IO.Directory.Exists(dir))System.IO.Directory.Delete(dir,true);}
        }
        [Test] public void PageMigrationKeepsPaintGlitterAndOriginalBuffers()
        {
            var original=new SavedPainting{id="old",width=2,height=2,artRevision=2,
                paint=new Color32[]{Color.red,Color.blue,Color.green,Color.white},effects=new byte[]{255,0,0,0},coverage=1};
            var page=PaintStore.ExpandToPage(original,1);
            Assert.AreEqual("old_page",page.id);Assert.AreEqual(4,page.width);Assert.AreEqual(.25f,page.coverage);
            Assert.AreEqual((Color32)Color.red,page.paint[5]);Assert.AreEqual((Color32)Color.white,page.paint[10]);
            Assert.AreEqual(255,page.effects[5]);Assert.AreEqual(0,page.paint[0].a);
            page.paint[5]=Color.black;Assert.AreEqual((Color32)Color.red,original.paint[0]);
            Assert.AreEqual("old",original.id);
        }
        [Test] public void ExtendedPageBackgroundDoesNotFillInteriorRegions()
        {
            var map=new RegionMap(2,2,1,new ushort[]{0,1,1,0}).WithPageMargins(1);
            var document=new PaintDocument(map);document.BeginAction();document.Fill(0,Color.blue);document.EndAction();
            Assert.AreEqual(14,document.PaintedPixels);Assert.AreEqual(0,document.Paint[6].a);
            Assert.IsTrue(document.Undo());Assert.AreEqual(0,document.PaintedPixels);
        }
        [Test] public void PatternsKeepSourceColorRegardlessOfSelectedSwatch()
        {
            var texture=new Texture2D(1,1);texture.SetPixel(0,0,Color.green);texture.Apply();
            try
            {
                var pattern=new PatternData(texture,true);
                Assert.AreEqual((Color32)Color.green,pattern.Sample(0,0,Color.red));
                Assert.AreEqual((Color32)Color.green,pattern.Sample(0,0,Color.blue));
            }
            finally{Object.DestroyImmediate(texture);}
        }
        [Test] public void AccessPolicyUnlocksOnlyFreePicturesUntilOwnershipIsVerified()
        {
            var settings=new GameSettings{previewAllCanvases=false,freeCanvasIds=new[]{"free"}};
            var entitlement=new FakeEntitlement{HasFullAccess=false,PurchasesAvailable=true};
            var policy=new AccessPolicy(settings,entitlement);
            Assert.IsTrue(policy.CanOpen("free"));Assert.IsFalse(policy.CanOpen("locked"));
            Assert.IsTrue(policy.PurchasesAvailable);Assert.IsFalse(policy.AdsAvailable);
            entitlement.HasFullAccess=true;
            Assert.IsTrue(policy.CanOpen("locked"));Assert.IsTrue(policy.HasFullAccess);
        }
        [Test] public void PreviewModeDoesNotPretendThatPurchasesOrAdsExist()
        {
            var policy=new AccessPolicy(new GameSettings{previewAllCanvases=true});
            Assert.IsTrue(policy.CanOpen("anything"));Assert.IsFalse(policy.PurchasesAvailable);Assert.IsFalse(policy.AdsAvailable);
        }
    }
}
