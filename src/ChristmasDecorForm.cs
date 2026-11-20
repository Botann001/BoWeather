using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BoWeather
{
    public class ChristmasDecorForm : Form
    {
        private readonly ChristmasManager _mgr;
        private readonly OverlayForm _overlay;
        private readonly bool _isDarkMode;

        // Layout Rectangles
        private Rectangle _headerRect;
        private Rectangle _closeBtnRect;
        private Rectangle[] _toggleRowRects = new Rectangle[5];
        private Rectangle _btnAddGiftRect;
        private Rectangle _btnResetRect;
        private Rectangle _btnCloseBottomRect;

        // Hover & Interaction States
        private int _hoveredRow = -1;
        private int _hoveredBtn = -1;
        private bool _hoverClose = false;
        private bool _isDraggingForm = false;
        private Point _dragStartCursor;
        private Point _dragStartFormPos;

        // Fonts
        private readonly Font _titleFont;
        private readonly Font _subFont;
        private readonly Font _labelFont;
        private readonly Font _descFont;
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

            Size = new Size(364, 530);

            // Center relative to primary screen or near top center
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Left + (screen.Width - Width) / 2, screen.Top + (screen.Height - Height) / 2);

            _titleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
            _subFont = new Font("Segoe UI", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
            _labelFont = new Font("Segoe UI", 13.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            _descFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
            _btnFont = new Font("Segoe UI", 12.5f, FontStyle.Bold, GraphicsUnit.Pixel);

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int w = ClientSize.Width;
            int pad = 16;
            int top = 16;

            _headerRect = new Rectangle(pad, top, w - pad * 2, 48);
            _closeBtnRect = new Rectangle(w - pad - 32, top + 6, 32, 32);

            int startY = top + 56;
            int rowH = 54;
            int rowGap = 6;

            for (int i = 0; i < 5; i++)
            {
                _toggleRowRects[i] = new Rectangle(pad, startY + i * (rowH + rowGap), w - pad * 2, rowH);
            }

            int actionsTop = startY + 5 * (rowH + rowGap) + 12;
            int btnGap = 8;
            int contentW = w - pad * 2;
            int halfW = (contentW - btnGap) / 2;
            int btnH = 40;

            _btnAddGiftRect = new Rectangle(pad, actionsTop, halfW, btnH);
            _btnResetRect = new Rectangle(pad + halfW + btnGap, actionsTop, halfW, btnH);
            _btnCloseBottomRect = new Rectangle(pad, actionsTop + btnH + btnGap, contentW, btnH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            // Palette
            Color bg = _isDarkMode ? Color.FromArgb(248, 22, 26, 33) : Color.FromArgb(250, 255, 255, 255);
            Color border = _isDarkMode ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0);
            Color textMain = _isDarkMode ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 22, 26, 33);
            Color textSub = _isDarkMode ? Color.FromArgb(160, 255, 255, 255) : Color.FromArgb(140, 30, 35, 45);
            Color cardBg = _isDarkMode ? Color.FromArgb(140, 32, 37, 46) : Color.FromArgb(240, 245, 248, 252);
            Color cardHover = _isDarkMode ? Color.FromArgb(190, 42, 48, 60) : Color.FromArgb(255, 238, 242, 248);
            Color accentColor = Color.FromArgb(255, 22, 119, 255);
            Color accentFestive = Color.FromArgb(255, 220, 38, 38);

            // 1. Form Rounded Background & Border
            using (GraphicsPath formPath = VectorIcons.CreateRoundedRect(new RectangleF(0.5f, 0.5f, w - 1f, h - 1f), 20f))
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

            // 2. Header
            using (SolidBrush titleB = new SolidBrush(textMain))
            using (SolidBrush subB = new SolidBrush(textSub))
            {
                g.DrawString("🎄 Dekorasi & Aksesoris Natal", _titleFont, titleB, 18, 18);
                g.DrawString("Pernak-pernik musim dingin untuk desktop Anda", _subFont, subB, 20, 42);
            }

            // Top Close Button (✕)
            using (GraphicsPath closePath = VectorIcons.CreateRoundedRect(new RectangleF(_closeBtnRect.X, _closeBtnRect.Y, _closeBtnRect.Width, _closeBtnRect.Height), 16f))
            {
                if (_hoverClose)
                {
                    using (SolidBrush ch = new SolidBrush(_isDarkMode ? Color.FromArgb(50, 255, 255, 255) : Color.FromArgb(25, 0, 0, 0)))
                    {
                        g.FillPath(ch, closePath);
                    }
                }
                VectorIcons.DrawIcon(g, "close", _closeBtnRect.X + 16, _closeBtnRect.Y + 16, 12f, textSub);
            }

            // 3. Five Toggle Cards
            string[] titles = new string[]
            {
                "Pohon Natal (Christmas Tree)",
                "Kado Natal (Gift Boxes)",
                "Lampu Natal Gantung",
                "Lampu Bintang (Star Lights)",
                "Topi Santa untuk Snowman"
            };

            string[] subs = new string[]
            {
                "Pohon cemara 3D berhias bintang & lampu",
                "Kotak kado 3D interaktif di atas salju",
                "Untaian lampu warna-warni kelap-kelip",
                "Bintang emas bercahaya di untaian lampu",
                "Pakaikan topi Sinterklas ke boneka salju"
            };

            string[] iconNames = new string[] { "tree", "gift", "lights", "star", "santahat" };
            bool[] states = new bool[]
            {
                _mgr.ShowTree,
                _mgr.ShowGifts,
                _mgr.ShowLights,
                _mgr.ShowStarLights,
                _mgr.ShowSantaHat
            };

            for (int i = 0; i < 5; i++)
            {
                Rectangle r = _toggleRowRects[i];
                bool isHover = (_hoveredRow == i);
                bool isOn = states[i];

                DrawToggleCard(g, r, iconNames[i], titles[i], subs[i], isOn, isHover, cardBg, cardHover, textMain, textSub, accentFestive);
            }

            // 4. Action Buttons
            // Button 0: + Tambah Kado
            DrawActionButton(g, _btnAddGiftRect, "gift", "+ Tambah Kado",
                _hoveredBtn == 0 ? cardHover : cardBg, textMain, _isDarkMode ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0));

            // Button 1: 🔄 Reset Posisi
            DrawActionButton(g, _btnResetRect, "refresh", "Reset Posisi",
                _hoveredBtn == 1 ? cardHover : cardBg, textMain, _isDarkMode ? Color.FromArgb(60, 255, 255, 255) : Color.FromArgb(30, 0, 0, 0));

            // Button 2: Tutup (Accent)
            DrawActionButton(g, _btnCloseBottomRect, "close", "Selesai",
                _hoveredBtn == 2 ? Color.FromArgb(20, 130, 235) : accentColor, Color.White, Color.Transparent);
        }

        private void DrawToggleCard(Graphics g, Rectangle r, string iconName, string title, string sub, bool isOn, bool isHover,
            Color cardBg, Color cardHover, Color textMain, Color textSub, Color toggleOnCol)
        {
            using (GraphicsPath cp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), 14f))
            {
                using (SolidBrush bgB = new SolidBrush(isHover ? cardHover : cardBg))
                {
                    g.FillPath(bgB, cp);
                }

                using (Pen bp = new Pen(_isDarkMode ? Color.FromArgb(35, 255, 255, 255) : Color.FromArgb(25, 0, 0, 0), 1f))
                {
                    g.DrawPath(bp, cp);
                }
            }

            // Icon square at left
            int iconBoxSize = 36;
            int iconBoxX = r.X + 8;
            int iconBoxY = r.Y + (r.Height - iconBoxSize) / 2;
            using (GraphicsPath ip = VectorIcons.CreateRoundedRect(new RectangleF(iconBoxX, iconBoxY, iconBoxSize, iconBoxSize), 10f))
            {
                Color ibg = isOn ? Color.FromArgb(40, toggleOnCol) : (_isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0));
                using (SolidBrush ib = new SolidBrush(ibg))
                {
                    g.FillPath(ib, ip);
                }
                Color ic = isOn ? toggleOnCol : textSub;
                VectorIcons.DrawIcon(g, iconName, iconBoxX + iconBoxSize * 0.5f, iconBoxY + iconBoxSize * 0.5f, 18f, ic, ic);
            }

            // Labels
            int textX = iconBoxX + iconBoxSize + 10;
            using (SolidBrush mb = new SolidBrush(textMain))
            using (SolidBrush sb = new SolidBrush(textSub))
            {
                g.DrawString(title, _labelFont, mb, textX, r.Y + 9);
                g.DrawString(sub, _descFont, sb, textX, r.Y + 28);
            }

            // Toggle Pill Switch at right
            int pillW = 44;
            int pillH = 24;
            int pillX = r.Right - pillW - 12;
            int pillY = r.Y + (r.Height - pillH) / 2;
            Rectangle pillRect = new Rectangle(pillX, pillY, pillW, pillH);

            Color switchTrack = isOn ? toggleOnCol : (_isDarkMode ? Color.FromArgb(255, 65, 75, 90) : Color.FromArgb(255, 215, 222, 230));
            using (GraphicsPath sp = VectorIcons.CreateRoundedRect(new RectangleF(pillRect.X, pillRect.Y, pillRect.Width, pillRect.Height), pillH * 0.5f))
            {
                using (SolidBrush tb = new SolidBrush(switchTrack))
                {
                    g.FillPath(tb, sp);
                }

                // Thumb knob circle
                float knobRadius = (pillH - 4) * 0.5f;
                float knobX = isOn ? (pillRect.Right - knobRadius - 2f) : (pillRect.Left + knobRadius + 2f);
                float knobY = pillRect.Top + 2f + knobRadius;

                using (SolidBrush kb = new SolidBrush(Color.White))
                {
                    g.FillEllipse(kb, knobX - knobRadius, knobY - knobRadius, knobRadius * 2f, knobRadius * 2f);
                }
            }
        }

        private void DrawActionButton(Graphics g, Rectangle r, string iconName, string text, Color bgCol, Color fgCol, Color borderCol)
        {
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), 12f))
            {
                using (SolidBrush bgB = new SolidBrush(bgCol))
                {
                    g.FillPath(bgB, bp);
                }
                if (borderCol != Color.Transparent)
                {
                    using (Pen bpPen = new Pen(borderCol, 1f))
                    {
                        g.DrawPath(bpPen, bp);
                    }
                }
            }

            using (SolidBrush fb = new SolidBrush(fgCol))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, _btnFont, fb, new RectangleF(r.X, r.Y, r.Width, r.Height), sf);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            // Close button top-right
            if (_closeBtnRect.Contains(e.Location))
            {
                Close();
                return;
            }

            // Toggles
            for (int i = 0; i < 5; i++)
            {
                if (_toggleRowRects[i].Contains(e.Location))
                {
                    switch (i)
                    {
                        case 0: _mgr.ShowTree = !_mgr.ShowTree; break;
                        case 1: _mgr.ShowGifts = !_mgr.ShowGifts; break;
                        case 2: _mgr.ShowLights = !_mgr.ShowLights; break;
                        case 3: _mgr.ShowStarLights = !_mgr.ShowStarLights; break;
                        case 4: _mgr.ShowSantaHat = !_mgr.ShowSantaHat; break;
                    }
                    _mgr.SaveSettings();
                    Invalidate();
                    return;
                }
            }

            // Button 0: + Tambah Kado
            if (_btnAddGiftRect.Contains(e.Location))
            {
                int floorY = Screen.PrimaryScreen.Bounds.Height - 48;
                _mgr.AddGift(Screen.PrimaryScreen.Bounds.Width * 0.5f, floorY - 6f, floorY);
                _mgr.ShowGifts = true;
                _mgr.SaveSettings();
                Invalidate();
                return;
            }

            // Button 1: Reset Posisi
            if (_btnResetRect.Contains(e.Location))
            {
                int floorY = Screen.PrimaryScreen.Bounds.Height - 48;
                _mgr.ResetPositions(Screen.PrimaryScreen.Bounds.Width, floorY);
                Invalidate();
                return;
            }

            // Button 2: Tutup
            if (_btnCloseBottomRect.Contains(e.Location))
            {
                Close();
                return;
            }

            // Header drag
            if (_headerRect.Contains(e.Location))
            {
                _isDraggingForm = true;
                _dragStartCursor = Cursor.Position;
                _dragStartFormPos = Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_isDraggingForm && e.Button == MouseButtons.Left)
            {
                Point cur = Cursor.Position;
                Location = new Point(_dragStartFormPos.X + (cur.X - _dragStartCursor.X), _dragStartFormPos.Y + (cur.Y - _dragStartCursor.Y));
                return;
            }

            int oldRow = _hoveredRow;
            int oldBtn = _hoveredBtn;
            bool oldClose = _hoverClose;

            _hoverClose = _closeBtnRect.Contains(e.Location);
            _hoveredRow = -1;
            for (int i = 0; i < 5; i++)
            {
                if (_toggleRowRects[i].Contains(e.Location))
                {
                    _hoveredRow = i;
                    break;
                }
            }

            _hoveredBtn = -1;
            if (_btnAddGiftRect.Contains(e.Location)) _hoveredBtn = 0;
            else if (_btnResetRect.Contains(e.Location)) _hoveredBtn = 1;
            else if (_btnCloseBottomRect.Contains(e.Location)) _hoveredBtn = 2;

            if (_hoverClose != oldClose || _hoveredRow != oldRow || _hoveredBtn != oldBtn)
            {
                Invalidate();
            }
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
                if (_titleFont != null) _titleFont.Dispose();
                if (_subFont != null) _subFont.Dispose();
                if (_labelFont != null) _labelFont.Dispose();
                if (_descFont != null) _descFont.Dispose();
                if (_btnFont != null) _btnFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
