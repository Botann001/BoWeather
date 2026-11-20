using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BoWeather.Particles;

namespace BoWeather
{
    public class ChristmasDecorForm : Form
    {
        private readonly ChristmasManager _mgr;
        private readonly OverlayForm _overlay;
        private readonly bool _isDarkMode;

        // Form Layout
        private Rectangle _headerIconRect;
        private Rectangle _headerTextRect;
        private Rectangle _closeBtnRect;
        private Rectangle _heroCardRect;
        private Rectangle _btnPlantRect;

        // Grid of 16 Tiles (4x4)
        private const int COLS = 4;
        private const int ROWS = 4;
        private const int TOTAL_TILES = 16;
        private Rectangle[] _tileRects = new Rectangle[TOTAL_TILES];

        private readonly string[] _tileIds = new string[]
        {
            "lights", "candle", "redbauble", "goldbauble",
            "star", "wreath", "stocking", "candycane",
            "gift", "bells", "gingerbread", "holly",
            "santahat", "snowglobe", "cocoa", "boat"
        };

        private readonly string[] _tileLabels = new string[]
        {
            "Lights", "Candle", "Red bauble", "Gold bauble",
            "Star", "Wreath", "Stocking", "Candy cane",
            "Gift", "Bells", "Gingerbread", "Holly",
            "Santa hat", "Snow globe", "Cocoa", "Boat"
        };

        // Gift color cycler
        private int _giftColorIdx = 0;
        private readonly Color[] _giftColors = new Color[]
        {
            Color.FromArgb(220, 38, 38),   // Red
            Color.FromArgb(22, 163, 74),   // Green
            Color.FromArgb(37, 99, 235),   // Blue
            Color.FromArgb(217, 119, 6)    // Gold
        };
        private readonly Color[] _ribbonColors = new Color[]
        {
            Color.FromArgb(255, 215, 0),
            Color.FromArgb(254, 240, 138),
            Color.FromArgb(255, 255, 255),
            Color.FromArgb(239, 68, 68)
        };

        // Footer
        private Rectangle _footerRect;
        private Rectangle _behindToggleRect;
        private Rectangle _btnTakeAllDownRect;

        // Interaction state
        private int _hoveredTile = -1;
        private bool _hoverHero = false;
        private bool _hoverClose = false;
        private bool _hoverBehind = false;
        private bool _hoverTakeAll = false;
        private bool _isDraggingForm = false;
        private Point _dragStartCursor;
        private Point _dragStartFormPos;

        // Fonts
        private readonly Font _headerTitleFont;
        private readonly Font _headerSubFont;
        private readonly Font _heroTitleFont;
        private readonly Font _heroSubFont;
        private readonly Font _tileLabelFont;
        private readonly Font _footerCountFont;
        private readonly Font _btnFont;

        public ChristmasDecorForm(ChristmasManager mgr, OverlayForm overlay, bool isDarkMode)
        {
            _mgr = mgr;
            _overlay = overlay;
            _isDarkMode = isDarkMode;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;

            Size = new Size(440, 640);
            BackColor = Color.FromArgb(18, 22, 34);
            Opacity = 0.95;

            // Clip exact rounded corners so zero white pixels peek through
            UpdateRegion();

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Left + (screen.Width - Width) / 2, screen.Top + (screen.Height - Height) / 2);

            _headerTitleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
            _headerSubFont = new Font("Segoe UI", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
            _heroTitleFont = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            _heroSubFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
            _tileLabelFont = new Font("Segoe UI", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
            _footerCountFont = new Font("Segoe UI", 12.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            _btnFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);

            CalculateLayout();
        }

        private void UpdateRegion()
        {
            try
            {
                using (GraphicsPath path = VectorIcons.CreateRoundedRect(new RectangleF(0, 0, Width, Height), 24f))
                {
                    Region = new Region(path);
                }
            }
            catch { }
        }

        private void CalculateLayout()
        {
            int w = ClientSize.Width;
            int pad = 16;

            // 1. Top Header
            _headerIconRect = new Rectangle(pad + 2, 14, 38, 44);
            _closeBtnRect = new Rectangle(w - pad - 26, 16, 26, 26);
            _headerTextRect = new Rectangle(_headerIconRect.Right + 8, 12, _closeBtnRect.Left - _headerIconRect.Right - 12, 46);

            // 2. Hero Card: Christmas Tree
            int heroY = 66;
            _heroCardRect = new Rectangle(pad, heroY, w - pad * 2, 78);
            int btnW = 86;
            int btnH = 34;
            _btnPlantRect = new Rectangle(_heroCardRect.Right - btnW - 12, _heroCardRect.Y + (_heroCardRect.Height - btnH) / 2, btnW, btnH);

            // 3. Grid of 16 Tiles (4x4)
            int gridY = _heroCardRect.Bottom + 12;
            int tileW = 94;
            int tileH = 84;
            int totalGridW = w - pad * 2;
            int gapX = (totalGridW - COLS * tileW) / (COLS - 1);
            int gapY = 8;

            for (int r = 0; r < ROWS; r++)
            {
                for (int c = 0; c < COLS; c++)
                {
                    int idx = r * COLS + c;
                    int tx = pad + c * (tileW + gapX);
                    int ty = gridY + r * (tileH + gapY);
                    _tileRects[idx] = new Rectangle(tx, ty, tileW, tileH);
                }
            }

            // 4. Footer Bar
            int footerY = ClientSize.Height - 48;
            _footerRect = new Rectangle(pad, footerY, w - pad * 2, 36);
            _behindToggleRect = new Rectangle(_footerRect.X + 106, footerY + 4, 142, 28);
            int takeBtnW = 132;
            _btnTakeAllDownRect = new Rectangle(_footerRect.Right - takeBtnW, footerY + 2, takeBtnW, 32);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // 1. Frosted Glass Body Gradient (Deep blue-gray frosted look matching photo 2)
            using (GraphicsPath formPath = VectorIcons.CreateRoundedRect(new RectangleF(0.5f, 0.5f, w - 1f, h - 1f), 24f))
            {
                using (LinearGradientBrush bgBrush = new LinearGradientBrush(
                    new PointF(0, 0), new PointF(0, h),
                    Color.FromArgb(250, 20, 26, 42),
                    Color.FromArgb(252, 12, 16, 26)))
                {
                    g.FillPath(bgBrush, formPath);
                }

                // Ambient frosted snowflake watermarks in glass
                DrawAmbientSnowflakes(g, w, h);

                // Top Specular Highlight sheen
                using (LinearGradientBrush topSheen = new LinearGradientBrush(
                    new PointF(0, 0), new PointF(0, 75),
                    Color.FromArgb(45, 255, 255, 255),
                    Color.Transparent))
                {
                    g.FillRectangle(topSheen, 0, 0, w, 75);
                }

                // Smooth Ice-Blue Border
                using (Pen borderPen = new Pen(Color.FromArgb(60, 130, 200, 255), 1.2f))
                {
                    g.DrawPath(borderPen, formPath);
                }
            }

            // 2. Header Area
            // Cute 3D Tree Icon on left of header
            if (!ChristmasAssets.DrawTreeHeader(g, _headerIconRect.X + 18f, _headerIconRect.Y + 22f, 32f, 38f))
            {
                DrawMiniTree(g, _headerIconRect.X + 18f, _headerIconRect.Y + 34f, 28f, 36f);
            }

            // Title: Christmas Decorations
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                g.DrawString("Christmas Decorations", _headerTitleFont, tb, _headerTextRect.X, _headerTextRect.Y + 2);
            }
            // Subtitle: Add festive decorations to make your desktop more merry and cozy!
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(195, 215, 240)))
            {
                g.DrawString("Add festive decorations to make your desktop\nmore merry and cozy!", _headerSubFont, sb, _headerTextRect.X, _headerTextRect.Y + 22);
            }

