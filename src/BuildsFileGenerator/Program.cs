﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BuildsFileGenerator
{
    class Program
    {
        private const string usageText =
@"Usage: BuildsFileGenerator.exe <corefx-src-dir>
    - Updates/Generates "".builds"" MSBuild project files as necessary in the corefx directory.";

        public static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine(usageText);
                return 1;
            }

            string corefxSrcPath = args[0];
            foreach (string libraryDir in Directory.EnumerateDirectories(corefxSrcPath))
            {
                string projSrcDir = Path.Combine(libraryDir, "src");
                try
                {
                    var projFiles = Directory.EnumerateFiles(projSrcDir, "*.csproj", SearchOption.AllDirectories).ToArray();
                    if (projFiles.Length > 0)
                    {
                        CreateBuildsFile(projSrcDir, projFiles);
                    }
                }
                catch (DirectoryNotFoundException) { }
            }

            return 0;
        }

        private static void CreateBuildsFile(string projSrcDir, string[] projFiles)
        {
            string includesBlock = "";
            string libName = new DirectoryInfo(projSrcDir).Parent.Name;

            foreach (string csprojPath in projFiles)
            {
                string csproj = File.ReadAllText(csprojPath);
                string relativePath = csprojPath.Substring(projSrcDir.Length + 1);
                bool foundConfig = false;
                if (csproj.Contains("'Debug|AnyCPU'") || csproj.Contains("'Windows_Debug|AnyCPU'"))
                {
                    includesBlock += Environment.NewLine
                        + string.Format(ProjectIncludeFormat, relativePath, "");
                    foundConfig = true;
                }

                if (csproj.Contains("'netcore50aot_Debug|AnyCPU'"))
                {
                    includesBlock += Environment.NewLine
                        + string.Format(ProjectIncludeFormat, relativePath, "netcore50aot");
                    foundConfig = true;
                }

                if (csproj.Contains("'netcore50_Debug|AnyCPU'"))
                {
                    includesBlock += Environment.NewLine
                        + string.Format(ProjectIncludeFormat, relativePath, "netcore50");
                    foundConfig = true;
                }

                if (csproj.Contains("'net46_Debug|AnyCPU'"))
                {
                    includesBlock += Environment.NewLine
                        + string.Format(ProjectIncludeFormat, relativePath, "net46");
                    foundConfig = true;
                }

                if (!foundConfig)
                {
                    includesBlock += Environment.NewLine
                        + string.Format(ProjectIncludeFormat, relativePath, "");
                }

            }

            string fileText = string.Format(s_buildsFileTemplateFormat, includesBlock);
            File.WriteAllText(Path.Combine(projSrcDir, $"{libName}.builds"), fileText);
        }

        private const string ProjectIncludeFormat =
@"    <Project Include=""{0}"">
      <AdditionalProperties>TargetGroup={1}</AdditionalProperties>
    </Project>";

        private static readonly string s_buildsFileTemplateFormat = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "BuildsFileTemplate.xml"));
    }
}
