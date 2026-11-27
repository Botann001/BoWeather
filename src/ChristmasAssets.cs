using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BoWeather
{
    public static class ChristmasAssets
    {
        private static readonly Dictionary<string, Image> _cache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static Bitmap _glowSprite = null;
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void EnsureLoaded()
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                string asmDir = "";
                try { asmDir = Path.GetDirectoryName(typeof(ChristmasAssets).Assembly.Location) ?? ""; } catch { }

                string[] candidateDirs = new string[]
                {
                    Path.Combine(asmDir, "assets", "christmas"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "christmas"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "assets", "christmas"),
                    Path.Combine(Environment.CurrentDirectory, "assets", "christmas")
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

            // Preserve natural aspect ratio without distortion
            float scale = Math.Min(w / (float)img.Width, h / (float)img.Height);
            float drawW = img.Width * scale;
            float drawH = img.Height * scale;
            RectangleF destRect = new RectangleF(cx - drawW * 0.5f, cy - drawH * 0.5f, drawW, drawH);
            g.DrawImage(img, destRect);

            g.InterpolationMode = oldInterpolation;
            g.PixelOffsetMode = oldPixelOffset;
            return true;
        }

        public static void DrawSoftGlow(Graphics g, float cx, float cy, float radius, Color color, int alpha)
        {
            if (alpha <= 2 || radius <= 1f) return;
            if (alpha > 255) alpha = 255;

            if (_glowSprite == null)
            {
                lock (_lock)
                {
                    if (_glowSprite == null)
                    {
                        int sz = 64;
                        Bitmap bmp = new Bitmap(sz, sz, PixelFormat.Format32bppArgb);
                        float half = sz * 0.5f;
                        for (int y = 0; y < sz; y++)
                        {
                            for (int x = 0; x < sz; x++)
                            {
                                float dx = (x + 0.5f - half) / half;
                                float dy = (y + 0.5f - half) / half;
                                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                                if (d >= 1f)
                                {
                                    bmp.SetPixel(x, y, Color.FromArgb(0, 255, 255, 255));
                                }
                                else
                                {
                                    // Smooth quadratic-cubic falloff for velvety optical bloom
                                    float falloff = (1f - d);
                                    float a = falloff * falloff * (1f - d * 0.25f);
                                    bmp.SetPixel(x, y, Color.FromArgb((int)(a * 255f), 255, 255, 255));
                                }
                            }
                        }
                        _glowSprite = bmp;
                    }
                }
            }

            float rScale = color.R / 255f;
            float gScale = color.G / 255f;
            float bScale = color.B / 255f;
            float aScale = alpha / 255f;

            ColorMatrix cm = new ColorMatrix(new float[][]
            {
                new float[] { rScale, 0, 0, 0, 0 },
                new float[] { 0, gScale, 0, 0, 0 },
                new float[] { 0, 0, bScale, 0, 0 },
                new float[] { 0, 0, 0, aScale, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });

            using (ImageAttributes ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm);
                Rectangle dest = new Rectangle(
                    (int)(cx - radius), (int)(cy - radius),
                    (int)(radius * 2f), (int)(radius * 2f));
                g.DrawImage(_glowSprite, dest, 0, 0, _glowSprite.Width, _glowSprite.Height, GraphicsUnit.Pixel, ia);
            }
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

            // Velvety warm glow behind star peak
            DrawSoftGlow(g, cx, cy - h * 0.35f, 18f, Color.FromArgb(255, 225, 70), 130);

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