            // Close button (✕)
            using (GraphicsPath closePath = VectorIcons.CreateRoundedRect(new RectangleF(_closeBtnRect.X, _closeBtnRect.Y, _closeBtnRect.Width, _closeBtnRect.Height), 13f))
            {
                if (_hoverClose)
                {
                    using (SolidBrush ch = new SolidBrush(Color.FromArgb(60, 255, 255, 255)))
                    {
                        g.FillPath(ch, closePath);
                    }
                }
                using (Pen xp = new Pen(_hoverClose ? Color.White : Color.FromArgb(210, 225, 245), 1.8f))
                {
                    xp.StartCap = LineCap.Round;
                    xp.EndCap = LineCap.Round;
                    g.DrawLine(xp, _closeBtnRect.X + 8, _closeBtnRect.Y + 8, _closeBtnRect.Right - 8, _closeBtnRect.Bottom - 8);
                    g.DrawLine(xp, _closeBtnRect.Right - 8, _closeBtnRect.Y + 8, _closeBtnRect.X + 8, _closeBtnRect.Bottom - 8);
                }
            }

            // 3. Hero Card: Christmas Tree
            Color heroBg = _hoverHero ? Color.FromArgb(170, 36, 46, 68) : Color.FromArgb(120, 26, 34, 52);
            using (GraphicsPath hp = VectorIcons.CreateRoundedRect(_heroCardRect, 16f))
            {
                using (SolidBrush hb = new SolidBrush(heroBg)) g.FillPath(hb, hp);
                using (Pen hpen = new Pen(Color.FromArgb(45, 140, 210, 255), 1.1f)) g.DrawPath(hpen, hp);
            }

            // 3D Tree preview inside hero card
            if (!ChristmasAssets.DrawTreeHero(g, _heroCardRect.X + 40f, _heroCardRect.Y + 39f, 48f, 58f))
            {
                Draw3DTreePreview(g, _heroCardRect.X + 40f, _heroCardRect.Y + 54f, 44f, 58f);
            }

