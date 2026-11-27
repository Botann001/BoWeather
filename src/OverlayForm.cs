using System;
using System.IO;
using System.Collections.Generic;
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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _mouseProc;
        private IntPtr _mouseHook = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);
        private const int VK_LBUTTON = 0x01;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        public TrayIconManager TrayManager { get; set; }

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
        private int _taskbarTop;
        private bool _isHookInteracting = false;
        private bool _isRightClickInteracting = false;
        private bool _isChristmasInteracting = false;
        private int _topmostCounter = 0;

        public ParticleSystem Particles { get { return _particleSystem; } }
        public WindowManager WindowMgr { get { return _windowManager; } }
        public ChristmasManager Christmas { get { return _particleSystem != null ? _particleSystem.Christmas : null; } }
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

            Screen primary = Screen.PrimaryScreen;
            Rectangle scrBounds = (primary != null) ? primary.Bounds : new Rectangle(0, 0, 1920, 1080);
            Bounds = scrBounds;
            int vw = scrBounds.Width;
            int vh = scrBounds.Height;

            _windowManager = new WindowManager(this.Handle, vw, vh);
            _particleSystem = new ParticleSystem(vw, vh);

            _taskbarTop = GetTaskbarTop();
            _particleSystem.TaskbarTop = _taskbarTop;

            try
            {
                _mouseProc = MouseHookCallback;
                IntPtr hMod = Marshal.GetHINSTANCE(typeof(OverlayForm).Module);
                _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, hMod, 0);
            }
            catch {}

            try
            {
                RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, (uint)Keys.W);
            }
            catch {}

            InitBuffers(vw, vh);

            _renderTimer = new Timer();
            _renderTimer.Interval = 16;
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                if (TrayManager != null)
                {
                    TrayManager.ToggleControlCenter();
                }
                return;
            }
            base.WndProc(ref m);
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

            // Ignore desktop background, taskbar, and shell flyouts
            GetClassName(root, _fgClassBuffer, _fgClassBuffer.Capacity);
            string cls = _fgClassBuffer.ToString();
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" ||
                cls.Contains("ControlCenter") || cls.Contains("Flyout") || cls.Contains("Island") ||
                cls == "Windows.UI.Core.CoreWindow")
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
            try
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
                        if (TrayManager != null) TrayManager.OnFullscreenStateChanged(true);
                    }
                    return; // 0% CPU, 0% GPU while playing games!
                }

                if (_wasAutoPaused)
                {
                    _wasAutoPaused = false;
                    if (TrayManager != null) TrayManager.OnFullscreenStateChanged(false);
                }
                else
                {
                    _wasAutoPaused = false;
                }

                _topmostCounter++;
                if (_topmostCounter >= 60)
                {
                    _topmostCounter = 0;
                    SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }

                _taskbarTop = GetTaskbarTop();
                _particleSystem.TaskbarTop = _taskbarTop;

                _windowManager.RefreshWindows(false);

                Point cursor = Cursor.Position;
                int mouseX = cursor.X - Bounds.Left;
                int mouseY = cursor.Y - Bounds.Top;
                bool isMouseDown = (_mouseHook == IntPtr.Zero) && ((GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0);

                if (_particleSystem.Christmas != null)
                {
                    _particleSystem.Christmas.UpdateHover(mouseX, mouseY);
                }

                _particleSystem.Update(_windowManager, mouseX, mouseY, isMouseDown);

                _bufferGraphics.Clear(Color.FromArgb(0, 0, 0, 0));

                _particleSystem.Draw(_bufferGraphics, _windowManager);

                RenderLayeredWindow();
            }
            catch { }
        }

        private static bool IsDesktopWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            StringBuilder sb = new StringBuilder(64);
            GetClassName(hWnd, sb, 64);
            string cls = sb.ToString();
            return (cls == "Progman" || cls == "WorkerW" || cls == "SHELLDLL_DefView" || cls == "SysListView32");
        }

        private static bool IsDesktopOrTaskbarWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            StringBuilder sb = new StringBuilder(64);
            GetClassName(hWnd, sb, 64);
            string cls = sb.ToString();
            return (cls == "Progman" || cls == "WorkerW" || cls == "SHELLDLL_DefView" || cls == "SysListView32" ||
                    cls == "Shell_TrayWnd" || cls == "Shell_SecondaryTrayWnd" || cls.Contains("Tray") || cls.Contains("Taskbar"));
        }

        private int GetTaskbarTop()
        {
            IntPtr hTaskbar = FindWindow("Shell_TrayWnd", null);
            if (hTaskbar != IntPtr.Zero)
            {
                RECT tbRect;
                if (GetWindowRect(hTaskbar, out tbRect) && tbRect.Top > 100 && tbRect.Top < Bounds.Height)
                {
                    return tbRect.Top - Bounds.Top;
                }
            }

            Screen scr = Screen.PrimaryScreen;
            if (scr != null)
            {
                int waBottom = scr.WorkingArea.Bottom - Bounds.Top;
                if (waBottom > 100 && waBottom <= Bounds.Height)
                {
                    return waBottom;
                }
            }

            return Bounds.Height - 48;
        }

        private bool IsPointOnSnowSurface(int x, int y)
        {
            if (_windowManager != null)
            {
                List<WindowInfo> windows = _windowManager.ActiveWindows;
                for (int w = 0; w < windows.Count; w++)
                {
                    WindowInfo win = windows[w];
                    if (win.SnowHeights == null || !win.HasSnow()) continue;
                    Rectangle b = win.Bounds;
                    if (x >= b.Left && x <= b.Right && y >= b.Top - 35 && y <= b.Top + 6)
                    {
                        return true;
                    }
                }

                if (_windowManager.FloorSnowPoints != null)
                {
                    int floorLevel = (_taskbarTop > 100 && _taskbarTop <= Bounds.Height) ? _taskbarTop : Bounds.Height;
                    if (y >= floorLevel - 110 && y <= Bounds.Height + 15)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && !_isPaused && _particleSystem != null && _particleSystem.CurrentEffect == EffectType.Snow)
                {
                    int msg = wParam.ToInt32();
                    MSLLHOOKSTRUCT hook = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));

                    // Multi-monitor fast-path: if cursor is on secondary monitor, pass through immediately!
                    if (!Bounds.Contains(hook.pt.x, hook.pt.y))
                    {
                        return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
                    }

                    int mouseX = hook.pt.x - Bounds.Left;
                    int mouseY = hook.pt.y - Bounds.Top;

                    if (msg == WM_LBUTTONDOWN)
                    {
                        // 1. Did the user click an existing Snowball / Snowman?
                        Snowball hit = _particleSystem.Snowballs.HitTest(mouseX, mouseY);
                        if (hit != null)
                        {
                            _isHookInteracting = true;
                            _particleSystem.Snowballs.StartDrag(hit, mouseX, mouseY);
                            return (IntPtr)1; // Consume event! No Desktop Selection Box!
                        }

                        // 1.2. Did the user click the Snowplow truck? (Left click reverses direction)
                        if (_particleSystem.Snowplow != null && _particleSystem.Snowplow.HitTest(mouseX, mouseY))
                        {
                            _particleSystem.Snowplow.ReverseDirection();
                            _particleSystem.SpawnSnowPuff(mouseX, mouseY, 12);
                            return (IntPtr)1;
                        }

                        // 1.5. Did the user click on a Christmas accessory (Delete button, Light handle, Tree, Star, or Gift)?
                        bool canInteractChristmas = true;
                        if (_particleSystem.Christmas != null && _particleSystem.Christmas.BehindActiveWindows)
                        {
                            IntPtr wndUnderCursor = WindowFromPoint(hook.pt);
                            IntPtr rootWnd = GetAncestor(wndUnderCursor, GA_ROOT);
                            if (rootWnd == IntPtr.Zero) rootWnd = wndUnderCursor;
                            if (!IsDesktopOrTaskbarWindow(rootWnd) && rootWnd != this.Handle)
                            {
                                canInteractChristmas = false;
                            }
                        }

                        if (canInteractChristmas && _particleSystem.Christmas != null)
                        {
                            int hitPart;
                            object xHit = _particleSystem.Christmas.HitTest(mouseX, mouseY, out hitPart);
                            if (xHit != null)
                            {
                                if (hitPart == ChristmasManager.PART_DELETE)
                                {
                                    _particleSystem.Christmas.RemoveItem(xHit);
                                    _particleSystem.SpawnSnowPuff(mouseX, mouseY, 18);
                                    return (IntPtr)1; // Consume event!
                                }
                                else
                                {
                                    _isChristmasInteracting = true;
                                    _particleSystem.Christmas.StartDrag(xHit, hitPart, mouseX, mouseY);
                                    return (IntPtr)1; // Consume event!
                                }
                            }
                        }

                        // 2. Did the user click on a snow surface?
                        if (IsPointOnSnowSurface(mouseX, mouseY))
                        {
                            IntPtr wndUnderCursor = WindowFromPoint(hook.pt);

                            // Allow rolling on desktop or taskbar, or on active floating window snow ledge
                            bool canRoll = false;
                            if (IsDesktopOrTaskbarWindow(wndUnderCursor) || (hook.pt.y >= _taskbarTop - 45 && hook.pt.y <= _taskbarTop + 25))
                            {
                                canRoll = true;
                            }
                            else
                            {
                                // Check if clicking the top snow ledge of an active floating window
                                List<WindowInfo> windows = _windowManager.ActiveWindows;
                                for (int w = 0; w < windows.Count; w++)
                                {
                                    WindowInfo win = windows[w];
                                    Rectangle b = win.Bounds;
                                    if (mouseX >= b.Left && mouseX <= b.Right && mouseY >= b.Top - 35 && mouseY <= b.Top + 6)
                                    {
                                        canRoll = true;
                                        break;
                                    }
                                }
                            }

                            if (canRoll)
                            {
                                _isHookInteracting = true;
                                _particleSystem.Snowballs.StartCandidateRoll(mouseX, mouseY);

                                // Consume event on desktop so Windows doesn't create desktop selection rectangle
                                if (IsDesktopWindow(wndUnderCursor))
                                {
                                    return (IntPtr)1;
                                }
                            }
                        }
                    }
                    else if (msg == WM_MOUSEMOVE)
                    {
                        if (_isHookInteracting)
                        {
                            int floorY = (_taskbarTop > 100 && _taskbarTop < Bounds.Height) ? _taskbarTop : Bounds.Height;
                            _particleSystem.Snowballs.OnMouseMove(mouseX, mouseY, _windowManager, Bounds.Width, floorY, _particleSystem.SpawnSnowPuff);
                        }
                        else if (_isChristmasInteracting && _particleSystem.Christmas != null)
                        {
                            int floorY = (_taskbarTop > 100 && _taskbarTop < Bounds.Height) ? _taskbarTop : Bounds.Height;
                            _particleSystem.Christmas.OnMouseMove(mouseX, mouseY, _windowManager, Bounds.Width, floorY, _particleSystem.SpawnSnowPuff);
                        }
                        // If user is not dragging, do ZERO work inside WH_MOUSE_LL hook for maximum cursor responsiveness!
                    }
                    else if (msg == WM_LBUTTONUP)
                    {
                        if (_isChristmasInteracting && _particleSystem.Christmas != null)
                        {
                            _isChristmasInteracting = false;
                            int floorY = (_taskbarTop > 100 && _taskbarTop < Bounds.Height) ? _taskbarTop : Bounds.Height;
                            _particleSystem.Christmas.OnMouseUp(mouseX, mouseY, _windowManager, floorY, _particleSystem.SpawnSnowPuff);
                            IntPtr wndUnderCursor = WindowFromPoint(hook.pt);
                            if (IsDesktopWindow(wndUnderCursor))
                            {
                                return (IntPtr)1;
                            }
                        }

                        if (_isHookInteracting)
                        {
                            _isHookInteracting = false;
                            int floorY = (_taskbarTop > 100 && _taskbarTop < Bounds.Height) ? _taskbarTop : Bounds.Height;
                            _particleSystem.Snowballs.OnMouseUp(mouseX, mouseY, _windowManager, floorY, _particleSystem.SpawnSnowPuff);
                            IntPtr wndUnderCursor = WindowFromPoint(hook.pt);
                            if (IsDesktopWindow(wndUnderCursor))
                            {
                                return (IntPtr)1; // Consume event so desktop doesn't receive mouse-up
                            }
                        }
                    }
                    else if (msg == WM_RBUTTONDOWN)
                    {
                        // Right-click on Christmas Accessory to immediately delete it!
                        bool canInteractChristmas = true;
                        if (_particleSystem.Christmas != null && _particleSystem.Christmas.BehindActiveWindows)
                        {
                            IntPtr wndUnderCursor = WindowFromPoint(hook.pt);
                            IntPtr rootWnd = GetAncestor(wndUnderCursor, GA_ROOT);
                            if (rootWnd == IntPtr.Zero) rootWnd = wndUnderCursor;
                            if (!IsDesktopOrTaskbarWindow(rootWnd) && rootWnd != this.Handle)
                            {
                                canInteractChristmas = false;
                            }
                        }

                        if (canInteractChristmas && _particleSystem.Christmas != null)
                        {
                            int hitPart;
                            object xHit = _particleSystem.Christmas.HitTest(mouseX, mouseY, out hitPart);
                            if (xHit != null)
                            {
                                _isRightClickInteracting = true;
                                _particleSystem.Christmas.RemoveItem(xHit);
                                _particleSystem.SpawnSnowPuff(mouseX, mouseY, 20);
                                return (IntPtr)1;
                            }
                        }

                        // Right-click Snowplow truck -> toggle Turbo boost!
                        if (_particleSystem.Snowplow != null && _particleSystem.Snowplow.HitTest(mouseX, mouseY))
                        {
                            _isRightClickInteracting = true;
                            _particleSystem.Snowplow.IsTurbo = !_particleSystem.Snowplow.IsTurbo;
                            _particleSystem.SpawnSnowPuff(mouseX, mouseY, 16);
                            return (IntPtr)1;
                        }

                        Snowball hit = _particleSystem.Snowballs.HitTest(mouseX, mouseY);
                        if (hit != null)
                        {
                            _isRightClickInteracting = true;
                            _particleSystem.Snowballs.ShatterBall(hit, _particleSystem.SpawnSnowPuff);
                            return (IntPtr)1; // Consume event! Shatter into flurry, no desktop context menu!
                        }

                        // Right-click on snow surface
                        if (IsPointOnSnowSurface(mouseX, mouseY))
                        {
                            IntPtr wndUnderCursor = WindowFromPoint(hook.pt);
                            if (IsDesktopWindow(wndUnderCursor))
                            {
                                _isRightClickInteracting = true;
                                _particleSystem.SpawnSnowPuff(mouseX, mouseY, 16);
                                return (IntPtr)1; // Consume event so no desktop context menu!
                            }
                        }
                    }
                    else if (msg == WM_RBUTTONUP)
                    {
                        if (_isRightClickInteracting)
                        {
                            _isRightClickInteracting = false;
                            return (IntPtr)1;
                        }
                    }
                }
            }
            catch { }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
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
                if (_mouseHook != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_mouseHook);
                    _mouseHook = IntPtr.Zero;
                }
                try
                {
                    UnregisterHotKey(this.Handle, HOTKEY_ID);
                }
                catch {}
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
