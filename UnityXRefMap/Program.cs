using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityXRefMap.Yaml;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace UnityXRefMap
{
    internal class Program
    {
        private static readonly string UnityCsReferenceRepositoryUrl = "https://github.com/Unity-Technologies/UnityCsReference";
        private static readonly string UnityCsReferenceLocalPath = Path.Join(Environment.CurrentDirectory, "UnityCsReference");
        private static readonly string GeneratedMetadataPath = Path.Join(Environment.CurrentDirectory, "ScriptReference");
        private static readonly string OutputFolder = Path.Join(Environment.CurrentDirectory, "out");

        private static void Main(string[] args)
        {
            if (!Directory.Exists(UnityCsReferenceLocalPath))
            {
                Repository.Clone(UnityCsReferenceRepositoryUrl, UnityCsReferenceLocalPath);
            }

            using (var repo = new Repository(UnityCsReferenceLocalPath))
            {
                Regex branchRegex = new Regex(@"^origin/(\d{4}\.\d+)$");

                foreach (Branch branch in repo.Branches.OrderByDescending(b => b.FriendlyName))
                {
                    Match match = branchRegex.Match(branch.FriendlyName);

                    if (!match.Success) continue;

                    string version = match.Groups[1].Value;

                    if (args.Length > 0 && Array.IndexOf(args, version) == -1)
                    {
                        Logger.Warning($"Skipping '{branch.FriendlyName}'");
                        continue;
                    }

                    Logger.Info($"Checking out '{branch.FriendlyName}'");

                    Commands.Checkout(repo, branch);

                    repo.Reset(ResetMode.Hard);
                    repo.RemoveUntrackedFiles();

                    int exitCode = RunDocFx();

                    if (exitCode != 0)
                    {
                        Logger.Error($"DocFX exited with code {exitCode}");
                        continue;
                    }

                    GenerateMap(version);
                }
            }
        }

        private static int RunDocFx()
        {
            Logger.Info("Running DocFX");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    FileName = "docfx",
                    Arguments = "metadata",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.OutputDataReceived += (sender, args) => Logger.Trace("[DocFX]" + args.Data, 1);
            process.ErrorDataReceived += (sender, args) => Logger.Error("[DocFX]" + args.Data);

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            return process.ExitCode;
        }

        private static void GenerateMap(string version)
        {
            Logger.Info($"Generating XRef map for Unity {version}");

            var serializer = new Serializer();
            var deserializer = new Deserializer();

            var references = new List<XRefMapReference>();

            foreach (var file in Directory.GetFiles(GeneratedMetadataPath, "*.yml"))
            {
                Logger.Trace($"Reading '{file}'", 1);

                using (TextReader reader = new StreamReader(file))
                {
                    if (reader.ReadLine() != "### YamlMime:ManagedReference") continue;

                    YamlMappingNode reference = deserializer.Deserialize<YamlMappingNode>(reader);

                    foreach (YamlMappingNode item in (YamlSequenceNode)reference.Children["items"])
                    {
                        string fullName = Normalize(item.GetScalarValue("fullName"));
                        string name     = Normalize(item.GetScalarValue("name"));
                        string type     = item.GetScalarValue("type");
                        string parent   = item.GetScalarValue("parent");

                        string documentationFileName;

                        switch (type)
                        {
                            case "Property":
                            case "Field":
                                if (char.IsLower(name[0]))
                                {
                                    documentationFileName = parent + "-" + name;
                                }
                                else
                                {
                                    documentationFileName = parent + "." + name;
                                }

                                break;

                            default:
                                documentationFileName = fullName;
                                break;
                        }

                        if (documentationFileName.StartsWith("UnityEngine.") || documentationFileName.StartsWith("UnityEditor."))
                        {
                            documentationFileName = documentationFileName.Substring(12);
                        }

                        string url = $"https://docs.unity3d.com/Documentation/ScriptReference/{documentationFileName}.html";

                        Logger.Trace($"Adding reference to '{fullName}'", 2);

                        references.Add(new XRefMapReference
                        {
                            Uid = item.GetScalarValue("uid"),
                            Name = name,
                            Href = url,
                            CommentId = item.GetScalarValue("commentId"),
                            FullName = fullName,
                            NameWithType = item.GetScalarValue("nameWithType")
                        });
                    }
                }
            }

            var serializedMap = serializer.Serialize(new XRefMap
            {
                Sorted = true,
                References = references.OrderBy(r => r.Uid).ToArray()
            });

            Directory.CreateDirectory(OutputFolder);

            string outputFilePath = Path.Join(OutputFolder, $"unity_{version}_xrefmap.yml");

            Logger.Info($"Saving XRef map to '{outputFilePath}'");

            File.WriteAllText(outputFilePath, "### YamlMime:XRefMap\n" + serializedMap);
        }

        private static string Normalize(string text)
        {
            if (text.Contains('(')) text = text.Remove(text.IndexOf('('));
            if (text.Contains('<')) text = text.Remove(text.IndexOf('<'));

            text = text.Replace('`', '_');
            text = text.Replace("#ctor", "ctor");

            return text;
        }
    }
}
