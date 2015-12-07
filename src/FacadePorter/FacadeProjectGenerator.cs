using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
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
            string specialNetNativeJsonText = (info.HasNetCoreForCoreClrBuild && info.ProjectNVersion != null)
                ? (Environment.NewLine + Environment.NewLine + SpecialNetNativeJson)
                : string.Empty;

            // targeting pack assembly references
            string assemblyRefs = "";
            if (info.ProjectKVersion != null || info.DesktopVersion != null)
            {
                assemblyRefs += Environment.NewLine + string.Format(ReferenceFormat, "mscorlib", Condition_DoesNotTargetAot);
            }
            if (info.DesktopVersion != null)
            {
                string[] desktopRefs = GetPartialFacadeRefs(info.Name, "Desktop");
                foreach (string assm in desktopRefs)
                {
                    assemblyRefs += Environment.NewLine + string.Format(ReferenceFormat, assm, Condition_TargetsDesktop);
                }
            }

            if (info.ProjectNVersion != null)
            {
                string[] projectNRefs = GetPartialFacadeRefs(info.Name, "NETNative");
                foreach (string assm in projectNRefs)
                {
                    assemblyRefs += Environment.NewLine + string.Format(ReferenceFormat, assm, Condition_TargetsAot);
                }
            }

            string projectRefsText = "";
            if (info.ProjectKVersion != null)
            {
                string[] projectKRefs = GetPartialFacadeRefs(info.Name, "ProjectK");
                if (projectKRefs.Length > 0)
                {
                    foreach (string refName in projectKRefs)
                    {
                        string refPath = $"$(SourceDir){refName}\\src\\{refName}.csproj";
                        projectRefsText += Environment.NewLine;
                        projectRefsText += string.Format(ProjectRefFormat, refPath);
                    }
                }
            }
            string projectRefsFullItemGroup = "";
            if (projectRefsText != "")
            {
                projectRefsFullItemGroup = Environment.NewLine +
                    string.Format(
                        ProjectRefsItemGroupFormat,
                        "'$(TargetGroup)' != 'netcore50aot'",
                        projectRefsText);
            }

            string defaultConfigurationBlock = null;
            if (info.ProjectKVersion != null)
            {
                defaultConfigurationBlock = ""; // Don't specify, just use ambient default (CoreCLR).
            }
            else if (info.ProjectNVersion != null)
            {
                string config = info.HasNetCoreForCoreClrBuild ? "netcore50aot" : "netcore50";
                defaultConfigurationBlock = Environment.NewLine + string.Format(DefaultConfigFormat, config);
            }
            else if (info.DesktopVersion != null)
            {
                defaultConfigurationBlock = Environment.NewLine + string.Format(DefaultConfigFormat, "net46");
            }
            else
            {
                throw new InvalidOperationException("Default configuration didn't get set somehow");
            }

            string fileText = string.Format(
                s_projTemplateText,
                info.Name,
                assemblyVersionBlock,
                configurationsBlock,
                specialNetNativeJsonText,
                assemblyRefs,
                projectRefsFullItemGroup,
                defaultConfigurationBlock);

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

            if (info.HasNetCoreForCoreClrBuild)
            {
                if (projectJsonBody != "")
                {
                    projectJsonBody += ',' + Environment.NewLine;
                }

                projectJsonBody += NetCoreForCoreClrJsonRef;
            }
            else if (info.ProjectNVersion != null)
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

            if (info.HasNetCoreForCoreClrBuild && info.ProjectNVersion != null)
            {
                string netNativeJsonText = string.Format(ProjectJsonFileFormat, NetNativeJsonRef);
                Directory.CreateDirectory(Path.Combine(outputPath, "NetNative"));
                string netNativeJsonOutputFile = Path.Combine(outputPath, "NetNative", "project.json");
                File.WriteAllText(netNativeJsonOutputFile, netNativeJsonText);
            }
        }

        private string[] GetPartialFacadeRefs(string name, string platform)
        {
            string facadeFile = Path.Combine(AppContext.BaseDirectory, "Facades", platform, name + ".dll");
            if (!File.Exists(facadeFile))
            {
                throw new InvalidOperationException($"Couldn't find {platform} facade for " + name);
            }
            else
            {
                using (var stream = File.OpenRead(facadeFile))
                using (var peReader = new PEReader(stream))
                {
                    var mdReader = peReader.GetMetadataReader();
                    var refs = mdReader.AssemblyReferences.Select(arh => mdReader.GetAssemblyReference(arh))
                        .Select(ar => mdReader.GetString(ar.Name)).Where(s => s != "mscorlib");
                    return refs.ToArray();
                }
            }
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

        private string Condition_TargetsDesktop => string.Format(ConditionFormat, "'$(TargetGroup)' == 'net46'");
        private string Condition_TargetsAot => string.Format(ConditionFormat, "'$(TargetGroup)' == 'netcore50aot'");
        private string Condition_DoesNotTargetAot => string.Format(ConditionFormat, "'$(TargetGroup)' != 'netcore50aot'");
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
                ""Microsoft.TargetingPack.Private.CoreCLR"": ""1.0.0-rc2-23604""
            }
        }";
        private const string DesktopJsonRef =
@"        ""net46"": {
            ""dependencies"": {
                ""Microsoft.TargetingPack.NETFramework.v4.6"": ""1.0.0-rc2-23530""
            }
        }";

        private const string NetCoreForCoreClrJsonRef =
@"        ""netcore50"": {
            ""dependencies"": {
                ""Microsoft.TargetingPack.Private.CoreCLR"": ""1.0.0-rc2-23604""
            }
        }";

        private const string NetNativeJsonRef =
@"        ""netcore50"": {
            ""dependencies"": {
                ""Microsoft.TargetingPack.Private.NetNative"": ""1.0.0-rc2-23607""
            }
        }";

        private const string SpecialNetNativeJson =
@"  <PropertyGroup Condition="" '$(TargetGroup)' == 'netcore50aot' "">
    <ProjectJson>NetNative\project.json</ProjectJson>
    <ProjectLockJson>NetNative\project.lock.json</ProjectLockJson>
  </PropertyGroup>";

        private const string ProjectRefsItemGroupFormat =
@"  <ItemGroup Condition=""{0}"">{1}
  </ItemGroup>
";

        private const string ProjectRefFormat =
@"    <ProjectReference Include=""{0}"" />";

        private const string DefaultConfigFormat =
@"  <PropertyGroup>
    <!-- Setting default TargetGroup before importing dir.prop -->
    <TargetGroup Condition=""'$(TargetGroup)' == ''"">{0}</TargetGroup>
  </PropertyGroup>
";
    }
}
