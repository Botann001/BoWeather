using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoWeather.Particles;

namespace BoWeather
{
    public class FloatingWidgetForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWCP_ROUND = 2;
        private const int CS_DROPSHADOW = 0x00020000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_TOPMOST | WS_EX_NOACTIVATE;
                return cp;
            }
        }

        private readonly TrayIconManager _trayManager;
        private readonly OverlayForm _overlay;
        private readonly Font _font;
        private readonly ToolTip _toolTip;

        private bool _isHovered = false;
        private bool _isDragging = false;
        private bool _wasControlCenterOpenOnMouseDown = false;
        private Point _mouseDownPos;
        private Point _formStartPos;

        public FloatingWidgetForm(OverlayForm overlay, TrayIconManager trayManager)
        {
            _overlay = overlay;
            _trayManager = trayManager;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            Size = new Size(116, 40);
            Cursor = Cursors.Hand;

            _font = CreateFont(14f, FontStyle.Bold);

            _toolTip = new ToolTip();
            _toolTip.SetToolTip(this, "Klik: Buka BoWeather Control Center\nGeser: Pindahkan tombol ini");

            // Initial position: Bottom-right, just above taskbar near the notification tray
            Screen scr = Screen.PrimaryScreen;
            Rectangle wa = scr.WorkingArea;
            Location = new Point(wa.Right - Width - 18, wa.Bottom - Height - 14);

            RecalculateSize();
            UpdateThemeAppearance();
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

        public void UpdateAppearance()
        {
            UpdateThemeAppearance();
            RecalculateSize();
            Invalidate();
        }

        private void UpdateThemeAppearance()
        {
            bool isDark = _trayManager != null ? _trayManager.IsDarkMode : true;
            BackColor = isDark ? Color.FromArgb(28, 29, 32) : Color.FromArgb(255, 255, 255);

            if (IsHandleCreated)
            {
                try
                {
                    int darkAttr = isDark ? 1 : 0;
                    DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkAttr, sizeof(int));
                    int pref = DWMWCP_ROUND;
                    DwmSetWindowAttribute(this.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, sizeof(int));
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                catch { }
            }
        }

        public void UpdateEffect()
        {
            RecalculateSize();
            Invalidate();
        }

        private void RecalculateSize()
        {
            string labelStr = GetCurrentLabel();
            Size size = TextRenderer.MeasureText(labelStr, _font);
            // padding 14 + icon 18 + gap 8 + text + gap 8 + chev 14 + padding 14 = size.Width + 76
            int desiredW = Math.Max(104, size.Width + 76);
            if (desiredW != Width)
            {
                int oldW = Width;
                this.Size = new Size(desiredW, 40);
                Screen scr = Screen.PrimaryScreen;
                if (Right > scr.WorkingArea.Right)
                {
                    this.Left = scr.WorkingArea.Right - desiredW - 8;
                }
            }
        }

        private string GetCurrentLabel()
        {
            bool isId = (_trayManager != null && _trayManager.CurrentLanguage == AppLanguage.Indonesian);
            if (_overlay != null && _overlay.IsPaused)
            {
                return isId ? "Jeda" : "Paused";
            }
            if (_overlay == null || _overlay.Particles == null) return isId ? "Salju" : "Snow";

            switch (_overlay.Particles.CurrentEffect)
            {
                case EffectType.Sakura: return "Sakura";
                case EffectType.Snow: return isId ? "Salju" : "Snow";
                case EffectType.AutumnLeaves: return isId ? "Daun Gugur" : "Autumn";
                case EffectType.Rain: return isId ? "Hujan" : "Rain";
                case EffectType.Fireflies: return isId ? "Kunang-kunang" : "Fireflies";
                default: return isId ? "Salju" : "Snow";
            }
        }

        private string GetCurrentIconName()
        {
            if (_overlay != null && _overlay.IsPaused) return "pause";
            if (_overlay == null || _overlay.Particles == null) return "snow";

            switch (_overlay.Particles.CurrentEffect)
            {
                case EffectType.Sakura: return "sakura";
                case EffectType.Snow: return "snow";
                case EffectType.AutumnLeaves: return "autumn";
                case EffectType.Rain: return "rain";
                case EffectType.Fireflies: return "fly";
                default: return "snow";
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateThemeAppearance();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateThemeAppearance();
            BringToFront();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                _wasControlCenterOpenOnMouseDown = (_trayManager != null && _trayManager.ControlCenter != null && _trayManager.ControlCenter.Visible);
                _isDragging = false;
                _mouseDownPos = Cursor.Position;
                _formStartPos = this.Location;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Button == MouseButtons.Left)
            {
                Point cur = Cursor.Position;
                int dx = cur.X - _mouseDownPos.X;
                int dy = cur.Y - _mouseDownPos.Y;

                if (!_isDragging && (Math.Abs(dx) > 4 || Math.Abs(dy) > 4))
                {
                    _isDragging = true;
                }

                if (_isDragging)
                {
                    this.Location = new Point(_formStartPos.X + dx, _formStartPos.Y + dy);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (!_isDragging)
                {
                    if (_wasControlCenterOpenOnMouseDown)
                    {
                        // It was already open when the user pressed this chip -> close it!
                        if (_trayManager != null && _trayManager.ControlCenter != null)
                        {
                            _trayManager.ControlCenter.Hide();
                        }
                    }
                    else
                    {
                        // It was closed -> open it!
                        if (_trayManager != null)
                        {
                            _trayManager.ToggleControlCenter(this.Bounds);
                        }
                    }
                }
                _isDragging = false;
                _wasControlCenterOpenOnMouseDown = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int w = ClientSize.Width;
            int h = ClientSize.Height;
            bool isDark = _trayManager != null ? _trayManager.IsDarkMode : true;

            // Design tokens from DESIGN-SPEC.md
            // Dark: chip rgba(28,29,32,.92), chipfg #FFFFFF
            // Light: chip rgba(255,255,255,.92), chipfg #16181D
            Color chipBg = isDark ? Color.FromArgb(28, 29, 32) : Color.FromArgb(255, 255, 255);
            Color chipFg = isDark ? Color.White : Color.FromArgb(22, 24, 29);
            Color chipBorder = _isHovered ? Color.FromArgb(10, 111, 214) :
                               (isDark ? Color.FromArgb(35, 255, 255, 255) : Color.FromArgb(22, 0, 0, 0));

            // 1. Chip background & outline (Height 40, Radius 14)
            RectangleF chipRect = new RectangleF(0.5f, 0.5f, w - 1, h - 1);
            using (GraphicsPath path = VectorIcons.CreateRoundedRect(chipRect, 14f))
            {
                using (SolidBrush brush = new SolidBrush(chipBg))
                {
                    g.FillPath(brush, path);
                }
                using (Pen pen = new Pen(chipBorder, 1.2f))
                {
                    g.DrawPath(pen, path);
                }
            }

            // 2. Weather vector icon (18px)
            string iconName = GetCurrentIconName();
            float iconCx = 14f + 9f; // pad 14 + r 9
            float iconCy = h / 2f;
            VectorIcons.DrawIcon(g, iconName, iconCx, iconCy, 18f, chipFg);

            // 3. Label text (14px Bold)
            string labelStr = GetCurrentLabel();
            using (SolidBrush textBrush = new SolidBrush(chipFg))
            {
                Rectangle textRect = new Rectangle(39, 0, w - 39 - 26, h);
                StringFormat sfCenter = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(labelStr, _font, textBrush, textRect, sfCenter);
            }

            // 4. Chevron 14px (opacity 0.7)
            float chevCx = w - 14f - 7f;
            float chevCy = h / 2f;
            Color chevColor = Color.FromArgb(178, chipFg);
            VectorIcons.DrawIcon(g, "chev", chevCx, chevCy, 14f, chevColor);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_toolTip != null) _toolTip.Dispose();
                if (_font != null) _font.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