            // Tree labels
            int heroTextX = _heroCardRect.X + 76;
            using (SolidBrush tb = new SolidBrush(Color.White))
            {
                g.DrawString("Christmas tree", _heroTitleFont, tb, heroTextX, _heroCardRect.Y + 16);
            }
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(175, 200, 230)))
            {
                Rectangle textR = new Rectangle(heroTextX, _heroCardRect.Y + 38, _heroCardRect.Width - 176, 32);
                g.DrawString("A beautiful 3D Christmas tree with lights and ornaments.", _heroSubFont, sb, textR);
            }

            // + Add (1) button on hero card (Emerald Green Pill with soft glow)
            string addText = _mgr.Trees.Count > 0 ? "+ Add (" + _mgr.Trees.Count + ")" : "+ Add";
            using (GraphicsPath pbp = VectorIcons.CreateRoundedRect(_btnPlantRect, 16f))
            {
                using (LinearGradientBrush addBrush = new LinearGradientBrush(
                    new PointF(_btnPlantRect.X, _btnPlantRect.Y),
                    new PointF(_btnPlantRect.X, _btnPlantRect.Bottom),
                    Color.FromArgb(255, 34, 197, 94),
                    Color.FromArgb(255, 21, 140, 65)))
                {
                    g.FillPath(addBrush, pbp);
                }
                using (Pen addPen = new Pen(Color.FromArgb(90, 255, 255, 255), 1f))
                {
                    g.DrawPath(addPen, pbp);
                }
            }
            using (SolidBrush pbt = new SolidBrush(Color.White))
            {
                StringFormat csf = new StringFormat();
                csf.Alignment = StringAlignment.Center;
                csf.LineAlignment = StringAlignment.Center;
                g.DrawString(addText, _btnFont, pbt, _btnPlantRect, csf);
            }

            // 4. Grid of 16 Tiles (4x4)
            for (int i = 0; i < TOTAL_TILES; i++)
            {
                Rectangle tr = _tileRects[i];
                bool isHover = (_hoveredTile == i);
                string id = _tileIds[i];

                Color tileBg = isHover ? Color.FromArgb(190, 42, 54, 78) : Color.FromArgb(115, 26, 34, 50);
                Color tileBorder = isHover ? Color.FromArgb(110, 160, 235, 255) : Color.FromArgb(35, 130, 190, 240);

                using (GraphicsPath tp = VectorIcons.CreateRoundedRect(tr, 14f))
                {
                    using (SolidBrush tb = new SolidBrush(tileBg)) g.FillPath(tb, tp);
                    using (Pen tpn = new Pen(tileBorder, 1.1f)) g.DrawPath(tpn, tp);
                }

                // 3D Tile Icon in upper area
                float iconCx = tr.X + tr.Width * 0.5f;
                float iconCy = tr.Y + 33;
                if (!ChristmasAssets.DrawIcon(g, id, iconCx, iconCy, 52f, 48f))
                {
                    Draw3DIcon(g, id, iconCx, iconCy);
                }

                // Tile Label in lower area
                using (SolidBrush lb = new SolidBrush(Color.FromArgb(240, 245, 255)))
                {
                    StringFormat lsf = new StringFormat();
                    lsf.Alignment = StringAlignment.Center;
                    lsf.LineAlignment = StringAlignment.Center;
                    Rectangle lr = new Rectangle(tr.X, tr.Bottom - 24, tr.Width, 20);
                    g.DrawString(_tileLabels[i], _tileLabelFont, lb, lr, lsf);
                }
            }

            // 5. Footer Bar
            // Left: {count} of 30 up
            using (SolidBrush fb = new SolidBrush(Color.FromArgb(185, 205, 230)))
            {
                g.DrawString(_mgr.TotalItemCount + " of 30 up", _footerCountFont, fb, _footerRect.X + 4, _footerRect.Y + 8);
            }

            // Middle: Behind windows toggle
            bool isBehind = _mgr.BehindActiveWindows;
            Color togBg = _hoverBehind ? Color.FromArgb(45, 255, 255, 255) : Color.Transparent;
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(_behindToggleRect, 8f))
            {
                if (_hoverBehind)
                {
                    using (SolidBrush bgb = new SolidBrush(togBg)) g.FillPath(bgb, bp);
                }
            }
            Color checkCol = isBehind ? Color.FromArgb(34, 197, 94) : Color.FromArgb(160, 180, 205);
            using (Pen cp = new Pen(checkCol, 1.6f))
            {
                g.DrawRectangle(cp, _behindToggleRect.X + 4, _behindToggleRect.Y + 7, 13, 13);
                if (isBehind)
                {
                    g.DrawLine(cp, _behindToggleRect.X + 6, _behindToggleRect.Y + 13, _behindToggleRect.X + 9, _behindToggleRect.Y + 17);
                    g.DrawLine(cp, _behindToggleRect.X + 9, _behindToggleRect.Y + 17, _behindToggleRect.X + 15, _behindToggleRect.Y + 9);
                }
            }
            using (SolidBrush tb = new SolidBrush(isBehind ? Color.White : Color.FromArgb(185, 205, 230)))
            {
                g.DrawString("Behind windows", _tileLabelFont, tb, _behindToggleRect.X + 22, _behindToggleRect.Y + 6);
            }

            // Right: Take all down button with down arrow
            Color takeBg = _hoverTakeAll ? Color.FromArgb(70, 220, 38, 38) : Color.FromArgb(50, 40, 52, 72);
            Color takeBorder = _hoverTakeAll ? Color.FromArgb(220, 38, 38) : Color.FromArgb(50, 160, 220, 255);
            using (GraphicsPath tap = VectorIcons.CreateRoundedRect(_btnTakeAllDownRect, 16f))
            {
                using (SolidBrush tab = new SolidBrush(takeBg)) g.FillPath(tab, tap);
                using (Pen tapen = new Pen(takeBorder, 1.1f)) g.DrawPath(tapen, tap);
            }
            using (SolidBrush tat = new SolidBrush(_hoverTakeAll ? Color.FromArgb(255, 100, 100) : Color.White))
            {
                StringFormat csf = new StringFormat();
                csf.Alignment = StringAlignment.Center;
                csf.LineAlignment = StringAlignment.Center;
                g.DrawString("\u2193 Take all down", _btnFont, tat, _btnTakeAllDownRect, csf);
            }
        }

        private void DrawAmbientSnowflakes(Graphics g, int w, int h)
        {
            // Subtle frosted snowflake shapes in the glass background
            using (Pen sfPen = new Pen(Color.FromArgb(20, 180, 225, 255), 1.2f))
            {
                DrawSnowflake(g, sfPen, w * 0.12f, h * 0.18f, 10f);
                DrawSnowflake(g, sfPen, w * 0.88f, h * 0.15f, 12f);
                DrawSnowflake(g, sfPen, w * 0.08f, h * 0.52f, 8f);
                DrawSnowflake(g, sfPen, w * 0.92f, h * 0.65f, 11f);
                DrawSnowflake(g, sfPen, w * 0.78f, h * 0.88f, 10f);
            }
        }

        private void DrawSnowflake(Graphics g, Pen pen, float cx, float cy, float r)
        {
            for (int i = 0; i < 3; i++)
            {
                double angle = i * Math.PI / 3.0;
                float dx = (float)(Math.Cos(angle) * r);
                float dy = (float)(Math.Sin(angle) * r);
                g.DrawLine(pen, cx - dx, cy - dy, cx + dx, cy + dy);
            }
        }

        private void DrawMiniTree(Graphics g, float cx, float cy, float tw, float th)
        {
            // Compact 3D Tree for Header
            using (SolidBrush trunkB = new SolidBrush(Color.FromArgb(255, 165, 95, 40)))
            {
                g.FillRectangle(trunkB, cx - 3.5f, cy - 6f, 7f, 7f);
            }
            DrawTierPoly(g, cx, cy - 4f, tw * 0.95f, th * 0.38f);
            DrawTierPoly(g, cx, cy - 13f, tw * 0.74f, th * 0.35f);
            DrawTierPoly(g, cx, cy - 22f, tw * 0.52f, th * 0.35f);

            // Glowing Golden Star on top
            ChristmasManager.DrawStarPolygon(g, cx, cy - 28f, 6.5f, 3.2f, Color.FromArgb(255, 255, 240, 100), Color.FromArgb(255, 245, 175, 20));
        }

        private void Draw3DTreePreview(Graphics g, float cx, float cy, float tw, float th)
        {
            // 3D Tree preview inside Hero Card matching photo 2
            float halfW = tw * 0.5f;

            // Trunk with texture
            using (SolidBrush trunkB = new SolidBrush(Color.FromArgb(255, 160, 90, 40)))
            {
                g.FillRectangle(trunkB, cx - 4.5f, cy - 8f, 9f, 9f);
            }

            // 3 lush tiers with volumetric green gradients and snow pillows
            DrawTierWithSnow(g, cx, cy - 6f, tw * 0.98f, th * 0.38f);
            DrawTierWithSnow(g, cx, cy - 17f, tw * 0.76f, th * 0.35f);
            DrawTierWithSnow(g, cx, cy - 27f, tw * 0.54f, th * 0.35f);

            // Shiny baubles
            using (SolidBrush rB = new SolidBrush(Color.FromArgb(245, 38, 52))) g.FillEllipse(rB, cx - 11f, cy - 14f, 5.5f, 5.5f);
            using (SolidBrush gB = new SolidBrush(Color.FromArgb(255, 205, 25))) g.FillEllipse(gB, cx + 8f, cy - 16f, 5.5f, 5.5f);
            using (SolidBrush bB = new SolidBrush(Color.FromArgb(56, 189, 248))) g.FillEllipse(bB, cx - 6f, cy - 23f, 5f, 5f);
            using (SolidBrush pB = new SolidBrush(Color.FromArgb(244, 114, 182))) g.FillEllipse(pB, cx + 6f, cy - 25f, 5f, 5f);

            // Fairy light garland glow
            using (Pen garland = new Pen(Color.FromArgb(200, 254, 240, 138), 1.5f))
            {
                g.DrawArc(garland, cx - 14f, cy - 18f, 28f, 10f, 15, 150);
                g.DrawArc(garland, cx - 10f, cy - 28f, 20f, 8f, 15, 150);
            }

            // Top Star with soft golden halo
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(90, 255, 215, 0)))
            {
                g.FillEllipse(glow, cx - 11f, cy - 43f, 22f, 22f);
            }
            ChristmasManager.DrawStarPolygon(g, cx, cy - 32f, 8.5f, 4f, Color.FromArgb(255, 255, 245, 120), Color.FromArgb(255, 240, 170, 20));
        }

        private void DrawTierWithSnow(Graphics g, float cx, float cy, float w, float h)
        {
            float halfW = w * 0.5f;
            PointF top = new PointF(cx, cy - h);
            PointF left = new PointF(cx - halfW, cy);
            PointF right = new PointF(cx + halfW, cy);

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLine(top, right);
                path.AddBezier(right, new PointF(cx + halfW * 0.4f, cy + 4f), new PointF(cx - halfW * 0.4f, cy + 4f), left);
                path.AddLine(left, top);

                using (LinearGradientBrush b = new LinearGradientBrush(
                    new PointF(cx - halfW, cy - h), new PointF(cx + halfW, cy),
                    Color.FromArgb(255, 34, 160, 68), Color.FromArgb(255, 16, 110, 45)))
                {
                    g.FillPath(b, path);
                }
            }

            // White snow pillows on bottom lobes
            using (SolidBrush snow = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
            {
                g.FillEllipse(snow, cx - halfW * 0.85f, cy - 2.5f, halfW * 0.65f, 5.5f);
                g.FillEllipse(snow, cx - halfW * 0.25f, cy - 1f, halfW * 0.5f, 6f);
                g.FillEllipse(snow, cx + halfW * 0.25f, cy - 2.5f, halfW * 0.65f, 5.5f);
            }
        }

        private void DrawTierPoly(Graphics g, float cx, float cy, float w, float h)
        {
            float halfW = w * 0.5f;
            PointF[] pts = new PointF[] {
                new PointF(cx, cy - h),
                new PointF(cx + halfW, cy),
                new PointF(cx, cy - 2f),
                new PointF(cx - halfW, cy)
            };
            using (LinearGradientBrush b = new LinearGradientBrush(
                new PointF(cx - halfW, cy - h), new PointF(cx + halfW, cy),
                Color.FromArgb(255, 34, 160, 68), Color.FromArgb(255, 16, 110, 45)))
            {
                g.FillPolygon(b, pts);
            }
        }

        private void Draw3DIcon(Graphics g, string id, float cx, float cy)
        {
            switch (id)
            {
                case "lights":
                    // 3D Sagging Wire with glowing colorful bulbs
                    using (Pen wp = new Pen(Color.FromArgb(220, 25, 45, 30), 2.2f))
                    {
                        g.DrawArc(wp, cx - 22f, cy - 14f, 44f, 22f, 20, 140);
                    }
                    Color[] bulbPalette = new Color[] {
                        Color.FromArgb(239, 68, 68),   // Red
                        Color.FromArgb(245, 158, 11),  // Amber
                        Color.FromArgb(16, 185, 129),  // Emerald
                        Color.FromArgb(6, 182, 212),   // Cyan
                        Color.FromArgb(236, 72, 153),  // Pink
                        Color.FromArgb(168, 85, 247)   // Purple
                    };
                    for (int b = 0; b < 6; b++)
                    {
                        float t = (b + 0.5f) / 6f;
                        float bx = cx - 20f + t * 40f;
                        float by = cy + (float)Math.Sin(t * Math.PI) * 9f - 2f;
                        Color bc = bulbPalette[b];

                        // Glow halo
                        using (SolidBrush glow = new SolidBrush(Color.FromArgb(80, bc)))
                        {
                            g.FillEllipse(glow, bx - 6f, by - 6f, 12f, 12f);
                        }
                        // 3D bulb sphere
                        using (SolidBrush bulb = new SolidBrush(bc))
                        {
                            g.FillEllipse(bulb, bx - 4f, by - 4f, 8f, 8f);
                        }
                        // White specular glare
                        using (SolidBrush glare = new SolidBrush(Color.White))
                        {
                            g.FillEllipse(glare, bx - 2f, by - 2.5f, 2.5f, 2.5f);
                        }
                    }
                    break;

                case "candle":
                    // Golden saucer base with metallic rim
                    using (LinearGradientBrush sGrad = new LinearGradientBrush(
                        new PointF(cx - 12f, cy + 8f), new PointF(cx + 12f, cy + 14f),
                        Color.FromArgb(255, 245, 205, 75), Color.FromArgb(255, 180, 130, 20)))
                    {
                        g.FillEllipse(sGrad, cx - 13f, cy + 7f, 26f, 8f);
                    }
                    using (Pen sPen = new Pen(Color.FromArgb(200, 150, 100, 15), 1.2f))
                    {
                        g.DrawEllipse(sPen, cx - 13f, cy + 7f, 26f, 8f);
                    }
                    // 3D Red candle pillar
                    using (LinearGradientBrush cGrad = new LinearGradientBrush(
                        new PointF(cx - 7f, cy - 8f), new PointF(cx + 7f, cy - 8f),
                        Color.FromArgb(255, 240, 50, 65), Color.FromArgb(255, 150, 20, 30)))
                    {
                        g.FillRectangle(cGrad, cx - 6f, cy - 8f, 12f, 17f);
                    }
                    // White wax drips
                    using (SolidBrush drip = new SolidBrush(Color.FromArgb(250, 255, 245, 235)))
                    {
                        g.FillEllipse(drip, cx - 6f, cy - 10f, 12f, 5f);
                        g.FillEllipse(drip, cx - 4f, cy - 8f, 3.5f, 6f);
                    }
                    // Wick
                    using (Pen wick = new Pen(Color.FromArgb(60, 35, 25), 1.3f))
                    {
                        g.DrawLine(wick, cx, cy - 9f, cx, cy - 13f);
                    }
                    // Glowing Flame & Aura
                    using (SolidBrush glow = new SolidBrush(Color.FromArgb(90, 255, 200, 0)))
                    {
                        g.FillEllipse(glow, cx - 10f, cy - 26f, 20f, 20f);
                    }
                    using (SolidBrush flame = new SolidBrush(Color.FromArgb(255, 255, 215, 20)))
                    {
                        g.FillEllipse(flame, cx - 4.5f, cy - 24f, 9f, 13f);
                    }
                    using (SolidBrush core = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(core, cx - 2f, cy - 20f, 4f, 7f);
                    }
                    break;

                case "redbauble":
                case "goldbauble":
                    bool isRed = (id == "redbauble");
                    Color cDark = isRed ? Color.FromArgb(120, 10, 20) : Color.FromArgb(160, 100, 10);
                    Color cMid = isRed ? Color.FromArgb(235, 30, 45) : Color.FromArgb(255, 195, 20);
                    Color cLight = isRed ? Color.FromArgb(255, 120, 130) : Color.FromArgb(255, 245, 140);

                    // Ornate Gold Cap & Ring
                    using (LinearGradientBrush capB = new LinearGradientBrush(
                        new PointF(cx - 4f, cy - 18f), new PointF(cx + 4f, cy - 13f),
                        Color.FromArgb(255, 255, 230, 110), Color.FromArgb(255, 190, 140, 25)))
                    {
                        g.FillRectangle(capB, cx - 4.5f, cy - 16f, 9f, 4.5f);
                    }
                    using (Pen ringP = new Pen(Color.FromArgb(255, 230, 170, 50), 1.5f))
                    {
                        g.DrawArc(ringP, cx - 3.5f, cy - 20.5f, 7f, 6f, 180, 180);
                    }
                    // 3D Metallic Sphere
                    using (GraphicsPath ballPath = new GraphicsPath())
                    {
                        ballPath.AddEllipse(cx - 14f, cy - 12f, 28f, 28f);
                        using (PathGradientBrush pgb = new PathGradientBrush(ballPath))
                        {
                            pgb.CenterPoint = new PointF(cx - 4f, cy - 4f);
                            pgb.CenterColor = cLight;
                            pgb.SurroundColors = new Color[] { cDark };
                            g.FillPath(pgb, ballPath);
                        }
                    }
                    // Crisp Curved Specular Reflection
                    using (Pen shineP = new Pen(Color.FromArgb(230, 255, 255, 255), 2.2f))
                    {
                        shineP.StartCap = LineCap.Round;
                        shineP.EndCap = LineCap.Round;
                        g.DrawArc(shineP, cx - 11f, cy - 9f, 16f, 16f, 210, 50);
                    }
                    break;

                case "star":
                    // 3D Beveled Star with warm ambient glow
                    using (SolidBrush glow = new SolidBrush(Color.FromArgb(80, 255, 215, 0)))
                    {
                        g.FillEllipse(glow, cx - 18f, cy - 18f, 36f, 36f);
                    }
                    // Alternating 3D facets (gold light / gold shadow)
                    Draw3DStar(g, cx, cy, 17f, 7.5f);
                    break;

                case "wreath":
                    // 3D Fir Wreath with layered scalloped lobes
                    using (Pen pBase = new Pen(Color.FromArgb(255, 22, 120, 50), 7f))
                    {
                        g.DrawEllipse(pBase, cx - 12f, cy - 12f, 24f, 24f);
                    }
                    using (Pen pLight = new Pen(Color.FromArgb(255, 45, 190, 85), 3.5f))
                    {
                        g.DrawEllipse(pLight, cx - 12f, cy - 12f, 24f, 24f);
                    }
                    // Red glossy berries around ring
                    Color berryCol = Color.FromArgb(235, 35, 50);
                    PointF[] bLocs = new PointF[] {
                        new PointF(cx - 10f, cy - 10f), new PointF(cx + 8f, cy - 10f),
                        new PointF(cx - 13f, cy + 1f), new PointF(cx + 12f, cy + 1f),
                        new PointF(cx - 5f, cy + 9f), new PointF(cx + 6f, cy + 9f)
                    };
                    for (int bi = 0; bi < bLocs.Length; bi++)
                    {
                        using (SolidBrush bB = new SolidBrush(berryCol)) g.FillEllipse(bB, bLocs[bi].X - 2.5f, bLocs[bi].Y - 2.5f, 5f, 5f);
                        using (SolidBrush sB = new SolidBrush(Color.White)) g.FillEllipse(sB, bLocs[bi].X - 1.2f, bLocs[bi].Y - 1.5f, 1.8f, 1.8f);
                    }
                    // Plush 3D Red Satin Bow at bottom
                    using (SolidBrush bowB = new SolidBrush(Color.FromArgb(235, 30, 45)))
                    {
                        g.FillEllipse(bowB, cx - 8f, cy + 7f, 7f, 6f);
                        g.FillEllipse(bowB, cx + 1f, cy + 7f, 7f, 6f);
                        g.FillEllipse(bowB, cx - 2.5f, cy + 8f, 5f, 5f);
                    }
                    break;

                case "stocking":
                    // 3D Red Stocking with fluffy white fleece cuff, heel and toe
                    using (GraphicsPath sockP = new GraphicsPath())
                    {
                        sockP.AddLine(cx - 6f, cy - 10f, cx + 5f, cy - 10f);
                        sockP.AddLine(cx + 5f, cy + 2f, cx + 11f, cy + 7f);
                        sockP.AddBezier(new PointF(cx + 11f, cy + 7f), new PointF(cx + 12f, cy + 13f), new PointF(cx + 6f, cy + 14f), new PointF(cx + 2f, cy + 14f));
                        sockP.AddBezier(new PointF(cx + 2f, cy + 14f), new PointF(cx - 8f, cy + 14f), new PointF(cx - 9f, cy + 7f), new PointF(cx - 6f, cy + 1f));
                        sockP.CloseFigure();

                        using (LinearGradientBrush sGrad = new LinearGradientBrush(
                            new PointF(cx - 8f, cy), new PointF(cx + 8f, cy),
                            Color.FromArgb(255, 235, 40, 55), Color.FromArgb(255, 160, 20, 30)))
                        {
                            g.FillPath(sGrad, sockP);
                        }
                    }
                    // Fluffy white heel & toe patches
                    using (SolidBrush fluff = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
                    {
                        g.FillEllipse(fluff, cx - 8f, cy + 6f, 7f, 7f); // heel
                        g.FillEllipse(fluff, cx + 5f, cy + 7f, 7f, 7f); // toe
                        // Thick puffy cuff
                        g.FillRectangle(fluff, cx - 8f, cy - 14f, 16f, 7f);
                        g.FillEllipse(fluff, cx - 9f, cy - 14f, 6f, 7f);
                        g.FillEllipse(fluff, cx + 3f, cy - 14f, 6f, 7f);
                    }
                    break;

                case "candycane":
                    // 3D Peppermint Candy Cane with diagonal red/white stripes
                    using (Pen cp = new Pen(Color.FromArgb(235, 35, 50), 6f))
                    {
                        cp.StartCap = LineCap.Round;
                        cp.EndCap = LineCap.Round;
                        GraphicsPath ccp = new GraphicsPath();
                        ccp.AddArc(cx - 4f, cy - 14f, 13f, 13f, 180, 180);
                        ccp.AddLine(cx - 4f, cy - 8f, cx - 4f, cy + 12f);
                        g.DrawPath(cp, ccp);
                    }
                    using (Pen sp = new Pen(Color.White, 2.5f))
                    {
                        g.DrawLine(sp, cx - 7f, cy + 9f, cx - 1f, cy + 6f);
                        g.DrawLine(sp, cx - 7f, cy + 3f, cx - 1f, cy);
                        g.DrawLine(sp, cx - 7f, cy - 3f, cx - 1f, cy - 6f);
                        g.DrawLine(sp, cx - 2f, cy - 13f, cx + 2f, cy - 14f);
                        g.DrawLine(sp, cx + 6f, cy - 12f, cx + 9f, cy - 9f);
                    }
                    // Green ribbon bow accent
                    using (SolidBrush gRib = new SolidBrush(Color.FromArgb(34, 160, 68)))
                    {
                        g.FillEllipse(gRib, cx - 8f, cy - 7f, 8f, 5f);
                    }
                    break;

                case "gift":
                    // 3D Volumetric Red Gift Box
                    PointF topC = new PointF(cx, cy - 14f);
                    PointF topL = new PointF(cx - 13f, cy - 8f);
                    PointF topR = new PointF(cx + 13f, cy - 8f);
                    PointF topB = new PointF(cx, cy - 2f);

                    PointF botL = new PointF(cx - 13f, cy + 10f);
                    PointF botC = new PointF(cx, cy + 16f);
                    PointF botR = new PointF(cx + 13f, cy + 10f);

                    // Top Face
                    using (SolidBrush tf = new SolidBrush(Color.FromArgb(250, 60, 75)))
                    {
                        g.FillPolygon(tf, new PointF[] { topC, topR, topB, topL });
                    }
                    // Left Face
                    using (SolidBrush lf = new SolidBrush(Color.FromArgb(220, 35, 45)))
                    {
                        g.FillPolygon(lf, new PointF[] { topL, topB, botC, botL });
                    }
                    // Right Face (Shaded)
                    using (SolidBrush rf = new SolidBrush(Color.FromArgb(160, 20, 30)))
                    {
                        g.FillPolygon(rf, new PointF[] { topR, topB, botC, botR });
                    }
                    // Gold Ribbons across faces
                    using (Pen ribP = new Pen(Color.FromArgb(255, 215, 0), 3.5f))
                    {
                        g.DrawLine(ribP, cx, cy - 14f, cx, cy + 16f);
                        g.DrawLine(ribP, cx - 13f, cy + 1f, cx + 13f, cy + 1f);
                    }
                    // 3D Bow Loops on Top
                    using (SolidBrush bowB = new SolidBrush(Color.FromArgb(255, 225, 45)))
                    {
                        g.FillEllipse(bowB, cx - 7f, cy - 18f, 6.5f, 6.5f);
                        g.FillEllipse(bowB, cx + 0.5f, cy - 18f, 6.5f, 6.5f);
                        g.FillEllipse(bowB, cx - 2.5f, cy - 15f, 5f, 4f);
                    }
                    break;

                case "bells":
                    // 3D Twin Golden Bells
                    using (LinearGradientBrush bGrad = new LinearGradientBrush(
                        new PointF(cx - 12f, cy - 8f), new PointF(cx + 12f, cy + 10f),
                        Color.FromArgb(255, 255, 230, 90), Color.FromArgb(255, 200, 135, 15)))
                    {
                        g.FillEllipse(bGrad, cx - 13f, cy - 5f, 13f, 15f);
                        g.FillEllipse(bGrad, cx, cy - 5f, 13f, 15f);
                    }
                    using (SolidBrush clap = new SolidBrush(Color.FromArgb(245, 175, 20)))
                    {
                        g.FillEllipse(clap, cx - 9f, cy + 7f, 5f, 5f);
                        g.FillEllipse(clap, cx + 4f, cy + 7f, 5f, 5f);
                    }
                    // Large 3D Red Velvet Bow joining bells
                    using (SolidBrush bow = new SolidBrush(Color.FromArgb(235, 30, 45)))
                    {
                        g.FillEllipse(bow, cx - 8f, cy - 11f, 8f, 6.5f);
                        g.FillEllipse(bow, cx, cy - 11f, 8f, 6.5f);
                        g.FillEllipse(bow, cx - 3f, cy - 9f, 6f, 6f);
                    }
                    break;

                case "gingerbread":
                    // 3D Golden-brown baked gingerbread man
                    using (SolidBrush cookie = new SolidBrush(Color.FromArgb(205, 130, 60)))
                    {
                        g.FillEllipse(cookie, cx - 6f, cy - 15f, 12f, 12f); // head
                        g.FillRectangle(cookie, cx - 5f, cy - 4f, 10f, 14f); // body
                        g.FillRectangle(cookie, cx - 13f, cy - 2f, 26f, 5.5f); // arms
                        g.FillRectangle(cookie, cx - 7f, cy + 8f, 5f, 8.5f); // left leg
                        g.FillRectangle(cookie, cx + 2f, cy + 8f, 5f, 8.5f); // right leg
                    }
                    // Crisp white icing details
                    using (Pen icing = new Pen(Color.White, 1.2f))
                    {
                        g.DrawArc(icing, cx - 3.5f, cy - 11f, 7f, 4f, 20, 140); // smile
                        g.DrawLine(icing, cx - 12f, cy, cx - 12f, cy + 3f);
                        g.DrawLine(icing, cx + 12f, cy, cx + 12f, cy + 3f);
                        g.DrawLine(icing, cx - 6f, cy + 15f, cx - 3f, cy + 15f);
                        g.DrawLine(icing, cx + 3f, cy + 15f, cx + 6f, cy + 15f);
                    }
                    using (SolidBrush eye = new SolidBrush(Color.FromArgb(60, 35, 20)))
                    {
                        g.FillEllipse(eye, cx - 3.5f, cy - 12f, 2f, 2f);
                        g.FillEllipse(eye, cx + 1.5f, cy - 12f, 2f, 2f);
                    }
                    // Candy buttons
                    using (SolidBrush btn = new SolidBrush(Color.FromArgb(235, 35, 50)))
                    {
                        g.FillEllipse(btn, cx - 1.5f, cy - 1f, 3.2f, 3.2f);
                        g.FillEllipse(btn, cx - 1.5f, cy + 4f, 3.2f, 3.2f);
                    }
                    break;

                case "holly":
                    // 3D Emerald Holly Leaves with veining
                    using (SolidBrush leaf = new SolidBrush(Color.FromArgb(32, 150, 65)))
                    {
                        g.FillEllipse(leaf, cx - 14f, cy - 9f, 14f, 11f);
                        g.FillEllipse(leaf, cx, cy - 9f, 14f, 11f);
                        g.FillEllipse(leaf, cx - 7f, cy + 1f, 14f, 11f);
                    }
                    using (Pen vein = new Pen(Color.FromArgb(80, 220, 115), 1.1f))
                    {
                        g.DrawLine(vein, cx - 11f, cy - 3f, cx - 2f, cy);
                        g.DrawLine(vein, cx + 11f, cy - 3f, cx + 2f, cy);
                    }
                    // 3 Glossy Red Berries with shine dots
                    using (SolidBrush berry = new SolidBrush(Color.FromArgb(235, 30, 45)))
                    {
                        g.FillEllipse(berry, cx - 5.5f, cy - 3f, 6.5f, 6.5f);
                        g.FillEllipse(berry, cx + 0.5f, cy - 3f, 6.5f, 6.5f);
                        g.FillEllipse(berry, cx - 2.5f, cy + 2.5f, 6.5f, 6.5f);
                    }
                    using (SolidBrush shine = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(shine, cx - 4f, cy - 2f, 2f, 2f);
                        g.FillEllipse(shine, cx + 2f, cy - 2f, 2f, 2f);
                    }
                    break;

                case "santahat":
                    // 3D Red Velvet Conical Hat with fold
                    PointF[] hatPts = new PointF[] {
                        new PointF(cx + 8f, cy - 13f),
                        new PointF(cx + 13f, cy + 4f),
                        new PointF(cx - 13f, cy + 4f)
                    };
                    using (LinearGradientBrush hatGrad = new LinearGradientBrush(
                        new PointF(cx - 12f, cy), new PointF(cx + 12f, cy),
                        Color.FromArgb(255, 240, 45, 60), Color.FromArgb(255, 170, 20, 30)))
                    {
                        g.FillPolygon(hatGrad, hatPts);
                    }
                    // Thick puffy white fleece trim and pom-pom
                    using (SolidBrush fluff = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
                    {
                        g.FillRectangle(fluff, cx - 15f, cy + 4f, 30f, 7f);
                        g.FillEllipse(fluff, cx + 6f, cy - 16f, 8.5f, 8.5f);
                    }
                    break;

                case "snowglobe":
                    // Polished mahogany wood base
                    using (SolidBrush baseB = new SolidBrush(Color.FromArgb(160, 95, 45)))
                    {
                        g.FillRectangle(baseB, cx - 11f, cy + 6f, 22f, 7f);
                    }
                    using (Pen bRim = new Pen(Color.FromArgb(240, 195, 80), 1.2f))
                    {
                        g.DrawLine(bRim, cx - 11f, cy + 6f, cx + 11f, cy + 6f);
                    }
                    // Crystal clear blue glass dome
                    using (SolidBrush glass = new SolidBrush(Color.FromArgb(160, 195, 235, 255)))
                    {
                        g.FillEllipse(glass, cx - 12f, cy - 12f, 24f, 24f);
                    }
                    // Mini Snowy Pine inside
                    using (SolidBrush miniT = new SolidBrush(Color.FromArgb(34, 155, 65)))
                    {
                        g.FillPolygon(miniT, new PointF[] { new PointF(cx, cy - 7f), new PointF(cx + 5.5f, cy + 3f), new PointF(cx - 5.5f, cy + 3f) });
                    }
                    // Falling snow & snowy floor
                    using (SolidBrush snowF = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(snowF, cx - 9f, cy + 3f, 18f, 5f);
                        g.FillEllipse(snowF, cx - 6f, cy - 5f, 2f, 2f);
                        g.FillEllipse(snowF, cx + 5f, cy - 4f, 2f, 2f);
                    }
                    // Glass shine glare arc
                    using (Pen gPen = new Pen(Color.FromArgb(200, 255, 255, 255), 1.5f))
                    {
                        g.DrawArc(gPen, cx - 10f, cy - 10f, 20f, 20f, 200, 60);
                    }
                    break;

                case "cocoa":
                    // 3D Red Ceramic Coffee Mug
                    using (LinearGradientBrush mugB = new LinearGradientBrush(
                        new PointF(cx - 9f, cy), new PointF(cx + 9f, cy),
                        Color.FromArgb(255, 235, 45, 55), Color.FromArgb(255, 170, 20, 30)))
                    {
                        g.FillRectangle(mugB, cx - 9f, cy - 4f, 18f, 17f);
                    }
                    using (Pen hand = new Pen(Color.FromArgb(220, 38, 38), 3f))
                    {
                        g.DrawArc(hand, cx + 5f, cy - 1f, 9f, 11f, 270, 180);
                    }
                    // Whipped cream swirl mound
                    using (SolidBrush cream = new SolidBrush(Color.FromArgb(250, 255, 255, 255)))
                    {
                        g.FillEllipse(cream, cx - 9f, cy - 8f, 18f, 7f);
                        g.FillEllipse(cream, cx - 5f, cy - 11f, 10f, 6f);
                    }
                    // Rising steam swirls
                    using (Pen steam = new Pen(Color.FromArgb(170, 255, 255, 255), 1.3f))
                    {
                        g.DrawArc(steam, cx - 5f, cy - 17f, 5f, 7f, 90, 180);
                        g.DrawArc(steam, cx + 1f, cy - 19f, 5f, 7f, 270, 180);
                    }
                    break;

                case "boat":
                    // 3D Christmas Tugboat
                    PointF[] bHull = new PointF[] {
                        new PointF(cx - 16f, cy + 2f),
                        new PointF(cx + 16f, cy + 2f),
                        new PointF(cx + 11f, cy + 12f),
                        new PointF(cx - 11f, cy + 12f)
                    };
                    using (SolidBrush bh = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillPolygon(bh, bHull);
                    }
                    using (SolidBrush bDeck = new SolidBrush(Color.FromArgb(250, 245, 235)))
                    {
                        g.FillRectangle(bDeck, cx - 15f, cy + 2f, 30f, 3.5f);
                    }
                    // Gold portholes
                    using (SolidBrush bDots = new SolidBrush(Color.FromArgb(254, 240, 138)))
                    {
                        g.FillEllipse(bDots, cx - 9f, cy + 6.5f, 3.5f, 3.5f);
                        g.FillEllipse(bDots, cx - 1.5f, cy + 6.5f, 3.5f, 3.5f);
                        g.FillEllipse(bDots, cx + 5.5f, cy + 6.5f, 3.5f, 3.5f);
                    }
                    // Mast and Fluttering Pennant Flag
                    using (Pen bMast = new Pen(Color.FromArgb(180, 110, 50), 1.8f))
                    {
                        g.DrawLine(bMast, cx + 5f, cy + 2f, cx + 5f, cy - 13f);
                    }
                    using (SolidBrush bFlag = new SolidBrush(Color.FromArgb(239, 68, 68)))
                    {
                        g.FillPolygon(bFlag, new PointF[] { new PointF(cx + 6f, cy - 13f), new PointF(cx + 14f, cy - 10f), new PointF(cx + 6f, cy - 7f) });
                    }
                    // Mini Decorated Christmas Tree on deck
                    using (SolidBrush bTree = new SolidBrush(Color.FromArgb(34, 155, 65)))
                    {
                        g.FillPolygon(bTree, new PointF[] { new PointF(cx - 6f, cy - 10f), new PointF(cx - 1f, cy + 2f), new PointF(cx - 11f, cy + 2f) });
                    }
                    break;
            }
        }

        private static void Draw3DStar(Graphics g, float cx, float cy, float rOuter, float rInner)
        {
            // 3D beveled star with alternating light and shadow facets
            Color lightFacet = Color.FromArgb(255, 255, 235, 100);
            Color darkFacet = Color.FromArgb(255, 215, 140, 15);

            PointF[] outerPts = new PointF[5];
            PointF[] innerPts = new PointF[5];

            for (int i = 0; i < 5; i++)
            {
                double aOuter = -Math.PI / 2.0 + i * 2.0 * Math.PI / 5.0;
                double aInner = aOuter + Math.PI / 5.0;

                outerPts[i] = new PointF(cx + (float)(Math.Cos(aOuter) * rOuter), cy + (float)(Math.Sin(aOuter) * rOuter));
                innerPts[i] = new PointF(cx + (float)(Math.Cos(aInner) * rInner), cy + (float)(Math.Sin(aInner) * rInner));
            }

            PointF center = new PointF(cx, cy);

            for (int i = 0; i < 5; i++)
            {
                int prevInner = (i + 4) % 5;

                // Left facet (illuminated)
                using (SolidBrush lb = new SolidBrush(lightFacet))
                {
                    g.FillPolygon(lb, new PointF[] { center, outerPts[i], innerPts[prevInner] });
                }
                // Right facet (shadowed)
                using (SolidBrush db = new SolidBrush(darkFacet))
                {
                    g.FillPolygon(db, new PointF[] { center, outerPts[i], innerPts[i] });
                }
            }

            // Central spark glare
            using (SolidBrush spark = new SolidBrush(Color.White))
            {
                g.FillEllipse(spark, cx - 2f, cy - 2f, 4f, 4f);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                // Top close button
                if (_closeBtnRect.Contains(e.Location))
                {
                    Close();
                    return;
                }

                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int floorY = screen.Bottom;

                // 1. Click Hero Card (Plant Christmas Tree)
                if (_heroCardRect.Contains(e.Location))
                {
                    // If it isn't snowing, turn snow on!
                    if (_overlay != null && _overlay.Particles != null)
                    {
                        if (_overlay.Particles.CurrentEffect != EffectType.Snow || _overlay.IsPaused)
                        {
                            _overlay.IsPaused = false;
                            _overlay.Particles.SwitchEffect(EffectType.Snow);
                        }
                    }

                    float spawnX = screen.Left + screen.Width * 0.70f;
                    float spawnY = screen.Top + screen.Height * 0.65f;
                    if (_mgr.Trees.Count > 0)
                    {
                        spawnX = screen.Left + screen.Width * 0.45f + (_mgr.Trees.Count * 65f) % (screen.Width * 0.4f);
                    }
                    _mgr.AddTree(spawnX, spawnY);
                    _overlay.Particles.SpawnSnowPuff(spawnX, spawnY, 16);
                    Invalidate();
                    return;
                }

                // 2. Click Grid Tiles
                for (int i = 0; i < TOTAL_TILES; i++)
                {
                    if (_tileRects[i].Contains(e.Location))
                    {
                        string id = _tileIds[i];
                        HandleTileClick(id, screen, floorY);
                        Invalidate();
                        return;
                    }
                }

                // 3. Click Behind Windows Toggle
                if (_behindToggleRect.Contains(e.Location))
                {
                    _mgr.BehindActiveWindows = !_mgr.BehindActiveWindows;
                    _mgr.SaveSettings();
                    Invalidate();
                    return;
                }

                // 4. Click Take All Down
                if (_btnTakeAllDownRect.Contains(e.Location))
                {
                    _mgr.ClearAll();
                    _overlay.Particles.SpawnSnowPuff(screen.Left + screen.Width * 0.5f, floorY - 20f, 25);
                    Invalidate();
                    return;
                }

                // Drag form from header or empty area
                if (e.Y < _heroCardRect.Y || e.Y > _footerRect.Y)
                {
                    _isDraggingForm = true;
                    _dragStartCursor = Cursor.Position;
                    _dragStartFormPos = Location;
                }
            }
        }

        private void HandleTileClick(string id, Rectangle screen, int floorY)
        {
            float rndOffset = (_mgr.TotalItemCount * 55f) % (screen.Width * 0.35f);

            switch (id)
            {
                case "lights":
                    float x1 = screen.Left + screen.Width * 0.28f;
                    float y1 = screen.Top + screen.Height * 0.30f + (_mgr.Lights.Count * 70f) % (screen.Height * 0.3f);
                    float x2 = screen.Left + screen.Width * 0.72f;
                    float y2 = y1;
                    _mgr.AddLights(x1, y1, x2, y2);
                    _overlay.Particles.SpawnSnowPuff((x1 + x2) * 0.5f, y1, 15);
                    break;

                case "star":
                    float sx = screen.Left + screen.Width * 0.50f + rndOffset;
                    float sy = screen.Top + screen.Height * 0.25f + (_mgr.Stars.Count * 40f) % (screen.Height * 0.25f);
                    _mgr.AddStar(sx, sy);
                    _overlay.Particles.SpawnSnowPuff(sx, sy, 12);
                    break;

                case "gift":
                    float gx = screen.Left + screen.Width * 0.55f + rndOffset;
                    float gy = screen.Top + screen.Height * 0.60f;
                    _giftColorIdx = (_giftColorIdx + 1) % _giftColors.Length;
                    _mgr.AddGift(gx, gy, _giftColors[_giftColorIdx], _ribbonColors[_giftColorIdx]);
                    _overlay.Particles.SpawnSnowPuff(gx, gy, 10);
                    break;

                default:
                    // Any general ornament (candle, redbauble, goldbauble, wreath, stocking, candycane, bells, gingerbread, holly, santahat, snowglobe, cocoa, boat)
                    float ox = screen.Left + screen.Width * 0.40f + rndOffset;
                    float oy = screen.Top + screen.Height * 0.48f + ((_mgr.Ornaments.Count * 22f) % (screen.Height * 0.22f));
                    _mgr.AddOrnament(id, ox, oy);
                    _overlay.Particles.SpawnSnowPuff(ox, oy, 12);
                    break;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDraggingForm)
            {
                Point cur = Cursor.Position;
                Location = new Point(
                    _dragStartFormPos.X + (cur.X - _dragStartCursor.X),
                    _dragStartFormPos.Y + (cur.Y - _dragStartCursor.Y)
                );
                return;
            }

            bool needRepaint = false;

            // Hover close
            bool hc = _closeBtnRect.Contains(e.Location);
            if (hc != _hoverClose) { _hoverClose = hc; needRepaint = true; }

            // Hover hero
            bool hh = _heroCardRect.Contains(e.Location);
            if (hh != _hoverHero) { _hoverHero = hh; needRepaint = true; }

            // Hover tiles
            int ht = -1;
            for (int i = 0; i < TOTAL_TILES; i++)
            {
                if (_tileRects[i].Contains(e.Location)) { ht = i; break; }
            }
            if (ht != _hoveredTile) { _hoveredTile = ht; needRepaint = true; }

            // Hover Take all down
            bool hta = _btnTakeAllDownRect.Contains(e.Location);
            if (hta != _hoverTakeAll) { _hoverTakeAll = hta; needRepaint = true; }

            // Hover behind toggle
            bool hb = _behindToggleRect.Contains(e.Location);
            if (hb != _hoverBehind) { _hoverBehind = hb; needRepaint = true; }

            if (needRepaint) Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingForm = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_headerTitleFont != null) _headerTitleFont.Dispose();
                if (_headerSubFont != null) _headerSubFont.Dispose();
                if (_heroTitleFont != null) _heroTitleFont.Dispose();
                if (_heroSubFont != null) _heroSubFont.Dispose();
                if (_tileLabelFont != null) _tileLabelFont.Dispose();
                if (_footerCountFont != null) _footerCountFont.Dispose();
                if (_btnFont != null) _btnFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
