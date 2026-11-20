using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using BoWeather.Particles;

namespace BoWeather
{
    public class ChristmasLightsItem
    {
        public float X1;
        public float Y1;
        public float X2;
        public float Y2;
        public float SagDepth;
        public bool IsGrabbedHandle1 = false;
        public bool IsGrabbedHandle2 = false;
        public bool IsGrabbedBody = false;
        public bool IsHovered = false;
        public int HoverPart = 0; // 0=none, 1=delete, 2=handle1, 3=handle2, 4=body

        public ChristmasLightsItem(float x1, float y1, float x2, float y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            RecalculateSag();
        }

        public void RecalculateSag()
        {
            float dx = X2 - X1;
            float dy = Y2 - Y1;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            SagDepth = Math.Min(90f, Math.Max(16f, dist * 0.08f));
        }

        public PointF GetMidpoint()
        {
            return new PointF((X1 + X2) * 0.5f, (Y1 + Y2) * 0.5f);
        }

        public RectangleF GetDeleteButtonBounds()
        {
            PointF mid = GetMidpoint();
            float my = mid.Y + SagDepth + 18f;
            return new RectangleF(mid.X - 10f, my - 10f, 20f, 20f);
        }

        public RectangleF GetHandle1Bounds()
        {
            return new RectangleF(X1 - 14f, Y1 - 14f, 28f, 28f);
        }

        public RectangleF GetHandle2Bounds()
        {
            return new RectangleF(X2 - 14f, Y2 - 14f, 28f, 28f);
        }

        public PointF GetBezierPoint(float t)
        {
            float u = 1f - t;
            PointF mid = GetMidpoint();
            float ctrl1X = X1 + (mid.X - X1) * 0.55f;
            float ctrl1Y = mid.Y + SagDepth;
            float ctrl2X = mid.X + (X2 - mid.X) * 0.45f;
            float ctrl2Y = mid.Y + SagDepth;

            float x = u * u * u * X1 + 3f * u * u * t * ctrl1X + 3f * u * t * t * ctrl2X + t * t * t * X2;
            float y = u * u * u * Y1 + 3f * u * u * t * ctrl1Y + 3f * u * t * t * ctrl2Y + t * t * t * Y2;
            return new PointF(x, y);
        }

        public bool IsNearWire(float px, float py, float maxDist = 20f)
        {
            for (int i = 0; i <= 16; i++)
            {
                float t = i / 16f;
                PointF pt = GetBezierPoint(t);
                float dx = px - pt.X;
                float dy = py - pt.Y;
                if (dx * dx + dy * dy <= maxDist * maxDist)
                {
                    return true;
                }
            }
            return false;
        }

        public void Draw(Graphics g, float animTick, bool isDarkMode)
        {
            Color[] bulbPalette = new Color[]
            {
                Color.FromArgb(239, 68, 68),   // Crimson Red
                Color.FromArgb(245, 158, 11),  // Amber Gold
                Color.FromArgb(16, 185, 129),  // Emerald Green
                Color.FromArgb(6, 182, 212),   // Sky Cyan
                Color.FromArgb(236, 72, 153),  // Hot Pink
                Color.FromArgb(254, 240, 138), // Warm White
                Color.FromArgb(168, 85, 247)   // Purple
            };

            PointF mid = GetMidpoint();
            float ctrl1X = X1 + (mid.X - X1) * 0.55f;
            float ctrl1Y = mid.Y + SagDepth;
            float ctrl2X = mid.X + (X2 - mid.X) * 0.45f;
            float ctrl2Y = mid.Y + SagDepth;

            // 1. Draw Catenary Wire
            using (GraphicsPath wirePath = new GraphicsPath())
            {
                wirePath.AddBezier(
                    new PointF(X1, Y1),
                    new PointF(ctrl1X, ctrl1Y),
                    new PointF(ctrl2X, ctrl2Y),
                    new PointF(X2, Y2));

                using (Pen wirePen = new Pen(Color.FromArgb(200, 25, 45, 30), 2.2f))
                {
                    g.DrawPath(wirePen, wirePath);
                }
            }

            // 2. Hanging Colored Bulbs along wire
            float dx = X2 - X1;
            float dy = Y2 - Y1;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            int bulbCount = Math.Max(4, (int)(dist / 30f));

            for (int b = 1; b < bulbCount; b++)
            {
                float t = (float)b / bulbCount;
                PointF pt = GetBezierPoint(t);
                Color bulbCol = bulbPalette[(b + (int)(X1 * 0.05f)) % bulbPalette.Length];
                float pulse = (float)(0.68 + 0.32 * Math.Sin(animTick * 3.4f + b * 1.15f + X1 * 0.02f));
                float r = 5.0f;

                // Socket cap
                using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(220, 20, 35, 25)))
                {
                    g.FillRectangle(capBrush, pt.X - 1.8f, pt.Y - 2.5f, 3.6f, 3.5f);
                }

                // Bulb glow halo
                using (SolidBrush halo = new SolidBrush(Color.FromArgb((int)(65 * pulse), bulbCol)))
                {
                    g.FillEllipse(halo, pt.X - r * 1.6f, pt.Y - r * 0.4f, r * 3.2f, r * 3.2f);
                }

                // Teardrop glass bulb
                using (SolidBrush bulbBrush = new SolidBrush(Color.FromArgb((int)(245 * pulse), bulbCol)))
                {
                    g.FillEllipse(bulbBrush, pt.X - r, pt.Y + 0.5f, r * 2f, r * 2.3f);
                }

                // Center bright sparkle
                using (SolidBrush glint = new SolidBrush(Color.FromArgb((int)(230 * pulse), 255, 255, 255)))
                {
                    g.FillEllipse(glint, pt.X - r * 0.45f, pt.Y + r * 0.4f, r * 0.7f, r * 0.7f);
                }
            }

            // 3. Handles & Delete Button
            // Following reference video: When hovered or interacted, show handles and delete (X) button
            bool showControls = IsHovered || IsGrabbedHandle1 || IsGrabbedHandle2 || IsGrabbedBody;

