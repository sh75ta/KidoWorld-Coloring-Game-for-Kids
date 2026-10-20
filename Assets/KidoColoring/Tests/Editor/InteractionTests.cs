using NUnit.Framework;
using UnityEngine;
using Kido.Coloring;

namespace Kido.Tests
{
    public sealed class InteractionTests
    {
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
        [Test] public void PageMappingPreservesArtworkAndBothWhiteMargins()
        {
            Assert.AreEqual(0,DrawingSurface.PageAxis(0,.2f,.8f),.00001f);
            Assert.AreEqual(1f/6,DrawingSurface.PageAxis(.2f,.2f,.8f),.00001f);
            Assert.AreEqual(.5f,DrawingSurface.PageAxis(.5f,.2f,.8f),.00001f);
            Assert.AreEqual(5f/6,DrawingSurface.PageAxis(.8f,.2f,.8f),.00001f);
            Assert.AreEqual(1,DrawingSurface.PageAxis(1,.2f,.8f),.00001f);
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
    }
}
