using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Torrent;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreateTreeFromTorrent
{
    class Program
    {
        static readonly long start = DateTime.Now.ToBinary();

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine(Path.GetFileName(Assembly.GetEntryAssembly().Location) + " <source> <output>");
                Console.WriteLine("source\tPath to the torrent files to be parsed");
                Console.WriteLine("output\tPath for the directory trees to be created at");
            }
            else
            {
                var paths = args.Select(x => x.EndsWith("\\") ? x.Substring(0, x.Length - 1) : x).ToList();
                paths.Remove(paths.Last());
                CreateTree(SearchFiles(paths), args.Last());
            }
        }

        static Dictionary<string, int> SearchFiles(List<string> paths, int level = 0)
        {
            var files = new Dictionary<string, int>();
            foreach (var path in paths)
            {
                foreach (var file in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path)))
                {
                    if (Path.GetExtension(file) == ".torrent")
                        files.Add(file, level);
                }
                foreach (var file in SearchFiles(Directory.GetDirectories(Path.GetDirectoryName(path), Path.GetFileName(path)).Select(x => x + "\\*").ToList(), level + 1))
                {
                    files.Add(file.Key, file.Value);
                }
            }
            return files;
        }

        static void CreateTree(Dictionary<string, int> files, string dir)
        {
            foreach (var file in files)
            {
                try
                {
                    Console.WriteLine(file.Key);
                    var metadata = Metadata.FromFile(file.Key);
                    var metadataFields = typeof(Metadata).GetFields(BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
                    var fileentries = (Dictionary<string, long>)metadataFields[11].GetValue(metadata);
                    foreach (var fileentry in fileentries)
                    {
                        var filename = fileentry.Key;
                        var bytes = Encoding.GetEncoding(1252).GetBytes(filename);
                        filename = Encoding.GetEncoding("utf-8").GetString(bytes);
                        var directory = Path.GetDirectoryName(file.Key);
                        var directorySegments = directory.Split("\\".ToCharArray(), directory.Count(x => x == '\\') - file.Value + 1);
                        var target = directorySegments.Last().Contains('\\') ? directorySegments.Last().Split("\\".ToCharArray(), 2)[1] : "";
                        var path = @"\\?\" + Path.GetFullPath(dir + '\\' + (string.IsNullOrEmpty(target) ? null : '\\' + Regex.Replace(target, "[\\/:*?\"<>|]", "_") + '\\') + Regex.Replace(Path.GetFileNameWithoutExtension(file.Key), "[\\/:*?\"<>|]", "_") + '\\' + Regex.Replace(filename, "[\\/:*?\"<>|]", "_"));
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        File.Create(path).Close();
                    }
                }
                catch (Exception e)
                {
                    File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Temp\CreateTreeFromTorrent_" + start + "_error.log", e.ToString() + "\r\n");
                }
            }
        }
    }
}
