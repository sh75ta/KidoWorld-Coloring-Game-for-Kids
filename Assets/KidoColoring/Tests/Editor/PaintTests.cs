using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Kido.Coloring;

namespace Kido.Tests
{
    public sealed class PaintTests
    {
        RegionMap Map()=>new RegionMap(4,2,2,new ushort[]{0,1,1,0,0,2,2,0});
        [Test] public void FillIsBoundedAndUndoRestoresBlank()
        {
            var d=new PaintDocument(Map());d.BeginAction();d.Fill(1,new Color32(255,0,0,255));d.EndAction();
            Assert.AreEqual(2,d.PaintedPixels);Assert.AreEqual(0,d.Paint[5].a);Assert.AreEqual(.5f,d.Coverage);
            Assert.IsTrue(d.Undo());Assert.AreEqual(0,d.PaintedPixels);Assert.IsFalse(d.CanUndo);
        }
        [Test] public void BrushCannotCrossStartingRegion()
        {
            var d=new PaintDocument(Map());d.BeginAction();d.Stroke(new Vector2(1,0),new Vector2(2,1),10,1,Color.red,PaintTool.Brush,0);d.EndAction();
            Assert.AreEqual(2,d.PaintedPixels);Assert.AreEqual(0,d.Paint[5].a);Assert.AreEqual(0,d.Paint[0].a);
        }
        [Test] public void EntireStrokeIsOneUndoAction()
        {
            var d=new PaintDocument(Map());d.BeginAction();d.Stroke(Vector2.one,Vector2.one,3,2,Color.red,PaintTool.Brush,0);d.Stroke(Vector2.one,Vector2.one,3,2,Color.blue,PaintTool.Brush,0);d.EndAction();
            d.Undo();Assert.AreEqual(0,d.PaintedPixels);Assert.IsFalse(d.CanUndo);
        }
        [Test] public void ResetIsUndoableAndLeavesNoPaint()
        {
            var d=new PaintDocument(Map());d.BeginAction();d.Fill(1,Color.green);d.EndAction();d.Clear();Assert.AreEqual(0,d.PaintedPixels);
            d.Undo();Assert.AreEqual(2,d.PaintedPixels);Assert.AreEqual(255,d.Paint[1].g);
        }
        [Test] public void EraserPreservesOtherRegion()
        {
            var d=new PaintDocument(Map());d.BeginAction();d.Fill(1,Color.red);d.Fill(2,Color.blue);d.EndAction();
            d.BeginAction();d.Stroke(Vector2.zero,Vector2.one,20,1,Color.white,PaintTool.Eraser,0);d.EndAction();
            Assert.AreEqual(2,d.PaintedPixels);Assert.AreEqual(255,d.Paint[5].b);d.Undo();Assert.AreEqual(4,d.PaintedPixels);
        }
        [Test] public void SaveRoundTripPreservesActualColors()
        {
            var d=new PaintDocument(Map());d.BeginAction();d.Fill(2,Color.magenta);d.EndAction();
            var bytes=PaintStore.Encode(new SavedPainting{sequence=1,id="test",width=4,height=2,artRevision=1,coverage=d.Coverage,paint=d.Paint});
            var restored=PaintStore.Decode(bytes);Assert.AreEqual(.5f,restored.coverage);CollectionAssert.AreEqual(d.Paint,restored.paint);
        }
        [Test] public void CorruptionFailsChecksum()
        {
            var d=new PaintDocument(Map());var bytes=PaintStore.Encode(new SavedPainting{sequence=1,id="test",width=4,height=2,artRevision=1,paint=d.Paint});bytes[20]^=4;
            Assert.Throws<InvalidDataException>(()=>PaintStore.Decode(bytes));
        }
        [Test] public void TornNewestSlotRecoversPreviousPicture()
        {
            var path=Path.Combine(Path.GetTempPath(),"kido-"+Guid.NewGuid());
            try
            {
                var store=new PaintStore(path);var d=new PaintDocument(Map());d.BeginAction();d.Fill(1,Color.red);d.EndAction();
                Assert.IsTrue(store.TrySave("test",1,d,out _));d.BeginAction();d.Fill(2,Color.blue);d.EndAction();Assert.IsTrue(store.TrySave("test",1,d,out _));
                File.WriteAllBytes(Path.Combine(path,"test.b.kido"),new byte[]{1,2,3});
                var loaded=store.Load("test",4,2,1);Assert.IsTrue(loaded.recovered);Assert.AreEqual(1,loaded.painting.sequence);Assert.AreEqual(.5f,loaded.painting.coverage);
                Assert.IsTrue(store.TrySave("test",1,d,out _));Assert.AreEqual(2,store.Load("test").painting.sequence);
            }
            finally{if(Directory.Exists(path))Directory.Delete(path,true);}
        }
        [Test] public void IncompatibleArtworkRevisionIsPreserved()
        {
            var path=Path.Combine(Path.GetTempPath(),"kido-"+Guid.NewGuid());
            try
            {
                var store=new PaintStore(path);var d=new PaintDocument(Map());Assert.IsTrue(store.TrySave("test",1,d,out _));
                Assert.IsFalse(store.TrySave("test",2,d,out _));Assert.AreEqual(1,store.Load("test").painting.artRevision);
            }
            finally{if(Directory.Exists(path))Directory.Delete(path,true);}
        }
        [Test] public void PathTraversalIsRejected()
        {Assert.Throws<ArgumentException>(()=>new PaintStore("unused").Load("../escape"));}
        [Test] public void TruncatedRegionMapIsRejected()
        {Assert.Throws<InvalidDataException>(()=>RegionMap.Decode(new byte[20]));}
        [Test] public void CatalogHasThirtyUniqueCanvasesAndAssets()
        {
            var catalog=Content.Load();Assert.AreEqual(30,catalog.canvases.Length);
            foreach(var entry in catalog.canvases)
            {
                var art=Resources.Load<Texture2D>(entry.artPath);Assert.IsNotNull(art);
                var map=RegionMap.Decode(Resources.Load<TextAsset>(entry.regionPath).bytes);
                Assert.AreEqual(art.width,map.Width);Assert.AreEqual(art.height,map.Height);Assert.Greater(map.Count,0);
            }
        }
    }
}
