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

        // Colors for gifts
        private readonly Color[] _giftBoxColors = new Color[]
        {
            Color.FromArgb(220, 38, 38),   // Red
            Color.FromArgb(22, 163, 74),   // Green
            Color.FromArgb(37, 99, 235),   // Blue
            Color.FromArgb(217, 119, 6)    // Amber Gold
        };

        private readonly Color[] _giftRibbonColors = new Color[]
        {
            Color.FromArgb(255, 215, 0),   // Gold
            Color.FromArgb(254, 240, 138), // Pale Gold
            Color.FromArgb(255, 255, 255), // White
            Color.FromArgb(239, 68, 68)    // Red
        };

        private int _selectedGiftColor = 0;

        // Layout Rectangles
        private Rectangle _headerRect;
        private Rectangle _closeBtnRect;
        private Rectangle _cardTreeRect;
        private Rectangle _cardLightsRect;
        private Rectangle _cardStarRect;
        private Rectangle _cardGiftRect;
        private Rectangle _cardHatRect;
        private Rectangle _cardBehindRect;

        private Rectangle _btnTreeAddRect;
        private Rectangle _btnLightsAddRect;
        private Rectangle _btnStarAddRect;
        private Rectangle _btnGiftAddRect;
        private Rectangle _toggleHatRect;
        private Rectangle _toggleBehindRect;
        private Rectangle[] _giftSwatchRects = new Rectangle[4];

        private Rectangle _btnClearAllRect;
        private Rectangle _btnCloseBottomRect;
        private Rectangle _hintRect;

        // Hover & Interaction States
        private int _hoveredCard = -1;
        private int _hoveredAction = -1;
        private int _hoveredSwatch = -1;
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
        private readonly Font _hintFont;

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

            Size = new Size(390, 620);

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Left + (screen.Width - Width) / 2, screen.Top + (screen.Height - Height) / 2);

            _titleFont = new Font("Segoe UI", 16f, FontStyle.Bold, GraphicsUnit.Pixel);
            _subFont = new Font("Segoe UI", 11.5f, FontStyle.Regular, GraphicsUnit.Pixel);
            _labelFont = new Font("Segoe UI", 13.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            _descFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
            _btnFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
            _hintFont = new Font("Segoe UI", 10.5f, FontStyle.Regular, GraphicsUnit.Pixel);

            CalculateLayout();
        }

        private void CalculateLayout()
        {
            int w = ClientSize.Width;
            int pad = 16;
            int top = 16;

            _headerRect = new Rectangle(pad, top, w - pad * 2, 46);
            _closeBtnRect = new Rectangle(w - pad - 32, top + 4, 32, 32);

            int startY = top + 50;
            int cardW = w - pad * 2;
            int btnW = 96;
            int btnH = 34;

            // Card 0: Tree (H=58)
            _cardTreeRect = new Rectangle(pad, startY, cardW, 58);
            _btnTreeAddRect = new Rectangle(_cardTreeRect.Right - btnW - 10, _cardTreeRect.Y + (_cardTreeRect.Height - btnH) / 2, btnW, btnH);

            // Card 1: Lights (H=58)
            _cardLightsRect = new Rectangle(pad, _cardTreeRect.Bottom + 6, cardW, 58);
            _btnLightsAddRect = new Rectangle(_cardLightsRect.Right - btnW - 10, _cardLightsRect.Y + (_cardLightsRect.Height - btnH) / 2, btnW, btnH);

            // Card 2: Star (H=58)
            _cardStarRect = new Rectangle(pad, _cardLightsRect.Bottom + 6, cardW, 58);
            _btnStarAddRect = new Rectangle(_cardStarRect.Right - btnW - 10, _cardStarRect.Y + (_cardStarRect.Height - btnH) / 2, btnW, btnH);

            // Card 3: Gift (H=78) with color swatches
            _cardGiftRect = new Rectangle(pad, _cardStarRect.Bottom + 6, cardW, 78);
            _btnGiftAddRect = new Rectangle(_cardGiftRect.Right - btnW - 10, _cardGiftRect.Y + 8, btnW, btnH);
            int swatchSize = 18;
            int swatchGap = 8;
            int swatchStartX = _cardGiftRect.X + 54;
            int swatchY = _cardGiftRect.Y + 49;
            for (int i = 0; i < 4; i++)
            {
                _giftSwatchRects[i] = new Rectangle(swatchStartX + i * (swatchSize + swatchGap), swatchY, swatchSize, swatchSize);
            }

            // Card 4: Santa Hat (H=52)
            _cardHatRect = new Rectangle(pad, _cardGiftRect.Bottom + 6, cardW, 52);
            int switchW = 46;
            int switchH = 26;
            _toggleHatRect = new Rectangle(_cardHatRect.Right - switchW - 14, _cardHatRect.Y + (_cardHatRect.Height - switchH) / 2, switchW, switchH);

            // Card 5: Behind Windows (H=52)
            _cardBehindRect = new Rectangle(pad, _cardHatRect.Bottom + 6, cardW, 52);
            _toggleBehindRect = new Rectangle(_cardBehindRect.Right - switchW - 14, _cardBehindRect.Y + (_cardBehindRect.Height - switchH) / 2, switchW, switchH);

            // Hint box
            _hintRect = new Rectangle(pad, _cardBehindRect.Bottom + 8, cardW, 30);

            // Bottom action buttons
            int bBtnH = 38;
            int bGap = 10;
            int halfW = (cardW - bGap) / 2;
            int bottomY = _hintRect.Bottom + 8;

            _btnClearAllRect = new Rectangle(pad, bottomY, halfW, bBtnH);
            _btnCloseBottomRect = new Rectangle(pad + halfW + bGap, bottomY, halfW, bBtnH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientSize.Width;
            int h = ClientSize.Height;

            Color bg = _isDarkMode ? Color.FromArgb(248, 22, 26, 33) : Color.FromArgb(250, 255, 255, 255);
            Color border = _isDarkMode ? Color.FromArgb(70, 255, 255, 255) : Color.FromArgb(40, 0, 0, 0);
            Color textMain = _isDarkMode ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 22, 26, 33);
            Color textSub = _isDarkMode ? Color.FromArgb(160, 255, 255, 255) : Color.FromArgb(140, 30, 35, 45);
            Color cardBg = _isDarkMode ? Color.FromArgb(140, 32, 37, 46) : Color.FromArgb(240, 245, 248, 252);
            Color cardHover = _isDarkMode ? Color.FromArgb(190, 42, 48, 60) : Color.FromArgb(255, 238, 242, 248);
            Color accentColor = Color.FromArgb(255, 22, 119, 255);
            Color accentGreen = Color.FromArgb(255, 22, 163, 74);
            Color accentFestive = Color.FromArgb(255, 220, 38, 38);
            Color accentGold = Color.FromArgb(255, 217, 119, 6);

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
                g.DrawString("🎄 Deck Your Screen", _titleFont, titleB, 18, 16);
                g.DrawString("Pilih aksesoris untuk dipasang & diatur di desktop", _subFont, subB, 20, 39);
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

            // 3. Card 0: Pohon Natal
            DrawCatalogCard(g, _cardTreeRect, "tree", "Pohon Natal 3D", "Pohon cemara 3D berhias salju, ornamen & lampu", _hoveredCard == 0, cardBg, cardHover, textMain, textSub);
            string treeBtnText = _mgr.Trees.Count > 0 ? "+ Tambah (" + _mgr.Trees.Count + ")" : "+ Pasang";
            DrawPillButton(g, _btnTreeAddRect, treeBtnText, _hoveredAction == 0, accentGreen);

            // 4. Card 1: Lampu Natal Gantung (NO STAR ON IT!)
            DrawCatalogCard(g, _cardLightsRect, "lights", "Lampu Gantung", "Tarik ujung kiri/kanan untuk atur ukuran!", _hoveredCard == 1, cardBg, cardHover, textMain, textSub);
            string lightsBtnText = _mgr.Lights.Count > 0 ? "+ Tambah (" + _mgr.Lights.Count + ")" : "+ Pasang";
            DrawPillButton(g, _btnLightsAddRect, lightsBtnText, _hoveredAction == 1, accentGold);

            // 5. Card 2: Lampu Bintang (Independent Star!)
            DrawCatalogCard(g, _cardStarRect, "star", "Lampu Bintang (Star)", "Bintang emas bersinar, geser sesukamu", _hoveredCard == 2, cardBg, cardHover, textMain, textSub);
            string starBtnText = _mgr.Stars.Count > 0 ? "+ Tambah (" + _mgr.Stars.Count + ")" : "+ Pasang";
            DrawPillButton(g, _btnStarAddRect, starBtnText, _hoveredAction == 2, Color.FromArgb(250, 204, 21), Color.FromArgb(120, 50, 0));

            // 6. Card 3: Kado Natal 3D
            DrawCatalogCard(g, _cardGiftRect, "gift", "Kado Natal 3D", "Kotak kado bisa ditumpuk & dilempar", _hoveredCard == 3, cardBg, cardHover, textMain, textSub);
            string giftBtnText = _mgr.Gifts.Count > 0 ? "+ Tambah (" + _mgr.Gifts.Count + ")" : "+ Tambah";
            DrawPillButton(g, _btnGiftAddRect, giftBtnText, _hoveredAction == 3, accentFestive);

            // Color Swatches on Gift Card
            using (SolidBrush stx = new SolidBrush(textSub))
            {
                g.DrawString("Warna:", _hintFont, stx, _cardGiftRect.X + 12, _cardGiftRect.Y + 52);
            }
            for (int i = 0; i < 4; i++)
            {
                Rectangle sr = _giftSwatchRects[i];
                bool isSelected = (_selectedGiftColor == i);
                bool isHover = (_hoveredSwatch == i);

                using (SolidBrush sb = new SolidBrush(_giftBoxColors[i]))
                {
                    g.FillEllipse(sb, sr);
                }

                if (isSelected || isHover)
                {
                    using (Pen sp = new Pen(isSelected ? Color.White : Color.FromArgb(180, 255, 255, 255), isSelected ? 2.2f : 1.5f))
                    {
                        g.DrawEllipse(sp, sr.X - 1, sr.Y - 1, sr.Width + 2, sr.Height + 2);
                    }
                }
            }

            // 7. Card 4: Topi Santa untuk Snowman
            DrawCatalogCard(g, _cardHatRect, "santahat", "Topi Santa Snowman", "Pakaikan topi Sinterklas ke boneka salju", _hoveredCard == 4, cardBg, cardHover, textMain, textSub);
            DrawToggleSwitch(g, _toggleHatRect, _mgr.ShowSantaHat, _hoveredAction == 4, accentFestive);

            // 8. Card 5: Di Belakang Jendela (Behind Windows)
            DrawCatalogCard(g, _cardBehindRect, "physics", "Di Belakang Jendela", "Dekorasi tidak menutupi isi aplikasi kerja", _hoveredCard == 5, cardBg, cardHover, textMain, textSub);
            DrawToggleSwitch(g, _toggleBehindRect, _mgr.BehindActiveWindows, _hoveredAction == 5, accentColor);

            // 9. Hint / Guide Notice
            using (GraphicsPath hp = VectorIcons.CreateRoundedRect(new RectangleF(_hintRect.X, _hintRect.Y, _hintRect.Width, _hintRect.Height), 8f))
            {
                using (SolidBrush hb = new SolidBrush(_isDarkMode ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(18, 0, 0, 0)))
                {
                    g.FillPath(hb, hp);
                }
            }
            using (SolidBrush hintBrush = new SolidBrush(textSub))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString("💡 Tarik ujung lampu untuk atur panjangnya. Klik (X) pada item untuk menghapus.", _hintFont, hintBrush, _hintRect, sf);
            }

            // 10. Bottom Buttons
            DrawOutlineButton(g, _btnClearAllRect, "🗑️ Hapus Semua", _hoveredAction == 6,
                Color.FromArgb(220, 38, 38), _isDarkMode ? Color.FromArgb(40, 220, 38, 38) : Color.FromArgb(20, 220, 38, 38));

            DrawPillButton(g, _btnCloseBottomRect, "Selesai", _hoveredAction == 7, accentColor);
        }

        private void DrawCatalogCard(Graphics g, Rectangle r, string iconName, string title, string sub, bool isHover,
            Color cardBg, Color cardHover, Color textMain, Color textSub)
        {
            using (GraphicsPath cp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), 13f))
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
            int iconBoxSize = 34;
            int iconBoxX = r.X + 8;
            int iconBoxY = r.Y + (r.Height - iconBoxSize) / 2;
            using (GraphicsPath ip = VectorIcons.CreateRoundedRect(new RectangleF(iconBoxX, iconBoxY, iconBoxSize, iconBoxSize), 9f))
            {
                Color ibg = _isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
                using (SolidBrush ib = new SolidBrush(ibg))
                {
                    g.FillPath(ib, ip);
                }
                VectorIcons.DrawIcon(g, iconName, iconBoxX + iconBoxSize * 0.5f, iconBoxY + iconBoxSize * 0.5f, 19f, textMain);
            }

            // Labels
            int textX = iconBoxX + iconBoxSize + 10;
            using (SolidBrush mb = new SolidBrush(textMain))
            using (SolidBrush sb = new SolidBrush(textSub))
            {
                g.DrawString(title, _labelFont, mb, textX, r.Y + 8);
                g.DrawString(sub, _descFont, sb, textX, r.Y + 27);
            }
        }

        private void DrawPillButton(Graphics g, Rectangle r, string text, bool isHover, Color baseColor, Color? textCol = null)
        {
            Color bg = isHover ? Color.FromArgb(Math.Min(255, (int)(baseColor.R * 1.15f)),
                                                Math.Min(255, (int)(baseColor.G * 1.15f)),
                                                Math.Min(255, (int)(baseColor.B * 1.15f))) : baseColor;

            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), r.Height * 0.5f))
            {
                using (SolidBrush bb = new SolidBrush(bg))
                {
                    g.FillPath(bb, bp);
                }
            }

            Color tc = textCol.HasValue ? textCol.Value : Color.White;
            using (SolidBrush tb = new SolidBrush(tc))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(text, _btnFont, tb, r, sf);
            }
        }

        private void DrawOutlineButton(Graphics g, Rectangle r, string text, bool isHover, Color accentColor, Color hoverBg)
        {
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), r.Height * 0.5f))
            {
                if (isHover)
                {
                    using (SolidBrush bb = new SolidBrush(hoverBg))
                    {
                        g.FillPath(bb, bp);
                    }
                }
                using (Pen pen = new Pen(accentColor, 1.2f))
                {
                    g.DrawPath(pen, bp);
                }
            }

            using (SolidBrush tb = new SolidBrush(accentColor))
            {
                StringFormat sf = new StringFormat();
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(text, _btnFont, tb, r, sf);
            }
        }

        private void DrawToggleSwitch(Graphics g, Rectangle r, bool isOn, bool isHover, Color onColor)
        {
            Color track = isOn ? onColor : (_isDarkMode ? Color.FromArgb(255, 65, 75, 90) : Color.FromArgb(255, 215, 222, 230));
            using (GraphicsPath sp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), r.Height * 0.5f))
            {
                using (SolidBrush tb = new SolidBrush(track))
                {
                    g.FillPath(tb, sp);
                }
            }

            int thumbR = r.Height - 6;
            int thumbX = isOn ? (r.Right - thumbR - 3) : (r.X + 3);
            int thumbY = r.Y + 3;

            using (SolidBrush thumbB = new SolidBrush(Color.White))
            {
                g.FillEllipse(thumbB, thumbX, thumbY, thumbR, thumbR);
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

                // Swatches
                for (int i = 0; i < 4; i++)
                {
                    if (_giftSwatchRects[i].Contains(e.Location))
                    {
                        _selectedGiftColor = i;
                        Invalidate();
                        return;
                    }
                }

                Rectangle screen = Screen.PrimaryScreen.WorkingArea;
                int floorY = screen.Bottom;

                // 1. Add Tree
                if (_btnTreeAddRect.Contains(e.Location))
                {
                    float spawnX = screen.Left + screen.Width * 0.72f;
                    float spawnY = floorY - 6f;
                    if (_mgr.Trees.Count > 0)
                    {
                        spawnX = screen.Left + screen.Width * 0.50f + (_mgr.Trees.Count * 60f) % (screen.Width * 0.4f);
                    }
                    _mgr.AddTree(spawnX, spawnY);
                    _overlay.Particles.SpawnSnowPuff(spawnX, spawnY, 15);
                    Invalidate();
                    return;
                }

                // 2. Add Lights (String Lights only, NO star attached!)
                if (_btnLightsAddRect.Contains(e.Location))
                {
                    float x1 = screen.Left + screen.Width * 0.28f;
                    float y1 = screen.Top + screen.Height * 0.30f + (_mgr.Lights.Count * 70f) % (screen.Height * 0.3f);
                    float x2 = screen.Left + screen.Width * 0.72f;
                    float y2 = y1;
                    _mgr.AddLights(x1, y1, x2, y2);
                    _overlay.Particles.SpawnSnowPuff((x1 + x2) * 0.5f, y1, 15);
                    Invalidate();
                    return;
                }

                // 3. Add Star (Independent golden Christmas star!)
                if (_btnStarAddRect.Contains(e.Location))
                {
                    float spawnX = screen.Left + screen.Width * 0.50f + (_mgr.Stars.Count * 50f) % (screen.Width * 0.3f);
                    float spawnY = screen.Top + screen.Height * 0.25f + (_mgr.Stars.Count * 40f) % (screen.Height * 0.25f);
                    _mgr.AddStar(spawnX, spawnY);
                    _overlay.Particles.SpawnSnowPuff(spawnX, spawnY, 12);
                    Invalidate();
                    return;
                }

                // 4. Add Gift
                if (_btnGiftAddRect.Contains(e.Location))
                {
                    float spawnX = screen.Left + screen.Width * 0.60f + (_mgr.Gifts.Count * 45f) % (screen.Width * 0.3f);
                    float spawnY = floorY - 6f;
                    Color boxCol = _giftBoxColors[_selectedGiftColor];
                    Color ribCol = _giftRibbonColors[_selectedGiftColor];
                    _mgr.AddGift(spawnX, spawnY, boxCol, ribCol);
                    _overlay.Particles.SpawnSnowPuff(spawnX, spawnY, 10);
                    Invalidate();
                    return;
                }

                // 5. Toggle Santa Hat
                if (_cardHatRect.Contains(e.Location))
                {
                    _mgr.ShowSantaHat = !_mgr.ShowSantaHat;
                    _mgr.SaveSettings();
                    Invalidate();
                    return;
                }

                // 6. Toggle Behind Windows (Option 1)
                if (_cardBehindRect.Contains(e.Location))
                {
                    _mgr.BehindActiveWindows = !_mgr.BehindActiveWindows;
                    _mgr.SaveSettings();
                    Invalidate();
                    return;
                }

                // 7. Clear All
                if (_btnClearAllRect.Contains(e.Location))
                {
                    _mgr.ClearAll();
                    Invalidate();
                    return;
                }

                // 8. Close Bottom
                if (_btnCloseBottomRect.Contains(e.Location))
                {
                    Close();
                    return;
                }

                // Drag window from header
                if (_headerRect.Contains(e.Location))
                {
                    _isDraggingForm = true;
                    _dragStartCursor = Cursor.Position;
                    _dragStartFormPos = Location;
                }
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

            // Hover card
            int hcIdx = -1;
            if (_cardTreeRect.Contains(e.Location)) hcIdx = 0;
            else if (_cardLightsRect.Contains(e.Location)) hcIdx = 1;
            else if (_cardStarRect.Contains(e.Location)) hcIdx = 2;
            else if (_cardGiftRect.Contains(e.Location)) hcIdx = 3;
            else if (_cardHatRect.Contains(e.Location)) hcIdx = 4;
            else if (_cardBehindRect.Contains(e.Location)) hcIdx = 5;

            if (hcIdx != _hoveredCard) { _hoveredCard = hcIdx; needRepaint = true; }

            // Hover action buttons
            int haIdx = -1;
            if (_btnTreeAddRect.Contains(e.Location)) haIdx = 0;
            else if (_btnLightsAddRect.Contains(e.Location)) haIdx = 1;
            else if (_btnStarAddRect.Contains(e.Location)) haIdx = 2;
            else if (_btnGiftAddRect.Contains(e.Location)) haIdx = 3;
            else if (_toggleHatRect.Contains(e.Location)) haIdx = 4;
            else if (_toggleBehindRect.Contains(e.Location)) haIdx = 5;
            else if (_btnClearAllRect.Contains(e.Location)) haIdx = 6;
            else if (_btnCloseBottomRect.Contains(e.Location)) haIdx = 7;

            if (haIdx != _hoveredAction) { _hoveredAction = haIdx; needRepaint = true; }

            // Hover swatch
            int hsIdx = -1;
            for (int i = 0; i < 4; i++)
            {
                if (_giftSwatchRects[i].Contains(e.Location)) { hsIdx = i; break; }
            }
            if (hsIdx != _hoveredSwatch) { _hoveredSwatch = hsIdx; needRepaint = true; }

            if (needRepaint) Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isDraggingForm = false;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
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
                if (_hintFont != null) _hintFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
