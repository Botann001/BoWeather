using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using BoWeather.Particles;

namespace BoWeather
{
    public class ChristmasGift
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

        public ChristmasGift(float x, float y, Color boxColor, Color ribbonColor)
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
                // Vertical ribbon
                RectangleF vertRib = new RectangleF(ribbonX, top, ribbonW, h);
                g.FillRectangle(ribGrad, vertRib);
                g.DrawRectangle(ribPen, vertRib.X, vertRib.Y, vertRib.Width, vertRib.Height);

                // Horizontal ribbon
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
                // Left bow loop
                GraphicsPath leftLoop = new GraphicsPath();
                leftLoop.AddEllipse(bowCx - loopW * 1.05f, bowCy - loopH * 0.85f, loopW, loopH);
                g.FillPath(bowBrush, leftLoop);
                g.DrawPath(bowPen, leftLoop);

                // Right bow loop
                GraphicsPath rightLoop = new GraphicsPath();
                rightLoop.AddEllipse(bowCx + loopW * 0.05f, bowCy - loopH * 0.85f, loopW, loopH);
                g.FillPath(bowBrush, rightLoop);
                g.DrawPath(bowPen, rightLoop);

                // Central bow knot
                float knotR = loopW * 0.38f;
                g.FillEllipse(bowBrush, bowCx - knotR, bowCy - knotR * 0.8f, knotR * 2f, knotR * 1.6f);
                g.DrawEllipse(bowPen, bowCx - knotR, bowCy - knotR * 0.8f, knotR * 2f, knotR * 1.6f);
            }
        }
    }

    public class ChristmasManager
    {
        // Settings
        public bool ShowTree { get; set; }
        public bool ShowGifts { get; set; }
        public bool ShowLights { get; set; }
        public bool ShowStarLights { get; set; }
        public bool ShowSantaHat { get; set; }

        // Tree State
        public float TreeX { get; set; }
        public float TreeY { get; set; }
        public float TreeWidth { get; set; }
        public float TreeHeight { get; set; }
        public bool TreeIsGrabbed { get; set; }
        public float TreeGrabOffsetX { get; set; }
        public float TreeGrabOffsetY { get; set; }
        public IntPtr TreeAttachedWindow { get; set; }
        public float TreeWindowOffsetX { get; set; }
        public float TreeWindowOffsetY { get; set; }

        // Gifts list
        public List<ChristmasGift> Gifts { get; private set; }

        // Dragging & Interaction
        private object _grabbedItem = null;
        private int _prevMouseX = -1;
        private int _prevMouseY = -1;
        private float _animTick = 0f;
        private readonly Random _rand = new Random();
        private readonly string _settingsPath;

        public ChristmasManager()
        {
            ShowTree = true;
            ShowGifts = true;
            ShowLights = true;
            ShowStarLights = true;
            ShowSantaHat = true;

            TreeWidth = 98f;
            TreeHeight = 138f;
            TreeX = -1f;
            TreeY = -1f;

            Gifts = new List<ChristmasGift>();
            _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "christmas_settings.txt");
            LoadSettings();
        }

        public void InitDefaultsIfNeeded(int screenWidth, int floorY)
        {
            if (TreeX < 0 || TreeY < 0)
            {
                TreeX = screenWidth * 0.74f;
                TreeY = floorY - 6f;
            }

            if (Gifts.Count == 0)
            {
                // Add 3 charming gift boxes next to the tree
                Gifts.Add(new ChristmasGift(TreeX - 44f, floorY - 6f, Color.FromArgb(220, 38, 38), Color.FromArgb(255, 215, 0))); // Red / Gold
                Gifts.Add(new ChristmasGift(TreeX + 46f, floorY - 6f, Color.FromArgb(22, 163, 74), Color.FromArgb(254, 240, 138))); // Green / Cream
                Gifts.Add(new ChristmasGift(TreeX - 74f, floorY - 6f, Color.FromArgb(37, 99, 235), Color.FromArgb(255, 255, 255))); // Blue / White
            }
        }

        public RectangleF GetTreeBounds()
        {
            return new RectangleF(TreeX - TreeWidth * 0.5f, TreeY - TreeHeight, TreeWidth, TreeHeight);
        }

        public object HitTest(float mouseX, float mouseY)
        {
            // 1. Check Gifts first (topmost first)
            if (ShowGifts)
            {
                for (int i = Gifts.Count - 1; i >= 0; i--)
                {
                    ChristmasGift g = Gifts[i];
                    RectangleF b = g.GetBounds();
                    b.Inflate(8f, 8f);
                    if (b.Contains(mouseX, mouseY))
                    {
                        return g;
                    }
                }
            }

            // 2. Check Christmas Tree
            if (ShowTree)
            {
                RectangleF tb = GetTreeBounds();
                tb.Inflate(6f, 6f);
                if (tb.Contains(mouseX, mouseY))
                {
                    return "tree";
                }
            }

            return null;
        }

        public void StartDrag(object hit, float mouseX, float mouseY)
        {
            _grabbedItem = hit;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;

            if (hit is ChristmasGift)
            {
                ChristmasGift g = (ChristmasGift)hit;
                g.IsGrabbed = true;
                g.IsFalling = false;
                g.AttachedWindow = IntPtr.Zero;
                g.GrabOffsetX = mouseX - g.X;
                g.GrabOffsetY = mouseY - g.Y;
                g.Vx = 0f;
                g.Vy = 0f;
            }
            else if ("tree".Equals(hit))
            {
                TreeIsGrabbed = true;
                TreeAttachedWindow = IntPtr.Zero;
                TreeGrabOffsetX = mouseX - TreeX;
                TreeGrabOffsetY = mouseY - TreeY;
            }
        }

        public void OnMouseMove(float mouseX, float mouseY, WindowManager winMgr, int screenWidth, int floorY, Action<float, float, int> onSnowPuff)
        {
            float dx = (_prevMouseX >= 0) ? (mouseX - _prevMouseX) : 0f;
            float dy = (_prevMouseY >= 0) ? (mouseY - _prevMouseY) : 0f;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;

            if (_grabbedItem is ChristmasGift)
            {
                ChristmasGift g = (ChristmasGift)_grabbedItem;
                g.X = mouseX - g.GrabOffsetX;
                g.Y = mouseY - g.GrabOffsetY;
                g.Vx = dx * 0.85f;
                g.Vy = dy * 0.85f;

                if (Math.Abs(dx) > 4f && onSnowPuff != null && _rand.Next(4) == 0)
                {
                    onSnowPuff(g.X, g.Y, 2);
                }
            }
            else if ("tree".Equals(_grabbedItem))
            {
                TreeX = mouseX - TreeGrabOffsetX;
                TreeY = mouseY - TreeGrabOffsetY;

                if (Math.Abs(dx) > 4f && onSnowPuff != null && _rand.Next(3) == 0)
                {
                    onSnowPuff(TreeX, TreeY, 3);
                }
            }
        }

        public void OnMouseUp(float mouseX, float mouseY, WindowManager winMgr, int floorY, Action<float, float, int> onSnowPuff)
        {
            if (_grabbedItem is ChristmasGift)
            {
                ChristmasGift g = (ChristmasGift)_grabbedItem;
                g.IsGrabbed = false;
                bool placed = false;

                // Check landing on floating window ledge
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

                // Check landing on Taskbar snow
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
            }
            else if ("tree".Equals(_grabbedItem))
            {
                TreeIsGrabbed = false;
                bool placed = false;

                // Check landing on floating window ledge
                if (winMgr != null)
                {
                    List<WindowInfo> windows = winMgr.ActiveWindows;
                    for (int w = 0; w < windows.Count; w++)
                    {
                        WindowInfo win = windows[w];
                        Rectangle r = win.Bounds;
                        if (TreeX >= r.Left && TreeX <= r.Right &&
                            TreeY >= r.Top - 45f && TreeY <= r.Top + 25f)
                        {
                            TreeY = r.Top;
                            TreeAttachedWindow = win.Handle;
                            TreeWindowOffsetX = TreeX - r.Left;
                            TreeWindowOffsetY = r.Top - TreeY;
                            placed = true;
                            if (onSnowPuff != null) onSnowPuff(TreeX, TreeY, 8);
                            break;
                        }
                    }
                }

                // Check landing on Taskbar
                if (!placed && TreeY >= floorY - 70f)
                {
                    TreeY = floorY - 6f;
                    TreeAttachedWindow = IntPtr.Zero;
                    placed = true;
                    if (onSnowPuff != null) onSnowPuff(TreeX, TreeY, 8);
                }

                if (!placed)
                {
                    // Snap safely to floor if released in the air
                    TreeY = floorY - 6f;
                    TreeAttachedWindow = IntPtr.Zero;
                }

                SaveSettings();
            }

            _grabbedItem = null;
        }

        public void AddGift(float mouseX, float mouseY, int floorY)
        {
            Color[] boxColors = new Color[]
            {
                Color.FromArgb(220, 38, 38),   // Red
                Color.FromArgb(22, 163, 74),   // Green
                Color.FromArgb(37, 99, 235),   // Blue
                Color.FromArgb(217, 119, 6),   // Amber Gold
                Color.FromArgb(147, 51, 234)   // Purple
            };

            Color[] ribColors = new Color[]
            {
                Color.FromArgb(255, 215, 0),   // Gold
                Color.FromArgb(255, 255, 255), // White
                Color.FromArgb(239, 68, 68)    // Red
            };

            int idx = Gifts.Count % boxColors.Length;
            Color bCol = boxColors[idx];
            Color rCol = ribColors[idx % ribColors.Length];

            float gx = (mouseX > 50 && mouseX < 1850) ? mouseX : TreeX + (float)(_rand.NextDouble() * 80 - 40);
            float gy = (mouseY > 100 && mouseY < floorY) ? mouseY : floorY - 6f;

            Gifts.Add(new ChristmasGift(gx, gy, bCol, rCol));
        }

        public void RemoveGift(ChristmasGift g)
        {
            if (g != null && Gifts.Contains(g))
            {
                Gifts.Remove(g);
            }
        }

        public void ResetPositions(int screenWidth, int floorY)
        {
            TreeX = screenWidth * 0.74f;
            TreeY = floorY - 6f;
            TreeAttachedWindow = IntPtr.Zero;

            Gifts.Clear();
            Gifts.Add(new ChristmasGift(TreeX - 44f, floorY - 6f, Color.FromArgb(220, 38, 38), Color.FromArgb(255, 215, 0)));
            Gifts.Add(new ChristmasGift(TreeX + 46f, floorY - 6f, Color.FromArgb(22, 163, 74), Color.FromArgb(254, 240, 138)));
            Gifts.Add(new ChristmasGift(TreeX - 74f, floorY - 6f, Color.FromArgb(37, 99, 235), Color.FromArgb(255, 255, 255)));

            SaveSettings();
        }

        public void Update(WindowManager winMgr, int screenWidth, int screenHeight, int floorY, Action<float, float, int> onSnowPuff)
        {
            _animTick += 0.045f;
            InitDefaultsIfNeeded(screenWidth, floorY);

            // 1. Update Christmas Tree Window Attachment
            if (!TreeIsGrabbed && TreeAttachedWindow != IntPtr.Zero && winMgr != null)
            {
                Rectangle curBounds;
                bool hasMoved;
                if (winMgr.TryGetWindowCurrentBounds(TreeAttachedWindow, out curBounds, out hasMoved))
                {
                    if (TreeX < curBounds.Left - 10f || TreeX > curBounds.Right + 10f)
                    {
                        // Tree fell off window ledge
                        TreeAttachedWindow = IntPtr.Zero;
                        TreeY = floorY - 6f;
                        if (onSnowPuff != null) onSnowPuff(TreeX, TreeY, 8);
                    }
                    else
                    {
                        TreeX = curBounds.Left + TreeWindowOffsetX;
                        TreeY = curBounds.Top - TreeWindowOffsetY;
                    }
                }
                else
                {
                    TreeAttachedWindow = IntPtr.Zero;
                    TreeY = floorY - 6f;
                }
            }

            // 2. Update Gifts Physics
            if (ShowGifts)
            {
                for (int i = Gifts.Count - 1; i >= 0; i--)
                {
                    ChristmasGift g = Gifts[i];
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
        }

        public void Draw(Graphics g, WindowManager winMgr, int screenWidth, int screenHeight, int floorY)
        {
            // 1. Draw Christmas String Lights
            if (ShowLights)
            {
                DrawChristmasStringLights(g, winMgr, screenWidth, floorY);
            }

            // 2. Draw Christmas Tree
            if (ShowTree)
            {
                DrawChristmasTree(g);
            }

            // 3. Draw Gifts
            if (ShowGifts)
            {
                for (int i = 0; i < Gifts.Count; i++)
                {
                    Gifts[i].Draw(g);
                }
            }
        }

        private void DrawChristmasTree(Graphics g)
        {
            float cx = TreeX;
            float cy = TreeY;
            float w = TreeWidth;
            float h = TreeHeight;

            // Contact shadow
            float shadowW = w * 0.95f;
            float shadowH = 14f;
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(60, 15, 30, 45)))
            {
                g.FillEllipse(shadowBrush, cx - shadowW * 0.5f, cy - shadowH * 0.5f, shadowW, shadowH);
            }

            // 1. Wooden Trunk
            float trunkW = 18f;
            float trunkH = 26f;
            RectangleF trunkRect = new RectangleF(cx - trunkW * 0.5f, cy - trunkH, trunkW, trunkH);
            using (GraphicsPath tp = VectorIcons.CreateRoundedRect(trunkRect, 3f))
            using (LinearGradientBrush trunkGrad = new LinearGradientBrush(
                new PointF(trunkRect.Left, trunkRect.Top),
                new PointF(trunkRect.Right, trunkRect.Top),
                Color.FromArgb(255, 120, 74, 42),
                Color.FromArgb(255, 78, 46, 22)))
            {
                g.FillPath(trunkGrad, tp);
            }

            // 2. Three Cascading Tiers of Pine Foliage
            // Tier 3 (Bottom):
            DrawPineTier(g, cx, cy - 20f, w * 0.96f, 44f, 0);
            // Tier 2 (Middle):
            DrawPineTier(g, cx, cy - 54f, w * 0.76f, 38f, 1);
            // Tier 1 (Top):
            DrawPineTier(g, cx, cy - 84f, w * 0.54f, 36f, 2);

            // 3. Glossy Colorful Baubles
            PointF[] baublePos = new PointF[]
            {
                new PointF(cx - 28f, cy - 32f),
                new PointF(cx + 26f, cy - 35f),
                new PointF(cx - 8f,  cy - 40f),
                new PointF(cx + 36f, cy - 38f),
                new PointF(cx - 22f, cy - 64f),
                new PointF(cx + 18f, cy - 66f),
                new PointF(cx - 4f,  cy - 72f),
                new PointF(cx + 24f, cy - 70f),
                new PointF(cx - 10f, cy - 95f),
                new PointF(cx + 11f, cy - 97f)
            };

            Color[] baubleColors = new Color[]
            {
                Color.FromArgb(220, 38, 38),   // Crimson Red
                Color.FromArgb(250, 204, 21),  // Shiny Gold
                Color.FromArgb(14, 165, 233),  // Sky Blue
                Color.FromArgb(236, 72, 153),  // Pink
                Color.FromArgb(250, 204, 21),  // Shiny Gold
                Color.FromArgb(220, 38, 38),   // Crimson Red
                Color.FromArgb(224, 242, 254), // Ice White
                Color.FromArgb(236, 72, 153),  // Pink
                Color.FromArgb(220, 38, 38),   // Crimson Red
                Color.FromArgb(250, 204, 21)   // Shiny Gold
            };

            for (int b = 0; b < baublePos.Length; b++)
            {
                float bx = baublePos[b].X;
                float by = baublePos[b].Y;
                float br = 5.2f;
                Color bCol = baubleColors[b % baubleColors.Length];

                // Cap hanging wire
                using (Pen capPen = new Pen(Color.FromArgb(120, 60, 40, 10), 1f))
                {
                    g.DrawLine(capPen, bx, by - br - 3f, bx, by - br);
                }

                // Bauble 3D Sphere
                using (GraphicsPath bp = new GraphicsPath())
                {
                    bp.AddEllipse(bx - br, by - br, br * 2f, br * 2f);
                    Color darkB = Color.FromArgb(
                        (int)(bCol.R * 0.65f),
                        (int)(bCol.G * 0.65f),
                        (int)(bCol.B * 0.65f));

                    using (LinearGradientBrush bGrad = new LinearGradientBrush(
                        new PointF(bx - br * 0.5f, by - br * 0.5f),
                        new PointF(bx + br, by + br),
                        bCol, darkB))
                    {
                        g.FillPath(bGrad, bp);
                    }
                }

                // Specular Glint
                using (SolidBrush glint = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
                {
                    g.FillEllipse(glint, bx - br * 0.45f, by - br * 0.45f, br * 0.65f, br * 0.65f);
                }
            }

            // 4. Twinkling Tree Lights
            PointF[] lightPos = new PointF[]
            {
                new PointF(cx - 16f, cy - 30f),
                new PointF(cx + 8f,  cy - 34f),
                new PointF(cx - 36f, cy - 28f),
                new PointF(cx + 34f, cy - 29f),
                new PointF(cx - 12f, cy - 60f),
                new PointF(cx + 8f,  cy - 62f),
                new PointF(cx - 24f, cy - 58f),
                new PointF(cx + 25f, cy - 59f),
                new PointF(cx - 4f,  cy - 88f),
                new PointF(cx + 5f,  cy - 92f),
                new PointF(cx - 14f, cy - 87f),
                new PointF(cx + 15f, cy - 89f)
            };

            for (int l = 0; l < lightPos.Length; l++)
            {
                float lx = lightPos[l].X;
                float ly = lightPos[l].Y;
                float pulse = (float)(0.60 + 0.40 * Math.Sin(_animTick * 3.5f + l * 0.85f));
                int alpha = (int)(255 * pulse);

                Color lCol = (l % 3 == 0) ? Color.FromArgb(255, 235, 59) :
                             (l % 3 == 1) ? Color.FromArgb(255, 255, 255) :
                                            Color.FromArgb(56, 189, 248);

                // Glow aura
                using (SolidBrush glow = new SolidBrush(Color.FromArgb((int)(75 * pulse), lCol)))
                {
                    g.FillEllipse(glow, lx - 5f, ly - 5f, 10f, 10f);
                }
                // Center point
                using (SolidBrush core = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                {
                    g.FillEllipse(core, lx - 1.8f, ly - 1.8f, 3.6f, 3.6f);
                }
            }

            // 5. Radiant Golden Star ⭐ on Tree Peak
            float starCx = cx;
            float starCy = cy - 120f;
            float starPulse = (float)(0.80 + 0.20 * Math.Sin(_animTick * 2.8f));

            // Star outer glow halo
            using (SolidBrush starGlow = new SolidBrush(Color.FromArgb((int)(65 * starPulse), 255, 215, 0)))
            {
                g.FillEllipse(starGlow, starCx - 22f, starCy - 22f, 44f, 44f);
            }

            // Star 5-pointed polygon
            DrawStarPolygon(g, starCx, starCy, 14f, 6.2f, Color.FromArgb(255, 215, 0), Color.FromArgb(245, 158, 11));
        }

        private void DrawPineTier(Graphics g, float cx, float cy, float tierW, float tierH, int tierIndex)
        {
            float halfW = tierW * 0.5f;
            float topY = cy - tierH;
            float botY = cy;

            // Scalloped pine foliage polygon
            PointF[] pts = new PointF[]
            {
                new PointF(cx, topY),
                new PointF(cx + halfW * 0.35f, topY + tierH * 0.35f),
                new PointF(cx + halfW * 0.28f, topY + tierH * 0.40f),
                new PointF(cx + halfW * 0.68f, topY + tierH * 0.72f),
                new PointF(cx + halfW * 0.58f, topY + tierH * 0.76f),
                new PointF(cx + halfW, botY),
                new PointF(cx + halfW * 0.65f, botY - 5f),
                new PointF(cx + halfW * 0.35f, botY - 3f),
                new PointF(cx, botY - 6f),
                new PointF(cx - halfW * 0.35f, botY - 3f),
                new PointF(cx - halfW * 0.65f, botY - 5f),
                new PointF(cx - halfW, botY),
                new PointF(cx - halfW * 0.58f, topY + tierH * 0.76f),
                new PointF(cx - halfW * 0.68f, topY + tierH * 0.72f),
                new PointF(cx - halfW * 0.28f, topY + tierH * 0.40f),
                new PointF(cx - halfW * 0.35f, topY + tierH * 0.35f)
            };

            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(pts);

                Color cTop = Color.FromArgb(255, 46, 138, 70);
                Color cBot = Color.FromArgb(255, 20, 76, 38);

                using (LinearGradientBrush grad = new LinearGradientBrush(
                    new PointF(cx, topY),
                    new PointF(cx, botY),
                    cTop, cBot))
                {
                    g.FillPath(grad, gp);
                }

                using (Pen rimPen = new Pen(Color.FromArgb(90, 15, 60, 30), 1f))
                {
                    g.DrawPath(rimPen, gp);
                }
            }

            // Fluffy white snow caps on branch shoulders
            using (SolidBrush snowBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
            {
                // Left shoulder snow
                g.FillEllipse(snowBrush, cx - halfW * 0.72f, topY + tierH * 0.68f - 3f, halfW * 0.52f, 6.5f);
                // Right shoulder snow
                g.FillEllipse(snowBrush, cx + halfW * 0.20f, topY + tierH * 0.68f - 3f, halfW * 0.52f, 6.5f);
            }
        }

        private void DrawChristmasStringLights(Graphics g, WindowManager winMgr, int screenWidth, int floorY)
        {
            Color[] bulbPalette = new Color[]
            {
                Color.FromArgb(239, 68, 68),   // Red
                Color.FromArgb(245, 158, 11),  // Amber Gold
                Color.FromArgb(16, 185, 129),  // Emerald Green
                Color.FromArgb(6, 182, 212),   // Cyan Blue
                Color.FromArgb(236, 72, 153),  // Pink
                Color.FromArgb(254, 240, 138)  // Warm White
            };

            // 1. Draw lights on active floating windows
            if (winMgr != null)
            {
                List<WindowInfo> windows = winMgr.ActiveWindows;
                for (int w = 0; w < windows.Count; w++)
                {
                    WindowInfo win = windows[w];
                    Rectangle r = win.Bounds;
                    if (r.Width < 140 || r.Top < -20 || r.Top > floorY + 50) continue;

                    DrawLightsSwag(g, r.Left + 8f, r.Top + 6f, r.Right - 8f, r.Top + 6f, 18f, bulbPalette, true);
                }
            }

            // 2. Draw lights along Taskbar snow
            int segCount = Math.Max(4, screenWidth / 240);
            float segW = (float)screenWidth / segCount;
            for (int s = 0; s < segCount; s++)
            {
                float x1 = s * segW;
                float x2 = (s + 1) * segW;
                float yBase = floorY - 6f;
                DrawLightsSwag(g, x1, yBase, x2, yBase, 16f, bulbPalette, false);
            }
        }

        private void DrawLightsSwag(Graphics g, float x1, float y1, float x2, float y2, float sagDepth, Color[] palette, bool isWindow)
        {
            float midX = (x1 + x2) * 0.5f;
            float midY = (y1 + y2) * 0.5f + sagDepth;

            // Draw Wire
            using (GraphicsPath wirePath = new GraphicsPath())
            {
                wirePath.AddBezier(
                    new PointF(x1, y1),
                    new PointF(x1 + (midX - x1) * 0.5f, midY),
                    new PointF(midX + (x2 - midX) * 0.5f, midY),
                    new PointF(x2, y2));

                using (Pen wirePen = new Pen(Color.FromArgb(180, 30, 60, 40), 1.6f))
                {
                    g.DrawPath(wirePen, wirePath);
                }
            }

            // Bulbs along curve
            int bulbCount = Math.Max(3, (int)((x2 - x1) / 32f));
            for (int b = 1; b < bulbCount; b++)
            {
                float t = (float)b / bulbCount;
                // Bezier interpolation
                float u = 1f - t;
                float bx = u * u * u * x1 + 3f * u * u * t * (x1 + (midX - x1) * 0.5f) + 3f * u * t * t * (midX + (x2 - midX) * 0.5f) + t * t * t * x2;
                float by = u * u * u * y1 + 3f * u * u * t * midY + 3f * u * t * t * midY + t * t * t * y2;

                Color bulbCol = palette[(b + (int)(x1 * 0.1f)) % palette.Length];
                float pulse = (float)(0.70 + 0.30 * Math.Sin(_animTick * 3.2f + b * 1.25f + x1 * 0.05f));
                float r = 4.8f;

                // Dark socket cap
                using (SolidBrush capBrush = new SolidBrush(Color.FromArgb(200, 35, 45, 40)))
                {
                    g.FillRectangle(capBrush, bx - 1.5f, by - 2.5f, 3f, 3f);
                }

                // Bulb glow halo
                using (SolidBrush halo = new SolidBrush(Color.FromArgb((int)(60 * pulse), bulbCol)))
                {
                    g.FillEllipse(halo, bx - r * 1.6f, by - r * 0.5f, r * 3.2f, r * 3.2f);
                }

                // Teardrop glass bulb
                using (SolidBrush bulbBrush = new SolidBrush(Color.FromArgb((int)(240 * pulse), bulbCol)))
                {
                    g.FillEllipse(bulbBrush, bx - r, by, r * 2f, r * 2.3f);
                }

                // Center bright sparkle
                using (SolidBrush glint = new SolidBrush(Color.FromArgb((int)(220 * pulse), 255, 255, 255)))
                {
                    g.FillEllipse(glint, bx - r * 0.4f, by + r * 0.35f, r * 0.7f, r * 0.7f);
                }
            }

            // Star in the middle of loop
            if (ShowStarLights)
            {
                float starPulse = (float)(0.75 + 0.25 * Math.Sin(_animTick * 2.5f + x1 * 0.02f));
                using (SolidBrush starGlow = new SolidBrush(Color.FromArgb((int)(55 * starPulse), 255, 215, 0)))
                {
                    g.FillEllipse(starGlow, midX - 16f, midY + 6f - 16f, 32f, 32f);
                }
                DrawStarPolygon(g, midX, midY + 6f, 8.5f, 3.8f, Color.FromArgb(255, 215, 0), Color.FromArgb(245, 158, 11));
            }
        }

        private static void DrawStarPolygon(Graphics g, float cx, float cy, float outerR, float innerR, Color cTop, Color cBot)
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

        public void SaveSettings()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("tree=" + ShowTree.ToString().ToLower());
                lines.Add("gifts=" + ShowGifts.ToString().ToLower());
                lines.Add("lights=" + ShowLights.ToString().ToLower());
                lines.Add("stars=" + ShowStarLights.ToString().ToLower());
                lines.Add("santahat=" + ShowSantaHat.ToString().ToLower());
                lines.Add("treex=" + TreeX.ToString("F1"));
                lines.Add("treey=" + TreeY.ToString("F1"));
                File.WriteAllLines(_settingsPath, lines.ToArray());
            }
            catch { }
        }

        public void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;
                string[] lines = File.ReadAllLines(_settingsPath);
                foreach (string l in lines)
                {
                    if (string.IsNullOrEmpty(l)) continue;
                    string[] parts = l.Split('=');
                    if (parts.Length != 2) continue;
                    string k = parts[0].Trim().ToLower();
                    string v = parts[1].Trim().ToLower();

                    switch (k)
                    {
                        case "tree": ShowTree = (v == "true"); break;
                        case "gifts": ShowGifts = (v == "true"); break;
                        case "lights": ShowLights = (v == "true"); break;
                        case "stars": ShowStarLights = (v == "true"); break;
                        case "santahat": ShowSantaHat = (v == "true"); break;
                        case "treex":
                            float tx;
                            if (float.TryParse(v, out tx) && tx > 0) TreeX = tx;
                            break;
                        case "treey":
                            float ty;
                            if (float.TryParse(v, out ty) && ty > 0) TreeY = ty;
                            break;
                    }
                }
            }
            catch { }
        }
    }
}
