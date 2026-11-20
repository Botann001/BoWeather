using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace BoWeather
{
    public static class ChristmasAssets
    {
        private static readonly Dictionary<string, Image> _cache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void EnsureLoaded()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                string[] candidateDirs = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "christmas"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "assets", "christmas"),
                    Path.Combine(Environment.CurrentDirectory, "assets", "christmas"),
                    @"c:\Users\oktam\OneDrive\Documents\wallpaper-engine\assets\christmas"
                };

                string foundDir = null;
                foreach (string dir in candidateDirs)
                {
                    if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, "star.png")))
                    {
                        foundDir = dir;
                        break;
                    }
                }

                if (foundDir != null)
                {
                    try
                    {
                        string[] files = Directory.GetFiles(foundDir, "*.png");
                        foreach (string file in files)
                        {
                            string key = Path.GetFileNameWithoutExtension(file);
                            try
                            {
                                using (Image temp = Image.FromFile(file))
                                {
                                    // Clone to decouple from file lock
                                    _cache[key] = new Bitmap(temp);
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }

                _initialized = true;
            }
        }

        public static Image GetAsset(string key)
        {
            EnsureLoaded();
            Image img;
            if (_cache.TryGetValue(key, out img)) return img;
            return null;
        }

        public static bool DrawIcon(Graphics g, string id, float cx, float cy, float w, float h)
        {
            EnsureLoaded();
            Image img;
            if (!_cache.TryGetValue(id, out img) || img == null) return false;

            InterpolationMode oldInterpolation = g.InterpolationMode;
            PixelOffsetMode oldPixelOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Optional ambient glows for illuminated ornaments
            if (string.Equals(id, "star", StringComparison.OrdinalIgnoreCase))
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(cx - w * 0.7f, cy - h * 0.7f, w * 1.4f, h * 1.4f);
                    using (PathGradientBrush pgb = new PathGradientBrush(gp))
                    {
                        pgb.CenterColor = Color.FromArgb(120, 255, 220, 70);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 200, 30) };
                        g.FillPath(pgb, gp);
                    }
                }
            }
            else if (string.Equals(id, "candle", StringComparison.OrdinalIgnoreCase))
            {
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(cx - w * 0.5f, cy - h * 0.65f, w * 1.0f, h * 0.7f);
                    using (PathGradientBrush pgb = new PathGradientBrush(gp))
                    {
                        pgb.CenterColor = Color.FromArgb(100, 255, 190, 60);
                        pgb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 170, 40) };
                        g.FillPath(pgb, gp);
                    }
                }
            }

            RectangleF destRect = new RectangleF(cx - w * 0.5f, cy - h * 0.5f, w, h);
            g.DrawImage(img, destRect);

            g.InterpolationMode = oldInterpolation;
            g.PixelOffsetMode = oldPixelOffset;
            return true;
        }

        public static bool DrawTreeHero(Graphics g, float cx, float cy, float w, float h)
        {
            EnsureLoaded();
            Image img;
            if (!_cache.TryGetValue("tree_hero", out img) || img == null) return false;

            InterpolationMode oldInterpolation = g.InterpolationMode;
            PixelOffsetMode oldPixelOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Star peak glow
            using (GraphicsPath gp = new GraphicsPath())
            {
                float starY = cy - h * 0.35f;
                gp.AddEllipse(cx - 18f, starY - 18f, 36f, 36f);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = Color.FromArgb(140, 255, 230, 90);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 215, 0) };
                    g.FillPath(pgb, gp);
                }
            }

            RectangleF destRect = new RectangleF(cx - w * 0.5f, cy - h * 0.5f, w, h);
            g.DrawImage(img, destRect);

            g.InterpolationMode = oldInterpolation;
            g.PixelOffsetMode = oldPixelOffset;
            return true;
        }

        public static bool DrawTreeHeader(Graphics g, float cx, float cy, float w, float h)
        {
            EnsureLoaded();
            Image img;
            if (!_cache.TryGetValue("tree_header", out img) || img == null)
            {
                // Fallback to tree_hero if header tree missing
                if (!_cache.TryGetValue("tree_hero", out img) || img == null) return false;
            }

            InterpolationMode oldInterpolation = g.InterpolationMode;
            PixelOffsetMode oldPixelOffset = g.PixelOffsetMode;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF destRect = new RectangleF(cx - w * 0.5f, cy - h * 0.5f, w, h);
            g.DrawImage(img, destRect);

            g.InterpolationMode = oldInterpolation;
            g.PixelOffsetMode = oldPixelOffset;
            return true;
        }
    }
}
