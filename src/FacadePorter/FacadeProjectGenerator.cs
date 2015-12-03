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
                                ? string.Format(ConditionFormat, "'$(TargetsProjectN)' == 'true'")
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
                                ? string.Format(ConditionFormat, "'$(TargetsDesktop)' == 'true'")
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

            string outputPath = Path.Combine(outputDir, info.Name + ".csproj");
            File.WriteAllText(outputPath, fileText);
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
    }
}
