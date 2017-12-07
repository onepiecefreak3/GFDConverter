using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GFDConverter.IO;

namespace GFDConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine("Usage:\nGFDConvert.exe <GFD File> <new internal path>");
                Environment.Exit(0);
            }
            if (args.Count() == 1)
            {
                Console.WriteLine("Too few arguments!");
                Environment.Exit(0);
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"File {args[0]} was not found!");
                Environment.Exit(0);
            }

            using (var br = new BinaryReaderX(File.OpenRead(args[0])))
            {
                br.BaseStream.Position = 4;
                if (br.ReadUInt32() != 0x00010c06)
                {
                    Console.WriteLine($"{args[0]} is not a GFD v1.");
                    Environment.Exit(0);
                }
            }

            var gfd = new GFD();
            gfd.Load(args[0]);
            gfd.Name = args[1];

            gfd.Save(args[0] + ".v2");
        }
    }
}
