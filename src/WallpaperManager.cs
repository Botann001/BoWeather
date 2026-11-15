using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using BoWeather.Particles;

namespace BoWeather
{
    [ComImport, Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    public class DesktopWallpaperCoClass {}

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDesktopWallpaper
    {
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
    }

    public class WallpaperManager
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        private readonly string _wallpaperFolder;

        public WallpaperManager()
        {
            _wallpaperFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpapers");
            if (!Directory.Exists(_wallpaperFolder))
            {
                Directory.CreateDirectory(_wallpaperFolder);
            }
        }

        // 8 time-of-day slots, following the KoiArt layout (see WALLPAPER_PROMPTS.md).
        // Dawn 04-06, Sunrise 06-08, Morning 08-11, Midday 11-15,
        // Golden hour 15-17, Sunset 17-19, Dusk 19-21, Night 21-04.
        public string GetCurrentTimeSlot()
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 4 && hour < 6) return "dawn";
            if (hour >= 6 && hour < 8) return "sunrise";
            if (hour >= 8 && hour < 11) return "morning";
            if (hour >= 11 && hour < 15) return "midday";
            if (hour >= 15 && hour < 17) return "golden_hour";
            if (hour >= 17 && hour < 19) return "sunset";
            if (hour >= 19 && hour < 21) return "dusk";
            return "night";
        }

        public void ApplyThemeWallpaper(EffectType effect)
        {
            List<string> effectNames = new List<string>();
            string primary = effect.ToString().ToLower();
            effectNames.Add(primary);
            if (effect == EffectType.AutumnLeaves)
            {
                effectNames.Add("autumn");
                effectNames.Add("autumn_leaves");
                effectNames.Add("autumnleaves");
            }

            string slot = GetCurrentTimeSlot();

            // Prefer high-quality PNG first, then JPG, JPEG, BMP
            string[] extensions = new string[] { ".png", ".jpg", ".jpeg", ".bmp" };

            // 1. Prefer time-of-day variant, e.g. theme_snow_midday.png or .jpg
            foreach (string name in effectNames)
            {
                foreach (string ext in extensions)
                {
                    string timedFileName = string.Format("theme_{0}_{1}{2}", name, slot, ext);
                    string timedPath = Path.Combine(_wallpaperFolder, timedFileName);
                    if (File.Exists(timedPath))
                    {
                        SetWallpaper(timedPath);
                        return;
                    }
                }
            }

            // 2. Fallback to base theme wallpaper, e.g. theme_snow.png or .jpg
            foreach (string name in effectNames)
            {
                foreach (string ext in extensions)
                {
                    string fileName = string.Format("theme_{0}{1}", name, ext);
                    string fullPath = Path.Combine(_wallpaperFolder, fileName);
                    if (File.Exists(fullPath))
                    {
                        SetWallpaper(fullPath);
                        return;
                    }
                }
            }

            // 3. Fallback to generating default procedural wallpaper if missing
            string defaultPath = Path.Combine(_wallpaperFolder, string.Format("theme_{0}.png", primary));
            if (!File.Exists(defaultPath))
            {
                GenerateAestheticWallpaper(effect, defaultPath);
            }

            SetWallpaper(defaultPath);
        }

        public void SetWallpaper(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return;

            try
            {
                IDesktopWallpaper dw = (IDesktopWallpaper)new DesktopWallpaperCoClass();
                dw.SetWallpaper(null, imagePath);
                return;
            }
            catch
            {
                // Fallback to legacy API if COM fails
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
            }
        }

        private void GenerateAestheticWallpaper(EffectType effect, string outputPath)
        {
            int w = 2560;
            int h = 1440;

            using (Bitmap bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                switch (effect)
                {
                    case EffectType.Sakura:
                        DrawSakuraTheme(g, w, h);
                        break;
                    case EffectType.Snow:
                        DrawSnowTheme(g, w, h);
                        break;
                    case EffectType.AutumnLeaves:
                        DrawAutumnTheme(g, w, h);
                        break;
                    case EffectType.Rain:
                        DrawRainTheme(g, w, h);
                        break;
                    case EffectType.Fireflies:
                        DrawFirefliesTheme(g, w, h);
                        break;
                }

                bmp.Save(outputPath, ImageFormat.Png);
            }
        }

        private void DrawSakuraTheme(Graphics g, int w, int h)
        {
            // Sunset / Dusk gradient in Japan
            using (LinearGradientBrush bg = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
                Color.FromArgb(255, 35, 25, 55), Color.FromArgb(255, 250, 160, 150)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            // Soft glowing sun
            using (SolidBrush sun = new SolidBrush(Color.FromArgb(90, 255, 230, 200)))
            {
                g.FillEllipse(sun, w * 0.5f - 180, h * 0.55f - 180, 360, 360);
            }

            // Mountain silhouettes in background
            DrawMountainRange(g, w, h, h * 0.65f, Color.FromArgb(180, 70, 45, 80));
            DrawMountainRange(g, w, h, h * 0.78f, Color.FromArgb(230, 40, 25, 55));
            DrawMountainRange(g, w, h, h * 0.88f, Color.FromArgb(255, 25, 15, 40));
        }

        private void DrawSnowTheme(Graphics g, int w, int h)
        {
            // Cold twilight winter gradient
            using (LinearGradientBrush bg = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
                Color.FromArgb(255, 15, 25, 45), Color.FromArgb(255, 160, 190, 220)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            // Winter moon
            using (SolidBrush moon = new SolidBrush(Color.FromArgb(160, 240, 250, 255)))
            {
                g.FillEllipse(moon, w * 0.75f - 90, h * 0.3f - 90, 180, 180);
            }

            DrawMountainRange(g, w, h, h * 0.65f, Color.FromArgb(190, 80, 105, 135));
            DrawMountainRange(g, w, h, h * 0.78f, Color.FromArgb(230, 40, 60, 90));
            DrawMountainRange(g, w, h, h * 0.90f, Color.FromArgb(255, 20, 35, 60));
        }

        private void DrawAutumnTheme(Graphics g, int w, int h)
        {
            // Golden hour autumn gradient
            using (LinearGradientBrush bg = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
                Color.FromArgb(255, 50, 20, 25), Color.FromArgb(255, 245, 140, 50)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            using (SolidBrush sun = new SolidBrush(Color.FromArgb(110, 255, 210, 120)))
            {
                g.FillEllipse(sun, w * 0.4f - 160, h * 0.6f - 160, 320, 320);
            }

            DrawMountainRange(g, w, h, h * 0.68f, Color.FromArgb(180, 120, 40, 35));
            DrawMountainRange(g, w, h, h * 0.80f, Color.FromArgb(230, 80, 25, 25));
            DrawMountainRange(g, w, h, h * 0.90f, Color.FromArgb(255, 45, 15, 18));
        }

        private void DrawRainTheme(Graphics g, int w, int h)
        {
            // Moody rainy indigo/slate gradient
            using (LinearGradientBrush bg = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
                Color.FromArgb(255, 10, 15, 25), Color.FromArgb(255, 40, 60, 80)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            // Distant rainy haze
            DrawMountainRange(g, w, h, h * 0.65f, Color.FromArgb(140, 35, 50, 65));
            DrawMountainRange(g, w, h, h * 0.78f, Color.FromArgb(200, 25, 38, 50));
            DrawMountainRange(g, w, h, h * 0.90f, Color.FromArgb(255, 15, 22, 32));
        }

        private void DrawFirefliesTheme(Graphics g, int w, int h)
        {
            // Deep magical night / enchanted forest
            using (LinearGradientBrush bg = new LinearGradientBrush(new Point(0, 0), new Point(0, h),
                Color.FromArgb(255, 5, 10, 20), Color.FromArgb(255, 15, 35, 35)))
            {
                g.FillRectangle(bg, 0, 0, w, h);
            }

            // Ambient emerald glow
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(50, 40, 180, 130)))
            {
                g.FillEllipse(glow, w * 0.5f - 400, h * 0.75f - 300, 800, 600);
            }

            DrawMountainRange(g, w, h, h * 0.70f, Color.FromArgb(160, 10, 25, 25));
            DrawMountainRange(g, w, h, h * 0.82f, Color.FromArgb(220, 8, 18, 20));
            DrawMountainRange(g, w, h, h * 0.92f, Color.FromArgb(255, 4, 10, 12));
        }

        private void DrawMountainRange(Graphics g, int w, int h, float baseline, Color color)
        {
            using (GraphicsPath path = new GraphicsPath())
            using (SolidBrush brush = new SolidBrush(color))
            {
                int points = 16;
                Random r = new Random((int)baseline);
                float step = (float)w / points;

                PointF[] pts = new PointF[points + 3];
                pts[0] = new PointF(0, h);
                pts[1] = new PointF(0, baseline);

                for (int i = 1; i <= points; i++)
                {
                    float x = i * step;
                    float y = baseline + (float)((r.NextDouble() - 0.5) * (h * 0.15f));
                    pts[i + 1] = new PointF(x, y);
                }

                pts[points + 2] = new PointF(w, h);

                path.AddPolygon(pts);
                g.FillPath(brush, path);
            }
        }
    }
}
