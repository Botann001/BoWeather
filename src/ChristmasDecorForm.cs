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
        private Rectangle _headerRect;
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
        private readonly Font _guideFont;
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

            Size = new Size(430, 620);

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Left + (screen.Width - Width) / 2, screen.Top + (screen.Height - Height) / 2);

            _guideFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
            _heroTitleFont = new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);
            _heroSubFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel);
            _tileLabelFont = new Font("Segoe UI", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
            _footerCountFont = new Font("Segoe UI", 13f, FontStyle.Bold, GraphicsUnit.Pixel);
            _btnFont = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Pixel);

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int w = ClientSize.Width;
            int pad = 16;

            // 1. Top Header Guide
            _headerRect = new Rectangle(pad, 14, w - pad * 2 - 32, 44);
            _closeBtnRect = new Rectangle(w - pad - 26, 16, 26, 26);

            // 2. Hero Card: Christmas Tree
            int heroY = _headerRect.Bottom + 6;
            _heroCardRect = new Rectangle(pad, heroY, w - pad * 2, 74);
            int btnW = 80;
            int btnH = 32;
            _btnPlantRect = new Rectangle(_heroCardRect.Right - btnW - 12, _heroCardRect.Y + (_heroCardRect.Height - btnH) / 2, btnW, btnH);

            // 3. Grid of 16 Tiles (4x4)
            int gridY = _heroCardRect.Bottom + 12;
            int tileW = 92;
            int tileH = 82;
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
            _behindToggleRect = new Rectangle(_footerRect.X + 105, footerY + 3, 145, 28);
            int takeBtnW = 125;
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

            Color bg = _isDarkMode ? Color.FromArgb(248, 22, 26, 36) : Color.FromArgb(250, 248, 250, 252);
            Color border = _isDarkMode ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0);
            Color textMain = _isDarkMode ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 20, 24, 33);
            Color textSub = _isDarkMode ? Color.FromArgb(175, 200, 215, 230) : Color.FromArgb(140, 45, 55, 70);

            // 1. Window Frosted Glass Body & Border
            using (GraphicsPath formPath = VectorIcons.CreateRoundedRect(new RectangleF(0.5f, 0.5f, w - 1f, h - 1f), 22f))
            {
                using (SolidBrush bgBrush = new SolidBrush(bg))
                {
                    g.FillPath(bgBrush, formPath);
                }
                using (Pen borderPen = new Pen(border, 1.2f))
                {
                    g.DrawPath(borderPen, formPath);
                }
            }

            // 2. Top Header Instruction Text
            using (SolidBrush guideBrush = new SolidBrush(textSub))
            {
                StringFormat sf = new StringFormat();
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString("Pick something and it lands on your desktop, behind your windows. Drag it anywhere, pull the ends of the lights to stretch them, and press \u00D7 to take one down.", _guideFont, guideBrush, _headerRect, sf);
            }

            // Close button (x)
            using (GraphicsPath closePath = VectorIcons.CreateRoundedRect(new RectangleF(_closeBtnRect.X, _closeBtnRect.Y, _closeBtnRect.Width, _closeBtnRect.Height), 13f))
            {
                if (_hoverClose)
                {
                    using (SolidBrush ch = new SolidBrush(_isDarkMode ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(25, 0, 0, 0)))
                    {
                        g.FillPath(ch, closePath);
                    }
                }
                using (Pen xp = new Pen(_hoverClose ? textMain : textSub, 1.8f))
                {
                    xp.StartCap = LineCap.Round;
                    xp.EndCap = LineCap.Round;
                    g.DrawLine(xp, _closeBtnRect.X + 8, _closeBtnRect.Y + 8, _closeBtnRect.Right - 8, _closeBtnRect.Bottom - 8);
                    g.DrawLine(xp, _closeBtnRect.Right - 8, _closeBtnRect.Y + 8, _closeBtnRect.X + 8, _closeBtnRect.Bottom - 8);
                }
            }

            // 3. Hero Card: Christmas Tree
            Color heroBg = _hoverHero ? (_isDarkMode ? Color.FromArgb(200, 42, 50, 68) : Color.FromArgb(240, 230, 238, 248)) :
                                        (_isDarkMode ? Color.FromArgb(140, 32, 38, 52) : Color.FromArgb(225, 240, 245, 252));
            using (GraphicsPath hp = VectorIcons.CreateRoundedRect(_heroCardRect, 14f))
            {
                using (SolidBrush hb = new SolidBrush(heroBg)) g.FillPath(hb, hp);
                using (Pen hpen = new Pen(_isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0), 1f)) g.DrawPath(hpen, hp);
            }

            // 3D Tree mini preview on left
            DrawMiniTree(g, _heroCardRect.X + 36, _heroCardRect.Y + _heroCardRect.Height * 0.5f + 18f, 38f, 54f);

            // Tree labels
            int heroTextX = _heroCardRect.X + 70;
            using (SolidBrush tb = new SolidBrush(textMain))
            {
                g.DrawString("Christmas tree", _heroTitleFont, tb, heroTextX, _heroCardRect.Y + 14);
            }
            using (SolidBrush sb = new SolidBrush(textSub))
            {
                Rectangle textR = new Rectangle(heroTextX, _heroCardRect.Y + 36, _heroCardRect.Width - 170, 32);
                g.DrawString("Plant one in the snow and dress it up. If it isn't snowing, the snow turns on.", _heroSubFont, sb, textR);
            }

            // Plant button on hero card
            string plantText = _mgr.Trees.Count > 0 ? "+ Plant (" + _mgr.Trees.Count + ")" : "+ Plant";
            using (GraphicsPath pbp = VectorIcons.CreateRoundedRect(_btnPlantRect, 16f))
            {
                using (SolidBrush pbb = new SolidBrush(Color.FromArgb(255, 22, 163, 74))) g.FillPath(pbb, pbp);
            }
            using (SolidBrush pbt = new SolidBrush(Color.White))
            {
                StringFormat csf = new StringFormat();
                csf.Alignment = StringAlignment.Center;
                csf.LineAlignment = StringAlignment.Center;
                g.DrawString(plantText, _btnFont, pbt, _btnPlantRect, csf);
            }

            // 4. Grid of 16 Tiles (4x4)
            for (int i = 0; i < TOTAL_TILES; i++)
            {
                Rectangle tr = _tileRects[i];
                bool isHover = (_hoveredTile == i);
                string id = _tileIds[i];

                Color tileBg = isHover ? (_isDarkMode ? Color.FromArgb(190, 44, 52, 70) : Color.FromArgb(240, 230, 238, 248)) :
                                         (_isDarkMode ? Color.FromArgb(120, 30, 36, 48) : Color.FromArgb(220, 238, 244, 250));

                using (GraphicsPath tp = VectorIcons.CreateRoundedRect(tr, 13f))
                {
                    using (SolidBrush tb = new SolidBrush(tileBg)) g.FillPath(tb, tp);
                    using (Pen tpn = new Pen(_isDarkMode ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(25, 0, 0, 0), 1f)) g.DrawPath(tpn, tp);
                }

                // Tile Icon in upper area
                float iconCx = tr.X + tr.Width * 0.5f;
                float iconCy = tr.Y + 34;
                DrawTileIcon(g, id, iconCx, iconCy, 34f);

                // Tile Label in lower area
                using (SolidBrush lb = new SolidBrush(textMain))
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
            using (SolidBrush fb = new SolidBrush(textSub))
            {
                g.DrawString(_mgr.TotalItemCount + " of 30 up", _footerCountFont, fb, _footerRect.X + 4, _footerRect.Y + 8);
            }

            // Middle: Behind windows toggle
            bool isBehind = _mgr.BehindActiveWindows;
            Color togBg = _hoverBehind ? (_isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0)) : Color.Transparent;
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(_behindToggleRect, 8f))
            {
                if (_hoverBehind)
                {
                    using (SolidBrush bgb = new SolidBrush(togBg)) g.FillPath(bgb, bp);
                }
            }
            Color checkCol = isBehind ? Color.FromArgb(34, 197, 94) : textSub;
            using (Pen cp = new Pen(checkCol, 1.5f))
            {
                g.DrawRectangle(cp, _behindToggleRect.X + 4, _behindToggleRect.Y + 7, 13, 13);
                if (isBehind)
                {
                    g.DrawLine(cp, _behindToggleRect.X + 6, _behindToggleRect.Y + 13, _behindToggleRect.X + 9, _behindToggleRect.Y + 17);
                    g.DrawLine(cp, _behindToggleRect.X + 9, _behindToggleRect.Y + 17, _behindToggleRect.X + 15, _behindToggleRect.Y + 9);
                }
            }
            using (SolidBrush tb = new SolidBrush(isBehind ? textMain : textSub))
            {
                g.DrawString("Behind windows", _tileLabelFont, tb, _behindToggleRect.X + 22, _behindToggleRect.Y + 6);
            }

            // Right: Take all down button
            Color takeBg = _hoverTakeAll ? (_isDarkMode ? Color.FromArgb(80, 220, 38, 38) : Color.FromArgb(40, 220, 38, 38)) :
                                           (_isDarkMode ? Color.FromArgb(45, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0));
            Color takeBorder = _hoverTakeAll ? Color.FromArgb(220, 38, 38) : (_isDarkMode ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0));
            using (GraphicsPath tap = VectorIcons.CreateRoundedRect(_btnTakeAllDownRect, 16f))
            {
                using (SolidBrush tab = new SolidBrush(takeBg)) g.FillPath(tab, tap);
                using (Pen tapen = new Pen(takeBorder, 1.2f)) g.DrawPath(tapen, tap);
            }
            using (SolidBrush tat = new SolidBrush(_hoverTakeAll ? Color.FromArgb(239, 68, 68) : textMain))
            {
                StringFormat csf = new StringFormat();
                csf.Alignment = StringAlignment.Center;
                csf.LineAlignment = StringAlignment.Center;
                g.DrawString("Take all down", _btnFont, tat, _btnTakeAllDownRect, csf);
            }
        }

        private void DrawMiniTree(Graphics g, float cx, float cy, float tw, float th)
        {
            // Mini 3D preview tree matching the redesign
            float halfW = tw * 0.5f;

            // Trunk
            using (SolidBrush trunkB = new SolidBrush(Color.FromArgb(255, 185, 110, 50)))
            {
                g.FillRectangle(trunkB, cx - 4, cy - 7, 8, 8);
            }

            // 3 tiers
            DrawTierPoly(g, cx, cy - 5, tw * 0.96f, th * 0.38f);
            DrawTierPoly(g, cx, cy - 15, tw * 0.76f, th * 0.35f);
            DrawTierPoly(g, cx, cy - 25, tw * 0.54f, th * 0.35f);

            // Mini glowing baubles
            using (SolidBrush rB = new SolidBrush(Color.FromArgb(245, 38, 52))) g.FillEllipse(rB, cx - 8f, cy - 12f, 4.5f, 4.5f);
            using (SolidBrush gB = new SolidBrush(Color.FromArgb(255, 195, 18))) g.FillEllipse(gB, cx + 5f, cy - 14f, 4.5f, 4.5f);
            using (SolidBrush bB = new SolidBrush(Color.FromArgb(125, 215, 255))) g.FillEllipse(bB, cx - 4f, cy - 20f, 4.5f, 4.5f);
            using (SolidBrush pB = new SolidBrush(Color.FromArgb(255, 85, 145))) g.FillEllipse(pB, cx + 4f, cy - 22f, 4.5f, 4.5f);

            // Golden star on peak
            using (SolidBrush sGlow = new SolidBrush(Color.FromArgb(80, 255, 215, 0)))
            {
                g.FillEllipse(sGlow, cx - 9f, cy - 43f, 18f, 18f);
            }
            ChristmasManager.DrawStarPolygon(g, cx, cy - 34f, 6.5f, 3.2f, Color.FromArgb(255, 255, 235, 80), Color.FromArgb(255, 245, 160, 20));
        }

        private void DrawTierPoly(Graphics g, float cx, float cy, float w, float h)
        {
            PointF[] pts = new PointF[] {
                new PointF(cx, cy - h),
                new PointF(cx + w * 0.5f, cy),
                new PointF(cx - w * 0.5f, cy)
            };
            using (GraphicsPath gp = new GraphicsPath())
            {
                gp.AddPolygon(pts);
                using (LinearGradientBrush lgb = new LinearGradientBrush(
                    new PointF(cx, cy - h), new PointF(cx, cy),
                    Color.FromArgb(255, 85, 215, 105), Color.FromArgb(255, 34, 145, 62)))
                {
                    g.FillPath(lgb, gp);
                }
            }
            // Snow shoulder caps
            using (SolidBrush snowB = new SolidBrush(Color.White))
            {
                g.FillEllipse(snowB, cx - w * 0.5f - 1f, cy - 2.5f, w * 0.35f, 5f);
                g.FillEllipse(snowB, cx + w * 0.15f, cy - 2.5f, w * 0.35f, 5f);
            }
        }

        private void DrawTileIcon(Graphics g, string id, float cx, float cy, float s)
        {
            float r = s * 0.5f;
            switch (id)
            {
                case "lights":
                    using (Pen wp = new Pen(Color.FromArgb(170, 255, 255, 255), 1.4f))
                    {
                        g.DrawArc(wp, cx - r * 0.9f, cy - r * 0.6f, r * 1.8f, r * 0.9f, 20, 140);
                    }
                    Color[] bCols = new Color[] { Color.Red, Color.Gold, Color.DodgerBlue, Color.LimeGreen, Color.Magenta };
                    for (int i = 0; i < 5; i++)
                    {
                        float bx = cx - r * 0.7f + i * (r * 0.35f);
                        float by = cy + (float)Math.Sin(i * 0.8) * 3f;
                        using (SolidBrush bb = new SolidBrush(bCols[i]))
                        {
                            g.FillEllipse(bb, bx - 3f, by - 3f, 6f, 6f);
                        }
                    }
                    break;

                case "candle":
                    using (SolidBrush baseB = new SolidBrush(Color.FromArgb(255, 230, 185, 50)))
                    {
                        g.FillEllipse(baseB, cx - 10f, cy + 8f, 20f, 6f);
                    }
                    using (LinearGradientBrush cb = new LinearGradientBrush(
                        new PointF(cx - 5f, cy - 6f), new PointF(cx + 5f, cy - 6f),
                        Color.FromArgb(255, 240, 50, 60), Color.FromArgb(255, 170, 20, 30)))
                    {
                        g.FillRectangle(cb, cx - 5f, cy - 6f, 10f, 16f);
                    }
                    using (SolidBrush flame = new SolidBrush(Color.FromArgb(255, 255, 215, 0)))
                    {
                        g.FillEllipse(flame, cx - 3.5f, cy - 15f, 7f, 10f);
                    }
                    using (SolidBrush core = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(core, cx - 1.5f, cy - 12f, 3f, 5f);
                    }
                    break;

                case "redbauble":
                case "goldbauble":
                    bool isRed = (id == "redbauble");
                    Color mainC = isRed ? Color.FromArgb(235, 35, 50) : Color.FromArgb(255, 195, 20);
                    Color darkC = isRed ? Color.FromArgb(120, 15, 25) : Color.FromArgb(160, 110, 10);
                    using (SolidBrush cap = new SolidBrush(Color.FromArgb(255, 250, 210, 80)))
                    {
                        g.FillRectangle(cap, cx - 3f, cy - r + 1f, 6f, 3.5f);
                    }
                    using (GraphicsPath bp = new GraphicsPath())
                    {
                        bp.AddEllipse(cx - r * 0.75f, cy - r * 0.75f + 3f, r * 1.5f, r * 1.5f);
                        using (PathGradientBrush pgb = new PathGradientBrush(bp))
                        {
                            pgb.CenterPoint = new PointF(cx - r * 0.25f, cy - r * 0.25f + 3f);
                            pgb.CenterColor = Color.White;
                            pgb.SurroundColors = new Color[] { mainC };
                            g.FillPath(pgb, bp);
                        }
                    }
                    break;

                case "star":
                    using (SolidBrush glow = new SolidBrush(Color.FromArgb(70, 255, 215, 0)))
                    {
                        g.FillEllipse(glow, cx - r * 1.1f, cy - r * 1.1f, r * 2.2f, r * 2.2f);
                    }
                    ChristmasManager.DrawStarPolygon(g, cx, cy, r * 0.9f, r * 0.42f, Color.FromArgb(255, 255, 235, 80), Color.FromArgb(255, 245, 160, 20));
                    break;

                case "wreath":
                    using (Pen wp = new Pen(Color.FromArgb(255, 34, 160, 68), 5.5f))
                    {
                        g.DrawEllipse(wp, cx - r * 0.65f, cy - r * 0.65f, r * 1.3f, r * 1.3f);
                    }
                    using (Pen wp2 = new Pen(Color.FromArgb(255, 60, 205, 95), 2.5f))
                    {
                        g.DrawEllipse(wp2, cx - r * 0.65f, cy - r * 0.65f, r * 1.3f, r * 1.3f);
                    }
                    // Tiny red berry dots around wreath
                    using (SolidBrush bDot = new SolidBrush(Color.FromArgb(245, 35, 50)))
                    {
                        g.FillEllipse(bDot, cx - 8f, cy - 8f, 3.5f, 3.5f);
                        g.FillEllipse(bDot, cx + 5f, cy - 8f, 3.5f, 3.5f);
                        g.FillEllipse(bDot, cx - 11f, cy + 1f, 3.5f, 3.5f);
                        g.FillEllipse(bDot, cx + 8f, cy + 1f, 3.5f, 3.5f);
                    }
                    using (SolidBrush bow = new SolidBrush(Color.FromArgb(235, 35, 50)))
                    {
                        g.FillEllipse(bow, cx - 6f, cy + r * 0.45f, 12f, 7f);
                    }
                    break;

                case "stocking":
                    using (SolidBrush sock = new SolidBrush(Color.FromArgb(235, 35, 50)))
                    {
                        g.FillRectangle(sock, cx - 6f, cy - 10f, 10f, 16f);
                        g.FillEllipse(sock, cx - 8f, cy - 1f, 16f, 13f);
                    }
                    using (SolidBrush cuff = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(cuff, cx - 8f, cy - 13f, 14f, 6f);
                        // Fluffy toe / heel
                        g.FillEllipse(cuff, cx - 8f, cy + 4f, 6f, 6f);
                    }
                    break;

                case "candycane":
                    using (Pen cp = new Pen(Color.FromArgb(235, 35, 50), 5.5f))
                    {
                        cp.StartCap = LineCap.Round;
                        cp.EndCap = LineCap.Round;
                        GraphicsPath ccp = new GraphicsPath();
                        ccp.AddArc(cx - 4f, cy - 13f, 12f, 12f, 180, 180);
                        ccp.AddLine(cx - 4f, cy - 7f, cx - 4f, cy + 11f);
                        g.DrawPath(cp, ccp);
                    }
                    // Crisp white diagonal stripes
                    using (Pen sp = new Pen(Color.White, 2.2f))
                    {
                        g.DrawLine(sp, cx - 6f, cy + 8f, cx - 2f, cy + 5f);
                        g.DrawLine(sp, cx - 6f, cy + 2f, cx - 2f, cy - 1f);
                        g.DrawLine(sp, cx - 6f, cy - 4f, cx - 2f, cy - 7f);
                        g.DrawLine(sp, cx - 2f, cy - 12f, cx + 2f, cy - 13f);
                        g.DrawLine(sp, cx + 5f, cy - 11f, cx + 8f, cy - 8f);
                    }
                    break;

                case "gift":
                    using (SolidBrush gb = new SolidBrush(Color.FromArgb(235, 35, 50)))
                    {
                        g.FillRectangle(gb, cx - 11f, cy - 7f, 22f, 18f);
                    }
                    using (SolidBrush rib = new SolidBrush(Color.FromArgb(255, 215, 0)))
                    {
                        g.FillRectangle(rib, cx - 3f, cy - 7f, 6f, 18f);
                        g.FillRectangle(rib, cx - 11f, cy, 22f, 5f);
                        g.FillEllipse(rib, cx - 6f, cy - 12f, 12f, 6f);
                    }
                    break;

                case "bells":
                    using (SolidBrush bell = new SolidBrush(Color.FromArgb(255, 215, 0)))
                    {
                        g.FillEllipse(bell, cx - 11f, cy - 6f, 11f, 15f);
                        g.FillEllipse(bell, cx, cy - 6f, 11f, 15f);
                    }
                    using (SolidBrush clap = new SolidBrush(Color.FromArgb(235, 160, 20)))
                    {
                        g.FillEllipse(clap, cx - 8f, cy + 5f, 4f, 4f);
                        g.FillEllipse(clap, cx + 4f, cy + 5f, 4f, 4f);
                    }
                    using (SolidBrush bow = new SolidBrush(Color.FromArgb(235, 35, 50)))
                    {
                        g.FillEllipse(bow, cx - 6f, cy - 10f, 12f, 7f);
                    }
                    break;

                case "gingerbread":
                    using (SolidBrush gman = new SolidBrush(Color.FromArgb(205, 130, 60)))
                    {
                        g.FillEllipse(gman, cx - 5f, cy - 13f, 10f, 10f);
                        g.FillRectangle(gman, cx - 4f, cy - 4f, 8f, 13f);
                        g.FillRectangle(gman, cx - 11f, cy - 2f, 22f, 5f);
                        g.FillRectangle(gman, cx - 6f, cy + 7f, 4f, 7f);
                        g.FillRectangle(gman, cx + 2f, cy + 7f, 4f, 7f);
                    }
                    using (SolidBrush icing = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(icing, cx - 1.5f, cy - 1f, 3f, 3f);
                        g.FillEllipse(icing, cx - 1.5f, cy + 3f, 3f, 3f);
                    }
                    break;

                case "holly":
                    using (SolidBrush leaf = new SolidBrush(Color.FromArgb(34, 160, 68)))
                    {
                        g.FillEllipse(leaf, cx - 12f, cy - 8f, 12f, 10f);
                        g.FillEllipse(leaf, cx, cy - 8f, 12f, 10f);
                    }
                    using (SolidBrush berry = new SolidBrush(Color.Red))
                    {
                        g.FillEllipse(berry, cx - 4f, cy - 3f, 5f, 5f);
                        g.FillEllipse(berry, cx + 1f, cy - 3f, 5f, 5f);
                        g.FillEllipse(berry, cx - 1.5f, cy + 2f, 5f, 5f);
                    }
                    break;

                case "santahat":
                    PointF[] hpts = new PointF[] {
                        new PointF(cx + 8f, cy - 12f),
                        new PointF(cx + 12f, cy + 4f),
                        new PointF(cx - 12f, cy + 4f)
                    };
                    using (SolidBrush hat = new SolidBrush(Color.FromArgb(235, 35, 50)))
                    {
                        g.FillPolygon(hat, hpts);
                    }
                    using (SolidBrush fluff = new SolidBrush(Color.White))
                    {
                        g.FillRectangle(fluff, cx - 14f, cy + 4f, 28f, 6f);
                        g.FillEllipse(fluff, cx + 6f, cy - 15f, 7f, 7f);
                    }
                    break;

                case "snowglobe":
                    using (SolidBrush baseB = new SolidBrush(Color.FromArgb(160, 95, 45)))
                    {
                        g.FillRectangle(baseB, cx - 10f, cy + 6f, 20f, 7f);
                    }
                    using (SolidBrush glass = new SolidBrush(Color.FromArgb(160, 190, 230, 255)))
                    {
                        g.FillEllipse(glass, cx - 11f, cy - 11f, 22f, 22f);
                    }
                    using (SolidBrush miniT = new SolidBrush(Color.FromArgb(34, 160, 68)))
                    {
                        g.FillPolygon(miniT, new PointF[] { new PointF(cx, cy - 7f), new PointF(cx + 5f, cy + 3f), new PointF(cx - 5f, cy + 3f) });
                    }
                    using (SolidBrush snowF = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(snowF, cx - 8f, cy + 3f, 16f, 4f);
                        g.FillEllipse(snowF, cx - 6f, cy - 5f, 2f, 2f);
                        g.FillEllipse(snowF, cx + 5f, cy - 3f, 2f, 2f);
                    }
                    break;

                case "cocoa":
                    using (SolidBrush mug = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillRectangle(mug, cx - 8f, cy - 4f, 16f, 16f);
                    }
                    using (Pen hand = new Pen(Color.FromArgb(220, 38, 38), 2.5f))
                    {
                        g.DrawArc(hand, cx + 5f, cy - 1f, 8f, 10f, 270, 180);
                    }
                    using (SolidBrush cream = new SolidBrush(Color.White))
                    {
                        g.FillEllipse(cream, cx - 8f, cy - 7f, 16f, 6f);
                    }
                    using (Pen steam = new Pen(Color.FromArgb(160, 255, 255, 255), 1.2f))
                    {
                        g.DrawArc(steam, cx - 4f, cy - 14f, 4f, 6f, 90, 180);
                        g.DrawArc(steam, cx + 1f, cy - 15f, 4f, 6f, 270, 180);
                    }
                    break;

                case "boat":
                    // Miniature Boat Icon matching reference
                    PointF[] bHull = new PointF[] {
                        new PointF(cx - 15f, cy + 2f),
                        new PointF(cx + 15f, cy + 2f),
                        new PointF(cx + 10f, cy + 11f),
                        new PointF(cx - 10f, cy + 11f)
                    };
                    using (SolidBrush bh = new SolidBrush(Color.FromArgb(220, 38, 38)))
                    {
                        g.FillPolygon(bh, bHull);
                    }
                    using (SolidBrush bDeck = new SolidBrush(Color.FromArgb(250, 245, 235)))
                    {
                        g.FillRectangle(bDeck, cx - 14f, cy + 2f, 28f, 3f);
                    }
                    using (SolidBrush bDots = new SolidBrush(Color.FromArgb(254, 240, 138)))
                    {
                        g.FillEllipse(bDots, cx - 8f, cy + 6f, 3f, 3f);
                        g.FillEllipse(bDots, cx - 1.5f, cy + 6f, 3f, 3f);
                        g.FillEllipse(bDots, cx + 5f, cy + 6f, 3f, 3f);
                    }
                    using (Pen bMast = new Pen(Color.FromArgb(180, 110, 50), 1.5f))
                    {
                        g.DrawLine(bMast, cx + 4f, cy + 2f, cx + 4f, cy - 12f);
                    }
                    using (SolidBrush bFlag = new SolidBrush(Color.FromArgb(239, 68, 68)))
                    {
                        g.FillPolygon(bFlag, new PointF[] { new PointF(cx + 5f, cy - 12f), new PointF(cx + 12f, cy - 9f), new PointF(cx + 5f, cy - 6f) });
                    }
                    using (SolidBrush bTree = new SolidBrush(Color.FromArgb(34, 155, 65)))
                    {
                        g.FillPolygon(bTree, new PointF[] { new PointF(cx - 6f, cy - 9f), new PointF(cx - 1f, cy + 2f), new PointF(cx - 11f, cy + 2f) });
                    }
                    break;
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

                    float spawnX = screen.Left + screen.Width * 0.72f;
                    float spawnY = floorY - 6f;
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
                    float gx = screen.Left + screen.Width * 0.60f + rndOffset;
                    float gy = floorY - 6f;
                    _giftColorIdx = (_giftColorIdx + 1) % _giftColors.Length;
                    _mgr.AddGift(gx, gy, _giftColors[_giftColorIdx], _ribbonColors[_giftColorIdx]);
                    _overlay.Particles.SpawnSnowPuff(gx, gy, 10);
                    break;

                case "santahat":
                    _mgr.ShowSantaHat = !_mgr.ShowSantaHat;
                    _mgr.SaveSettings();
                    float hx = screen.Left + screen.Width * 0.5f;
                    float hy = floorY - 10f;
                    _overlay.Particles.SpawnSnowPuff(hx, hy, 12);
                    break;

                default:
                    // Any general ornament (candle, redbauble, goldbauble, wreath, stocking, candycane, bells, gingerbread, holly, snowglobe, cocoa, boat)
                    float ox = screen.Left + screen.Width * 0.40f + rndOffset;
                    float oy = floorY - 6f;
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
                if (_guideFont != null) _guideFont.Dispose();
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
