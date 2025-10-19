using System.Configuration;
using System.Data;
using System.Windows;

namespace SwissKnifeApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Test JSON loading
            System.Diagnostics.Debug.WriteLine("===== TESTING JSON LOAD =====");
            TestJsonLoad.TestLoad();
            System.Diagnostics.Debug.WriteLine("===== JSON TEST COMPLETE =====");
        }
    }

}
