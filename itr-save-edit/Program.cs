using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace itr_save_edit
{
    class Program
    {
        public class Options
        {
            [Option('s', "save", Required = false, HelpText = "Path to .SAVE file")]
            public string SaveFile { get; set; }

            [Option('c', "credits", Required = false, Default = 1000000, HelpText = "New player credit balance")]
            public int Credits { get; set; }

            [Option('l', "level", Required = false, Default = 5, HelpText = "New player security level 1-5")]
            public int Level { get; set; }
        }

        static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AdditionalNewLineAfterOption = false;
                h.Heading = "  substatica";
                h.Copyright = "  youtube.com/substatica";
                h.AdditionalNewLineAfterOption = true;
                return HelpText.DefaultParsingErrorsHandler(result, h);
            }, e => e);
            Console.WriteLine(helpText);
        }

        // "PlayerLevel"
        static byte[] playerlevel_string_bytes = new byte[] { 0x50, 0x6C, 0x61, 0x79, 0x65, 0x72, 0x4C, 0x65, 0x76, 0x65, 0x6C };
        static int playerlevel_value_offset = 42; // 0x2A

        // "Money"
        static byte[] money_string_bytes = new byte[] { 0x4D, 0x6F, 0x6E, 0x65, 0x79 };
        static int money_value_offset = 42; // 0x2A

        static byte[] chunk_header_bytes = new byte[8];
        static byte[] max_chunk_size_bytes = new byte[8];

        static void Main(string[] args)
        {
            Console.WriteLine();
            Console.WriteLine("                             __ _");
            Console.WriteLine("                      _wr\"\"        \"-q__     ");                        
            Console.WriteLine("                   _dP                 9m_");
            Console.WriteLine("                 _#P                     9#_                         ");
            Console.WriteLine("                d#@                       9#m                        ");
            Console.WriteLine("               d##                         ###                       ");
            Console.WriteLine("              J###                         ###L                      ");
            Console.WriteLine("              {###K                       J###K                      ");
            Console.WriteLine("              ]####K      ___aaa___      J####F                      ");
            Console.WriteLine("          __gmM######_  w#P\"\"   \"\"9#m  _d#####Mmw__                  ");
            Console.WriteLine("       _g##############mZ_         __g##############m_               ");
            Console.WriteLine("     _d####M@PPPP@@M#######Mmp gm#########@@PPP9@M####m_             ");
            Console.WriteLine("    a###\"\"          ,Z\"#####@\" '######\"\\g          \"\"M##m            ");
            Console.WriteLine("   J#@\"             0L  \"*##     ##@\"  J#              *#K           ");
            Console.WriteLine("   #\"               `#    \"_gmwgm_~    dF               `#_          ");
            Console.WriteLine("  7F                 \"#_   ]#####F   _dK                 JE          ");
            Console.WriteLine("  ]                    *m__ ##### __g@\"                   F");
            Console.WriteLine("                         \"PJ#####LP\"\"");
            Console.WriteLine("   `                       0######_                      '           ");
            Console.WriteLine("                         _0########_                        ");
            Console.WriteLine("      .                _d#####^#####m__                ,       ");
            Console.WriteLine("        \" * w_________am#####P\"   ~9#####mw_________w*\"           ");       
            Console.WriteLine("            \"\"9@#####@M\"\"           \"\"P@#####@M\"\"          ");
            Console.WriteLine();
            Console.WriteLine("  ----------------------------------------------------------");
            Console.WriteLine("  Into the Radius Save Edit");
            Console.WriteLine();
            Console.WriteLine("  substatica");
            Console.WriteLine("  https://youtube.com/substatica");
            Console.WriteLine("  ----------------------------------------------------------");
            Console.WriteLine();

            int credits = 1000000;
            int level = 5;
            string filename = null;

            var parser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);

            parserResult
                .WithNotParsed(errs => DisplayHelp(parserResult, errs))
                .WithParsed<Options>(o =>
                {
                    if (!String.IsNullOrEmpty(o.SaveFile))
                    {
                        filename = o.SaveFile;
                    }

                    credits = o.Credits;
                    level = o.Level;
                });

            if(!File.Exists(filename))
            {
                Console.WriteLine("  Error: Could not read file");
                System.Environment.Exit(1);
            }

            var binary_reader = new BinaryReader(File.Open(filename, FileMode.Open));

            byte[] file_header_bytes = binary_reader.ReadBytes(8);

            List<byte[]> decompressed_chunks = new List<byte[]>();

            byte[] decompressed_file = new byte[0];

            Console.Write("  Extracting");
            while (binary_reader.BaseStream.Position < binary_reader.BaseStream.Length)
            {
                Console.Write(".");
                var uncompressed_chunk = DecompressChunk(binary_reader);
                decompressed_file = decompressed_file.Concat(uncompressed_chunk).ToArray();
                decompressed_chunks.Add(uncompressed_chunk);
            }
            Console.WriteLine("complete");

            binary_reader.Close();

            // cash money
            Console.WriteLine("  Updating credits");
            int count = 0;
            foreach (var decompressed_chunk in decompressed_chunks)
            {
                if (ReplaceBytes(decompressed_chunk, money_string_bytes, money_value_offset, credits))
                {
                    count++;
                    if (count == 2)
                    {
                        break;
                    }
                    else
                    {
                        var searchOffset = GetPositionAfterMatch(decompressed_chunk, money_string_bytes);

                        if (searchOffset > -1)
                        {
                            if (ReplaceBytes(decompressed_chunk, money_string_bytes, money_value_offset, credits, searchOffset))
                            {
                                count++;
                                if (count == 2)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // security level
            Console.WriteLine("  Updating security level");
            foreach (var decompressed_chunk in decompressed_chunks)
            {
                if (ReplaceBytes(decompressed_chunk, playerlevel_string_bytes, playerlevel_value_offset, level))
                {
                    break;
                }
            }

            byte[] edited_decompressed_file = new byte[0];
            foreach (var decompressed_chunk in decompressed_chunks)
            {
                edited_decompressed_file = edited_decompressed_file.Concat(decompressed_chunk).ToArray();
            }

            Console.Write("  Compressing");
            byte[] edited_compressed_file = file_header_bytes;
            foreach (var decompressed_chunk in decompressed_chunks)
            {
                Console.Write(".");
                // chunk header
                edited_compressed_file = edited_compressed_file.Concat(chunk_header_bytes).ToArray();
                // max chunk size
                edited_compressed_file = edited_compressed_file.Concat(max_chunk_size_bytes).ToArray();

                byte[] compressed_chunk = ZlibCodecCompress(decompressed_chunk);

                // compressed size
                edited_compressed_file = edited_compressed_file.Concat(BitConverter.GetBytes(compressed_chunk.Length)).ToArray();
                edited_compressed_file = edited_compressed_file.Concat(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ToArray();
                // decompressed size
                edited_compressed_file = edited_compressed_file.Concat(BitConverter.GetBytes(decompressed_chunk.Length)).ToArray();
                edited_compressed_file = edited_compressed_file.Concat(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ToArray();

                // repeat
                edited_compressed_file = edited_compressed_file.Concat(BitConverter.GetBytes(compressed_chunk.Length)).ToArray();
                edited_compressed_file = edited_compressed_file.Concat(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ToArray();
                edited_compressed_file = edited_compressed_file.Concat(BitConverter.GetBytes(decompressed_chunk.Length)).ToArray();
                edited_compressed_file = edited_compressed_file.Concat(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ToArray();
                edited_compressed_file = edited_compressed_file.Concat(compressed_chunk).ToArray();
            }
            Console.WriteLine("complete");

            int backupIndex = 0;

            while (File.Exists(filename + ".bak." + backupIndex.ToString("D3")))
            {
                backupIndex++;
                if (backupIndex > 999)
                {
                    Console.WriteLine("  Error: Too many backup files, can't generate backup filename");
                    Environment.Exit(1);
                }
            }


            Console.WriteLine("  Creating backup");
            File.Copy(filename, filename + ".bak." + backupIndex.ToString("D3"));
            File.Delete(filename);

            Console.WriteLine("  Saving");
            File.WriteAllBytes(filename, edited_compressed_file);

            Console.WriteLine("  Finished");
            Console.WriteLine();
            Console.WriteLine("  Good Luck Explorer");
        }

        static byte[] DecompressChunk(BinaryReader binary_reader)
        {
            // Skip UAsset Bytes and 0 Int
            chunk_header_bytes = binary_reader.ReadBytes(8);

            max_chunk_size_bytes = binary_reader.ReadBytes(8);

            var compressed_chunksize = ReadInt16(binary_reader);
            var uncompressed_chunksize = ReadInt16(binary_reader);

            // Skip second compressed size and uncompressed size
            binary_reader.BaseStream.Seek(16, SeekOrigin.Current);

            var compressed_chunk = new Byte[compressed_chunksize];
            binary_reader.BaseStream.Read(compressed_chunk, 0, compressed_chunksize);
            return ZlibCodecDecompress(compressed_chunk);
        }

        static int ReadInt16(BinaryReader binary_reader)
        {
            var bytes = binary_reader.ReadBytes(8);
            return BitConverter.ToInt32(bytes, 0);
        }

        // https://github.com/eropple/dotnetzip/blob/master/Examples/C%23/ZLIB/ZlibDeflateInflate.cs
        private static byte[] ZlibCodecDecompress(byte[] compressed)
        {
            int outputSize = 2048;
            byte[] output = new Byte[outputSize];

            // If you have a ZLIB stream, set this to true.  If you have
            // a bare DEFLATE stream, set this to false.
            bool expectRfc1950Header = true;

            using (MemoryStream ms = new MemoryStream())
            {
                ZlibCodec compressor = new ZlibCodec();
                compressor.InitializeInflate(expectRfc1950Header);

                compressor.InputBuffer = compressed;
                compressor.AvailableBytesIn = compressed.Length;
                compressor.NextIn = 0;
                compressor.OutputBuffer = output;

                foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
                {
                    int bytesToWrite = 0;
                    do
                    {
                        compressor.AvailableBytesOut = outputSize;
                        compressor.NextOut = 0;
                        compressor.Inflate(f);

                        bytesToWrite = outputSize - compressor.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            ms.Write(output, 0, bytesToWrite);
                    }
                    while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                           (f == FlushType.Finish && bytesToWrite != 0));
                }

                compressor.EndInflate();

                return ms.ToArray();
            }
        }

        private static byte[] ZlibCodecCompress(byte[] uncompressed)
        {
            int outputSize = 2048;
            byte[] output = new Byte[outputSize];
            int lengthToCompress = uncompressed.Length;

            // If you want a ZLIB stream, set this to true.  If you want
            // a bare DEFLATE stream, set this to false.
            bool wantRfc1950Header = true;

            using (MemoryStream ms = new MemoryStream())
            {
                ZlibCodec compressor = new ZlibCodec();
                compressor.InitializeDeflate(Ionic.Zlib.CompressionLevel.Default, wantRfc1950Header);

                compressor.InputBuffer = uncompressed;
                compressor.AvailableBytesIn = lengthToCompress;
                compressor.NextIn = 0;
                compressor.OutputBuffer = output;

                foreach (var f in new FlushType[] { FlushType.None, FlushType.Finish })
                {
                    int bytesToWrite = 0;
                    do
                    {
                        compressor.AvailableBytesOut = outputSize;
                        compressor.NextOut = 0;
                        compressor.Deflate(f);

                        bytesToWrite = outputSize - compressor.AvailableBytesOut;
                        if (bytesToWrite > 0)
                            ms.Write(output, 0, bytesToWrite);
                    }
                    while ((f == FlushType.None && (compressor.AvailableBytesIn != 0 || compressor.AvailableBytesOut == 0)) ||
                           (f == FlushType.Finish && bytesToWrite != 0));
                }

                compressor.EndDeflate();

                ms.Flush();
                return ms.ToArray();
            }
        }

        static bool ReplaceBytes(byte[] sourceBytes, byte[] patternArray, int valueOffset, int newValue, int startOffset = 0)
        {
            int offset = GetPositionAfterMatch(sourceBytes, patternArray, startOffset);

            if (offset < 0)
            {
                return false;
            }

            byte[] intBytes = BitConverter.GetBytes(newValue);
            for (int i = 0; i < intBytes.Length; i++)
            {
                sourceBytes[offset + valueOffset + i] = intBytes[i];
            }

            return true;
        }

        static string BytesToHexString(byte[] bytes)
        {
            string result = "{ ";
            foreach (byte b in bytes)
            {
                result += $"0x{ b:x2}, ";
            }
            result += " }";
            return result;
        }

        static int GetPositionAfterMatch(byte[] data, byte[] pattern, int startOffest = 0)
        {
            try
            {
                for (int i = startOffest; i < data.Length - pattern.Length; i++)
                {
                    bool match = true;
                    for (int k = 0; k < pattern.Length; k++)
                    {
                        if (data[i + k] != pattern[k])
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match)
                    {
                        return i + pattern.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            return -1;
        }
    }
}
