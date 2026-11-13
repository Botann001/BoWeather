using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using BoWeather.Particles;

namespace BoWeather
{
    public class OverlayForm : Form
    {
        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        private const uint GA_ROOT = 2;

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);
        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_LBUTTON = 0x01;

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE { public int cx; public int cy; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        private const byte AC_SRC_OVER = 0;
        private const byte AC_SRC_ALPHA = 1;
        private const int ULW_ALPHA = 2;

        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED;
                cp.ExStyle |= WS_EX_TRANSPARENT;
                cp.ExStyle |= WS_EX_TOOLWINDOW;
                cp.ExStyle |= WS_EX_NOACTIVATE;
                cp.ExStyle |= WS_EX_TOPMOST;
                return cp;
            }
        }

        private readonly Timer _renderTimer;
        private readonly WindowManager _windowManager;
        private readonly ParticleSystem _particleSystem;
        private readonly StringBuilder _fgClassBuffer = new StringBuilder(256);
        private Bitmap _backBuffer;
        private Graphics _bufferGraphics;
        private bool _isPaused = false;
        private bool _wasAutoPaused = false;
        private bool _autoPauseOnFullscreenGame = true;

        public ParticleSystem Particles { get { return _particleSystem; } }
        public WindowManager WindowMgr { get { return _windowManager; } }
        public bool IsPaused
        {
            get { return _isPaused; }
            set { _isPaused = value; }
        }

        public bool AutoPauseOnFullscreenGame
        {
            get { return _autoPauseOnFullscreenGame; }
            set
            {
                _autoPauseOnFullscreenGame = value;
                if (!_autoPauseOnFullscreenGame && _wasAutoPaused)
                {
                    _wasAutoPaused = false;
                }
            }
        }

        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;

            int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            if (vw <= 0 || vh <= 0)
            {
                Rectangle virtualScreen = SystemInformation.VirtualScreen;
                vx = virtualScreen.Left;
                vy = virtualScreen.Top;
                vw = virtualScreen.Width;
                vh = virtualScreen.Height;
            }

            Bounds = new Rectangle(vx, vy, vw, vh);

            _windowManager = new WindowManager(this.Handle, vw, vh);
            _particleSystem = new ParticleSystem(vw, vh);

            InitBuffers(vw, vh);

            _renderTimer = new Timer();
            _renderTimer.Interval = 16;
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
        }

        private void InitBuffers(int width, int height)
        {
            if (_bufferGraphics != null) _bufferGraphics.Dispose();
            if (_backBuffer != null) _backBuffer.Dispose();

            _backBuffer = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _bufferGraphics = Graphics.FromImage(_backBuffer);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (Bounds.Width > 0 && Bounds.Height > 0)
            {
                InitBuffers(Bounds.Width, Bounds.Height);
                if (_particleSystem != null)
                {
                    _particleSystem.SetScreenDimensions(Bounds.Width, Bounds.Height);
                }
                if (_windowManager != null)
                {
                    _windowManager.SetDimensions(Bounds.Width, Bounds.Height);
                }
            }
        }

        private bool CheckIfFullscreenGameActive()
        {
            IntPtr fgWnd = GetForegroundWindow();
            if (fgWnd == IntPtr.Zero || fgWnd == this.Handle)
            {
                return false;
            }

            IntPtr root = GetAncestor(fgWnd, GA_ROOT);
            if (root == IntPtr.Zero) root = fgWnd;
            if (root == this.Handle) return false;

            // Ignore desktop background and taskbar
            GetClassName(root, _fgClassBuffer, _fgClassBuffer.Capacity);
            string cls = _fgClassBuffer.ToString();
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd")
            {
                return false;
            }

            RECT rect;
            int hr = DwmGetWindowAttribute(root, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
            if (hr != 0)
            {
                if (!GetWindowRect(root, out rect)) return false;
            }

            Screen scr = Screen.FromHandle(root);
            if (scr == null) scr = Screen.PrimaryScreen;
            Rectangle scrBounds = scr.Bounds;

            // In Windows, a fullscreen game covers the entire physical monitor (taskbar hidden)
            if (rect.Left <= scrBounds.Left + 5 &&
                rect.Top <= scrBounds.Top + 5 &&
                rect.Right >= scrBounds.Right - 5 &&
                rect.Bottom >= scrBounds.Bottom - 5)
            {
                return true;
            }

            return false;
        }

        private void OnRenderTick(object sender, EventArgs e)
        {
            if (_isPaused) return;

            // Auto-pause when a fullscreen game is active
            if (_autoPauseOnFullscreenGame && CheckIfFullscreenGameActive())
            {
                if (!_wasAutoPaused)
                {
                    _wasAutoPaused = true;
                    _bufferGraphics.Clear(Color.FromArgb(0, 0, 0, 0));
                    RenderLayeredWindow();
                }
                return; // 0% CPU, 0% GPU while playing games!
            }

            _wasAutoPaused = false;

            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            _windowManager.RefreshWindows(false);

            Point cursor = Cursor.Position;
            int mouseX = cursor.X - Bounds.Left;
            int mouseY = cursor.Y - Bounds.Top;
            bool isMouseDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

            _particleSystem.Update(_windowManager, mouseX, mouseY, isMouseDown);

            _bufferGraphics.Clear(Color.FromArgb(0, 0, 0, 0));

            _particleSystem.Draw(_bufferGraphics, _windowManager);

            RenderLayeredWindow();
        }

        private void RenderLayeredWindow()
        {
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                hBitmap = _backBuffer.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memDc, hBitmap);

                SIZE size = new SIZE { cx = _backBuffer.Width, cy = _backBuffer.Height };
                POINT pointSrc = new POINT { x = 0, y = 0 };
                POINT pointDst = new POINT { x = Bounds.Left, y = Bounds.Top };

                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.BlendFlags = 0;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = AC_SRC_ALPHA;

                UpdateLayeredWindow(this.Handle, screenDc, ref pointDst, ref size, memDc, ref pointSrc, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) SelectObject(memDc, oldBitmap);
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                DeleteDC(memDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_renderTimer != null)
                {
                    _renderTimer.Stop();
                    _renderTimer.Dispose();
                }
                if (_bufferGraphics != null) _bufferGraphics.Dispose();
                if (_backBuffer != null) _backBuffer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
