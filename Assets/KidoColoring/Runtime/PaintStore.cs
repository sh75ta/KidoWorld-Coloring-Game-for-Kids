using System;
using System.IO;
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
                    var item=Decode(File.ReadAllBytes(path));
                    if (item.id != id || (width>0 && item.width!=width) || (height>0 && item.height!=height) || (artRevision>0 && item.artRevision!=artRevision))
                        throw new InvalidDataException("Saved artwork revision does not match.");
                    if(result.painting==null || item.sequence>result.painting.sequence) result.painting=item;
                }
                catch(Exception e) when(e is IOException || e is ArgumentException || e is OverflowException || e is UnauthorizedAccessException)
                { invalid=true; result.error=e.Message; }
            }
            result.recovered=invalid && result.painting!=null;return result;
        }
        public bool TrySave(string id,int artRevision,PaintDocument document,out string error)
        {
            error=null;
            try
            {
                var previous=Load(id,document.Map.Width,document.Map.Height,artRevision);
                if(previous.filesExist && previous.painting==null) throw new IOException("Existing artwork could not be recovered. It was preserved.");
                long sequence=(previous.painting?.sequence ?? 0)+1;
                var item=new SavedPainting{sequence=sequence,id=id,width=document.Map.Width,height=document.Map.Height,artRevision=artRevision,coverage=document.Coverage,paint=document.Paint};
                byte[] bytes=Encode(item); string slot=sequence%2==1?"a":"b";
                Directory.CreateDirectory(root);
                string target=PathFor(id,slot),temp=target+".tmp";
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
                {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                Decode(File.ReadAllBytes(temp));
                // Only the older slot is replaced. The highest valid generation remains in the other slot.
                if(File.Exists(target)) File.Delete(target);
                File.Move(temp,target);return true;
            }
            catch(Exception e) when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is OverflowException)
            {error=e.Message;return false;}
        }
        public static byte[] Encode(SavedPainting item)
        {
            byte[] body;
            using(var stream=new MemoryStream())
            {
                using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))
                {
                    writer.Write(Encoding.ASCII.GetBytes("KIDOSAV1"));writer.Write(1);writer.Write(item.sequence);
                    writer.Write(item.id);writer.Write(item.width);writer.Write(item.height);writer.Write(item.artRevision);writer.Write(item.coverage);
                    writer.Write(item.paint.Length);
                    foreach(var c in item.paint){writer.Write(c.r);writer.Write(c.g);writer.Write(c.b);writer.Write(c.a);}
                }
                body=stream.ToArray();
            }
            using(var sha=SHA256.Create())
            {
                var hash=sha.ComputeHash(body);var all=new byte[body.Length+hash.Length];
                Buffer.BlockCopy(body,0,all,0,body.Length);Buffer.BlockCopy(hash,0,all,body.Length,hash.Length);return all;
            }
        }
        public static SavedPainting Decode(byte[] bytes)
        {
            if(bytes.Length<64 || bytes.Length>17000000) throw new InvalidDataException("Invalid save size.");
            int bodyLength=bytes.Length-32;
            using(var sha=SHA256.Create())
            {
                byte[] hash=sha.ComputeHash(bytes,0,bodyLength);int mismatch=0;
                for(int i=0;i<32;i++) mismatch|=hash[i]^bytes[bodyLength+i];
                if(mismatch!=0) throw new InvalidDataException("Save checksum failed.");
            }
            using(var stream=new MemoryStream(bytes,0,bodyLength))
            using(var reader=new BinaryReader(stream))
            {
                if(Encoding.ASCII.GetString(reader.ReadBytes(8))!="KIDOSAV1" || reader.ReadInt32()!=1) throw new InvalidDataException("Unknown save format.");
                var item=new SavedPainting{sequence=reader.ReadInt64(),id=reader.ReadString(),width=reader.ReadInt32(),height=reader.ReadInt32(),artRevision=reader.ReadInt32(),coverage=reader.ReadSingle()};
                int n=reader.ReadInt32();
                if(item.id.Length>128 || item.sequence<1 || item.width<1 || item.height<1 || item.width>2048 || item.height>2048 || n!=item.width*item.height || stream.Length-stream.Position!=(long)n*4)
                    throw new InvalidDataException("Invalid save dimensions.");
                if(float.IsNaN(item.coverage) || float.IsInfinity(item.coverage) || item.coverage<0 || item.coverage>1) throw new InvalidDataException("Invalid saved progress.");
                item.paint=new Color32[n];
                for(int i=0;i<n;i++)item.paint[i]=new Color32(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte());
                return item;
            }
        }
    }
}
