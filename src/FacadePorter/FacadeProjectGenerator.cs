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

            // Special .NET Native project.json references
            string specialNetNativeJsonText = (info.HasNetCoreForCoreClrBuild) ? SpecialNetNativeJson : string.Empty;

            // targeting pack assembly references
            string assemblyRefs = "";
            if (info.ProjectKVersion != null || info.DesktopVersion != null)
            {
                assemblyRefs += Environment.NewLine + string.Format(ReferenceFormat, "mscorlib", Condition_DoesNotTargetAot);
            }
            if (info.DesktopVersion != null)
            {
                string[] desktopRefs = GetDesktopRefs(info.Name);
                foreach (string assm in desktopRefs)
                {
                    assemblyRefs += Environment.NewLine + string.Format(ReferenceFormat, assm, Condition_TargetsDesktop);
                }
            }

            if (info.ProjectNVersion != null)
            {
                string[] projectNRefs = GetProjectNRefs(info.Name);
                foreach (string assm in projectNRefs)
                {
                    assemblyRefs += Environment.NewLine + string.Format(ReferenceFormat, assm, Condition_TargetsAot);
                }
            }

            string fileText = string.Format(
                s_projTemplateText,
                info.Name,
                assemblyVersionBlock,
                configurationsBlock,
                specialNetNativeJsonText,
                assemblyRefs);

            string outputPath = Path.Combine(outputDir, info.Name, "src", "facade");
            Directory.CreateDirectory(outputPath);
            string outputFile = Path.Combine(outputPath, info.Name + ".csproj");
            File.WriteAllText(outputFile, fileText);

            // Create project.json for facade project.
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

        private string[] GetProjectNRefs(string name)
        {
            return new string[]
            {
                "System.Private.CoreLib",
                "System.Private.Reflection"
            };
        }

        private string[] GetDesktopRefs(string name)
        {
            return new string[]
            {
                "System",
                "System.Core",
                "System.ComponentModel.Composition"
            };
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
        private string Condition_DoesNotTargetAot => string.Format(ConditionFormat, "'$(IsAot)' != 'true'");
        private string Condition_None => string.Empty;

        private const string ReferenceFormat =
@"    <TargetingPackReference Include=""{0}""{1} />";

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

        private const string SpecialNetNativeJson =
@"  <PropertyGroup Condition="" '$(IsAot)' == 'true' "">
    <ProjectJson>NetNative\project.json</ProjectJson>
    <ProjectLockJson>NetNative\project.lock.json</ProjectLockJson>
  </PropertyGroup>
";
    }
}
