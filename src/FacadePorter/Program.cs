using System;
using System.Collections.Generic;
using System.IO;

namespace AssemblyBrowser
{
    public class Program
    {
        public static unsafe void Main(string[] args)
        {
            string csvText = File.ReadAllText("Contracts.csv");
            StringReader sr = new StringReader(csvText);
            sr.ReadLine(); // Skip header.
            List<FacadeBuildInfo> infos = new List<FacadeBuildInfo>();

            string line;
            while ((line = sr.ReadLine()) != null)
            {
                FacadeBuildInfo fbi = FacadeBuildInfo.ParseFromLine(line);

                if (fbi.ProjectNVersion != null || fbi.WindowsStoreVersion != null || fbi.DesktopVersion != null
                    || fbi.ProjectKVersion != null || fbi.TestNetVersion != null || fbi.PhoneVersion != null)
                {
                    infos.Add(fbi);
                }
            }
        }
    }

    public class FacadeBuildInfo
    {
        public string Name { get; set; }
        public string ProjectNVersion { get; set; }
        public string WindowsStoreVersion { get; set; }
        public string DesktopVersion { get; set; }
        public string ProjectKVersion { get; set; }
        public string TestNetVersion { get; set; }
        public string PhoneVersion { get; set; }

        public static unsafe FacadeBuildInfo ParseFromLine(string line)
        {
            string[] elements = line.Split(',');
            FacadeBuildInfo fbi = new FacadeBuildInfo();
            fbi.Name = elements[0];
            if (elements[2] == "Y")
            {
                fbi.ProjectNVersion = elements[1];
            }

            if (elements[4] == "Y")
            {
                fbi.WindowsStoreVersion = elements[3];
            }

            if (elements[6] == "Y")
            {
                fbi.DesktopVersion = elements[5];
            }

            if (elements[8] == "Y")
            {
                fbi.ProjectKVersion = elements[7];
            }

            if (elements[10] == "Y")
            {
                fbi.TestNetVersion = elements[9];
            }

            if (elements[12] == "Y")
            {
                fbi.PhoneVersion = elements[11];
            }

            return fbi;
        }
    }
}