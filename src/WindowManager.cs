using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace BoWeather
{
    public class WindowInfo
    {
        public IntPtr Handle;
        public Rectangle Bounds;
        public Rectangle PrevBounds;
        public bool HasMoved;
        public float VelocityX;
        public float VelocityY;
        public float Speed;

        // Dynamic snow height array across window width
        public float[] SnowHeights;
        public float[] MaxSnowHeights;
        public bool IsFlung;

        public bool HasSnow()
        {
            if (SnowHeights == null) return false;
            for (int i = 0; i < SnowHeights.Length; i++)
            {
                if (SnowHeights[i] > 1.2f) return true;
            }
            return false;
        }
    }

    public class DepartedSnowShelf
    {
        public float Left;
        public float Top;
        public float Width;
        public float[] SnowHeights;
        public float VelocityX;
    }

    public class WindowManager
    {
        private readonly List<DepartedSnowShelf> _departedSnowShelves = new List<DepartedSnowShelf>();

        public List<DepartedSnowShelf> TakeDepartedSnowShelves()
        {
            lock (_lock)
            {
                if (_departedSnowShelves.Count == 0) return null;
                List<DepartedSnowShelf> list = new List<DepartedSnowShelf>(_departedSnowShelves);
                _departedSnowShelves.Clear();
                return list;
            }
        }
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);
        private const uint GA_ROOT = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);
        private const uint GW_OWNER = 4;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        private const int GWL_STYLE = -16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_CHILD = 0x40000000;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_MAXIMIZE = 0x01000000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
        private const int DWMWA_CLOAKED = 14;

        private readonly IntPtr _selfHandle;
        private readonly List<WindowInfo> _windows = new List<WindowInfo>();
        private readonly List<Rectangle> _allAppBounds = new List<Rectangle>();
        private readonly object _lock = new object();
        private DateTime _lastScanTime = DateTime.MinValue;
        private int _screenWidth;
        private int _screenHeight;

        public List<Rectangle> AllAppBounds
        {
            get
            {
                lock (_lock)
                {
                    return new List<Rectangle>(_allAppBounds);
                }
            }
        }

        public float[] FloorSnowPoints;
        public float[] MaxFloorSnowPoints;

        public static bool IsWindowAliveAndVisible(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;
            if (!IsWindow(hWnd)) return false;
            if (!IsWindowVisible(hWnd)) return false;
            if (IsIconic(hWnd)) return false;

            int cloaked = 0;
            int hr = DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
            if (hr == 0 && cloaked != 0) return false;

            return true;
        }

        public List<WindowInfo> ActiveWindows
        {
            get
            {
                lock (_lock)
                {
                    List<WindowInfo> alive = new List<WindowInfo>(_windows.Count);
                    for (int i = 0; i < _windows.Count; i++)
                    {
                        if (IsWindowAliveAndVisible(_windows[i].Handle))
                        {
                            alive.Add(_windows[i]);
                        }
                    }
                    return alive;
                }
            }
        }

        public WindowManager(IntPtr selfHandle, int screenWidth, int screenHeight)
        {
            _selfHandle = selfHandle;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            InitFloorSnow(screenWidth);
        }

        public void SetDimensions(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            InitFloorSnow(screenWidth);
        }

        public void InitFloorSnow(int screenWidth)
        {
            int pts = Math.Max(40, screenWidth / 16);
            FloorSnowPoints = new float[pts];
            MaxFloorSnowPoints = new float[pts];
            for (int i = 0; i < pts; i++)
            {
                float t = (float)i / (pts - 1);
                // Soft rolling pillowy dunes across the taskbar/floor
                float h = 16f + 4.0f * (float)Math.Sin(t * Math.PI * 6.0) + 2.0f * (float)Math.Cos(t * Math.PI * 11.0);
                FloorSnowPoints[i] = h;
                MaxFloorSnowPoints[i] = h;
            }
        }

        private static void InitWindowSnow(WindowInfo cur)
        {
            float w = cur.Bounds.Width;
            float snowWidth = Math.Max(20f, w - 6f);
            int pts = Math.Max(35, (int)(snowWidth / 6.0f));
            cur.SnowHeights = new float[pts];
            cur.MaxSnowHeights = new float[pts];

            for (int p = 0; p < pts; p++)
            {
                float t = (float)p / (pts - 1);
                float xLocal = t * snowWidth;
                float distEdge = Math.Min(xLocal, snowWidth - xLocal);

                // Smooth rounded dome fillet at window corners (over ~18px)
                float fade = 1.0f;
                if (distEdge < 18f)
                {
                    float u = distEdge / 18f;
                    fade = (float)Math.Sin(u * Math.PI * 0.5);
                }

                // Thick, pillowy snow blanket: 15.5px to 21.5px tall (Snowy aesthetic)
                float wave = 17.0f + 3.0f * (float)Math.Sin(t * Math.PI * 3.0) + 1.5f * (float)Math.Cos(t * Math.PI * 6.0);
                float h = wave * fade;
                cur.MaxSnowHeights[p] = h;
                cur.SnowHeights[p] = 0f; // Starts clean! Accumulates naturally from snowfall
            }
        }

        private static void ResampleWindowSnow(WindowInfo cur, WindowInfo old)
        {
            float w = cur.Bounds.Width;
            float snowWidth = Math.Max(20f, w - 6f);
            int pts = Math.Max(35, (int)(snowWidth / 6.0f));
            cur.SnowHeights = new float[pts];
            cur.MaxSnowHeights = new float[pts];

            for (int p = 0; p < pts; p++)
            {
                float t = (float)p / (pts - 1);
                float xLocal = t * snowWidth;
                float distEdge = Math.Min(xLocal, snowWidth - xLocal);

                float fade = 1.0f;
                if (distEdge < 18f)
                {
                    float u = distEdge / 18f;
                    fade = (float)Math.Sin(u * Math.PI * 0.5);
                }

                float wave = 17.0f + 3.0f * (float)Math.Sin(t * Math.PI * 3.0) + 1.5f * (float)Math.Cos(t * Math.PI * 6.0);
                cur.MaxSnowHeights[p] = wave * fade;

                if (old != null && old.SnowHeights != null && old.SnowHeights.Length > 0)
                {
                    int oldIdx = (int)(t * (old.SnowHeights.Length - 1));
                    if (oldIdx >= 0 && oldIdx < old.SnowHeights.Length)
                    {
                        cur.SnowHeights[p] = Math.Min(cur.MaxSnowHeights[p], old.SnowHeights[oldIdx]);
                    }
                }
            }
        }

        public void RefreshWindows(bool force)
        {
            DateTime now = DateTime.Now;
            // Balanced 120ms interval for fluid window tracking with near-zero CPU overhead
            if (!force && (now - _lastScanTime).TotalMilliseconds < 120)
            {
                return;
            }
            _lastScanTime = now;

            List<WindowInfo> scanned = new List<WindowInfo>();
            List<Rectangle> coveredRects = new List<Rectangle>();
            List<Rectangle> scannedAppBounds = new List<Rectangle>();
            bool stopEnumeration = false;

            StringBuilder classNameBuffer = new StringBuilder(256);
            StringBuilder titleBuffer = new StringBuilder(256);

            EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
            {
                if (stopEnumeration) return false;
                if (hWnd == _selfHandle) return true;
                if (!IsWindowVisible(hWnd)) return true;
                if (IsIconic(hWnd)) return true;

                // Skip owned windows (popups, dialogs, dropdowns, tooltips)
                if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero) return true;

                // Skip tool windows and child windows
                int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                if ((exStyle & WS_EX_TOOLWINDOW) != 0) return true;

                int style = GetWindowLong(hWnd, GWL_STYLE);
                if ((style & WS_CHILD) != 0) return true;

                // Must have standard window caption or frame
                if ((style & WS_CAPTION) == 0 && (style & WS_THICKFRAME) == 0) return true;

                // Must have a title
                int titleLen = GetWindowTextLength(hWnd);
                if (titleLen == 0) return true;

                GetWindowText(hWnd, titleBuffer, titleBuffer.Capacity);
                string title = titleBuffer.ToString();
                if (title == "Snap Flyout" || title == "Snap Bar" || title == "DesktopWindowXamlSource" ||
                    title == "Program Manager" || title == "Task Switching" || title == "Windows Input Experience" ||
                    title == "Quick settings" || title == "Control Center" || title == "Notification Center")
                {
                    return true;
                }

                // Skip cloaked windows
                int cloaked = 0;
                int hr = DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
                if (hr == 0 && cloaked != 0) return true;

                // Skip desktop background, taskbar, snap overlays, edge UI, control center
                GetClassName(hWnd, classNameBuffer, classNameBuffer.Capacity);
                string className = classNameBuffer.ToString();
                if (className == "Progman" || className == "WorkerW" ||
                    className == "Shell_TrayWnd" || className == "Shell_SecondaryTrayWnd" ||
                    className.Contains("Snap") || className.Contains("Flyout") ||
                    className.Contains("Island") || className.Contains("EdgeUi") ||
                    className.Contains("ControlCenter") || className.Contains("NotificationCenter") ||
                    className == "Windows.UI.Core.CoreWindow")
                {
                    return true;
                }

                // Get window bounds
                RECT rect;
                int boundsHr = DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(RECT)));
                if (boundsHr != 0)
                {
                    if (!GetWindowRect(hWnd, out rect)) return true;
                }

                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                if (w < 250 || h < 150) return true;

                scannedAppBounds.Add(new Rectangle(rect.Left, rect.Top, w, h));

                // Check if this window is MAXIMIZED or FULLSCREEN
                bool isMaximized = IsZoomed(hWnd) || ((style & WS_MAXIMIZE) != 0);
                if (!isMaximized)
                {
                    // Size check: spans the entire screen
                    if (rect.Left <= 12 && rect.Top <= 12 && w >= _screenWidth - 40 && h >= _screenHeight - 90)
                    {
                        isMaximized = true;
                    }
                }

                if (isMaximized)
                {
                    // If a maximized window is reached in Z-order:
                    // Any window below it is completely occluded!
                    // Stop enumeration immediately.
                    stopEnumeration = true;
                    return false;
                }

                // CRITICAL: Floating window must have its titlebar on screen
                // Never put snow if Top < 15 (e.g. Snap Bar dropping from top bezel or window dragged off top)
                if (rect.Top < 15 || rect.Top >= _screenHeight - 100)
                {
                    return true;
                }

                // For floating windows:
                // Check if its top ledge (where snow sits) is occluded by ANY higher window in Z-order!
                Rectangle titleBar = new Rectangle(rect.Left, rect.Top, w, 35);
                for (int c = 0; c < coveredRects.Count; c++)
                {
                    if (coveredRects[c].IntersectsWith(titleBar))
                    {
                        // Its titlebar is covered by a window in front of it! Skip!
                        coveredRects.Add(new Rectangle(rect.Left, rect.Top, w, h));
                        return true;
                    }
                }

                // This is a visible floating window!
                WindowInfo info = new WindowInfo();
                info.Handle = hWnd;
                info.Bounds = new Rectangle(rect.Left, rect.Top, w, h);
                scanned.Add(info);
                coveredRects.Add(info.Bounds);

                // Limit active floating snow windows to prevent visual clutter
                if (scanned.Count >= 3)
                {
                    stopEnumeration = true;
                    return false;
                }

                return true;
            }, IntPtr.Zero);

            lock (_lock)
            {
                _allAppBounds.Clear();
                _allAppBounds.AddRange(scannedAppBounds);

                Dictionary<IntPtr, WindowInfo> prevMap = new Dictionary<IntPtr, WindowInfo>();
                for (int i = 0; i < _windows.Count; i++)
                {
                    prevMap[_windows[i].Handle] = _windows[i];
                }

                // Check for departed windows (closed, minimized, or occluded) that had accumulated snow!
                HashSet<IntPtr> scannedHandles = new HashSet<IntPtr>();
                for (int i = 0; i < scanned.Count; i++)
                {
                    scannedHandles.Add(scanned[i].Handle);
                }

                for (int i = 0; i < _windows.Count; i++)
                {
                    WindowInfo oldWin = _windows[i];
                    if (!scannedHandles.Contains(oldWin.Handle))
                    {
                        if (oldWin.SnowHeights != null && oldWin.SnowHeights.Length > 0)
                        {
                            float maxH = 0f;
                            for (int p = 0; p < oldWin.SnowHeights.Length; p++)
                            {
                                if (oldWin.SnowHeights[p] > maxH) maxH = oldWin.SnowHeights[p];
                            }

                            if (maxH > 1.0f)
                            {
                                DepartedSnowShelf shelf = new DepartedSnowShelf();
                                shelf.Left = oldWin.Bounds.Left;
                                shelf.Top = oldWin.Bounds.Top + 0.5f;
                                shelf.Width = oldWin.Bounds.Width;
                                shelf.SnowHeights = (float[])oldWin.SnowHeights.Clone();
                                shelf.VelocityX = oldWin.VelocityX;
                                _departedSnowShelves.Add(shelf);
                            }
                        }
                    }
                }

                for (int i = 0; i < scanned.Count; i++)
                {
                    WindowInfo cur = scanned[i];
                    WindowInfo old;
                    if (prevMap.TryGetValue(cur.Handle, out old))
                    {
                        cur.PrevBounds = old.Bounds;
                        cur.VelocityX = cur.Bounds.Left - old.Bounds.Left;
                        cur.VelocityY = cur.Bounds.Top - old.Bounds.Top;
                        cur.Speed = (float)Math.Sqrt(cur.VelocityX * cur.VelocityX + cur.VelocityY * cur.VelocityY);
                        cur.HasMoved = (cur.Speed > 1.5f);

                        // Shake detection: when user shakes or flings window / tabs (Speed > 30 px/tick)
                        if (cur.Speed > 30.0f)
                        {
                            cur.IsFlung = true;
                        }
                        else
                        {
                            cur.IsFlung = false;
                        }

                        // If window width is preserved, keep existing snow mounds
                        if (old.SnowHeights != null && Math.Abs(cur.Bounds.Width - old.Bounds.Width) < 20)
                        {
                            cur.SnowHeights = old.SnowHeights;
                            cur.MaxSnowHeights = old.MaxSnowHeights;
                        }
                        else if (old.SnowHeights != null)
                        {
                            ResampleWindowSnow(cur, old);
                        }
                        else
                        {
                            InitWindowSnow(cur);
                        }
                    }
                    else
                    {
                        cur.PrevBounds = cur.Bounds;
                        cur.HasMoved = false;
                        cur.VelocityX = 0;
                        cur.VelocityY = 0;
                        cur.Speed = 0;
                        cur.IsFlung = false;
                        InitWindowSnow(cur);
                    }
                }

                _windows.Clear();
                _windows.AddRange(scanned);
            }
        }

        public bool CheckLedgeCollision(float x, float y, float prevY, out IntPtr hitHandle, out int windowTop, out float hitOffsetX)
        {
            hitHandle = IntPtr.Zero;
            windowTop = 0;
            hitOffsetX = 0;

            lock (_lock)
            {
                for (int i = 0; i < _windows.Count; i++)
                {
                    WindowInfo win = _windows[i];
                    Rectangle b = win.Bounds;

                    float snowLeft = b.Left + 3f;
                    float snowWidth = b.Width - 6f;

                    if (x >= snowLeft && x <= snowLeft + snowWidth)
                    {
                        int top = b.Top;
                        if ((prevY <= top && y >= top - 2) || (Math.Abs(y - top) <= 5))
                        {
                            hitHandle = win.Handle;
                            windowTop = top;
                            hitOffsetX = x - b.Left;

                            // Falling snowflakes quickly and naturally rebuild snow height at that column and neighbors
                            if (win.SnowHeights != null && win.MaxSnowHeights != null)
                            {
                                float rel = (x - snowLeft) / snowWidth;
                                int col = (int)(rel * (win.SnowHeights.Length - 1));
                                if (col >= 0 && col < win.SnowHeights.Length)
                                {
                                    AddSnowWindow(win, col, 1.2f);
                                    if (col > 0) AddSnowWindow(win, col - 1, 0.8f);
                                    if (col < win.SnowHeights.Length - 1) AddSnowWindow(win, col + 1, 0.8f);
                                    if (col > 1) AddSnowWindow(win, col - 2, 0.4f);
                                    if (col < win.SnowHeights.Length - 2) AddSnowWindow(win, col + 2, 0.4f);
                                }
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void AddSnowWindow(WindowInfo win, int col, float amount)
        {
            win.SnowHeights[col] = Math.Min(win.MaxSnowHeights[col], win.SnowHeights[col] + amount);
        }

        public bool TryGetWindowCurrentBounds(IntPtr hWnd, out Rectangle currentBounds, out bool hasMoved)
        {
            currentBounds = Rectangle.Empty;
            hasMoved = false;

            lock (_lock)
            {
                for (int i = 0; i < _windows.Count; i++)
                {
                    if (_windows[i].Handle == hWnd)
                    {
                        currentBounds = _windows[i].Bounds;
                        hasMoved = _windows[i].HasMoved;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
