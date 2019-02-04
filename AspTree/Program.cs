using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DotNet.Globbing;

namespace AspTree
{
    class AspFile
    {
        private readonly HashSet<AspFile> _dependencies = new HashSet<AspFile>();
        private readonly HashSet<AspFile> _dependents = new HashSet<AspFile>();

        public string FilePath { get; }

        public IEnumerable<AspFile> Dependencies => _dependencies;
        public IEnumerable<AspFile> Dependents => _dependents;

        public AspFile(string filePath)
        {
            FilePath = filePath;
        }

        public AspFile DependsOn(AspFile dependency)
        {
            if (!_dependencies.Contains(dependency)) _dependencies.Add(dependency);
            return this;
        }

        public AspFile DependentOf(AspFile other)
        {
            if (!_dependents.Contains(other)) _dependents.Add(other);
            return this;
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }
    }
    
    class Program
    {
        private static Dictionary<string, AspFile> _files = new Dictionary<string, AspFile>();
        
        static void Main(string[] args)
        {
            string folderPath = string.Empty;
            string filePath = null;
            if (args.Length < 1)
            {
                Console.Write("Path to folder to search: ");
                folderPath = Console.ReadLine();
            }
            else if (args.Length < 2)
            {
                folderPath = args[0];
            }
            else
            {
                folderPath = args[0];
                filePath = args[1];
            }
            
            Console.WriteLine("Recursively searching for files that fit the pattern '*.asp'.");
            var files = Directory.GetFiles(folderPath, "*.asp", SearchOption.AllDirectories);
            
            Console.WriteLine($"Found {files.Length} files.");

            var rootUri = new Uri(folderPath);
            foreach (var file in files)
            {
                /*
                 * we can shorten the apparent path of the file to make it more friendly to read in the node graph
                 * ... this also potentially improves comparison times in the dictionary
                 */
                var fileUri = new Uri(file);
                var diff = rootUri.MakeRelativeUri(fileUri);
                
                _files.TryAdd(file, new AspFile(diff.OriginalString));
            }
            
            /*
             * compiled regex operators for virtual and file asp includes
             */
            var virtualInclude = new Regex("<!--\\s*#include\\s+virtual\\s*=\\s*\"(.+)\"\\s*-->");
            var fileInclude = new Regex("<!--\\s*#include\\s+file\\s*=\\s*\"(.+)\"\\s*-->");

            foreach (var kvp in _files)
            {
                /*
                 * start parsing the file for dependencies
                 */
                Console.WriteLine($"Opening {kvp.Value.FilePath} to scan for dependencies.");
                using (var file = File.Open(kvp.Key, FileMode.Open, FileAccess.Read, FileShare.None))
                using (var reader = new StreamReader(file))
                {
                    var text = reader.ReadToEnd();

                    var virtualMatches = virtualInclude.Matches(text);
                    var fileMatches = fileInclude.Matches(text);

                    /*
                     * from all of the matches, get all of the files in our dictionary that match
                     */
                    var virtualDependencies = virtualMatches.Select(
                        (match) =>
                        {
                            var includedFile = match.Groups[1].Value.Substring(1);
                            var fullPath = new Uri(Path.Combine(folderPath, includedFile));
                            
                            Console.WriteLine($"FullPath from {folderPath} and {includedFile}: {fullPath.AbsolutePath}");
                            
                            return _files.TryGetValue(fullPath.AbsolutePath, out var matchedFile) ? matchedFile : null;
                        });
                    var fileDependencies = fileMatches.Select(
                        (match) =>
                        {
                            var parentDir = Path.GetDirectoryName(kvp.Key);
                            var includedFile = match.Groups[1].Value;
                            var fullPath = new Uri(Path.Combine(parentDir, includedFile));
                            
                            Console.WriteLine($"FullPath from {parentDir} and {includedFile}: {fullPath.AbsolutePath}");
                            
                            return _files.TryGetValue(fullPath.AbsolutePath, out var matchedFile) ? matchedFile : null;
                        });

                    /*
                     * link all of our dependencies to each other
                     */
                    foreach (var dependency in virtualDependencies)
                    {
                        if (dependency == null) continue;
                        
                        dependency.DependentOf(kvp.Value);
                        kvp.Value.DependsOn(dependency);
                    }

                    foreach (var dependency in fileDependencies)
                    {
                        if (dependency == null) continue;
                        
                        dependency.DependentOf(kvp.Value);
                        kvp.Value.DependsOn(dependency);
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine("digraph asptree {");

            if (filePath != null)
            {
                if (_files.TryGetValue(Path.Combine(rootUri.AbsolutePath, filePath), out var rootFile))
                {
                    /*
                    * get dependency all files that depend on this file
                    */
                    foreach (var dependency in rootFile.Dependencies) {
                        sb.AppendLine($"    \"{rootFile.FilePath}\" -> \"{dependency.FilePath}\";");
                    }

                    /*
                    * get dependent graph of this file
                    */
                    foreach (var dependent in _files.Where(kvp => kvp.Value.Dependencies.Contains(rootFile)))
                    {
                        sb.AppendLine($"    \"{dependent.Value.FilePath}\" -> \"{rootFile.FilePath}\";");
                    }
                }
            }
            else
            {
                foreach (var kvp in _files)
                {
                    foreach (var dependency in kvp.Value.Dependencies)
                    {
                        sb.AppendLine($"    \"{kvp.Value.FilePath}\" -> \"{dependency.FilePath}\";");
                    }
                }
            }
            
            sb.AppendLine("}");
            
            File.WriteAllText("out.txt", sb.ToString());
        }
    }
}
