using Figgle;
using System.IO;
using System;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using ICSharpCode.SharpZipLib.Zip.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XBS_Tool
{
    public class Program
    {
        private string version = "1.0";

        private Dictionary<string, byte[]> fileTypes = new Dictionary<string, byte[]>
        {
            { "dds", new byte[] { 0x44, 0x44, 0x53, 0x20 } },
            { "xbxm", new byte[] { 0x58, 0x42, 0x58, 0x4D } },
            { "anmx", new byte[] { 0x41, 0x4E, 0x4D, 0x58 } }
        };

        public Program(string[] args)
        {
            Console.Title = $"XBS Tool v{version} - Dan";

            printTitle();

            Console.WriteLine($" >>> XBS Tool v{version} <<< \n");

            if (args.Length == 0)
            {
                Console.WriteLine("[XBS-Tool]: Please drag a file onto the xbstool.exe.");
                Console.ReadLine();
                return;
            }

            //Get cmd line args. [0] is the file path.
            string filePath = args[0];
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            Console.WriteLine($"[XBS-Tool]: Reading -> {fileName}.");

            byte[] deflated = removeDeflate(filePath, 0x40);

            if (deflated.Length > 0)
            {
                retrieveData(deflated, fileName);
            }
            else
            {
                Console.WriteLine("[XBS-Tool]: File data is invalid.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("[XBS-Tool]: Completed.");

            Console.ReadLine();
        }

        void retrieveData(byte[] data, string folderName)
        {
            Console.WriteLine($"[XBS-Tool]: Decrypting: {data.Length} bytes, folderName -> {folderName}");

            List<(string type, int offset)> foundFiles = new List<(string, int)>();

            try
            {
                for (int i = 0; i <= data.Length - 4; i++)
                {
                    foreach (var fileType in fileTypes)
                    {
                        if (data[i] == fileType.Value[0] &&
                            data[i + 1] == fileType.Value[1] &&
                            data[i + 2] == fileType.Value[2] &&
                            data[i + 3] == fileType.Value[3])
                        {
                            foundFiles.Add((fileType.Key, i));

                            i += 3;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[XBS-Tool]: Finding files failed -> {ex.Message}.");
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[XBS-Tool]: Retrieved {foundFiles.Count()} files, writing...");

            Directory.CreateDirectory(folderName);

            int fileCount = 0;

            try
            {
                foreach (var file in foundFiles)
                {
                    string outputPath = Path.Combine(folderName, $"{file.type}_{++fileCount}.{file.type}");

                    if (file.type == "xbxm")
                    {
                        int nameLength = Array.IndexOf(data, (byte)0, file.offset + 48) - (file.offset + 48);
                        string fileName = Encoding.UTF8.GetString(data, file.offset + 48, nameLength);
                        outputPath = Path.Combine(folderName, $"{fileName}.xbmesh");
                    }

                    int nextOffset = foundFiles.IndexOf(file) < foundFiles.Count - 1 ? foundFiles[foundFiles.IndexOf(file) + 1].offset : data.Length;
                    File.WriteAllBytes(outputPath, data.Skip(file.offset).Take(nextOffset - file.offset).ToArray());
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[XBS-Tool]: Error writing the files -> {ex.Message}.");
            }
        }

        byte[] removeDeflate(string filePath, long offset)
        {
            using (var file = File.OpenRead(filePath))
            {
                file.Seek(offset, SeekOrigin.Begin);

                bool noZLib = false;

                //Ensure that there is ZLIB, check 0x40 for one of the file types.?
                byte[] first4 = new byte[4];
                file.Read(first4, 0, 4);

                foreach (KeyValuePair<string, byte[]> pair in fileTypes)
                {
                    if (first4.SequenceEqual(pair.Value))
                    {
                        Console.WriteLine("[XBS-Tool]: No ZLib?, Continuing...");
                        noZLib = true;
                    }
                }

                //Put file buffer pos back to offset.
                file.Seek(offset, SeekOrigin.Begin);

                if(!noZLib)
                {
                    try
                    {
                        using (var resultStream = new MemoryStream())
                        {
                            using (var decompressionStream = new InflaterInputStream(file, new Inflater()))
                            {
                                decompressionStream.CopyTo(resultStream);
                                Console.WriteLine("[XBS-Tool]: ZLib removed.");
                                return resultStream.ToArray();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[XBS-Tool]: ZLib failed -> {ex.Message}.");
                        return new byte[] { };
                    }
                }
                else
                {
                    byte[] wholeFile = new byte[file.Length];
                    file.Read(wholeFile, 0, (int)file.Length);
                    return wholeFile;
                }
            }
        }

        public void printTitle()
        {
            Console.WriteLine(FiggleFonts.Merlin1.Render("danny"));
        }

        public static void Main(string[] args)
        {
            Program p = new Program(args);
        }
    }
}
