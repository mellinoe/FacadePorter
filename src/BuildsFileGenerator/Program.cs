using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            foreach (string file in Directory.EnumerateFiles(corefxSrcPath, "*.builds", SearchOption.AllDirectories))
            {
                Console.WriteLine(file);
            }

            return 0;
        }
    }
}
