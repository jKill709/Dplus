using Microsoft.Extensions.Logging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using mLogger;

namespace Dplus_Desktop
{    
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Load settings once on startup
            Settings.LoadSettings();
            if (!Settings.isLoaded)
            {
                MessageBox.Show("Failed to load settings. The application will now exit.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return; // Exit if settings failed to load
            }

            Logger.Instance.Initialize("CamManager");

            Application.Run(new Main());
        }
    }
}