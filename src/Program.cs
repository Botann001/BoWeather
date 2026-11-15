using System;
using System.IO;
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
            string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "run.log");
            File.WriteAllText(logFile, "Main entered at " + DateTime.Now.ToString("HH:mm:ss.fff") + "\n");
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

            File.AppendAllText(logFile, "DPI done\n");
            bool createdNew;
            using (Mutex mutex = new Mutex(true, AppMutexName, out createdNew))
            {
                File.AppendAllText(logFile, "Mutex createdNew=" + createdNew + "\n");
                if (!createdNew)
                {
                    File.AppendAllText(logFile, "Mutex conflict, exiting\n");
                    MessageBox.Show("BoWeather sudah berjalan di System Tray (pojok kanan bawah dekat jam).",
                                    "BoWeather",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, ev) => {
                    File.AppendAllText(logFile, "ThreadException: " + ev.Exception.ToString() + "\n");
                };
                AppDomain.CurrentDomain.UnhandledException += (s, ev) => {
                    File.AppendAllText(logFile, "AppDomainException: " + ev.ExceptionObject.ToString() + "\n");
                };

                try
                {
                    File.AppendAllText(logFile, "Creating OverlayForm...\n");
                    OverlayForm overlay = new OverlayForm();
                    File.AppendAllText(logFile, "OverlayForm created\n");
                    WallpaperManager wallpaperManager = new WallpaperManager();
                    File.AppendAllText(logFile, "WallpaperManager created\n");

                    using (TrayIconManager tray = new TrayIconManager(overlay, wallpaperManager))
                    {
                        File.AppendAllText(logFile, "TrayIconManager created. Calling Application.Run(overlay)...\n");
                        overlay.FormClosing += (s, e) => {
                            File.AppendAllText(logFile, "OverlayForm FormClosing: CloseReason=" + e.CloseReason + "\n");
                        };
                        overlay.FormClosed += (s, e) => {
                            File.AppendAllText(logFile, "OverlayForm FormClosed: CloseReason=" + e.CloseReason + "\n");
                        };
                        Application.Run(overlay);
                        File.AppendAllText(logFile, "Application.Run returned!\n");
                    }
                }
                catch (Exception ex)
                {
                    File.AppendAllText(logFile, "Exception in Main: " + ex.ToString() + "\n");
                }
            }
            File.AppendAllText(logFile, "Main exited\n");
        }
    }
}
