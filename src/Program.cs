using System;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("WipeFilter")]
[assembly: AssemblyProduct("WipeFilter")]
[assembly: AssemblyDescription("Filtre de couleurs et viseur pour Rust - https://wipefilter.fr")]
[assembly: AssemblyCompany("WipeFilter.fr")]
[assembly: AssemblyCopyright("WipeFilter.fr")]
[assembly: AssemblyVersion("4.1.1.0")]
[assembly: AssemblyFileVersion("4.1.1.0")]

namespace RustFilter
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length >= 1 && args[0] == "--selftest")
            {
                Environment.ExitCode = App.SelfTest();
                return;
            }
            if (args.Length >= 2 && args[0] == "--shot")
            {
                int page = 0;
                if (args.Length >= 3) int.TryParse(args[2], out page);
                App.Shot(args[1], page);
                return;
            }
            App.Run();
        }
    }
}
