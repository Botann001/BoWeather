using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BoWeather
{
    public static class VectorIcons
    {
        public static void DrawIcon(Graphics g, string name, float cx, float cy, float size, Color color, Color? fill = null)
        {
            GraphicsState state = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Translate to center and scale to target size from 24x24 viewBox
            g.TranslateTransform(cx, cy);
            float scale = size / 24f;
            g.ScaleTransform(scale, scale);
            g.TranslateTransform(-12f, -12f);

            Color fillColor = fill.HasValue ? fill.Value : color;

            using (Pen pen = new Pen(color, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                switch (name.ToLowerInvariant())
                {
                    case "sakura":
                        DrawSakura(g, pen, fillColor);
                        break;
                    case "snow":
                        DrawSnow(g, pen);
                        break;
                    case "autumn":
                        DrawAutumn(g, pen);
                        break;
                    case "rain":
                        DrawRain(g, pen);
                        break;
                    case "fly":
                        DrawFly(g, pen, fillColor);
                        break;
                    case "pause":
                        DrawPause(g, pen, fillColor);
                        break;
                    case "play":
                        DrawPlay(g, pen, fillColor);
                        break;
                    case "wind":
                        DrawWind(g, pen);
                        break;
                    case "density":
                        DrawDensity(g, pen);
                        break;
                    case "physics":
                        DrawPhysics(g, pen, fillColor);
                        break;
                    case "gamepad":
                        DrawGamepad(g, pen, fillColor);
                        break;
                    case "clock":
                        DrawClock(g, pen);
                        break;
                    case "image":
                        DrawImage(g, pen, fillColor);
                        break;
                    case "folder":
                        DrawFolder(g, pen);
                        break;
                    case "globe":
                        DrawGlobe(g, pen);
                        break;
                    case "close":
                        DrawClose(g, pen);
                        break;
                    case "moon":
                        DrawMoon(g, pen);
                        break;
                    case "sun":
                        DrawSun(g, pen);
                        break;
                    case "chev":
                        DrawChev(g, pen);
                        break;
                }
            }

            g.Restore(state);
        }

        private static void DrawSakura(Graphics g, Pen pen, Color fillColor)
        {
            // 5 petals rotated at 0, 72, 144, 216, 288 degrees around (12, 12)
            // ellipse cx=0, cy=-5, rx=3, ry=4.2
            for (int i = 0; i < 5; i++)
            {
                GraphicsState ps = g.Save();
                g.TranslateTransform(12f, 12f);
                g.RotateTransform(i * 72f);
                g.DrawEllipse(pen, -3f, -9.2f, 6f, 8.4f);
                g.Restore(ps);
            }
            using (SolidBrush b = new SolidBrush(fillColor))
            {
                g.FillEllipse(b, 12f - 1.2f, 12f - 1.2f, 2.4f, 2.4f);
            }
        }

        private static void DrawSnow(Graphics g, Pen pen)
        {
            // Central 6-arm star
            g.DrawLine(pen, 12f, 2f, 12f, 22f);
            g.DrawLine(pen, 3.3f, 7f, 20.7f, 17f);
            g.DrawLine(pen, 3.3f, 17f, 20.7f, 7f);

            // Arrow branches on arms
            // Top vertical
            g.DrawLine(pen, 9.5f, 3.8f, 12f, 6f);
            g.DrawLine(pen, 14.5f, 3.8f, 12f, 6f);
            // Bottom vertical
            g.DrawLine(pen, 9.5f, 20.2f, 12f, 18f);
            g.DrawLine(pen, 14.5f, 20.2f, 12f, 18f);

            // Diagonal branches
            g.DrawLine(pen, 4.5f, 9.5f, 6.5f, 8.8f);
            g.DrawLine(pen, 7.2f, 6.2f, 6.5f, 8.8f);

            g.DrawLine(pen, 19.5f, 14.5f, 17.5f, 15.2f);
            g.DrawLine(pen, 16.8f, 17.8f, 17.5f, 15.2f);

            g.DrawLine(pen, 4.5f, 14.5f, 6.5f, 15.2f);
            g.DrawLine(pen, 7.2f, 17.8f, 6.5f, 15.2f);

            g.DrawLine(pen, 19.5f, 9.5f, 17.5f, 8.8f);
            g.DrawLine(pen, 16.8f, 6.2f, 17.5f, 8.8f);
        }

        private static void DrawAutumn(Graphics g, Pen pen)
        {
            // M5 19c0-8 5-14 15-14 0 9-5 15-12 15-1.2 0-2-.4-3-1z
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddBezier(5f, 19f, 5f, 11f, 10f, 5f, 20f, 5f);
                path.AddBezier(20f, 5f, 20f, 14f, 15f, 20f, 8f, 20f);
                path.AddLine(8f, 20f, 5f, 19f);
                g.DrawPath(pen, path);
            }
            // Vein: M5 19c3-4 6-7 10-9
            using (GraphicsPath vein = new GraphicsPath())
            {
                vein.AddBezier(5f, 19f, 8f, 15f, 11f, 12f, 15f, 10f);
                g.DrawPath(pen, vein);
            }
        }

        private static void DrawRain(Graphics g, Pen pen)
        {
            // Cloud: M7 14a4.5 4.5 0 0 1-.6-8.9A6 6 0 0 1 18 7a3.8 3.8 0 0 1 0 7
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(2.5f, 9.5f, 8.5f, 8.5f, 90f, 170f);
                path.AddArc(5.5f, 1f, 11.5f, 11.5f, 200f, 140f);
                path.AddArc(14.2f, 6.5f, 7.5f, 7.5f, 280f, 170f);
                path.AddLine(18f, 14f, 7f, 14f);
                g.DrawPath(pen, path);
            }
            // Drops: M8 17l-1 3M12 17l-1 3M16 17l-1 3
            g.DrawLine(pen, 8f, 17f, 7f, 20f);
            g.DrawLine(pen, 12f, 17f, 11f, 20f);
            g.DrawLine(pen, 16f, 17f, 15f, 20f);
        }

        private static void DrawFly(Graphics g, Pen pen, Color fillColor)
        {
            // Sparkle 1: M10 4l1.6 4.4L16 10l-4.4 1.6L10 16l-1.6-4.4L4 10l4.4-1.6z
            PointF[] s1 = new PointF[]
            {
                new PointF(10f, 4f),
                new PointF(11.6f, 8.4f),
                new PointF(16f, 10f),
                new PointF(11.6f, 11.6f),
                new PointF(10f, 16f),
                new PointF(8.4f, 11.6f),
                new PointF(4f, 10f),
                new PointF(8.4f, 8.4f)
            };
            g.DrawPolygon(pen, s1);

            // Sparkle 2: M18 14l.8 2.2L21 17l-2.2.8L18 20l-.8-2.2L15 17l2.2-.8z
            PointF[] s2 = new PointF[]
            {
                new PointF(18f, 14f),
                new PointF(18.8f, 16.2f),
                new PointF(21f, 17f),
                new PointF(18.8f, 17.8f),
                new PointF(18f, 20f),
                new PointF(17.2f, 17.8f),
                new PointF(15f, 17f),
                new PointF(17.2f, 16.2f)
            };
            g.DrawPolygon(pen, s2);
        }

        private static void DrawPause(Graphics g, Pen pen, Color fillColor)
        {
            // rect x=6 y=5 w=4 h=14 rx=1.2
            // rect x=14 y=5 w=4 h=14 rx=1.2
            using (GraphicsPath p1 = CreateRoundedRect(new RectangleF(6f, 5f, 4f, 14f), 1.2f))
            using (GraphicsPath p2 = CreateRoundedRect(new RectangleF(14f, 5f, 4f, 14f), 1.2f))
            {
                g.DrawPath(pen, p1);
                g.DrawPath(pen, p2);
            }
        }

        private static void DrawPlay(Graphics g, Pen pen, Color fillColor)
        {
            // M8 5l11 7-11 7z
            PointF[] pts = new PointF[]
            {
                new PointF(8f, 5f),
                new PointF(19f, 12f),
                new PointF(8f, 19f)
            };
            g.DrawPolygon(pen, pts);
        }

        private static void DrawWind(Graphics g, Pen pen)
        {
            // M3 9h11a3 3 0 1 0-3-3
            g.DrawLine(pen, 3f, 9f, 14f, 9f);
            g.DrawArc(pen, 11f, 3f, 6f, 6f, 90f, -200f);

            // M3 14h15a3 3 0 1 1-3 3
            g.DrawLine(pen, 3f, 14f, 18f, 14f);
            g.DrawArc(pen, 15f, 14f, 6f, 6f, 270f, 200f);

            // M3 19h6
            g.DrawLine(pen, 3f, 19f, 9f, 19f);
        }

        private static void DrawDensity(Graphics g, Pen pen)
        {
            // rect x=3 y=3 w=7 h=7 rx=2
            using (GraphicsPath p1 = CreateRoundedRect(new RectangleF(3f, 3f, 7f, 7f), 2f))
            using (GraphicsPath p2 = CreateRoundedRect(new RectangleF(14f, 3f, 7f, 7f), 2f))
            using (GraphicsPath p3 = CreateRoundedRect(new RectangleF(3f, 14f, 7f, 7f), 2f))
            {
                g.DrawPath(pen, p1);
                g.DrawPath(pen, p2);
                g.DrawPath(pen, p3);
            }
            // circle cx=17.5 cy=17.5 r=3.5
            g.DrawEllipse(pen, 14f, 14f, 7f, 7f);
        }

        private static void DrawPhysics(Graphics g, Pen pen, Color fillColor)
        {
            // rect x=3 y=7 w=18 h=13 rx=2.5
            using (GraphicsPath p = CreateRoundedRect(new RectangleF(3f, 7f, 18f, 13f), 2.5f))
            {
                g.DrawPath(pen, p);
            }
            // path M3 11h18
            g.DrawLine(pen, 3f, 11f, 21f, 11f);
            // circle cx=8 cy=3.5 r=1
            g.DrawEllipse(pen, 7f, 2.5f, 2f, 2f);
            // circle cx=13 cy=4.5 r=1
            g.DrawEllipse(pen, 12f, 3.5f, 2f, 2f);
        }

        private static void DrawGamepad(Graphics g, Pen pen, Color fillColor)
        {
            // rect x=2.5 y=7 w=19 h=11 rx=5.5
            using (GraphicsPath p = CreateRoundedRect(new RectangleF(2.5f, 7f, 19f, 11f), 5.5f))
            {
                g.DrawPath(pen, p);
            }
            // Dpad: M8 10.5v4 M6 12.5h4
            g.DrawLine(pen, 8f, 10.5f, 8f, 14.5f);
            g.DrawLine(pen, 6f, 12.5f, 10f, 12.5f);

            // Buttons: circle cx=16 cy=11.5 r=.8, circle cx=18 cy=13.5 r=.8
            using (SolidBrush b = new SolidBrush(fillColor))
            {
                g.FillEllipse(b, 15.2f, 10.7f, 1.6f, 1.6f);
                g.FillEllipse(b, 17.2f, 12.7f, 1.6f, 1.6f);
            }
        }

        private static void DrawClock(Graphics g, Pen pen)
        {
            // circle cx=12 cy=12 r=9
            g.DrawEllipse(pen, 3f, 3f, 18f, 18f);
            // M12 7v5l3 2
            g.DrawLine(pen, 12f, 7f, 12f, 12f);
            g.DrawLine(pen, 12f, 12f, 15f, 14f);
        }

        private static void DrawImage(Graphics g, Pen pen, Color fillColor)
        {
            // rect x=3 y=4 w=18 h=16 rx=3
            using (GraphicsPath p = CreateRoundedRect(new RectangleF(3f, 4f, 18f, 16f), 3f))
            {
                g.DrawPath(pen, p);
            }
            // circle cx=9 cy=10 r=1.6
            g.DrawEllipse(pen, 7.4f, 8.4f, 3.2f, 3.2f);
            // M21 16l-5-5-8 9
            g.DrawLine(pen, 21f, 16f, 16f, 11f);
            g.DrawLine(pen, 16f, 11f, 8f, 20f);
        }

        private static void DrawFolder(Graphics g, Pen pen)
        {
            // M3 7a2 2 0 0 1 2-2h4l2 2.5h8a2 2 0 0 1 2 2V18a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddLine(5f, 5f, 9f, 5f);
                p.AddLine(9f, 5f, 11f, 7.5f);
                p.AddLine(11f, 7.5f, 19f, 7.5f);
                p.AddArc(17f, 7.5f, 4f, 4f, 270f, 90f);
                p.AddLine(21f, 9.5f, 21f, 18f);
                p.AddArc(17f, 16f, 4f, 4f, 0f, 90f);
                p.AddLine(19f, 20f, 5f, 20f);
                p.AddArc(3f, 16f, 4f, 4f, 90f, 90f);
                p.AddLine(3f, 18f, 3f, 7f);
                p.AddArc(3f, 5f, 4f, 4f, 180f, 90f);
                p.CloseFigure();
                g.DrawPath(pen, p);
            }
        }

        private static void DrawGlobe(Graphics g, Pen pen)
        {
            // circle cx=12 cy=12 r=9
            g.DrawEllipse(pen, 3f, 3f, 18f, 18f);
            // M3 12h18
            g.DrawLine(pen, 3f, 12f, 21f, 12f);
            // M12 3c3 3 3 15 0 18
            using (GraphicsPath p1 = new GraphicsPath())
            {
                p1.AddBezier(12f, 3f, 15f, 6f, 15f, 18f, 12f, 21f);
                g.DrawPath(pen, p1);
            }
            // M12 3c-3 3-3 15 0 18
            using (GraphicsPath p2 = new GraphicsPath())
            {
                p2.AddBezier(12f, 3f, 9f, 6f, 9f, 18f, 12f, 21f);
                g.DrawPath(pen, p2);
            }
        }

        private static void DrawClose(Graphics g, Pen pen)
        {
            // M6 6l12 12 M18 6L6 18
            g.DrawLine(pen, 6f, 6f, 18f, 18f);
            g.DrawLine(pen, 18f, 6f, 6f, 18f);
        }

        private static void DrawMoon(Graphics g, Pen pen)
        {
            // M20 14.5A8.5 8.5 0 0 1 9.5 4a8.5 8.5 0 1 0 10.5 10.5z
            using (GraphicsPath p = new GraphicsPath())
            {
                p.AddArc(3.5f, 3.5f, 17f, 17f, 40f, 280f);
                p.AddBezier(9.5f, 4f, 11f, 10f, 16f, 13f, 20f, 14.5f);
                g.DrawPath(pen, p);
            }
        }

        private static void DrawSun(Graphics g, Pen pen)
        {
            // circle cx=12 cy=12 r=4
            g.DrawEllipse(pen, 8f, 8f, 8f, 8f);

            // Rays
            g.DrawLine(pen, 12f, 2f, 12f, 4f);
            g.DrawLine(pen, 12f, 20f, 12f, 22f);
            g.DrawLine(pen, 4.9f, 4.9f, 6.3f, 6.3f);
            g.DrawLine(pen, 17.7f, 17.7f, 19.1f, 19.1f);
            g.DrawLine(pen, 2f, 12f, 4f, 12f);
            g.DrawLine(pen, 20f, 12f, 22f, 12f);
            g.DrawLine(pen, 4.9f, 19.1f, 6.3f, 17.7f);
            g.DrawLine(pen, 17.7f, 6.3f, 19.1f, 4.9f);
        }

        private static void DrawChev(Graphics g, Pen pen)
        {
            // M6 9l6 6 6-6
            PointF[] pts = new PointF[]
            {
                new PointF(6f, 9f),
                new PointF(12f, 15f),
                new PointF(18f, 9f)
            };
            g.DrawLines(pen, pts);
        }

        public static GraphicsPath CreateRoundedRect(RectangleF r, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;

            path.AddArc(r.Left, r.Top, d, d, 180f, 90f);
            path.AddArc(r.Right - d, r.Top, d, d, 270f, 90f);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0f, 90f);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90f, 90f);
            path.CloseFigure();
            return path;
        }
    }
}
