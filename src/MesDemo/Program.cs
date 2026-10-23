using MesDemo.Forms;
using MesDemo.Services;

namespace MesDemo;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--selftest"))
            return SelfTest.Run();

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }
}
