using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace assemblyversion
{
    class Program
    {
        static private string inputPathFile = "Properties\\AssemblyInfo.cs";
        static private string sourcePath;
        static private string step;
        static private string version;
        static private string content;
        static private string DefaultPattern = "(?<={0}\\(\").*?(?=\"\\))";


        private static string replaceTag(string key, string substitution)
        {
            string pattern = string.Format(DefaultPattern, key);

            RegexOptions options = RegexOptions.Multiline;

            Regex regex = new Regex(pattern, options);
            return regex.Replace(content, substitution);
        }

        private static string getTag(string key)
        {
            string pattern = string.Format(DefaultPattern, key);

            RegexOptions options = RegexOptions.Multiline;

            Regex regex = new Regex(pattern, options);
            return regex.Match(content).Value;
        }

        static string getVersion(string key)
        {
            string pattern = string.Format(DefaultPattern, key);
            RegexOptions options = RegexOptions.Multiline;
            Regex regex = new Regex(pattern, options);
            return regex.Match(content).Value;
        }

        // retrive numeric base version
        static string getVersionBase(string version)
        {
            // from git version v2.0.0.0-5-sdfsdfsdf"
            string pattern = "\\d\\.\\d.\\d";
            Regex regex = new Regex(pattern);
            string bVersion = regex.Match(version).Value + ".{0}";
            regex = new Regex("(?<=\\-).\\d?(?=\\-)");
            // add build
            string buildVersion = regex.Match(version).Value;
            if (buildVersion == "") buildVersion = "0";
            bVersion = string.Format(bVersion, buildVersion);

            return bVersion;
        }


        static string RunCommand(string args, Process _gitProcess)
        {
            _gitProcess.StartInfo.Arguments = args;
            _gitProcess.Start();            
            _gitProcess.WaitForExit();
            string output = _gitProcess.StandardOutput.ReadToEnd().Trim();
            return output;
        }

        static Process RepositoryInformation(string gitPath)
        {
            var processInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = "git.exe",
                CreateNoWindow = true,
                WorkingDirectory = gitPath
            };

            Process _gitProcess = new Process();
            _gitProcess.StartInfo = processInfo;
            return _gitProcess;
        }

        static string gitDescribe(Process _gitProcess)
        {
            return RunCommand("describe --dirty", _gitProcess);
        }

        static void Main(string[] args)
        {            
            if (args.Length<2)
            {
                Console.WriteLine("Needed Arguments: cmd [sourcePath] [step-puild]");
                return;
            }    

            sourcePath = args[0];
            step = args[1];
            Process gitProcess = RepositoryInformation(sourcePath);

            if (args.Length > 2)
                version = args[2];
            else
                version = gitDescribe(gitProcess);

            // Compatible version AssembyFile
            string versionBase = getVersionBase(version);
            
            if (args.Length > 3)
                inputPathFile = args[3];

            string filePath = Path.Combine(sourcePath, inputPathFile);
            if (!File.Exists(filePath))
            {
                Console.WriteLine("File assembly not found!!: " + filePath);
                return;
            }
            
            switch (step)
            {
                case "pre-build":
                    {
                        content = File.ReadAllText(filePath);
                        // original description
                        string description = getTag("AssemblyDescription");
                        // save a copy
                        File.WriteAllText(filePath + ".ori", content);
                        // replace file version
                        content = replaceTag("AssemblyVersion", versionBase);
                        // version dirty
                        if (version.Contains("-dirty"))
                            content = replaceTag("AssemblyFileVersion", version);
                        else
                            content = replaceTag("AssemblyFileVersion", versionBase);

                        // write description file alphanumeric
                        content = replaceTag("AssemblyDescription", description + " " + version);

                        File.WriteAllText(filePath, content);
                    }
                    break;
                case "post-build":
                    {
                        // restore old file
                        content = File.ReadAllText(filePath + ".ori");
                        File.WriteAllText(filePath, content);
                    }
                    break;
                default:
                    Console.WriteLine("No step build found: " + step);
                    break;
            }
        }
    }
}
