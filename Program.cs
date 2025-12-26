using Microsoft.UI.Xaml;

namespace PocketFence_Simple;

public class Program
{
    [STAThread] 
    public static void Main(string[] args)
    {
        global::Microsoft.UI.Xaml.Application.Start((p) => new App());
    }
}
