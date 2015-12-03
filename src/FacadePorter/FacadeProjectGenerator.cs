using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FacadePorter
{
    public class FacadeProjectGenerator
    {
        private static string s_projTemplateText = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "FacadeProjTemplate.xml"));

        internal void GenerateFacadeProject(FacadeBuildInfo info, string outputDir)
        {
            string assemblyVersionBlock = "";
            string configurationsBlock = "";

            if (info.ProjectKVersion != null)
            {
                assemblyVersionBlock += Environment.NewLine + string.Format(AssemblyVersionFormat, string.Empty, info.ProjectKVersion);
                configurationsBlock += Environment.NewLine + ProjectKConfigurations;
            }
            if (info.ProjectNVersion != null)
            {
                if (info.ProjectNVersion != info.ProjectKVersion)
                {
                    assemblyVersionBlock += Environment.NewLine +
                        string.Format(
                            AssemblyVersionFormat,
                                (info.ProjectKVersion != null
                                ? Condition_TargetsAot
                                : string.Empty),
                            info.ProjectNVersion);
                }
                configurationsBlock += Environment.NewLine + ProjectNConfigurations;
            }
            if (info.HasNetCoreForCoreClrBuild)
            {
                configurationsBlock += Environment.NewLine + NetCoreForCoreClrConfigurations;
            }
            if (info.DesktopVersion != null)
            {
                if (info.DesktopVersion != info.ProjectKVersion)
                {
                    assemblyVersionBlock += Environment.NewLine +
                        string.Format(
                            AssemblyVersionFormat,
                                (info.ProjectKVersion != null
                                ? Condition_TargetsDesktop
                                : string.Empty),
                            info.DesktopVersion);
                }

                configurationsBlock += Environment.NewLine + DesktopConfigurations;
            }

            string fileText = string.Format(
                s_projTemplateText,
                info.Name,
                assemblyVersionBlock,
                configurationsBlock);

            string outputPath = Path.Combine(outputDir, info.Name, "src", "facade");
            Directory.CreateDirectory(outputPath);
            string outputFile = Path.Combine(outputPath, info.Name + ".csproj");
            File.WriteAllText(outputFile, fileText);

            string projectJsonBody = "";
            if (info.ProjectKVersion != null)
            {
                projectJsonBody += DnxCore50JsonRef;
            }

            if (info.ProjectNVersion != null)
            {
                if (projectJsonBody != "")
                {
                    projectJsonBody += ',' + Environment.NewLine;
                }

                projectJsonBody += NetNativeJsonRef;
            }

            if (info.DesktopVersion != null)
            {
                if (projectJsonBody != "")
                {
                    projectJsonBody += ',' + Environment.NewLine;
                }

                projectJsonBody += DesktopJsonRef;
            }

            string projectJsonFull = string.Format(ProjectJsonFileFormat, projectJsonBody);
            string jsonOutputFile = Path.Combine(outputPath, "project.json");
            File.WriteAllText(jsonOutputFile, projectJsonFull);
        }

        private const string ProjectKConfigurations =
@"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "" />
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "" />";

        private const string ProjectNConfigurations =
@"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'netcore50aot_Debug|AnyCPU' "" />
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'netcore50aot_Release|AnyCPU' "" />";

        private const string NetCoreForCoreClrConfigurations =
@"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'netcore50_Debug|AnyCPU' "" />
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'netcore50_Release|AnyCPU' "" />";

        private const string DesktopConfigurations =
@"  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'net46_Debug|AnyCPU' "" />
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'net46_Release|AnyCPU' "" />";

        private const string AssemblyVersionFormat =
@"    <AssemblyVersion{0}>{1}</AssemblyVersion>";

        private const string ConditionFormat =
@" Condition="" {0} """;

        private string Condition_TargetsDesktop => string.Format(ConditionFormat, "'$(TargetsDesktop)' == 'true'");
        private string Condition_TargetsAot => string.Format(ConditionFormat, "'$(IsAot)' == 'true'");
        private string Condition_None => string.Empty;

        private const string MscorlibReferenceFormat =
@"    <TargetingPackReference Include=""mscorlib"" Condition="" '$(IsAot)' != 'true' "" />
";

        private const string ProjectJsonFileFormat =
@"{{
    ""frameworks"": {{
{0}
    }}
}}";

        private const string DnxCore50JsonRef =
@"        ""dnxcore50"": {
            ""dependencies"": {
                ""Microsoft.TargetingPack.Private.CoreCLR"": ""1.0.0-rc2-23530""
            }
        }";
        private const string DesktopJsonRef =
@"        ""net46"": {
            ""dependencies"": {
                ""Microsoft.TargetingPack.NETFramework.v4.6"": ""1.0.0-rc2-23530""
            }
        }";

        private const string NetNativeJsonRef =
@"        ""netcore50"": {
            ""dependencies"": {
                ""Microsoft.TargetingPack.Private.NetNative"": ""1.0.0-rc2-23530""
            }
        }";
    }
}
