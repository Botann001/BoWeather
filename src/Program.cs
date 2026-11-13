using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace BoWeather
{
    static class Program
    {
        private const string AppMutexName = "BoWeather_SingleInstance_Mutex_2026";

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            // Enable True Per-Monitor DPI Awareness so resolutions like 2560x1440 (125%/150%) match physical pixels perfectly
            try
            {
                SetProcessDpiAwareness(2); // PROCESS_PER_MONITOR_DPI_AWARE
            }
            catch
            {
                try
                {
                    SetProcessDPIAware();
                }
                catch { }
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, AppMutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("BoWeather sudah berjalan di System Tray (pojok kanan bawah dekat jam).",
                                    "BoWeather",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                OverlayForm overlay = new OverlayForm();
                WallpaperManager wallpaperManager = new WallpaperManager();

                using (TrayIconManager tray = new TrayIconManager(overlay, wallpaperManager))
                {
                    Application.Run(overlay);
                }
            }
        }
    }
}
