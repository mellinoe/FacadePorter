using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace FacadePorter
{
    public class Program
    {
        public static unsafe void Main(string[] args)
        {
            string outputDir = "Output";
            Directory.CreateDirectory(outputDir);

            string csvText = File.ReadAllText("Contracts.csv");
            StringReader sr = new StringReader(csvText);
            sr.ReadLine(); // Skip header.
            List<FacadeBuildInfo> infos = new List<FacadeBuildInfo>();

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                FacadeBuildInfo fbi = FacadeBuildInfo.ParseFromLine(line);

                if (fbi.ProjectNVersion != null || fbi.DesktopVersion != null || fbi.ProjectKVersion != null)
                {
                    infos.Add(fbi);
                }
            }

            FacadeProjectGenerator fpg = new FacadeProjectGenerator();
            foreach (FacadeBuildInfo info in infos)
            {
                fpg.GenerateFacadeProject(info, outputDir);
            }
        }
    }

    public class FacadeBuildInfo
    {
        public string Name { get; set; }
        public Version ProjectNVersion { get; set; }
        public Version WindowsStoreVersion { get; set; }
        public Version DesktopVersion { get; set; }
        public Version ProjectKVersion { get; set; }
        public Version TestNetVersion { get; set; }
        public Version PhoneVersion { get; set; }
        public bool HasNetCoreForCoreClrBuild { get; set; }

        public static unsafe FacadeBuildInfo ParseFromLine(string line)
        {
            string[] elements = line.Split(',');
            FacadeBuildInfo fbi = new FacadeBuildInfo();
            fbi.Name = elements[0];
            if (elements[2] == "Y")
            {
                fbi.ProjectNVersion = Version.Parse(elements[1]);
            }

            if (elements[4] == "Y")
            {
                fbi.WindowsStoreVersion = Version.Parse(elements[3]);
            }

            if (elements[6] == "Y")
            {
                fbi.DesktopVersion = Version.Parse(elements[5]);
            }

            if (elements[8] == "Y")
            {
                fbi.ProjectKVersion = Version.Parse(elements[7]);
            }

            if (elements[10] == "Y")
            {
                fbi.TestNetVersion = Version.Parse(elements[9]);
            }

            if (elements[12] == "Y")
            {
                fbi.PhoneVersion = Version.Parse(elements[11]);
            }

            string ncfccPath = Path.Combine(AppContext.BaseDirectory, "Facades", "NetCoreForCoreCLR", fbi.Name + ".dll");
            if (File.Exists(ncfccPath))
            {
                if (IsFullFacade(ncfccPath))
                {
                    fbi.HasNetCoreForCoreClrBuild = true;
                }
            }

            return fbi;
        }

        private static readonly string[] s_skippedTypes =
        {
            "<Module>"
        };

        private static bool IsFullFacade(string ncfccPath)
        {
            using (var stream = File.OpenRead(ncfccPath))
            using (var peReader = new PEReader(stream))
            {
                var mdReader = peReader.GetMetadataReader();
                var realTypes = mdReader.TypeDefinitions.Select(tdh => mdReader.GetString(mdReader.GetTypeDefinition(tdh).Name))
                    .Where(s => !s_skippedTypes.Contains(s)).ToArray();
                bool isFullFacade = realTypes.Length == 0;

                return isFullFacade;
            }
        }
    }
}