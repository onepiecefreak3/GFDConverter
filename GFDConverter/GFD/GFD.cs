using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System;
using System.Text;
using System.Linq;
using GFDConverter.IO;

namespace GFDConverter
{
    public class GFD
    {
        #region Struct Version 1
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Headerv1
        {
            public Magic Magic;
            public uint Version;
            public int unk0;
            public int unk1;
            public int unk2;
            public int FontSize;
            public int FontTexCount;
            public int CharCount;
            public int FCount;
            public float BaseLine;
            public float DescentLine;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Entryv1
        {
            public uint Character;
            public byte TexID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] GlyphPos;
            public byte unk0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] GlyphSize;
            public byte CharWidth;
            public byte XCorrection;
            public byte YCorrection;
            public byte Padding;
        }
        #endregion

        #region Struct Version 2
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Headerv2
        {
            public Magic Magic = "GFD\0";
            public uint Version = 0x00010f06;
            public int unk0 = 4;
            public int unk1 = 6;
            public int unk2 = 0;
            public int FontSize;
            public int FontTexCount;
            public int CharCount;
            public int unk3 = 0;
            public int FCount = 0;
            public float MaxCharWidth;
            public float MaxCharHeight;
            public float BaseLine;
            public float DescentLine;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class Entryv2
        {
            public uint Character;
            public byte TexID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] GlyphPos;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] GlyphSize;
            public byte Padding = 0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] CharacterSize;
            public byte MaxCharHeight = 0x14;
            public byte XCorrection;
            public byte YCorrection;
            public ushort EndMark = 0xFFFF;
        }
        #endregion

        public Headerv1 header;
        public List<float> headerFloats = new List<float>();
        public string Name;
        public List<Entryv1> entries = new List<Entryv1>();

        public void Load(string filename)
        {
            using (var br = new BinaryReaderX(File.OpenRead(filename)))
            {
                //Header
                header = br.ReadStruct<Headerv1>();
                headerFloats = br.ReadMultiple<float>(header.FCount);

                //Name
                var nameSize = br.ReadInt32();
                Name = br.ReadCStringA();

                //Character Entries
                entries = br.ReadMultiple<Entryv1>(header.CharCount);
            }
        }

        public void Save(string filename)
        {
            int GetInt(byte[] byteA) => byteA.Aggregate(0, (output, b) => (output << 8) | b);
            byte[] GetByteA(int i)
            {
                List<byte> res = new List<byte>();
                for (int j = 3; j >= 0; j--)
                    res.Add((byte)((i >> j * 8) & 0xFF));
                return res.ToArray();
            };

            //Create Version 2 Entries
            var entriesv2 = new List<Entryv2>();
            foreach (var entry in entries)
                entriesv2.Add(new Entryv2
                {
                    Character = entry.Character,
                    TexID = entry.TexID,
                    GlyphPos = entry.GlyphPos,
                    GlyphSize = entry.GlyphSize,
                    CharacterSize = GetByteA(GetInt(entry.GlyphSize.Reverse().ToArray()) & 0xFFF000 | entry.CharWidth).Reverse().ToArray(),
                    MaxCharHeight = (byte)(GetInt(entry.GlyphSize.Reverse().ToArray()) >> 12),
                    XCorrection = 0,
                    YCorrection = 0
                });

            //Create Header
            var headerv2 = new Headerv2
            {
                FontSize = header.FontSize,
                FontTexCount = header.FontTexCount,
                CharCount = header.CharCount,
                FCount = headerFloats.Count(),
                MaxCharWidth = 16,
                MaxCharHeight = header.BaseLine + header.DescentLine,
                BaseLine = header.BaseLine,
                DescentLine = header.DescentLine
            };

            using (var bw = new BinaryWriterX(File.Create(filename)))
            {
                //Header
                bw.WriteStruct(headerv2);
                foreach (var f in headerFloats)
                    bw.Write(f);
                bw.Write(Encoding.ASCII.GetByteCount(Name));
                bw.Write(Encoding.ASCII.GetBytes(Name + "\0"));

                //Entries
                foreach (var entry in entriesv2)
                    bw.WriteStruct(entry);
            }
        }
    }
}
