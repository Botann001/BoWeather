using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoWeather.Particles;

namespace BoWeather
{
    public class ControlCenterForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TOPMOST = 0x00000008;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
                return cp;
            }
        }

        private readonly OverlayForm _overlay;
        private readonly WallpaperManager _wallpaperManager;
        private readonly TrayIconManager _trayManager;

        // Configuration path for Dark/Light mode preference
        private readonly string _themeModePath;
        private bool _isDarkMode = true;
        public bool IsDarkMode { get { return _isDarkMode; } }

        // State matching control-center.html
        private bool _appliedWallpaper = false;
        private int _windValue = 50;     // 0 - 100
        private int _densityValue = 50;  // 0 - 100

        // Fonts
        private readonly Font _titleFont;
        private readonly Font _labelFont;
        private readonly Font _subLabelFont;
        private readonly Font _tileLabelFont;
        private readonly Font _sliderValFont;
        private readonly Font _btnFont;

        // Interactive Rectangles
        private Rectangle _modeBtnRect;
        private Rectangle _closeBtnRect;
        private readonly Rectangle[] _tileRects = new Rectangle[6];
        private Rectangle _windTrackRect;
        private Rectangle _densityTrackRect;
        private Rectangle _togglesContainerRect;
        private Rectangle _physicsRowRect;
        private Rectangle _autoPauseRowRect;
        private Rectangle _autoWallpaperRowRect;
        private Rectangle _snowplowRowRect;
        private Rectangle _btnApplyWallRect;
        private Rectangle _btnBrowseWallRect;
        private Rectangle _btnLangRect;
        private Rectangle _btnExitRect;
        private Rectangle _btnChristmasRect;
        private Rectangle _btnSnowplowRect;

        // Hover & Drag states
        private int _hoveredTile = -1;
        private int _hoveredRow = -1;    // 0=physics, 1=autopause, 2=autowall, 3=snowplow
        private int _hoveredBtn = -1;    // 0=apply, 1=browse, 2=lang, 3=exit, 4=xmas, 5=snowplow
        private bool _hoverMode = false;
        private bool _hoverClose = false;
        private bool _draggingWind = false;
        private bool _draggingDensity = false;

        public ControlCenterForm(OverlayForm overlay, WallpaperManager wallpaperManager, TrayIconManager trayManager)
        {
            _overlay = overlay;
            _wallpaperManager = wallpaperManager;
            _trayManager = trayManager;

            _themeModePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme_mode.txt");
            LoadThemeMode();

            // Initialize slider values from current particle engine settings
            SyncSlidersFromEngine();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;

            // Dimensions: Width 392 px, Height 720 px (fits comfortably within 1280x800 working area)
            Size = new Size(392, 720);
            UpdateThemeAppearance();

            // Fonts matching typography specification (Inter or Segoe UI fallback)
            _titleFont = CreateFont(17f, FontStyle.Bold);
            _labelFont = CreateFont(14f, FontStyle.Bold);
            _subLabelFont = CreateFont(12f, FontStyle.Regular);
            _tileLabelFont = CreateFont(12f, FontStyle.Bold);
            _sliderValFont = CreateFont(13f, FontStyle.Bold);
            _btnFont = CreateFont(13f, FontStyle.Bold);

            CalculateLayout();
        }

        private static Font CreateFont(float sizePixels, FontStyle style)
        {
            string[] families = new string[] { "Inter", "Segoe UI Variable Text", "Segoe UI", "sans-serif" };
            foreach (string fam in families)
            {
                try
                {
                    using (Font test = new Font(fam, sizePixels, style, GraphicsUnit.Pixel))
                    {
                        if (test.Name.Equals(fam, StringComparison.OrdinalIgnoreCase))
                        {
                            return new Font(fam, sizePixels, style, GraphicsUnit.Pixel);
                        }
                    }
                }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, sizePixels, style, GraphicsUnit.Pixel);
        }

        private void SyncSlidersFromEngine()
        {
            if (_overlay == null || _overlay.Particles == null) return;
            switch (_overlay.Particles.Wind)
            {
                case WindLevel.Calm: _windValue = 18; break;
                case WindLevel.Breeze: _windValue = 50; break;
                case WindLevel.Windy: _windValue = 84; break;
            }
            switch (_overlay.Particles.Density)
            {
                case DensityLevel.Low: _densityValue = 18; break;
                case DensityLevel.Medium: _densityValue = 50; break;
                case DensityLevel.High: _densityValue = 84; break;
            }
        }

        private void LoadThemeMode()
        {
            try
            {
                if (File.Exists(_themeModePath))
                {
                    string txt = File.ReadAllText(_themeModePath).Trim().ToLowerInvariant();
                    if (txt == "light") _isDarkMode = false;
                    else _isDarkMode = true;
                }
            }
            catch { }
        }

        private void SaveThemeMode()
        {
            try
            {
                File.WriteAllText(_themeModePath, _isDarkMode ? "dark" : "light");
            }
            catch { }
        }

        private void UpdateThemeAppearance()
        {
            BackColor = _isDarkMode ? Color.FromArgb(28, 29, 32) : Color.FromArgb(250, 252, 255);

            try
            {
                int darkAttr = _isDarkMode ? 1 : 0;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkAttr, sizeof(int));
                int pref = DWMWCP_ROUND;
                DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
            }
            catch { }
        }

        public void NotifyThemeChanged()
        {
            _appliedWallpaper = false;
            Invalidate();
        }

        private void CalculateLayout()
        {
            int w = ClientSize.Width;
            int pad = 16;
            int gap = 12;

            // 1. Header (Height: 32)
            int headerY = pad;
            int btnSize = 32;
            _closeBtnRect = new Rectangle(w - pad - btnSize, headerY, btnSize, btnSize);
            _modeBtnRect = new Rectangle(w - pad - btnSize - 6 - btnSize, headerY, btnSize, btnSize);

            // 2. Grid Tiles: 3 cols x 2 rows (gap 8, height 64)
            int tileStartTop = headerY + 32 + gap;
            int tileGap = 8;
            int contentW = w - (pad * 2);
            int tileW = (contentW - (tileGap * 2)) / 3;
            int tileH = 64;

            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                int tx = pad + col * (tileW + tileGap);
                int ty = tileStartTop + row * (tileH + tileGap);
                int thisW = (col == 2) ? (contentW - (tileW + tileGap) * 2) : tileW; // ensure pixel alignment
                _tileRects[i] = new Rectangle(tx, ty, thisW, tileH);
            }

            // 3. Sliders (Track height 8, row height 22, track area 24)
            int slidersTop = tileStartTop + (2 * tileH) + tileGap + gap;
            _windTrackRect = new Rectangle(pad, slidersTop + 22 + 4, contentW, 24);

            int densityTop = slidersTop + 22 + 4 + 24 + 10;
            _densityTrackRect = new Rectangle(pad, densityTop + 22 + 4, contentW, 24);

            // 4. Toggles Container Card (padding 4, radius 18, 4 rows of 50)
            int togglesTop = densityTop + 22 + 4 + 24 + gap;
            int cardPad = 4;
            int rowH = 50;
            int containerH = cardPad * 2 + rowH * 4 + 3; // 211 px
            _togglesContainerRect = new Rectangle(pad, togglesTop, contentW, containerH);

            _physicsRowRect = new Rectangle(pad + cardPad, togglesTop + cardPad, contentW - cardPad * 2, rowH);
            _autoPauseRowRect = new Rectangle(pad + cardPad, _physicsRowRect.Bottom + 1, contentW - cardPad * 2, rowH);
            _autoWallpaperRowRect = new Rectangle(pad + cardPad, _autoPauseRowRect.Bottom + 1, contentW - cardPad * 2, rowH);
            _snowplowRowRect = new Rectangle(pad + cardPad, _autoWallpaperRowRect.Bottom + 1, contentW - cardPad * 2, rowH);

            // 4.5. Feature Buttons Row: Christmas Decor | Summon Snowplow (Half Width each, Height 42, Radius 14)
            int featTop = togglesTop + containerH + gap;
            int btnGap = 8;
            int btnW = (contentW - btnGap) / 2;
            int btnH = 42;
            _btnChristmasRect = new Rectangle(pad, featTop, btnW, btnH);
            _btnSnowplowRect = new Rectangle(pad + btnW + btnGap, featTop, contentW - btnW - btnGap, btnH);

            // 5. Action Buttons (2x2 grid, height 42, gap 8, radius 14)
            int actionsTop = featTop + btnH + gap;

            _btnApplyWallRect = new Rectangle(pad, actionsTop, btnW, btnH);
            _btnBrowseWallRect = new Rectangle(pad + btnW + btnGap, actionsTop, contentW - btnW - btnGap, btnH);
            _btnLangRect = new Rectangle(pad, actionsTop + btnH + btnGap, btnW, btnH);
            _btnExitRect = new Rectangle(pad + btnW + btnGap, actionsTop + btnH + btnGap, contentW - btnW - btnGap, btnH);
        }

        private long _lastDeactivateTick = 0;

        public void ToggleVisibility(Rectangle? anchor = null)
        {
            long now = DateTime.UtcNow.Ticks;
            long elapsedMs = (now - _lastDeactivateTick) / TimeSpan.TicksPerMillisecond;

            if (this.Visible)
            {
                this.Hide();
            }
            else if (elapsedMs < 450)
            {
                // Just closed within 450ms due to focus loss when clicking the trigger/chip.
                // Do not re-open — keep it closed!
                return;
            }
            else
            {
                ShowFlyout(anchor);
            }
        }

        public void ShowFlyout(Rectangle? anchor = null)
        {
            Screen scr = Screen.PrimaryScreen;
            Rectangle wa = scr.WorkingArea;
            int x = wa.Right - this.Width - 14;
            int y = wa.Bottom - this.Height - 12;

            if (anchor.HasValue)
            {
                Rectangle a = anchor.Value;
                // Align center horizontally with anchor chip, clamped to screen
                x = a.Left + (a.Width - this.Width) / 2;
                if (x < wa.Left + 12) x = wa.Left + 12;
                if (x + this.Width > wa.Right - 12) x = wa.Right - this.Width - 12;

                // Position above or below anchor
                if (a.Bottom + 12 + this.Height <= wa.Bottom)
                {
                    y = a.Bottom + 12;
                }
                else
                {
                    y = a.Top - 12 - this.Height;
                }

                if (y < wa.Top + 8) y = wa.Top + 8;
                if (y + this.Height > wa.Bottom - 8) y = wa.Bottom - this.Height - 8;
            }

            this.Location = new Point(x, y);
            this.Show();
            this.Activate();
            this.BringToFront();
            this.Invalidate();
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            _lastDeactivateTick = DateTime.UtcNow.Ticks;
            this.Hide();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Hide();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            bool isId = (_trayManager.CurrentLanguage == AppLanguage.Indonesian);

            // ==========================================
            // DESIGN TOKENS (Dark & Light Mode)
            // ==========================================
            Color panelBg = _isDarkMode ? Color.FromArgb(28, 29, 32) : Color.FromArgb(250, 252, 255);
            Color borderColor = _isDarkMode ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
            Color textMain = _isDarkMode ? Color.White : Color.FromArgb(22, 24, 29);
            Color textSub = _isDarkMode ? Color.FromArgb(178, 255, 255, 255) : Color.FromArgb(173, 22, 24, 29);
            Color valueColor = _isDarkMode ? Color.FromArgb(140, 194, 255) : Color.FromArgb(10, 91, 184);
            Color tileBg = _isDarkMode ? Color.FromArgb(20, 255, 255, 255) : Color.FromArgb(13, 0, 0, 0);
            Color tileHover = _isDarkMode ? Color.FromArgb(35, 255, 255, 255) : Color.FromArgb(22, 0, 0, 0);
            Color cardBg = _isDarkMode ? Color.FromArgb(15, 255, 255, 255) : Color.FromArgb(10, 0, 0, 0);
            Color iconBoxBg = _isDarkMode ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(15, 0, 0, 0);
            Color dividerColor = _isDarkMode ? Color.FromArgb(20, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
            Color trackBg = _isDarkMode ? Color.FromArgb(41, 255, 255, 255) : Color.FromArgb(33, 0, 0, 0);
            Color closeBtnBg = _isDarkMode ? Color.FromArgb(20, 255, 255, 255) : Color.FromArgb(15, 0, 0, 0);
            Color ghostBtnBg = _isDarkMode ? Color.FromArgb(26, 255, 255, 255) : Color.FromArgb(15, 0, 0, 0);
            Color ghostHover = _isDarkMode ? Color.FromArgb(40, 255, 255, 255) : Color.FromArgb(25, 0, 0, 0);
            Color swoffBg = _isDarkMode ? Color.FromArgb(56, 255, 255, 255) : Color.FromArgb(56, 0, 0, 0);
            Color quitBg = _isDarkMode ? Color.FromArgb(51, 255, 99, 88) : Color.FromArgb(31, 220, 53, 40);
            Color quitHover = _isDarkMode ? Color.FromArgb(70, 255, 99, 88) : Color.FromArgb(45, 220, 53, 40);
            Color quitFg = _isDarkMode ? Color.FromArgb(255, 185, 178) : Color.FromArgb(179, 38, 30);
            Color accentColor = Color.FromArgb(10, 111, 214); // #0A6FD6
            Color onAccentColor = Color.White;

            // 1. Panel Background & Outline (Radius 22)
            using (GraphicsPath panelPath = VectorIcons.CreateRoundedRect(new RectangleF(0.5f, 0.5f, w - 1, h - 1), 22f))
            {
                using (SolidBrush pb = new SolidBrush(panelBg))
                {
                    g.FillPath(pb, panelPath);
                }
                using (Pen bp = new Pen(borderColor, 1f))
                {
                    g.DrawPath(bp, panelPath);
                }
            }

            // 2. Header
            using (SolidBrush tb = new SolidBrush(textMain))
            {
                g.DrawString("BoWeather Control Center", _titleFont, tb, 16, 21);
            }

            // Header Mode Toggle Button (Moon / Sun)
            Color modeBg = _hoverMode ? ghostHover : closeBtnBg;
            using (GraphicsPath mp = VectorIcons.CreateRoundedRect(new RectangleF(_modeBtnRect.X, _modeBtnRect.Y, _modeBtnRect.Width, _modeBtnRect.Height), 10f))
            using (SolidBrush mb = new SolidBrush(modeBg))
            {
                g.FillPath(mb, mp);
            }
            VectorIcons.DrawIcon(g, _isDarkMode ? "moon" : "sun", _modeBtnRect.X + 16, _modeBtnRect.Y + 16, 17f, textMain);

            // Header Close Button (✕)
            Color closeBg = _hoverClose ? ghostHover : closeBtnBg;
            using (GraphicsPath cp = VectorIcons.CreateRoundedRect(new RectangleF(_closeBtnRect.X, _closeBtnRect.Y, _closeBtnRect.Width, _closeBtnRect.Height), 10f))
            using (SolidBrush cb = new SolidBrush(closeBg))
            {
                g.FillPath(cb, cp);
            }
            VectorIcons.DrawIcon(g, "close", _closeBtnRect.X + 16, _closeBtnRect.Y + 16, 16f, textMain);

            // 3. Grid Tiles: Themes (Sakura, Salju, Daun Gugur, Hujan, Kunang-kunang) & Jeda
            EffectType curEffect = _overlay.Particles.CurrentEffect;
            bool isPaused = _overlay.IsPaused;

            string[] tileNames = new string[] { "sakura", "snow", "autumn", "rain", "fly", isPaused ? "play" : "pause" };
            string[] tileLabelsId = new string[] { "Sakura", "Salju", "Daun Gugur", "Hujan", "Kunang-kunang", isPaused ? "Lanjut" : "Jeda" };
            string[] tileLabelsEn = new string[] { "Sakura", "Snow", "Autumn Leaves", "Rain", "Fireflies", isPaused ? "Resume" : "Pause" };
            string[] tileLabels = isId ? tileLabelsId : tileLabelsEn;

            for (int i = 0; i < 6; i++)
            {
                Rectangle tr = _tileRects[i];
                bool isActive = false;
                if (i == 0 && curEffect == EffectType.Sakura && !isPaused) isActive = true;
                else if (i == 1 && curEffect == EffectType.Snow && !isPaused) isActive = true;
                else if (i == 2 && curEffect == EffectType.AutumnLeaves && !isPaused) isActive = true;
                else if (i == 3 && curEffect == EffectType.Rain && !isPaused) isActive = true;
                else if (i == 4 && curEffect == EffectType.Fireflies && !isPaused) isActive = true;
                else if (i == 5 && isPaused) isActive = true;

                bool isHover = (_hoveredTile == i);
                Color currentTileBg = isActive ? accentColor : (isHover ? tileHover : tileBg);
                Color currentTileFg = isActive ? onAccentColor : textMain;

                using (GraphicsPath tp = VectorIcons.CreateRoundedRect(new RectangleF(tr.X, tr.Y, tr.Width, tr.Height), 16f))
                using (SolidBrush tb = new SolidBrush(currentTileBg))
                {
                    g.FillPath(tb, tp);
                }

                // Vector Icon 26 px
                float iconCx = tr.Left + tr.Width / 2f;
                float iconCy = tr.Top + 24f;
                VectorIcons.DrawIcon(g, tileNames[i], iconCx, iconCy, 26f, currentTileFg);

                // Label below icon (12px bold, nowrap)
                using (SolidBrush lb = new SolidBrush(currentTileFg))
                {
                    StringFormat sfCenter = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    Rectangle lblBox = new Rectangle(tr.Left + 2, tr.Top + 42, tr.Width - 4, 20);
                    g.DrawString(tileLabels[i], _tileLabelFont, lb, lblBox, sfCenter);
                }
            }

            // 4. Sliders: Kekuatan Angin & Kepadatan Partikel
            // Wind labels: <34 Tenang/Calm, 34-66 Sepoi-sepoi/Breeze, >=67 Kencang/Windy
            string windValStr = (_windValue < 34) ? (isId ? "Tenang" : "Calm") :
                                (_windValue < 67) ? (isId ? "Sepoi-sepoi" : "Breeze") : (isId ? "Kencang" : "Windy");

            // Density labels: <34 Jarang/Sparse, 34-66 Sedang/Medium, >=67 Padat/Dense
            string densValStr = (_densityValue < 34) ? (isId ? "Jarang" : "Sparse") :
                                (_densityValue < 67) ? (isId ? "Sedang" : "Medium") : (isId ? "Padat" : "Dense");

            // Slider 1: Wind
            float windLabelY = _windTrackRect.Top - 24;
            VectorIcons.DrawIcon(g, "wind", 18 + 10, windLabelY + 12, 20f, textMain);
            using (SolidBrush lb = new SolidBrush(textMain))
            {
                g.DrawString(isId ? "Kekuatan Angin" : "Wind Strength", _labelFont, lb, 18 + 26, windLabelY + 2);
            }
            using (SolidBrush vb = new SolidBrush(valueColor))
            {
                StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(windValStr, _sliderValFont, vb, w - 18, windLabelY + 3, sfRight);
            }
            DrawModernSlider(g, _windTrackRect, _windValue, trackBg, accentColor);

            // Slider 2: Density
            float densLabelY = _densityTrackRect.Top - 24;
            VectorIcons.DrawIcon(g, "density", 18 + 10, densLabelY + 12, 20f, textMain);
            using (SolidBrush lb = new SolidBrush(textMain))
            {
                g.DrawString(isId ? "Kepadatan Partikel" : "Particle Density", _labelFont, lb, 18 + 26, densLabelY + 2);
            }
            using (SolidBrush vb = new SolidBrush(valueColor))
            {
                StringFormat sfRight = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString(densValStr, _sliderValFont, vb, w - 18, densLabelY + 3, sfRight);
            }
            DrawModernSlider(g, _densityTrackRect, _densityValue, trackBg, accentColor);

            // 5. Toggles Unified Card (Radius 18)
            using (GraphicsPath cp = VectorIcons.CreateRoundedRect(new RectangleF(_togglesContainerRect.X, _togglesContainerRect.Y, _togglesContainerRect.Width, _togglesContainerRect.Height), 18f))
            using (SolidBrush cb = new SolidBrush(cardBg))
            {
                g.FillPath(cb, cp);
            }

            bool physicsOn = _overlay.Particles.EnableWindowPhysics;
            bool autoPauseOn = _overlay.AutoPauseOnFullscreenGame;
            bool autoWallOn = _trayManager.IsAutoWallpaperEnabled;
            bool autoPlowOn = _overlay.Particles.Snowplow != null && _overlay.Particles.Snowplow.AutoPatrolEnabled;

            // Row 0: Window Physics
            DrawToggleRow(g, _physicsRowRect, "physics", "Window Physics",
                isId ? "Partikel bertengger di window" : "Particles rest on windows",
                physicsOn, _hoveredRow == 0, iconBoxBg, swoffBg, accentColor, textMain, textSub);

            // Divider 1
            using (Pen divPen = new Pen(dividerColor, 1f))
            {
                g.DrawLine(divPen, _togglesContainerRect.Left + 12, _physicsRowRect.Bottom, _togglesContainerRect.Right - 12, _physicsRowRect.Bottom);
            }

            // Row 1: Auto-Pause Game
            DrawToggleRow(g, _autoPauseRowRect, "gamepad", "Auto-Pause Game",
                isId ? "Hemat daya saat fullscreen / main game" : "Power saving during fullscreen games",
                autoPauseOn, _hoveredRow == 1, iconBoxBg, swoffBg, accentColor, textMain, textSub);

            // Divider 2
            using (Pen divPen = new Pen(dividerColor, 1f))
            {
                g.DrawLine(divPen, _togglesContainerRect.Left + 12, _autoPauseRowRect.Bottom, _togglesContainerRect.Right - 12, _autoPauseRowRect.Bottom);
            }

            // Row 2: Wallpaper 8 Waktu
            DrawToggleRow(g, _autoWallpaperRowRect, "clock", isId ? "Wallpaper 8 Waktu" : "Auto Wallpaper (8 Time Slots)",
                isId ? "Ganti otomatis ikut tema & waktu" : "Auto-switch wallpaper by time",
                autoWallOn, _hoveredRow == 2, iconBoxBg, swoffBg, accentColor, textMain, textSub);

            // Divider 3
            using (Pen divPen = new Pen(dividerColor, 1f))
            {
                g.DrawLine(divPen, _togglesContainerRect.Left + 12, _autoWallpaperRowRect.Bottom, _togglesContainerRect.Right - 12, _autoWallpaperRowRect.Bottom);
            }

            // Row 3: Truk Salju Otomatis
            DrawToggleRow(g, _snowplowRowRect, "snowplow", isId ? "Truk Salju Otomatis" : "Auto Snowplow Patrol",
                isId ? "Bersihkan salju taskbar berkala" : "Auto-clear taskbar snow periodically",
                autoPlowOn, _hoveredRow == 3, iconBoxBg, swoffBg, accentColor, textMain, textSub);

            // 5.5. Feature Buttons Row: Christmas Decor | Summon Snowplow
            Color xmasBg = _isDarkMode ? Color.FromArgb(200, 20, 80, 48) : Color.FromArgb(240, 220, 248, 230);
            Color xmasHover = _isDarkMode ? Color.FromArgb(250, 28, 105, 62) : Color.FromArgb(255, 200, 240, 215);
            Color xmasFg = _isDarkMode ? Color.White : Color.FromArgb(255, 15, 80, 35);
            DrawActionButton(g, _btnChristmasRect, "tree", isId ? "Dekorasi Natal…" : "Christmas…",
                xmasBg, xmasFg, _hoveredBtn == 4 ? xmasHover : xmasBg, false);

            bool plowActive = _overlay.Particles.Snowplow != null && _overlay.Particles.Snowplow.IsActive;
            Color plowBg = _isDarkMode ? Color.FromArgb(215, 180, 95, 10) : Color.FromArgb(245, 254, 240, 195);
            Color plowHover = _isDarkMode ? Color.FromArgb(255, 217, 119, 6) : Color.FromArgb(255, 253, 230, 155);
            Color plowFg = _isDarkMode ? Color.White : Color.FromArgb(255, 146, 64, 14);
            string plowBtnText = plowActive
                ? (isId ? "Truk Turbo!" : "Turbo Plow!")
                : (isId ? "Panggil Truk" : "Call Snowplow");
            DrawActionButton(g, _btnSnowplowRect, "snowplow", plowBtnText,
                plowBg, plowFg, _hoveredBtn == 5 ? plowHover : plowBg, false);

            // 6. Action Buttons 2x2 Grid (Radius 14)
            // Button 0: Terapkan Wallpaper (Accent Blue, turns into "Diterapkan" on click)
            string applyText = _appliedWallpaper ? (isId ? "Diterapkan" : "Applied") : (isId ? "Terapkan Wallpaper" : "Apply Wallpaper");
            DrawActionButton(g, _btnApplyWallRect, "image", applyText, accentColor, onAccentColor, _hoveredBtn == 0 ? Color.FromArgb(20, 130, 235) : accentColor, false);

            // Button 1: Pilih Gambar...
            DrawActionButton(g, _btnBrowseWallRect, "folder", isId ? "Pilih Gambar…" : "Browse Image…", ghostBtnBg, textMain, _hoveredBtn == 1 ? ghostHover : ghostBtnBg, false);

            // Button 2: Bahasa
            DrawActionButton(g, _btnLangRect, "globe", isId ? "Bahasa: ID" : "Language: EN", ghostBtnBg, textMain, _hoveredBtn == 2 ? ghostHover : ghostBtnBg, false);

            // Button 3: Keluar (Danger Red)
            DrawActionButton(g, _btnExitRect, "close", isId ? "Keluar" : "Exit", quitBg, quitFg, _hoveredBtn == 3 ? quitHover : quitBg, true);
        }

        private void DrawModernSlider(Graphics g, Rectangle r, int value, Color trackBg, Color fillAccent)
        {
            float trackH = 8f;
            float trackY = r.Top + (r.Height - trackH) / 2f;
            float ratio = Math.Max(0f, Math.Min(1f, value / 100f));
            float fillW = r.Width * ratio;

            // Background Track (Radius 4)
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(new RectangleF(r.Left, trackY, r.Width, trackH), 4f))
            using (SolidBrush bb = new SolidBrush(trackBg))
            {
                g.FillPath(bb, bp);
            }

            // Filled Accent Bar
            if (fillW > 2)
            {
                using (GraphicsPath fp = VectorIcons.CreateRoundedRect(new RectangleF(r.Left, trackY, fillW, trackH), 4f))
                using (SolidBrush fb = new SolidBrush(fillAccent))
                {
                    g.FillPath(fb, fp);
                }
            }

            // White 22px Round Thumb Knob with Drop Shadow
            float thumbR = 11f;
            float thumbCx = r.Left + fillW;
            float thumbCy = r.Top + r.Height / 2f;

            // Soft Shadow
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
            {
                g.FillEllipse(sb, thumbCx - thumbR - 1f, thumbCy - thumbR + 1.5f, (thumbR + 1f) * 2f, (thumbR + 1f) * 2f);
            }

            // White Knob
            using (SolidBrush wb = new SolidBrush(Color.White))
            {
                g.FillEllipse(wb, thumbCx - thumbR, thumbCy - thumbR, thumbR * 2f, thumbR * 2f);
            }
        }

        private void DrawToggleRow(Graphics g, Rectangle r, string iconName, string title, string sub, bool isOn, bool isHover,
            Color iconBoxBg, Color swoffBg, Color accentColor, Color textMain, Color textSub)
        {
            if (isHover)
            {
                using (GraphicsPath hp = VectorIcons.CreateRoundedRect(new RectangleF(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4), 12f))
                using (SolidBrush hb = new SolidBrush(Color.FromArgb(12, _isDarkMode ? Color.White : Color.Black)))
                {
                    g.FillPath(hb, hp);
                }
            }

            // Left Icon Box: 36x36 px, radius 11 px
            int boxSize = 36;
            int boxX = r.Left + 12;
            int boxY = r.Top + (r.Height - boxSize) / 2;
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(new RectangleF(boxX, boxY, boxSize, boxSize), 11f))
            using (SolidBrush bb = new SolidBrush(iconBoxBg))
            {
                g.FillPath(bb, bp);
            }

            // Icon 20px
            VectorIcons.DrawIcon(g, iconName, boxX + boxSize / 2f, boxY + boxSize / 2f, 20f, textMain);

            // Title & Subtitle
            int textX = boxX + boxSize + 12;
            using (SolidBrush tb = new SolidBrush(textMain))
            {
                g.DrawString(title, _labelFont, tb, textX, r.Top + 6);
            }
            using (SolidBrush sb = new SolidBrush(textSub))
            {
                g.DrawString(sub, _subLabelFont, sb, textX, r.Top + 26);
            }

            // iOS / Win11 Modern Switch: 44x26 px, radius 13 px
            int swW = 44;
            int swH = 26;
            int swX = r.Right - swW - 12;
            int swY = r.Top + (r.Height - swH) / 2;
            RectangleF swRect = new RectangleF(swX, swY, swW, swH);

            Color swBg = isOn ? accentColor : swoffBg;
            using (GraphicsPath sp = VectorIcons.CreateRoundedRect(swRect, 13f))
            using (SolidBrush sb = new SolidBrush(swBg))
            {
                g.FillPath(sb, sp);
            }

            // White Knob: 20x20 px
            float knobR = 10f;
            float knobX = isOn ? (swRect.Right - knobR - 3f) : (swRect.Left + knobR + 3f);
            float knobY = swRect.Top + swH / 2f;

            // Shadow
            using (SolidBrush sh = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
            {
                g.FillEllipse(sh, knobX - knobR, knobY - knobR + 1f, knobR * 2f, knobR * 2f);
            }
            // Knob
            using (SolidBrush kb = new SolidBrush(Color.White))
            {
                g.FillEllipse(kb, knobX - knobR, knobY - knobR, knobR * 2f, knobR * 2f);
            }
        }

        private void DrawActionButton(Graphics g, Rectangle r, string iconName, string text, Color bgCol, Color fgCol, Color currentBg, bool isDanger)
        {
            using (GraphicsPath bp = VectorIcons.CreateRoundedRect(new RectangleF(r.X, r.Y, r.Width, r.Height), 14f))
            using (SolidBrush bb = new SolidBrush(currentBg))
            {
                g.FillPath(bb, bp);
            }

            // Measure & Center Icon + Text
            SizeF textSize = g.MeasureString(text, _btnFont);
            float iconW = 18f;
            float gap = 8f;
            float totalW = iconW + gap + textSize.Width;
            float startX = r.Left + (r.Width - totalW) / 2f;
            float midY = r.Top + r.Height / 2f;

            VectorIcons.DrawIcon(g, iconName, startX + iconW / 2f, midY, 18f, fgCol);

            using (SolidBrush tb = new SolidBrush(fgCol))
            {
                g.DrawString(text, _btnFont, tb, startX + iconW + gap, midY - textSize.Height / 2f);
            }
        }

        // ==========================================
        //  MOUSE INTERACTION & EVENTS
        // ==========================================

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Point pt = e.Location;
            bool needRepaint = false;

            // Mode button
            bool isMode = _modeBtnRect.Contains(pt);
            if (isMode != _hoverMode) { _hoverMode = isMode; needRepaint = true; }

            // Close button
            bool isClose = _closeBtnRect.Contains(pt);
            if (isClose != _hoverClose) { _hoverClose = isClose; needRepaint = true; }

            // Tiles
            int newTile = -1;
            for (int i = 0; i < 6; i++)
            {
                if (_tileRects[i].Contains(pt)) { newTile = i; break; }
            }
            if (newTile != _hoveredTile) { _hoveredTile = newTile; needRepaint = true; }

            // Sliders drag
            if (_draggingWind && e.Button == MouseButtons.Left)
            {
                UpdateWindFromMouse(pt.X);
                needRepaint = true;
            }
            else if (_draggingDensity && e.Button == MouseButtons.Left)
            {
                UpdateDensityFromMouse(pt.X);
                needRepaint = true;
            }

            // Toggles
            int newRow = -1;
            if (_physicsRowRect.Contains(pt)) newRow = 0;
            else if (_autoPauseRowRect.Contains(pt)) newRow = 1;
            else if (_autoWallpaperRowRect.Contains(pt)) newRow = 2;
            else if (_snowplowRowRect.Contains(pt)) newRow = 3;
            if (newRow != _hoveredRow) { _hoveredRow = newRow; needRepaint = true; }

            // Buttons
            int newBtn = -1;
            if (_btnApplyWallRect.Contains(pt)) newBtn = 0;
            else if (_btnBrowseWallRect.Contains(pt)) newBtn = 1;
            else if (_btnLangRect.Contains(pt)) newBtn = 2;
            else if (_btnExitRect.Contains(pt)) newBtn = 3;
            else if (_btnChristmasRect.Contains(pt)) newBtn = 4;
            else if (_btnSnowplowRect.Contains(pt)) newBtn = 5;
            if (newBtn != _hoveredBtn) { _hoveredBtn = newBtn; needRepaint = true; }

            // Cursor
            if (_hoverMode || _hoverClose || _hoveredTile >= 0 || _hoveredRow >= 0 || _hoveredBtn >= 0 ||
                _windTrackRect.Contains(pt) || _densityTrackRect.Contains(pt))
            {
                Cursor = Cursors.Hand;
            }
            else
            {
                Cursor = Cursors.Default;
            }

            if (needRepaint) Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Point pt = e.Location;

            // Mode Toggle
            if (_modeBtnRect.Contains(pt))
            {
                _isDarkMode = !_isDarkMode;
                SaveThemeMode();
                UpdateThemeAppearance();
                _trayManager.OnThemeModeChanged();
                Invalidate();
                return;
            }

            // Close
            if (_closeBtnRect.Contains(pt))
            {
                this.Hide();
                return;
            }

            // Tiles
            for (int i = 0; i < 6; i++)
            {
                if (_tileRects[i].Contains(pt))
                {
                    _appliedWallpaper = false;
                    if (i == 0) { _overlay.IsPaused = false; _trayManager.SetEffect(EffectType.Sakura); }
                    else if (i == 1) { _overlay.IsPaused = false; _trayManager.SetEffect(EffectType.Snow); }
                    else if (i == 2) { _overlay.IsPaused = false; _trayManager.SetEffect(EffectType.AutumnLeaves); }
                    else if (i == 3) { _overlay.IsPaused = false; _trayManager.SetEffect(EffectType.Rain); }
                    else if (i == 4) { _overlay.IsPaused = false; _trayManager.SetEffect(EffectType.Fireflies); }
                    else if (i == 5)
                    {
                        _overlay.IsPaused = !_overlay.IsPaused;
                        _trayManager.UpdatePauseState();
                    }

                    Invalidate();
                    return;
                }
            }

            // Wind Slider
            if (new Rectangle(_windTrackRect.Left - 10, _windTrackRect.Top - 8, _windTrackRect.Width + 20, _windTrackRect.Height + 16).Contains(pt))
            {
                _draggingWind = true;
                UpdateWindFromMouse(pt.X);
                Invalidate();
                return;
            }

            // Density Slider
            if (new Rectangle(_densityTrackRect.Left - 10, _densityTrackRect.Top - 8, _densityTrackRect.Width + 20, _densityTrackRect.Height + 16).Contains(pt))
            {
                _draggingDensity = true;
                UpdateDensityFromMouse(pt.X);
                Invalidate();
                return;
            }

            // Toggles
            if (_physicsRowRect.Contains(pt))
            {
                _overlay.Particles.EnableWindowPhysics = !_overlay.Particles.EnableWindowPhysics;
                Invalidate();
                return;
            }
            if (_autoPauseRowRect.Contains(pt))
            {
                _overlay.AutoPauseOnFullscreenGame = !_overlay.AutoPauseOnFullscreenGame;
                Invalidate();
                return;
            }
            if (_autoWallpaperRowRect.Contains(pt))
            {
                _trayManager.ToggleAutoWallpaper();
                Invalidate();
                return;
            }
            if (_snowplowRowRect.Contains(pt) && _overlay.Particles.Snowplow != null)
            {
                _overlay.Particles.Snowplow.AutoPatrolEnabled = !_overlay.Particles.Snowplow.AutoPatrolEnabled;
                Invalidate();
                return;
            }

            // Action Buttons
            if (_btnChristmasRect.Contains(pt))
            {
                OpenChristmasDecor();
                return;
            }
            if (_btnSnowplowRect.Contains(pt))
            {
                if (_overlay.Particles.CurrentEffect != EffectType.Snow)
                {
                    _trayManager.SetEffect(EffectType.Snow);
                }
                _overlay.IsPaused = false;
                _overlay.Particles.EnableWindowPhysics = true;
                _overlay.Particles.SummonSnowplow();
                Invalidate();
                return;
            }
            if (_btnApplyWallRect.Contains(pt))
            {
                _appliedWallpaper = true;
                _trayManager.ApplyCurrentWallpaper();
                Invalidate();
                return;
            }
            if (_btnBrowseWallRect.Contains(pt))
            {
                this.Hide();
                _trayManager.BrowseCustomWallpaper();
                return;
            }
            if (_btnLangRect.Contains(pt))
            {
                AppLanguage nextLang = (_trayManager.CurrentLanguage == AppLanguage.Indonesian) ? AppLanguage.English : AppLanguage.Indonesian;
                _trayManager.SetLanguage(nextLang);
                Invalidate();
                return;
            }
            if (_btnExitRect.Contains(pt))
            {
                this.Hide();
                _trayManager.ExitApp();
                return;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _draggingWind = false;
            _draggingDensity = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoveredTile = -1;
            _hoveredRow = -1;
            _hoveredBtn = -1;
            _hoverMode = false;
            _hoverClose = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        private void UpdateWindFromMouse(int mouseX)
        {
            float rel = (float)(mouseX - _windTrackRect.Left) / _windTrackRect.Width;
            _windValue = Math.Max(0, Math.Min(100, (int)(rel * 100f)));

            if (_windValue < 34) _trayManager.SetWind(WindLevel.Calm);
            else if (_windValue < 67) _trayManager.SetWind(WindLevel.Breeze);
            else _trayManager.SetWind(WindLevel.Windy);
        }

        private void UpdateDensityFromMouse(int mouseX)
        {
            float rel = (float)(mouseX - _densityTrackRect.Left) / _densityTrackRect.Width;
            _densityValue = Math.Max(0, Math.Min(100, (int)(rel * 100f)));

            if (_densityValue < 34) _trayManager.SetDensity(DensityLevel.Low);
            else if (_densityValue < 67) _trayManager.SetDensity(DensityLevel.Medium);
            else _trayManager.SetDensity(DensityLevel.High);
        }

        public void OpenChristmasDecor()
        {
            if (_overlay == null || _overlay.Christmas == null) return;
            bool isId = (_trayManager != null && _trayManager.CurrentLanguage == AppLanguage.Indonesian);
            using (ChristmasDecorForm form = new ChristmasDecorForm(_overlay.Christmas, _overlay, _isDarkMode, isId))
            {
                form.ShowDialog();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_titleFont != null) _titleFont.Dispose();
                if (_labelFont != null) _labelFont.Dispose();
                if (_subLabelFont != null) _subLabelFont.Dispose();
                if (_tileLabelFont != null) _tileLabelFont.Dispose();
                if (_sliderValFont != null) _sliderValFont.Dispose();
                if (_btnFont != null) _btnFont.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
