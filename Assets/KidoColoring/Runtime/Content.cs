using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Kido.Coloring
{
    [Serializable] public sealed class CanvasEntry
    {
        public string id, title, artPath, thumbPath, regionPath;
        public int category, artRevision;
    }
    [Serializable] public sealed class ContentCatalog { public CanvasEntry[] canvases; }
    [Serializable] public sealed class GameSettings
    {
        public bool previewAllCanvases = true;
        public string[] freeCanvasIds = Array.Empty<string>();
        public string appleProductId = "", googleProductId = "";
        public float completionThreshold = .90f;
    }
    public static class Content
    {
        public static readonly string[] Categories = { "Christmas", "Animals", "Vehicles" };
        public static readonly string[] Headlines = { "A little winter wonder", "Meet your new friends", "Ready, set, create!" };
        public static ContentCatalog Load()
        {
            var asset = Resources.Load<TextAsset>("Kido/catalog");
            if (!asset) throw new InvalidDataException("Kido catalog missing.");
            var catalog = JsonUtility.FromJson<ContentCatalog>(asset.text);
            if (catalog?.canvases == null || catalog.canvases.Length != 30) throw new InvalidDataException("Expected 30 canvases.");
            var ids = new HashSet<string>(); var counts = new int[3];
            foreach (var item in catalog.canvases)
            {
                if (item.category < 0 || item.category > 2 || string.IsNullOrEmpty(item.id) || !ids.Add(item.id))
                    throw new InvalidDataException("Invalid canvas catalog.");
                counts[item.category]++;
            }
            foreach (var count in counts) if (count != 10) throw new InvalidDataException("Expected 10 canvases per world.");
            return catalog;
        }
        public static GameSettings Settings()
        {
            var asset = Resources.Load<TextAsset>("Kido/settings");
            return asset ? JsonUtility.FromJson<GameSettings>(asset.text) : new GameSettings();
        }
    }
    public sealed class RegionMap
    {
        public int Width { get; }
        public int Height { get; }
        public int Count { get; }
        public ushort[] Labels { get; }
        public int PaintablePixels { get; }
        public RegionMap(int width, int height, int count, ushort[] labels)
        {
            if (width <= 0 || height <= 0 || width > 2048 || height > 2048 || count <= 0 || count > 65535 || labels.Length != width * height)
                throw new InvalidDataException("Invalid region dimensions.");
            Width = width; Height = height; Count = count; Labels = labels;
            int paintable = 0;
            foreach (var label in labels)
            {
                if (label > count) throw new InvalidDataException("Invalid region label.");
                if (label != 0) paintable++;
            }
            PaintablePixels = paintable;
            if (PaintablePixels == 0) throw new InvalidDataException("Artwork has no coloring regions.");
        }
        public static RegionMap Decode(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                if (System.Text.Encoding.ASCII.GetString(reader.ReadBytes(8)) != "KIDORGN1") throw new InvalidDataException("Region map header mismatch.");
                int w = reader.ReadInt32(), h = reader.ReadInt32(), n = reader.ReadInt32();
                if (w < 1 || h < 1 || w > 2048 || h > 2048 || stream.Length != 20L + (long)w * h * 2)
                    throw new InvalidDataException("Truncated or invalid region map.");
                var labels = new ushort[w * h];
                for (int i = 0; i < labels.Length; i++) labels[i] = reader.ReadUInt16();
                return new RegionMap(w, h, n, labels);
            }
        }
    }
}
