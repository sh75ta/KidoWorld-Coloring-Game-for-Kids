using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Kido.Coloring
{
    public sealed class SavedPainting
    {
        public long sequence;
        public int width,height,artRevision;
        public string id;
        public float coverage;
        public Color32[] paint;
        public byte[] effects;
    }
    public sealed class LoadResult
    {
        public SavedPainting painting;
        public bool filesExist;
        public bool recovered;
        public string error;
    }
    // Two independently checksummed slots: a torn new write never destroys the other slot.
    public sealed class PaintStore
    {
        readonly string root;
        readonly Dictionary<string,long> sequences=new Dictionary<string,long>();
        public PaintStore(string root) { this.root=root; }
        public string Root => root;
        string PathFor(string id, string slot)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("Missing canvas ID.");
            foreach(char c in id) if (!(char.IsLetterOrDigit(c) || c=='_' || c=='-')) throw new ArgumentException("Unsafe canvas ID.");
            return Path.Combine(root,id+"."+slot+".kido");
        }
        public LoadResult Load(string id,int width=0,int height=0,int artRevision=0)
        {
            var result = new LoadResult(); bool invalid=false;
            foreach(string slot in new[]{"a","b"})
            {
                string path=PathFor(id,slot);
                if (!File.Exists(path)) continue;
                result.filesExist=true;
                try
                {
                    if(new FileInfo(path).Length>22000000)throw new InvalidDataException("Saved file is too large.");
                    var item=Decode(File.ReadAllBytes(path));
                    if (item.id != id || (width>0 && item.width!=width) || (height>0 && item.height!=height) || (artRevision>0 && item.artRevision!=artRevision))
                        throw new InvalidDataException("Saved artwork revision does not match.");
                    if(result.painting==null || item.sequence>result.painting.sequence) result.painting=item;
                }
                catch(Exception e) when(e is IOException || e is InvalidDataException || e is ArgumentException || e is OverflowException || e is UnauthorizedAccessException)
                { invalid=true; result.error=e.Message; }
            }
            if(result.painting!=null)
            {
                var p=result.painting;
                sequences[id+":"+p.artRevision+":"+p.width+":"+p.height]=p.sequence;
            }
            result.recovered=invalid && result.painting!=null;return result;
        }
        public static SavedPainting Snapshot(string id,int artRevision,PaintDocument document)
        {
            return new SavedPainting {id=id,artRevision=artRevision,width=document.Map.Width,height=document.Map.Height,
                coverage=document.Coverage,paint=(Color32[])document.Paint.Clone(),effects=(byte[])document.Effects.Clone()};
        }
        // Centers a saved painting in a larger page. Paint, glitter and coverage are preserved.
        public static SavedPainting Reframe(SavedPainting original,int width,int height,string id)
        {
            if(original==null)return null;
            if(width<original.width||height<original.height||width>2048||height>2048)throw new ArgumentOutOfRangeException(nameof(width));
            int offX=(width-original.width)/2,offY=(height-original.height)/2;
            var result=new SavedPainting{id=id,sequence=original.sequence,artRevision=original.artRevision,
                width=width,height=height,paint=new Color32[width*height],effects=new byte[width*height]};
            for(int y=0;y<original.height;y++)
            {
                Array.Copy(original.paint,y*original.width,result.paint,(y+offY)*width+offX,original.width);
                if(original.effects!=null)Array.Copy(original.effects,y*original.width,result.effects,(y+offY)*width+offX,original.width);
            }
            result.coverage=original.coverage*original.width*original.height/((float)width*height);return result;
        }
        // Current format "<id>_page2" (2048x1536). Older "<id>_page" (1536 square) and original
        // "<id>" saves are read and reframed in memory; their files are never modified or deleted.
        public LoadResult LoadPage(string id,int artRevision,int artWidth,int artHeight)
        {
            const int W=RegionMap.PageWidth,H=RegionMap.PageHeight;string target=id+"_page2";
            var current=Load(target,W,H,artRevision);
            if(current.filesExist)return current;
            var legacy=Load(id+"_page",artWidth+512,artHeight+512,artRevision);
            if(legacy.filesExist){legacy.painting=Reframe(legacy.painting,W,H,target);return legacy;}
            var original=Load(id,artWidth,artHeight,artRevision);
            original.painting=Reframe(original.painting,W,H,target);return original;
        }
        public static SavedPainting ExpandToPage(SavedPainting original,int margin=256)
        {
            if(original==null)return null;
            if(margin<0||margin>(2048-Math.Max(original.width,original.height))/2)throw new ArgumentOutOfRangeException(nameof(margin));
            int w=original.width+margin*2,h=original.height+margin*2;
            var result=new SavedPainting{id=original.id+"_page",sequence=original.sequence,artRevision=original.artRevision,
                width=w,height=h,paint=new Color32[w*h],effects=new byte[w*h]};
            for(int y=0;y<original.height;y++)
            {
                Array.Copy(original.paint,y*original.width,result.paint,(y+margin)*w+margin,original.width);
                if(original.effects!=null)Array.Copy(original.effects,y*original.width,result.effects,(y+margin)*w+margin,original.width);
            }
            result.coverage=original.coverage*original.width*original.height/(w*h);return result;
        }
        public bool TrySave(string id,int artRevision,PaintDocument document,out string error)
            => TrySaveSnapshot(Snapshot(id,artRevision,document),out error);
        // Only managed data and filesystem operations: safe for a single background writer.
        public bool TrySaveSnapshot(SavedPainting item,out string error)
        {
            string id=item.id;int artRevision=item.artRevision;
            error=null;
            try
            {
                // Cache only matching revision/dimensions; never reuse another artwork generation.
                string generation=id+":"+artRevision+":"+item.width+":"+item.height;
                if(!sequences.TryGetValue(generation,out var previousSequence))
                {
                    var previous=Load(id,item.width,item.height,artRevision);
                    if(previous.filesExist&&previous.painting==null)throw new IOException("Existing artwork could not be recovered. It was preserved.");
                    previousSequence=previous.painting?.sequence??0;
                }
                long sequence=previousSequence+1;
                item.sequence=sequence;
                byte[] bytes=Encode(item); string slot=sequence%2==1?"a":"b";
                Directory.CreateDirectory(root);
                string target=PathFor(id,slot),temp=target+".tmp";
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
                {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                VerifyChecksum(Unpack(File.ReadAllBytes(temp)));
                // Only the older slot is replaced. The highest valid generation remains in the other slot.
                if(File.Exists(target)) File.Delete(target);
                File.Move(temp,target);sequences[generation]=sequence;return true;
            }
            catch(Exception e) when(e is IOException || e is InvalidDataException || e is UnauthorizedAccessException || e is ArgumentException || e is OverflowException)
            {error=e.Message;return false;}
        }
        public static byte[] Encode(SavedPainting item)
        {
            if(item.paint==null || (item.effects!=null && item.effects.Length!=item.paint.Length))throw new ArgumentException("Invalid painting buffers.");
            using(var stream=new MemoryStream(256+item.paint.Length*5))
            {
                using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("KIDOSAV2"));writer.Write(2);writer.Write(item.sequence);
                    writer.Write(item.id);writer.Write(item.width);writer.Write(item.height);writer.Write(item.artRevision);writer.Write(item.coverage);
                    writer.Write(item.paint.Length);
                    foreach(var c in item.paint){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
                    writer.Write(item.effects ?? new byte[item.paint.Length]);
                }
                // Hash and compress the stream's owned buffer directly. Avoid
                // two extra full-page byte arrays (~22.5 MiB at 1536 square).
                using(var sha=SHA256.Create())
                {
                    var buffer=stream.GetBuffer();int length=checked((int)stream.Length);
                    var hash=sha.ComputeHash(buffer,0,length);
                    using(var packed=new MemoryStream())
                    {
                        byte[] header=Encoding.ASCII.GetBytes("KIDOZIP2");packed.Write(header,0,header.Length);
                        using(var zip=new DeflateStream(packed,System.IO.Compression.CompressionLevel.Fastest,true))
                        {zip.Write(buffer,0,length);zip.Write(hash,0,hash.Length);}
                        return packed.ToArray();
                    }
                }
            }
        }
        public static SavedPainting Decode(byte[] bytes)
        {
            bytes=Unpack(bytes);
            VerifyChecksum(bytes);
            int bodyLength=bytes.Length-32;
            using(var stream=new MemoryStream(bytes,0,bodyLength))
            using(var reader=new BinaryReader(stream))
            {
                string magic=Encoding.ASCII.GetString(reader.ReadBytes(8));int version=reader.ReadInt32();
                if(!((magic=="KIDOSAV1"&&version==1)||(magic=="KIDOSAV2"&&version==2)))throw new InvalidDataException("Unknown save format.");
                var item=new SavedPainting{sequence=reader.ReadInt64(),id=reader.ReadString(),width=reader.ReadInt32(),height=reader.ReadInt32(),artRevision=reader.ReadInt32(),coverage=reader.ReadSingle()};
                int n=reader.ReadInt32();
                if(item.id.Length>128 || item.sequence<1 || item.width<1 || item.height<1 || item.width>2048 || item.height>2048 || n!=item.width*item.height || stream.Length-stream.Position!=(long)n*(version==2?5:4))
                    throw new InvalidDataException("Invalid save dimensions.");
                if(float.IsNaN(item.coverage) || float.IsInfinity(item.coverage) || item.coverage<0 || item.coverage>1) throw new InvalidDataException("Invalid saved progress.");
                item.paint=new Color32[n];
                for(int i=0;i<n;i++)item.paint[i]=new Color32(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte());
                item.effects=version==2?reader.ReadBytes(n):new byte[n];
                return item;
            }
        }
        static void VerifyChecksum(byte[] bytes)
        {
            if(bytes.Length<64 || bytes.Length>22000000) throw new InvalidDataException("Invalid save size.");
            int bodyLength=bytes.Length-32;
            using(var sha=SHA256.Create())
            {
                byte[] hash=sha.ComputeHash(bytes,0,bodyLength);int mismatch=0;
                for(int i=0;i<32;i++) mismatch|=hash[i]^bytes[bodyLength+i];
                if(mismatch!=0) throw new InvalidDataException("Save checksum failed.");
            }
        }
        static byte[] Unpack(byte[] bytes)
        {
            if(bytes.Length<8||Encoding.ASCII.GetString(bytes,0,8)!="KIDOZIP2")return bytes;
            if(bytes.Length>22000000)throw new InvalidDataException("Compressed save too large.");
            using(var source=new MemoryStream(bytes,8,bytes.Length-8))
            using(var zip=new DeflateStream(source,CompressionMode.Decompress))
            using(var output=new MemoryStream())
            {
                var block=new byte[65536];int count;
                while((count=zip.Read(block,0,block.Length))>0)
                {if(output.Length+count>22000000)throw new InvalidDataException("Expanded save too large.");output.Write(block,0,count);}
                return output.ToArray();
            }
        }
    }
}