            if (showControls)
            {
                // Resize/Stretch Handles (Left & Right)
                DrawHandle(g, X1, Y1, IsGrabbedHandle1, HoverPart == 2);
                DrawHandle(g, X2, Y2, IsGrabbedHandle2, HoverPart == 3);

                // Delete Button (X) above center
                RectangleF delRect = GetDeleteButtonBounds();
                bool isDelHover = (HoverPart == 1);
                ChristmasManager.DrawDeleteButton(g, delRect.X + delRect.Width * 0.5f, delRect.Y + delRect.Height * 0.5f, isDelHover);
            }
            else
            {
                // When not hovered, draw subtle unobtrusive anchor pins so lights look clean on desktop!
                using (SolidBrush pinBrush = new SolidBrush(Color.FromArgb(120, 255, 255, 255)))
                {
                    g.FillEllipse(pinBrush, X1 - 3f, Y1 - 3f, 6f, 6f);
                    g.FillEllipse(pinBrush, X2 - 3f, Y2 - 3f, 6f, 6f);
                }
            }
        }

        private void DrawHandle(Graphics g, float hx, float hy, bool isGrabbed, bool isHover)
        {
            float baseR = isGrabbed ? 14f : (isHover ? 13f : 10f);

            // Outer golden glow
            using (SolidBrush halo = new SolidBrush(Color.FromArgb(isGrabbed ? 120 : (isHover ? 90 : 50), 250, 204, 21)))
            {
                g.FillEllipse(halo, hx - baseR * 1.5f, hy - baseR * 1.5f, baseR * 3f, baseR * 3f);
            }

            // Outer ring
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(240, 30, 36, 45)))
            {
                g.FillEllipse(bgBrush, hx - baseR, hy - baseR, baseR * 2f, baseR * 2f);
            }

            using (Pen ringPen = new Pen(isHover || isGrabbed ? Color.FromArgb(255, 255, 215, 0) : Color.White, 2f))
            {
                g.DrawEllipse(ringPen, hx - baseR, hy - baseR, baseR * 2f, baseR * 2f);
            }

            // Center gold core
            float coreR = baseR * 0.42f;
            using (SolidBrush coreBrush = new SolidBrush(Color.FromArgb(255, 250, 204, 21)))
            {
                g.FillEllipse(coreBrush, hx - coreR, hy - coreR, coreR * 2f, coreR * 2f);
            }
        }
    }

    public class ChristmasStarItem
    {
        public float X;
        public float Y;
        public float Size = 38f;
        public bool IsGrabbed = false;
        public float GrabOffsetX = 0f;
        public float GrabOffsetY = 0f;
        public IntPtr AttachedWindow = IntPtr.Zero;
        public float WindowOffsetX = 0f;
        public float WindowOffsetY = 0f;
        public bool IsHovered = false;
        public bool HoverDelete = false;

        public ChristmasStarItem(float x, float y, float size = 38f)
        {
            X = x;
            Y = y;
            Size = size;
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(X - Size * 0.5f, Y - Size * 0.5f, Size, Size);
        }

        public RectangleF GetDeleteButtonBounds()
        {
            return new RectangleF(X + Size * 0.5f - 8f, Y - Size * 0.5f - 8f, 18f, 18f);
        }

        public void Draw(Graphics g, float animTick)
        {
            float pulse = (float)(0.76 + 0.24 * Math.Sin(animTick * 2.8f + X * 0.015f));
            float rOut = Size * 0.5f;
            float rIn = rOut * 0.42f;

            // Radiant golden outer halo
            using (SolidBrush glow1 = new SolidBrush(Color.FromArgb((int)(65 * pulse), 255, 215, 0)))
            {
                g.FillEllipse(glow1, X - rOut * 1.8f, Y - rOut * 1.8f, rOut * 3.6f, rOut * 3.6f);
            }
            using (SolidBrush glow2 = new SolidBrush(Color.FromArgb((int)(35 * pulse), 255, 245, 170)))
            {
                g.FillEllipse(glow2, X - rOut * 2.6f, Y - rOut * 2.6f, rOut * 5.2f, rOut * 5.2f);
            }

            // 3D Shaded Star Polygon
            ChristmasManager.DrawStarPolygon(g, X, Y, rOut, rIn, Color.FromArgb(255, 255, 235, 110), Color.FromArgb(255, 235, 145, 20));

            // Center sparkle
            using (SolidBrush glint = new SolidBrush(Color.FromArgb((int)(240 * pulse), 255, 255, 255)))
            {
                g.FillEllipse(glint, X - 2.5f, Y - 2.5f, 5f, 5f);
            }

            // Delete (X) button on hover
            if (IsHovered || HoverDelete)
            {
                RectangleF delRect = GetDeleteButtonBounds();
                ChristmasManager.DrawDeleteButton(g, delRect.X + delRect.Width * 0.5f, delRect.Y + delRect.Height * 0.5f, HoverDelete);
            }
        }
    }

    public class ChristmasTreeItem
    {
        public float X;
        public float Y;
        public float Width = 108f;
        public float Height = 148f;
        public bool IsGrabbed = false;
        public float GrabOffsetX = 0f;
        public float GrabOffsetY = 0f;
        public IntPtr AttachedWindow = IntPtr.Zero;
        public float WindowOffsetX = 0f;
        public float WindowOffsetY = 0f;
        public bool IsHovered = false;
        public bool HoverDelete = false;

        private struct BaubleInfo
        {
            public float XRel, YRel, Radius;
            public Color Color;
            public BaubleInfo(float xr, float yr, float r, Color c) { XRel = xr; YRel = yr; Radius = r; Color = c; }
        }

        private struct LightInfo
        {
            public float XRel, YRel;
            public Color Color;
            public float Phase;
            public LightInfo(float xr, float yr, Color c, float p) { XRel = xr; YRel = yr; Color = c; Phase = p; }
        }

        private static readonly BaubleInfo[] _baubles = new BaubleInfo[]
        {
            new BaubleInfo(0.04f,  -0.67f, 6.2f, Color.FromArgb(255, 85, 145)),  // Candy Pink (top cone)
            new BaubleInfo(-0.14f, -0.56f, 5.8f, Color.FromArgb(125, 215, 255)), // Sky Blue (upper left)
            new BaubleInfo(-0.22f, -0.39f, 6.8f, Color.FromArgb(242, 34, 52)),   // Ruby Red (mid left)
            new BaubleInfo(0.26f,  -0.37f, 7.0f, Color.FromArgb(255, 185, 15)),  // Shiny Gold (mid right)
            new BaubleInfo(-0.28f, -0.22f, 6.5f, Color.FromArgb(125, 215, 255)), // Sky Blue (lower left)
            new BaubleInfo(-0.07f, -0.15f, 7.0f, Color.FromArgb(255, 185, 15)),  // Shiny Gold (lower mid-left)
            new BaubleInfo(0.30f,  -0.20f, 6.5f, Color.FromArgb(255, 85, 145)),  // Candy Pink (lower right)
            new BaubleInfo(0.06f,  -0.07f, 7.0f, Color.FromArgb(242, 34, 52))    // Ruby Red (bottom center)
        };

        private static readonly LightInfo[] _fairyLights = new LightInfo[]
        {
            new LightInfo(0.17f,  -0.65f, Color.FromArgb(255, 240, 130), 0.0f),
            new LightInfo(-0.04f, -0.60f, Color.FromArgb(255, 215, 75),  1.3f),
            new LightInfo(0.09f,  -0.54f, Color.FromArgb(255, 175, 65),  2.6f),
            new LightInfo(-0.03f, -0.44f, Color.FromArgb(255, 248, 155), 0.9f),
            new LightInfo(0.12f,  -0.42f, Color.FromArgb(255, 215, 75),  3.2f),
            new LightInfo(-0.13f, -0.31f, Color.FromArgb(255, 240, 130), 1.8f),
            new LightInfo(0.09f,  -0.29f, Color.FromArgb(255, 185, 85),  4.1f),
            new LightInfo(0.20f,  -0.30f, Color.FromArgb(255, 248, 165), 2.5f),
            new LightInfo(-0.30f, -0.14f, Color.FromArgb(255, 215, 75),  0.6f),
            new LightInfo(-0.17f, -0.13f, Color.FromArgb(255, 248, 165), 3.7f),
            new LightInfo(0.22f,  -0.11f, Color.FromArgb(255, 240, 130), 1.5f),
            new LightInfo(0.34f,  -0.14f, Color.FromArgb(255, 215, 75),  4.8f)
        };

        public ChristmasTreeItem(float x, float y)
        {
            X = x;
            Y = y;
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(X - Width * 0.5f, Y - Height, Width, Height);
        }

        public RectangleF GetDeleteButtonBounds()
        {
            return new RectangleF(X + Width * 0.42f - 10f, Y - Height + 10f - 10f, 20f, 20f);
        }

        public void Draw(Graphics g, float animTick)
        {
            float cx = X;
            float cy = Y;
            float w = Width;
            float h = Height;

            // 0. Soft contact shadow on ground / taskbar
            float shadowW = w * 0.88f;
            float shadowH = 12f;
            using (GraphicsPath sp = new GraphicsPath())
            {
                sp.AddEllipse(cx - shadowW * 0.5f, cy - shadowH * 0.5f, shadowW, shadowH);
                using (PathGradientBrush pgb = new PathGradientBrush(sp))
                {
                    pgb.CenterColor = Color.FromArgb(120, 15, 25, 40);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 15, 25, 40) };
                    g.FillPath(pgb, sp);
                }
            }

            // 1. Trunk (rounded warm wood with vertical grain lines)
            float trunkW = w * 0.17f;
            float trunkH = h * 0.13f;
            RectangleF trunkRect = new RectangleF(cx - trunkW * 0.5f, cy - trunkH, trunkW, trunkH);
            using (GraphicsPath tp = VectorIcons.CreateRoundedRect(trunkRect, 4f))
            {
                using (LinearGradientBrush tGrad = new LinearGradientBrush(
                    new PointF(trunkRect.Left, trunkRect.Top),
                    new PointF(trunkRect.Right, trunkRect.Top),
                    Color.FromArgb(255, 205, 125, 58),
                    Color.FromArgb(255, 145, 78, 30)))
                {
                    g.FillPath(tGrad, tp);
                }
                using (Pen grain = new Pen(Color.FromArgb(50, 255, 255, 255), 1f))
                {
                    g.DrawLine(grain, cx - trunkW * 0.18f, cy - trunkH + 2f, cx - trunkW * 0.18f, cy - 1f);
                    g.DrawLine(grain, cx + trunkW * 0.15f, cy - trunkH + 2f, cx + trunkW * 0.15f, cy - 2f);
                }
                using (Pen tRim = new Pen(Color.FromArgb(90, 85, 42, 15), 1f))
                {
                    g.DrawPath(tRim, tp);
                }
            }

            // 2. The 4 Cascading Pine Tiers (Bottom Tier 4 up to Top Tier 1)
            float baseCy = cy - trunkH * 0.42f;

            DrawTier(g, cx, baseCy, w * 1.00f, h * 0.38f, 5, 4);
            DrawTier(g, cx, baseCy - h * 0.18f, w * 0.84f, h * 0.35f, 4, 3);
            DrawTier(g, cx, baseCy - h * 0.35f, w * 0.67f, h * 0.33f, 3, 2);
            DrawTier(g, cx, baseCy - h * 0.52f, w * 0.48f, h * 0.35f, 3, 1);

            // 3. Snow mounds resting naturally on branch tips & shoulders
            DrawTreeSnow(g, cx, baseCy, w, h);

            // 4. Fairy Light Garlands with twinkling warm glow
            DrawFairyLights(g, cx, cy, w, h, animTick);

            // 5. Glossy Colorful Baubles
            DrawBaubles(g, cx, cy, w, h);

            // 6. Radiant Golden Star on peak
            float starCy = baseCy - h * 0.84f;
            DrawPeakStar(g, cx, starCy, 12.5f, animTick);

            // 7. Delete Button (X) on hover
            if (IsHovered || HoverDelete)
            {
                RectangleF delRect = GetDeleteButtonBounds();
                ChristmasManager.DrawDeleteButton(g, delRect.X + delRect.Width * 0.5f, delRect.Y + delRect.Height * 0.5f, HoverDelete);
            }
        }

        private void DrawTier(Graphics g, float cx, float cy, float tw, float th, int lobes, int tierIdx)
        {
            float halfW = tw * 0.5f;
            float topY = cy - th;
            float botY = cy;

            using (GraphicsPath path = new GraphicsPath())
            {
                PointF topPt = new PointF(cx, topY);
                PointF rightTip = new PointF(cx + halfW, botY - th * 0.08f);
                PointF leftTip = new PointF(cx - halfW, botY - th * 0.08f);

                PointF ctrlR1 = new PointF(cx + halfW * 0.22f, topY + th * 0.22f);
                PointF ctrlR2 = new PointF(cx + halfW * 0.96f, topY + th * 0.68f);
                path.AddBezier(topPt, ctrlR1, ctrlR2, rightTip);

                float lobeW = tw / lobes;
                for (int i = lobes - 1; i >= 0; i--)
                {
                    float xRight = (cx - halfW) + (i + 1) * lobeW;
                    float xLeft = (cx - halfW) + i * lobeW;

                    float yOuterR = (i == lobes - 1) ? (botY - th * 0.08f) : (botY - 2.5f);
                    float yOuterL = (i == 0) ? (botY - th * 0.08f) : (botY - 2.5f);
                    float yDip = botY + th * 0.16f;

                    path.AddBezier(
                        new PointF(xRight, yOuterR),
                        new PointF(xRight - lobeW * 0.15f, yDip),
                        new PointF(xLeft + lobeW * 0.15f, yDip),
                        new PointF(xLeft, yOuterL)
                    );
                }

                PointF ctrlL2 = new PointF(cx - halfW * 0.96f, topY + th * 0.68f);
                PointF ctrlL1 = new PointF(cx - halfW * 0.22f, topY + th * 0.22f);
                path.AddBezier(leftTip, ctrlL2, ctrlL1, topPt);
                path.CloseFigure();

                Color cTop = Color.FromArgb(255, 96, 226, 118);
                Color cMid = Color.FromArgb(255, 62, 198, 92);
                Color cBot = Color.FromArgb(255, 38, 154, 68);

                using (LinearGradientBrush grad = new LinearGradientBrush(
                    new PointF(cx, topY),
                    new PointF(cx, botY + 8f),
                    cTop, cBot))
                {
                    ColorBlend cb = new ColorBlend(3);
                    cb.Colors = new Color[] { cTop, cMid, cBot };
                    cb.Positions = new float[] { 0f, 0.45f, 1f };
                    grad.InterpolationColors = cb;
                    g.FillPath(grad, path);
                }

                using (Pen rim = new Pen(Color.FromArgb(95, 24, 115, 50), 1f))
                {
                    g.DrawPath(rim, path);
                }

                if (tierIdx < 4)
                {
                    using (GraphicsPath shadowPath = new GraphicsPath())
                    {
                        shadowPath.AddEllipse(cx - halfW * 0.88f, botY - 2f, tw * 0.88f, 12f);
                        using (SolidBrush sBrush = new SolidBrush(Color.FromArgb(70, 18, 80, 35)))
                        {
                            g.FillPath(sBrush, shadowPath);
                        }
                    }
                }
            }
        }

        private void DrawTreeSnow(Graphics g, float cx, float cy, float w, float h)
        {
            DrawSnowPuff(g, cx - w * 0.46f, cy - 2f, 14f, 7f);
            DrawSnowPuff(g, cx + w * 0.46f, cy - 2f, 14f, 7f);
            DrawSnowPuff(g, cx - w * 0.18f, cy + 2f, 12f, 6f);
            DrawSnowPuff(g, cx + w * 0.18f, cy + 2f, 12f, 6f);

            float t3Y = cy - h * 0.18f;
            DrawSnowPuff(g, cx - w * 0.38f, t3Y - 2f, 13f, 6.5f);
            DrawSnowPuff(g, cx + w * 0.38f, t3Y - 2f, 13f, 6.5f);
            DrawSnowPuff(g, cx, t3Y + 1.5f, 11f, 5.5f);

            float t2Y = cy - h * 0.35f;
            DrawSnowPuff(g, cx - w * 0.30f, t2Y - 2f, 12f, 6f);
            DrawSnowPuff(g, cx + w * 0.30f, t2Y - 2f, 12f, 6f);

            float t1Y = cy - h * 0.52f;
            DrawSnowPuff(g, cx - w * 0.21f, t1Y - 1.5f, 10f, 5f);
            DrawSnowPuff(g, cx + w * 0.21f, t1Y - 1.5f, 10f, 5f);
        }

        private void DrawSnowPuff(Graphics g, float px, float py, float pw, float ph)
        {
            float hw = pw * 0.5f;
            float hh = ph * 0.5f;

            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(75, 20, 70, 32)))
            {
                g.FillEllipse(shadow, px - hw - 1f, py - hh + 1.5f, pw + 2f, ph);
            }
            using (SolidBrush blueBase = new SolidBrush(Color.FromArgb(255, 220, 238, 252)))
            {
                g.FillEllipse(blueBase, px - hw, py - hh, pw, ph);
                g.FillEllipse(blueBase, px - hw * 0.4f, py - hh * 1.3f, pw * 0.55f, ph * 0.9f);
                g.FillEllipse(blueBase, px + hw * 0.1f, py - hh * 1.2f, pw * 0.50f, ph * 0.85f);
            }
            using (SolidBrush whiteTop = new SolidBrush(Color.FromArgb(255, 255, 255, 255)))
            {
                g.FillEllipse(whiteTop, px - hw + 0.8f, py - hh - 1f, pw - 1.6f, ph * 0.85f);
                g.FillEllipse(whiteTop, px - hw * 0.35f, py - hh * 1.3f, pw * 0.50f, ph * 0.80f);
                g.FillEllipse(whiteTop, px + hw * 0.15f, py - hh * 1.2f, pw * 0.45f, ph * 0.75f);
            }
        }

        private void DrawBaubles(Graphics g, float cx, float cy, float w, float h)
        {
            for (int i = 0; i < _baubles.Length; i++)
            {
                BaubleInfo b = _baubles[i];
                float bx = cx + b.XRel * w;
                float by = cy + b.YRel * h;
                float br = b.Radius;
                Color c = b.Color;

                // Gold hanger cap
                using (SolidBrush cap = new SolidBrush(Color.FromArgb(255, 252, 212, 85)))
                {
                    g.FillRectangle(cap, bx - br * 0.30f, by - br - 3f, br * 0.60f, 3.2f);
                }
                using (Pen capWire = new Pen(Color.FromArgb(210, 165, 125, 25), 1f))
                {
                    g.DrawArc(capWire, bx - br * 0.22f, by - br - 4.5f, br * 0.44f, 3.2f, 180, 180);
                }

                // Glossy spherical body
                using (GraphicsPath bp = new GraphicsPath())
                {
                    bp.AddEllipse(bx - br, by - br, br * 2f, br * 2f);
                    Color darkC = Color.FromArgb(
                        (int)(c.R * 0.38f),
                        (int)(c.G * 0.38f),
                        (int)(c.B * 0.38f));

                    using (PathGradientBrush pgb = new PathGradientBrush(bp))
                    {
                        pgb.CenterPoint = new PointF(bx - br * 0.35f, by - br * 0.35f);
                        pgb.CenterColor = Color.FromArgb(255,
                            Math.Min(255, (int)(c.R * 1.55f)),
                            Math.Min(255, (int)(c.G * 1.55f)),
                            Math.Min(255, (int)(c.B * 1.55f)));
                        pgb.SurroundColors = new Color[] { darkC };
                        g.FillPath(pgb, bp);
                    }
                }

                // Glossy highlights
                using (SolidBrush glint = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
                {
                    g.FillEllipse(glint, bx - br * 0.58f, by - br * 0.58f, br * 0.62f, br * 0.44f);
                }
                using (SolidBrush bounce = new SolidBrush(Color.FromArgb(50, 255, 255, 255)))
                {
                    g.FillEllipse(bounce, bx + br * 0.20f, by + br * 0.20f, br * 0.45f, br * 0.45f);
                }
            }
        }

        private void DrawFairyLights(Graphics g, float cx, float cy, float w, float h, float animTick)
        {
            for (int i = 0; i < _fairyLights.Length; i++)
            {
                LightInfo l = _fairyLights[i];
                float lx = cx + l.XRel * w;
                float ly = cy + l.YRel * h;

                float pulse = (float)(0.75 + 0.25 * Math.Sin(animTick * 3.2f + l.Phase));
                int alpha = (int)(255 * pulse);

                // Radiant halo
                using (SolidBrush halo = new SolidBrush(Color.FromArgb((int)(80 * pulse), l.Color)))
                {
                    g.FillEllipse(halo, lx - 7f, ly - 7f, 14f, 14f);
                }
                // Inner warm ring
                using (SolidBrush inner = new SolidBrush(Color.FromArgb((int)(150 * pulse), 255, 245, 160)))
                {
                    g.FillEllipse(inner, lx - 3.2f, ly - 3.2f, 6.4f, 6.4f);
                }
                // Bright white-gold core
                using (SolidBrush core = new SolidBrush(Color.FromArgb(alpha, 255, 255, 245)))
                {
                    g.FillEllipse(core, lx - 1.6f, ly - 1.6f, 3.2f, 3.2f);
                }
            }
        }

        private void DrawPeakStar(Graphics g, float cx, float cy, float r, float animTick)
        {
            float pulse = (float)(0.80 + 0.20 * Math.Sin(animTick * 2.8f));

            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddEllipse(cx - r * 2.2f, cy - r * 2.2f, r * 4.4f, r * 4.4f);
                using (PathGradientBrush pgb = new PathGradientBrush(gp))
                {
                    pgb.CenterColor = Color.FromArgb((int)(135 * pulse), 255, 220, 30);
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 255, 200, 0) };
                    g.FillPath(pgb, gp);
                }
            }

            ChristmasManager.DrawStarPolygon(g, cx, cy, r, r * 0.46f,
                Color.FromArgb(255, 255, 242, 85),
                Color.FromArgb(255, 248, 175, 18));
        }
    }

    public class ChristmasGiftItem
    {
        public float X;
        public float Y;
        public float Width = 38f;
        public float Height = 34f;
        public Color BoxColor;
        public Color RibbonColor;
        public bool IsGrabbed = false;
        public bool IsFalling = false;
        public float Vx = 0f;
        public float Vy = 0f;
        public float GrabOffsetX = 0f;
        public float GrabOffsetY = 0f;
        public IntPtr AttachedWindow = IntPtr.Zero;
        public float WindowOffsetX = 0f;
        public float WindowOffsetY = 0f;
        public bool IsHovered = false;
        public bool HoverDelete = false;

        public ChristmasGiftItem(float x, float y, Color boxColor, Color ribbonColor)
        {
            X = x;
            Y = y;
            BoxColor = boxColor;
            RibbonColor = ribbonColor;
        }

        public RectangleF GetBounds()
        {
            return new RectangleF(X - Width * 0.5f, Y - Height, Width, Height);
        }

        public RectangleF GetDeleteButtonBounds()
        {
            return new RectangleF(X + Width * 0.5f + 2f - 9f, Y - Height - 2f - 9f, 18f, 18f);
        }

        public void Draw(Graphics g, float alpha = 1.0f)
        {
            if (alpha <= 0.01f) return;
            alpha = Math.Max(0f, Math.Min(1f, alpha));

            float left = X - Width * 0.5f;
            float top = Y - Height;
            float w = Width;
            float h = Height;

            // 1. Soft contact shadow under the gift box
            float shadowW = w * 1.25f;
            float shadowH = Math.Max(4f, h * 0.22f);
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb((int)(55 * alpha), 15, 25, 40)))
            {
                g.FillEllipse(shadowBrush, X - shadowW * 0.5f, Y - shadowH * 0.45f, shadowW, shadowH);
            }

            // 2. Main Gift Box Body (with 3D gradient shading)
            float lidH = 8f;
            RectangleF bodyRect = new RectangleF(left + 2f, top + lidH, w - 4f, h - lidH);
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(bodyRect, 3f))
            {
                Color darkBox = Color.FromArgb(
                    (int)(BoxColor.R * 0.72f),
                    (int)(BoxColor.G * 0.72f),
                    (int)(BoxColor.B * 0.72f));

                using (LinearGradientBrush bodyGrad = new LinearGradientBrush(
                    new PointF(left, top + lidH),
                    new PointF(left + w, top + h),
                    Color.FromArgb((int)(255 * alpha), BoxColor),
                    Color.FromArgb((int)(255 * alpha), darkBox)))
                {
                    g.FillPath(bodyGrad, bp);
                }
                using (Pen bodyPen = new Pen(Color.FromArgb((int)(80 * alpha), 0, 0, 0), 1f))
                {
                    g.DrawPath(bodyPen, bp);
                }
            }

            // 3. Overhanging Lid on top
            RectangleF lidRect = new RectangleF(left, top, w, lidH + 2f);
            using (GraphicsPath lp = VectorIcons.CreateRoundedRect(lidRect, 3f))
            {
                Color lightBox = Color.FromArgb(
                    Math.Min(255, (int)(BoxColor.R * 1.18f)),
                    Math.Min(255, (int)(BoxColor.G * 1.18f)),
                    Math.Min(255, (int)(BoxColor.B * 1.18f)));

                using (LinearGradientBrush lidGrad = new LinearGradientBrush(
                    new PointF(left, top),
                    new PointF(left, top + lidH + 2f),
                    Color.FromArgb((int)(255 * alpha), lightBox),
                    Color.FromArgb((int)(255 * alpha), BoxColor)))
                {
                    g.FillPath(lidGrad, lp);
                }
                using (Pen lidPen = new Pen(Color.FromArgb((int)(90 * alpha), 0, 0, 0), 1f))
                {
                    g.DrawPath(lidPen, lp);
                }
            }

            // 4. Crossed Ribbons (Vertical & Horizontal)
            float ribbonW = Math.Max(5f, w * 0.17f);
            float ribbonX = X - ribbonW * 0.5f;

            Color lightRibbon = Color.FromArgb(
                Math.Min(255, (int)(RibbonColor.R * 1.25f)),
                Math.Min(255, (int)(RibbonColor.G * 1.25f)),
                Math.Min(255, (int)(RibbonColor.B * 1.25f)));

            using (LinearGradientBrush ribGrad = new LinearGradientBrush(
                new PointF(ribbonX, top),
                new PointF(ribbonX + ribbonW, top),
                Color.FromArgb((int)(255 * alpha), lightRibbon),
                Color.FromArgb((int)(255 * alpha), RibbonColor)))
            using (Pen ribPen = new Pen(Color.FromArgb((int)(70 * alpha), 60, 40, 10), 1f))
            {
                RectangleF vertRib = new RectangleF(ribbonX, top, ribbonW, h);
                g.FillRectangle(ribGrad, vertRib);
                g.DrawRectangle(ribPen, vertRib.X, vertRib.Y, vertRib.Width, vertRib.Height);

                float horizH = ribbonW * 0.85f;
                float horizY = top + lidH + (h - lidH) * 0.45f - horizH * 0.5f;
                RectangleF horizRib = new RectangleF(left + 2f, horizY, w - 4f, horizH);
                g.FillRectangle(ribGrad, horizRib);
                g.DrawRectangle(ribPen, horizRib.X, horizRib.Y, horizRib.Width, horizRib.Height);
            }

            // 5. Tied Ribbon Bow on top
            float bowCx = X;
            float bowCy = top + 1f;
            float loopW = w * 0.28f;
            float loopH = h * 0.32f;

            using (SolidBrush bowBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), RibbonColor)))
            using (Pen bowPen = new Pen(Color.FromArgb((int)(110 * alpha), 50, 30, 10), 1f))
            {
                GraphicsPath leftLoop = new GraphicsPath();
                leftLoop.AddEllipse(bowCx - loopW * 1.05f, bowCy - loopH * 0.85f, loopW, loopH);
                g.FillPath(bowBrush, leftLoop);
                g.DrawPath(bowPen, leftLoop);

                GraphicsPath rightLoop = new GraphicsPath();
                rightLoop.AddEllipse(bowCx + loopW * 0.05f, bowCy - loopH * 0.85f, loopW, loopH);
                g.FillPath(bowBrush, rightLoop);
                g.DrawPath(bowPen, rightLoop);

                float knotR = loopW * 0.38f;
                g.FillEllipse(bowBrush, bowCx - knotR, bowCy - knotR * 0.8f, knotR * 2f, knotR * 1.6f);
                g.DrawEllipse(bowPen, bowCx - knotR, bowCy - knotR * 0.8f, knotR * 2f, knotR * 1.6f);
            }

            // 6. Delete Button (X) on hover
            if (IsHovered || HoverDelete)
            {
                RectangleF delRect = GetDeleteButtonBounds();
                ChristmasManager.DrawDeleteButton(g, delRect.X + delRect.Width * 0.5f, delRect.Y + delRect.Height * 0.5f, HoverDelete);
            }
        }
    }

    public class ChristmasManager
    {
        public const int PART_NONE = 0;
        public const int PART_DELETE = 1;
        public const int PART_HANDLE1 = 2;
        public const int PART_HANDLE2 = 3;
        public const int PART_BODY = 4;

        public List<ChristmasTreeItem> Trees { get; private set; }
        public List<ChristmasGiftItem> Gifts { get; private set; }
        public List<ChristmasLightsItem> Lights { get; private set; }
        public List<ChristmasStarItem> Stars { get; private set; }
        public bool ShowSantaHat { get; set; }
        public bool BehindActiveWindows { get; set; }

        public int TotalItemCount
        {
            get { return Trees.Count + Gifts.Count + Lights.Count + Stars.Count; }
        }

        private object _grabbedItem = null;
        private int _grabbedPart = PART_NONE;
        private int _prevMouseX = -1;
        private int _prevMouseY = -1;
        private float _animTick = 0f;
        private readonly Random _rand = new Random();
        private readonly string _settingsPath;

        public ChristmasManager()
        {
            Trees = new List<ChristmasTreeItem>();
            Gifts = new List<ChristmasGiftItem>();
            Lights = new List<ChristmasLightsItem>();
            Stars = new List<ChristmasStarItem>();
            ShowSantaHat = false;
            BehindActiveWindows = true;

            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "christmas_settings.txt");
            LoadSettings();
        }

        public ChristmasTreeItem AddTree(float x, float y)
        {
            ChristmasTreeItem item = new ChristmasTreeItem(x, y);
            Trees.Add(item);
            SaveSettings();
            return item;
        }

        public ChristmasGiftItem AddGift(float x, float y, Color? boxCol = null, Color? ribCol = null)
        {
            Color bCol = boxCol.HasValue ? boxCol.Value : Color.FromArgb(220, 38, 38);
            Color rCol = ribCol.HasValue ? ribCol.Value : Color.FromArgb(255, 215, 0);

            ChristmasGiftItem item = new ChristmasGiftItem(x, y, bCol, rCol);
            Gifts.Add(item);
            SaveSettings();
            return item;
        }

        public ChristmasLightsItem AddLights(float x1, float y1, float x2, float y2)
        {
            ChristmasLightsItem item = new ChristmasLightsItem(x1, y1, x2, y2);
            item.IsHovered = true; // Show handles immediately when placed
            Lights.Add(item);
            SaveSettings();
            return item;
        }

        public ChristmasStarItem AddStar(float x, float y, float size = 38f)
        {
            ChristmasStarItem item = new ChristmasStarItem(x, y, size);
            item.IsHovered = true;
            Stars.Add(item);
            SaveSettings();
            return item;
        }

        public void RemoveItem(object item)
        {
            if (item is ChristmasTreeItem)
            {
                Trees.Remove((ChristmasTreeItem)item);
                SaveSettings();
            }
            else if (item is ChristmasGiftItem)
            {
                Gifts.Remove((ChristmasGiftItem)item);
                SaveSettings();
            }
            else if (item is ChristmasLightsItem)
            {
                Lights.Remove((ChristmasLightsItem)item);
                SaveSettings();
            }
            else if (item is ChristmasStarItem)
            {
                Stars.Remove((ChristmasStarItem)item);
                SaveSettings();
            }
        }

        public void ClearAll()
        {
            Trees.Clear();
            Gifts.Clear();
            Lights.Clear();
            Stars.Clear();
            SaveSettings();
        }

        public void UpdateHover(float mouseX, float mouseY)
        {
            // Update hover flags for visual affordances (delete buttons, stretch handles)
            for (int i = 0; i < Lights.Count; i++)
            {
                ChristmasLightsItem l = Lights[i];
                l.IsHovered = false;
                l.HoverPart = PART_NONE;

                if (l.GetDeleteButtonBounds().Contains(mouseX, mouseY))
                {
                    l.IsHovered = true;
                    l.HoverPart = PART_DELETE;
                }
                else if (l.GetHandle1Bounds().Contains(mouseX, mouseY))
                {
                    l.IsHovered = true;
                    l.HoverPart = PART_HANDLE1;
                }
                else if (l.GetHandle2Bounds().Contains(mouseX, mouseY))
                {
                    l.IsHovered = true;
                    l.HoverPart = PART_HANDLE2;
                }
                else if (l.IsNearWire(mouseX, mouseY, 24f))
                {
                    l.IsHovered = true;
                    l.HoverPart = PART_BODY;
                }
            }

            for (int i = 0; i < Stars.Count; i++)
            {
                ChristmasStarItem s = Stars[i];
                RectangleF b = s.GetBounds();
                b.Inflate(8f, 8f);
                s.IsHovered = b.Contains(mouseX, mouseY);
                s.HoverDelete = s.GetDeleteButtonBounds().Contains(mouseX, mouseY);
            }

            for (int i = 0; i < Trees.Count; i++)
            {
                ChristmasTreeItem t = Trees[i];
                t.IsHovered = t.GetBounds().Contains(mouseX, mouseY);
                t.HoverDelete = t.GetDeleteButtonBounds().Contains(mouseX, mouseY);
            }

            for (int i = 0; i < Gifts.Count; i++)
            {
                ChristmasGiftItem g = Gifts[i];
                g.IsHovered = g.GetBounds().Contains(mouseX, mouseY);
                g.HoverDelete = g.GetDeleteButtonBounds().Contains(mouseX, mouseY);
            }
        }

        public object HitTest(float mouseX, float mouseY, out int hitPart)
        {
            hitPart = PART_NONE;

            // 1. Check Delete Buttons FIRST across all items so (X) click always succeeds
            for (int i = Lights.Count - 1; i >= 0; i--)
            {
                if (Lights[i].IsHovered && Lights[i].GetDeleteButtonBounds().Contains(mouseX, mouseY))
                {
                    hitPart = PART_DELETE;
                    return Lights[i];
                }
            }

            for (int i = Stars.Count - 1; i >= 0; i--)
            {
                if (Stars[i].GetDeleteButtonBounds().Contains(mouseX, mouseY))
                {
                    hitPart = PART_DELETE;
                    return Stars[i];
                }
            }

            for (int i = Trees.Count - 1; i >= 0; i--)
            {
                if (Trees[i].GetDeleteButtonBounds().Contains(mouseX, mouseY))
                {
                    hitPart = PART_DELETE;
                    return Trees[i];
                }
            }

            for (int i = Gifts.Count - 1; i >= 0; i--)
            {
                if (Gifts[i].GetDeleteButtonBounds().Contains(mouseX, mouseY))
                {
                    hitPart = PART_DELETE;
                    return Gifts[i];
                }
            }

            // 2. Check Lights Handles (Left & Right stretch anchors)
            for (int i = Lights.Count - 1; i >= 0; i--)
            {
                ChristmasLightsItem l = Lights[i];
                if (l.GetHandle1Bounds().Contains(mouseX, mouseY))
                {
                    hitPart = PART_HANDLE1;
                    return l;
                }
                if (l.GetHandle2Bounds().Contains(mouseX, mouseY))
                {
                    hitPart = PART_HANDLE2;
                    return l;
                }
            }

            // 3. Check Stars (clickable / draggable)
            for (int i = Stars.Count - 1; i >= 0; i--)
            {
                ChristmasStarItem s = Stars[i];
                RectangleF b = s.GetBounds();
                b.Inflate(8f, 8f);
                if (b.Contains(mouseX, mouseY))
                {
                    hitPart = PART_BODY;
                    return s;
                }
            }

            // 4. Check Gifts (clickable / draggable)
            for (int i = Gifts.Count - 1; i >= 0; i--)
            {
                ChristmasGiftItem g = Gifts[i];
                RectangleF b = g.GetBounds();
                b.Inflate(6f, 6f);
                if (b.Contains(mouseX, mouseY))
                {
                    hitPart = PART_BODY;
                    return g;
                }
            }

            // 5. Check Trees
            for (int i = Trees.Count - 1; i >= 0; i--)
            {
                ChristmasTreeItem t = Trees[i];
                RectangleF tb = t.GetBounds();
                tb.Inflate(4f, 4f);
                if (tb.Contains(mouseX, mouseY))
                {
                    hitPart = PART_BODY;
                    return t;
                }
            }

            // 6. Check Lights Wire Body
            for (int i = Lights.Count - 1; i >= 0; i--)
            {
                ChristmasLightsItem l = Lights[i];
                if (l.IsNearWire(mouseX, mouseY, 18f))
                {
                    hitPart = PART_BODY;
                    return l;
                }
            }

            return null;
        }

        public void StartDrag(object hit, int hitPart, float mouseX, float mouseY)
        {
            _grabbedItem = hit;
            _grabbedPart = hitPart;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;

            if (hit is ChristmasLightsItem)
            {
                ChristmasLightsItem l = (ChristmasLightsItem)hit;
                if (hitPart == PART_HANDLE1)
                {
                    l.IsGrabbedHandle1 = true;
                }
                else if (hitPart == PART_HANDLE2)
                {
                    l.IsGrabbedHandle2 = true;
                }
                else
                {
                    l.IsGrabbedBody = true;
                }
            }
            else if (hit is ChristmasStarItem)
            {
                ChristmasStarItem s = (ChristmasStarItem)hit;
                s.IsGrabbed = true;
                s.AttachedWindow = IntPtr.Zero;
                s.GrabOffsetX = mouseX - s.X;
                s.GrabOffsetY = mouseY - s.Y;
            }
            else if (hit is ChristmasTreeItem)
            {
                ChristmasTreeItem t = (ChristmasTreeItem)hit;
                t.IsGrabbed = true;
                t.AttachedWindow = IntPtr.Zero;
                t.GrabOffsetX = mouseX - t.X;
                t.GrabOffsetY = mouseY - t.Y;
            }
            else if (hit is ChristmasGiftItem)
            {
                ChristmasGiftItem g = (ChristmasGiftItem)hit;
                g.IsGrabbed = true;
                g.IsFalling = false;
                g.AttachedWindow = IntPtr.Zero;
                g.GrabOffsetX = mouseX - g.X;
                g.GrabOffsetY = mouseY - g.Y;
                g.Vx = 0f;
                g.Vy = 0f;
            }
        }

        public void OnMouseMove(float mouseX, float mouseY, WindowManager winMgr, int screenWidth, int floorY, Action<float, float, int> onSnowPuff)
        {
            float dx = (_prevMouseX >= 0) ? (mouseX - _prevMouseX) : 0f;
            float dy = (_prevMouseY >= 0) ? (mouseY - _prevMouseY) : 0f;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;

            if (_grabbedItem is ChristmasLightsItem)
            {
                ChristmasLightsItem l = (ChristmasLightsItem)_grabbedItem;
                if (_grabbedPart == PART_HANDLE1)
                {
                    l.X1 = mouseX;
                    l.Y1 = mouseY;
                    l.RecalculateSag();
                }
                else if (_grabbedPart == PART_HANDLE2)
                {
                    l.X2 = mouseX;
                    l.Y2 = mouseY;
                    l.RecalculateSag();
                }
                else if (_grabbedPart == PART_BODY)
                {
                    l.X1 += dx;
                    l.Y1 += dy;
                    l.X2 += dx;
                    l.Y2 += dy;
                }
            }
            else if (_grabbedItem is ChristmasStarItem)
            {
                ChristmasStarItem s = (ChristmasStarItem)_grabbedItem;
                s.X = mouseX - s.GrabOffsetX;
                s.Y = mouseY - s.GrabOffsetY;

                if (Math.Abs(dx) > 4f && onSnowPuff != null && _rand.Next(3) == 0)
                {
                    onSnowPuff(s.X, s.Y, 2);
                }
            }
            else if (_grabbedItem is ChristmasTreeItem)
            {
                ChristmasTreeItem t = (ChristmasTreeItem)_grabbedItem;
                t.X = mouseX - t.GrabOffsetX;
                t.Y = mouseY - t.GrabOffsetY;

                if (Math.Abs(dx) > 4f && onSnowPuff != null && _rand.Next(3) == 0)
                {
                    onSnowPuff(t.X, t.Y, 3);
                }
            }
            else if (_grabbedItem is ChristmasGiftItem)
            {
                ChristmasGiftItem g = (ChristmasGiftItem)_grabbedItem;
                g.X = mouseX - g.GrabOffsetX;
                g.Y = mouseY - g.GrabOffsetY;
                g.Vx = dx * 0.85f;
                g.Vy = dy * 0.85f;

                if (Math.Abs(dx) > 4f && onSnowPuff != null && _rand.Next(4) == 0)
                {
                    onSnowPuff(g.X, g.Y, 2);
                }
            }
        }

        public void OnMouseUp(float mouseX, float mouseY, WindowManager winMgr, int floorY, Action<float, float, int> onSnowPuff)
        {
            if (_grabbedItem is ChristmasLightsItem)
            {
                ChristmasLightsItem l = (ChristmasLightsItem)_grabbedItem;
                l.IsGrabbedHandle1 = false;
                l.IsGrabbedHandle2 = false;
                l.IsGrabbedBody = false;
                SaveSettings();
            }
            else if (_grabbedItem is ChristmasStarItem)
            {
                ChristmasStarItem s = (ChristmasStarItem)_grabbedItem;
                s.IsGrabbed = false;

                // Attachment to active window
                if (winMgr != null)
                {
                    List<WindowInfo> windows = winMgr.ActiveWindows;
                    for (int w = 0; w < windows.Count; w++)
                    {
                        WindowInfo win = windows[w];
                        Rectangle r = win.Bounds;
                        if (s.X >= r.Left && s.X <= r.Right &&
                            s.Y >= r.Top - 30f && s.Y <= r.Bottom)
                        {
                            s.AttachedWindow = win.Handle;
                            s.WindowOffsetX = s.X - r.Left;
                            s.WindowOffsetY = s.Y - r.Top;
                            if (onSnowPuff != null) onSnowPuff(s.X, s.Y, 6);
                            break;
                        }
                    }
                }
                SaveSettings();
            }
            else if (_grabbedItem is ChristmasTreeItem)
            {
                ChristmasTreeItem t = (ChristmasTreeItem)_grabbedItem;
                t.IsGrabbed = false;
                bool placed = false;

                // Land on floating window top
                if (winMgr != null)
                {
                    List<WindowInfo> windows = winMgr.ActiveWindows;
                    for (int w = 0; w < windows.Count; w++)
                    {
                        WindowInfo win = windows[w];
                        Rectangle r = win.Bounds;
                        if (t.X >= r.Left && t.X <= r.Right &&
                            t.Y >= r.Top - 45f && t.Y <= r.Top + 25f)
                        {
                            t.Y = r.Top;
                            t.AttachedWindow = win.Handle;
                            t.WindowOffsetX = t.X - r.Left;
                            t.WindowOffsetY = r.Top - t.Y;
                            placed = true;
                            if (onSnowPuff != null) onSnowPuff(t.X, t.Y, 8);
                            break;
                        }
                    }
                }

                // Land on Taskbar
                if (!placed && t.Y >= floorY - 70f)
                {
                    t.Y = floorY - 6f;
                    t.AttachedWindow = IntPtr.Zero;
                    placed = true;
                    if (onSnowPuff != null) onSnowPuff(t.X, t.Y, 8);
                }

                if (!placed)
                {
                    t.Y = floorY - 6f;
                    t.AttachedWindow = IntPtr.Zero;
                }

                SaveSettings();
            }
            else if (_grabbedItem is ChristmasGiftItem)
            {
                ChristmasGiftItem g = (ChristmasGiftItem)_grabbedItem;
                g.IsGrabbed = false;
                bool placed = false;

                if (winMgr != null)
                {
                    List<WindowInfo> windows = winMgr.ActiveWindows;
                    for (int w = 0; w < windows.Count; w++)
                    {
                        WindowInfo win = windows[w];
                        Rectangle r = win.Bounds;
                        if (g.X >= r.Left && g.X <= r.Right &&
                            g.Y >= r.Top - 35f && g.Y <= r.Top + 20f)
                        {
                            g.Y = r.Top;
                            g.Vx = 0f;
                            g.Vy = 0f;
                            g.IsFalling = false;
                            g.AttachedWindow = win.Handle;
                            g.WindowOffsetX = g.X - r.Left;
                            g.WindowOffsetY = r.Top - g.Y;
                            placed = true;
                            if (onSnowPuff != null) onSnowPuff(g.X, g.Y, 5);
                            break;
                        }
                    }
                }

                if (!placed && g.Y >= floorY - 50f)
                {
                    g.Y = floorY - 6f;
                    g.Vx = 0f;
                    g.Vy = 0f;
                    g.IsFalling = false;
                    g.AttachedWindow = IntPtr.Zero;
                    placed = true;
                    if (onSnowPuff != null) onSnowPuff(g.X, g.Y, 5);
                }

                if (!placed)
                {
                    g.IsFalling = true;
                    g.AttachedWindow = IntPtr.Zero;
                }

                SaveSettings();
            }

            _grabbedItem = null;
            _grabbedPart = PART_NONE;
        }

        public void Update(WindowManager winMgr, int screenWidth, int screenHeight, int floorY, Action<float, float, int> onSnowPuff)
        {
            _animTick += 0.045f;

            // 1. Update Stars Window Attachment
            for (int i = 0; i < Stars.Count; i++)
            {
                ChristmasStarItem s = Stars[i];
                if (!s.IsGrabbed && s.AttachedWindow != IntPtr.Zero && winMgr != null)
                {
                    Rectangle curBounds;
                    bool hasMoved;
                    if (winMgr.TryGetWindowCurrentBounds(s.AttachedWindow, out curBounds, out hasMoved))
                    {
                        s.X = curBounds.Left + s.WindowOffsetX;
                        s.Y = curBounds.Top + s.WindowOffsetY;
                    }
                    else
                    {
                        s.AttachedWindow = IntPtr.Zero;
                    }
                }
            }

            // 2. Update Trees Window Attachment
            for (int i = 0; i < Trees.Count; i++)
            {
                ChristmasTreeItem t = Trees[i];
                if (!t.IsGrabbed && t.AttachedWindow != IntPtr.Zero && winMgr != null)
                {
                    Rectangle curBounds;
                    bool hasMoved;
                    if (winMgr.TryGetWindowCurrentBounds(t.AttachedWindow, out curBounds, out hasMoved))
                    {
                        if (t.X < curBounds.Left - 10f || t.X > curBounds.Right + 10f)
                        {
                            t.AttachedWindow = IntPtr.Zero;
                            t.Y = floorY - 6f;
                            if (onSnowPuff != null) onSnowPuff(t.X, t.Y, 8);
                        }
                        else
                        {
                            t.X = curBounds.Left + t.WindowOffsetX;
                            t.Y = curBounds.Top - t.WindowOffsetY;
                        }
                    }
                    else
                    {
                        t.AttachedWindow = IntPtr.Zero;
                        t.Y = floorY - 6f;
                    }
                }
            }

            // 3. Update Gifts Physics & Window Attachment
            for (int i = Gifts.Count - 1; i >= 0; i--)
            {
                ChristmasGiftItem g = Gifts[i];
                if (g.IsGrabbed) continue;

                if (g.IsFalling)
                {
                    g.Vy += 0.95f;
                    g.X += g.Vx;
                    g.Y += g.Vy;
                    g.Vx *= 0.98f;

                    // Landing on window
                    if (winMgr != null)
                    {
                        List<WindowInfo> windows = winMgr.ActiveWindows;
                        for (int w = 0; w < windows.Count; w++)
                        {
                            WindowInfo win = windows[w];
                            Rectangle r = win.Bounds;
                            if (g.X >= r.Left && g.X <= r.Right &&
                                g.Y >= r.Top && (g.Y - g.Vy) <= r.Top + 16f)
                            {
                                g.Y = r.Top;
                                g.Vy = 0f;
                                g.Vx = 0f;
                                g.IsFalling = false;
                                g.AttachedWindow = win.Handle;
                                g.WindowOffsetX = g.X - r.Left;
                                g.WindowOffsetY = r.Top - g.Y;
                                if (onSnowPuff != null) onSnowPuff(g.X, g.Y, 5);
                                break;
                            }
                        }
                    }

                    // Landing on Taskbar
                    if (g.IsFalling && g.Y >= floorY - 6f)
                    {
                        g.Y = floorY - 6f;
                        g.Vy = 0f;
                        g.Vx = 0f;
                        g.IsFalling = false;
                        g.AttachedWindow = IntPtr.Zero;
                        if (onSnowPuff != null) onSnowPuff(g.X, g.Y, 5);
                    }
                }
                else if (g.AttachedWindow != IntPtr.Zero && winMgr != null)
                {
                    Rectangle curBounds;
                    bool hasMoved;
                    if (winMgr.TryGetWindowCurrentBounds(g.AttachedWindow, out curBounds, out hasMoved))
                    {
                        if (g.X < curBounds.Left - 8f || g.X > curBounds.Right + 8f)
                        {
                            g.AttachedWindow = IntPtr.Zero;
                            g.IsFalling = true;
                            g.Vy = 2.0f;
                            continue;
                        }
                        g.X = curBounds.Left + g.WindowOffsetX;
                        g.Y = curBounds.Top - g.WindowOffsetY;
                    }
                    else
                    {
                        g.AttachedWindow = IntPtr.Zero;
                        g.IsFalling = true;
                    }
                }
            }
        }

        public void Draw(Graphics g, WindowManager winMgr, int screenWidth, int screenHeight, int floorY)
        {
            GraphicsState gState = g.Save();
            try
            {
                if (BehindActiveWindows && winMgr != null)
                {
                    List<Rectangle> appBounds = winMgr.AllAppBounds;
                    if (appBounds != null)
                    {
                        for (int b = 0; b < appBounds.Count; b++)
                        {
                            Rectangle r = appBounds[b];
                            if (r.Width > 50 && r.Height > 50)
                            {
                                g.ExcludeClip(r);
                            }
                        }
                    }
                }

                // 1. Draw String Lights (Pure string with hanging bulbs, no star attached)
                for (int i = 0; i < Lights.Count; i++)
                {
                    Lights[i].Draw(g, _animTick, true);
                }

                // 2. Draw Trees
                for (int i = 0; i < Trees.Count; i++)
                {
                    Trees[i].Draw(g, _animTick);
                }

                // 3. Draw Gifts
                for (int i = 0; i < Gifts.Count; i++)
                {
                    Gifts[i].Draw(g);
                }

                // 4. Draw Stars (Independent radiant golden Christmas stars)
                for (int i = 0; i < Stars.Count; i++)
                {
                    Stars[i].Draw(g, _animTick);
                }
            }
            finally
            {
                g.Restore(gState);
            }
        }

        public static void DrawStarPolygon(Graphics g, float cx, float cy, float outerR, float innerR, Color cTop, Color cBot)
        {
            PointF[] pts = new PointF[10];
            double angleStep = Math.PI / 5.0;
            double startAngle = -Math.PI / 2.0;

            for (int i = 0; i < 10; i++)
            {
                double a = startAngle + i * angleStep;
                float r = (i % 2 == 0) ? outerR : innerR;
                pts[i] = new PointF(cx + (float)(Math.Cos(a) * r), cy + (float)(Math.Sin(a) * r));
            }

            using (GraphicsPath sp = new GraphicsPath())
            {
                sp.AddPolygon(pts);
                using (LinearGradientBrush sg = new LinearGradientBrush(
                    new PointF(cx, cy - outerR),
                    new PointF(cx, cy + outerR),
                    cTop, cBot))
                {
                    g.FillPath(sg, sp);
                }
                using (Pen spPen = new Pen(Color.FromArgb(90, 160, 100, 10), 1f))
                {
                    g.DrawPath(spPen, sp);
                }
            }
        }

        public static void DrawDeleteButton(Graphics g, float cx, float cy, bool isHover)
        {
            float r = isHover ? 11f : 9f;
            Color bg = isHover ? Color.FromArgb(239, 68, 68) : Color.FromArgb(200, 220, 38, 38);

            if (isHover)
            {
                using (SolidBrush halo = new SolidBrush(Color.FromArgb(80, 239, 68, 68)))
                {
                    g.FillEllipse(halo, cx - r * 1.4f, cy - r * 1.4f, r * 2.8f, r * 2.8f);
                }
            }

            using (SolidBrush bBrush = new SolidBrush(bg))
            {
                g.FillEllipse(bBrush, cx - r, cy - r, r * 2f, r * 2f);
            }
            using (Pen borderPen = new Pen(Color.White, 1.2f))
            {
                g.DrawEllipse(borderPen, cx - r, cy - r, r * 2f, r * 2f);
            }

            float xSize = r * 0.45f;
            using (Pen xPen = new Pen(Color.White, 1.8f))
            {
                xPen.StartCap = LineCap.Round;
                xPen.EndCap = LineCap.Round;
                g.DrawLine(xPen, cx - xSize, cy - xSize, cx + xSize, cy + xSize);
                g.DrawLine(xPen, cx + xSize, cy - xSize, cx - xSize, cy + xSize);
            }
        }

        public void SaveSettings()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("santahat=" + ShowSantaHat.ToString().ToLower());
                lines.Add("behindwindows=" + BehindActiveWindows.ToString().ToLower());

                foreach (ChristmasTreeItem t in Trees)
                {
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "tree={0:F1},{1:F1}", t.X, t.Y));
                }

                foreach (ChristmasGiftItem g in Gifts)
                {
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "gift={0:F1},{1:F1},#{2:X2}{3:X2}{4:X2},#{5:X2}{6:X2}{7:X2}",
                        g.X, g.Y, g.BoxColor.R, g.BoxColor.G, g.BoxColor.B, g.RibbonColor.R, g.RibbonColor.G, g.RibbonColor.B));
                }

                foreach (ChristmasLightsItem l in Lights)
                {
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "lights={0:F1},{1:F1},{2:F1},{3:F1}",
                        l.X1, l.Y1, l.X2, l.Y2));
                }

                foreach (ChristmasStarItem s in Stars)
                {
                    lines.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "star={0:F1},{1:F1}", s.X, s.Y));
                }

                File.WriteAllLines(_settingsPath, lines.ToArray());
            }
            catch { }
        }

        public void LoadSettings()
        {
            try
            {
                Trees.Clear();
                Gifts.Clear();
                Lights.Clear();
                Stars.Clear();

                if (!File.Exists(_settingsPath)) return;
                string[] lines = File.ReadAllLines(_settingsPath);
                foreach (string rawLine in lines)
                {
                    if (string.IsNullOrEmpty(rawLine)) continue;
                    string line = rawLine.Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim().ToLower();
                    string val = line.Substring(eq + 1).Trim();

                    if (key == "santahat")
                    {
                        ShowSantaHat = (val.ToLower() == "true");
                    }
                    else if (key == "behindwindows")
                    {
                        BehindActiveWindows = (val.ToLower() == "true");
                    }
                    else if (key == "tree")
                    {
                        string[] parts = val.Split(',');
                        if (parts.Length >= 2)
                        {
                            float tx, ty;
                            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tx) &&
                                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out ty))
                            {
                                Trees.Add(new ChristmasTreeItem(tx, ty));
                            }
                        }
                    }
                    else if (key == "gift")
                    {
                        string[] parts = val.Split(',');
                        if (parts.Length >= 2)
                        {
                            float gx, gy;
                            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out gx) &&
                                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out gy))
                            {
                                Color bCol = Color.FromArgb(220, 38, 38);
                                Color rCol = Color.FromArgb(255, 215, 0);
                                if (parts.Length >= 3)
                                {
                                    try { bCol = ColorTranslator.FromHtml(parts[2]); } catch { }
                                }
                                if (parts.Length >= 4)
                                {
                                    try { rCol = ColorTranslator.FromHtml(parts[3]); } catch { }
                                }
                                Gifts.Add(new ChristmasGiftItem(gx, gy, bCol, rCol));
                            }
                        }
                    }
                    else if (key == "lights")
                    {
                        string[] parts = val.Split(',');
                        if (parts.Length >= 4)
                        {
                            float x1, y1, x2, y2;
                            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x1) &&
                                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y1) &&
                                float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x2) &&
                                float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y2))
                            {
                                Lights.Add(new ChristmasLightsItem(x1, y1, x2, y2));
                            }
                        }
                    }
                    else if (key == "star")
                    {
                        string[] parts = val.Split(',');
                        if (parts.Length >= 2)
                        {
                            float sx, sy;
                            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out sx) &&
                                float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out sy))
                            {
                                Stars.Add(new ChristmasStarItem(sx, sy));
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
}
