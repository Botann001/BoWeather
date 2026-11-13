using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BoWeather.Particles
{
    public class Snowball
    {
        public float X;
        public float Y;
        public float Radius;
        public float BaseRadius;
        public float TorsoRadius;
        public float HeadRadius;
        public int BallCount; // 1 = single ball, 2 = torso on base, 3 = full snowman!
        public bool IsSnowman;
        public float Angle;
        public float Vx;
        public float Vy;
        public bool IsRolling;
        public bool IsGrabbed;
        public bool IsFalling;
        public IntPtr AttachedWindow;
        public float WindowOffsetX;
        public float WindowOffsetY;
        public float GrabOffsetX;
        public float GrabOffsetY;

        public Snowball(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
            BaseRadius = radius;
            TorsoRadius = radius * 0.85f;
            HeadRadius = radius * 0.68f;
            BallCount = 1;
            IsSnowman = false;
            Angle = 0f;
            Vx = 0f;
            Vy = 0f;
            IsRolling = false;
            IsGrabbed = false;
            IsFalling = false;
            AttachedWindow = IntPtr.Zero;
        }

        public RectangleF GetBounds()
        {
            if (BallCount == 1)
            {
                return new RectangleF(X - BaseRadius, Y - BaseRadius, BaseRadius * 2f, BaseRadius * 2f);
            }
            else if (BallCount == 2)
            {
                float totalH = BaseRadius * 2f + TorsoRadius * 2f - 4f;
                return new RectangleF(X - BaseRadius, Y - totalH + BaseRadius, BaseRadius * 2f, totalH);
            }
            else
            {
                float totalH = BaseRadius * 2f + TorsoRadius * 2f + HeadRadius * 2f - 8f;
                return new RectangleF(X - BaseRadius, Y - totalH + BaseRadius, BaseRadius * 2f, totalH);
            }
        }
    }

    public class SnowballManager
    {
        private readonly List<Snowball> _snowballs = new List<Snowball>();
        private Snowball _draggedBall = null;
        private Snowball _rollingBall = null;
        private bool _prevMouseDown = false;
        private int _prevMouseX = -1;
        private int _prevMouseY = -1;
        private bool _isCandidateRolling = false;
        private float _rollStartX = 0f;
        private float _rollStartY = 0f;
        private readonly Random _rand = new Random();

        public List<Snowball> Snowballs { get { return _snowballs; } }

        public void Clear()
        {
            _snowballs.Clear();
            _draggedBall = null;
            _rollingBall = null;
            _isCandidateRolling = false;
        }

        public void Update(WindowManager winMgr, int mouseX, int mouseY, bool isMouseDown, int screenWidth, int screenHeight, Action<float, float, int> onSnowPuff)
        {
            bool mouseJustPressed = isMouseDown && !_prevMouseDown;
            bool mouseJustReleased = !isMouseDown && _prevMouseDown;
            float mouseDx = (_prevMouseX >= 0) ? (mouseX - _prevMouseX) : 0f;
            float mouseDy = (_prevMouseY >= 0) ? (mouseY - _prevMouseY) : 0f;

            // 1. Mouse Just Pressed: Check Grab or Candidate Roll
            if (mouseJustPressed && mouseX >= 0 && mouseY >= 0)
            {
                _draggedBall = null;
                _rollingBall = null;
                _isCandidateRolling = false;

                // Check hit on existing snowballs (from top to bottom of list)
                for (int i = _snowballs.Count - 1; i >= 0; i--)
                {
                    Snowball b = _snowballs[i];
                    RectangleF rect = b.GetBounds();
                    rect.Inflate(10f, 10f); // friendly grab margin

                    if (rect.Contains(mouseX, mouseY))
                    {
                        _draggedBall = b;
                        b.IsGrabbed = true;
                        b.IsFalling = false;
                        b.AttachedWindow = IntPtr.Zero;
                        b.GrabOffsetX = mouseX - b.X;
                        b.GrabOffsetY = mouseY - b.Y;
                        b.Vx = 0f;
                        b.Vy = 0f;
                        break;
                    }
                }

                // If not grabbing an existing ball, check if mouse is on a snow surface to roll a new ball
                if (_draggedBall == null && winMgr != null)
                {
                    bool onSnow = false;
                    // Check floating windows
                    List<WindowInfo> windows = winMgr.ActiveWindows;
                    for (int w = 0; w < windows.Count; w++)
                    {
                        WindowInfo win = windows[w];
                        if (win.SnowHeights == null) continue;
                        Rectangle b = win.Bounds;
                        if (mouseX >= b.Left && mouseX <= b.Right && mouseY >= b.Top - 25 && mouseY <= b.Top + 15)
                        {
                            onSnow = true;
                            break;
                        }
                    }

                    // Check floor / taskbar snow
                    if (!onSnow && winMgr.FloorSnowPoints != null && mouseY >= screenHeight - 50)
                    {
                        onSnow = true;
                    }

                    if (onSnow)
                    {
                        _isCandidateRolling = true;
                        _rollStartX = mouseX;
                        _rollStartY = mouseY;
                    }
                }
            }

            // 2. Mouse Held / Dragging
            if (isMouseDown && mouseX >= 0 && mouseY >= 0)
            {
                if (_draggedBall != null)
                {
                    _draggedBall.X = mouseX - _draggedBall.GrabOffsetX;
                    _draggedBall.Y = mouseY - _draggedBall.GrabOffsetY;
                    _draggedBall.Vx = mouseDx * 0.85f;
                    _draggedBall.Vy = mouseDy * 0.85f;
                }
                else if (_isCandidateRolling)
                {
                    // Start rolling once cursor moves horizontally over 6 pixels
                    if (Math.Abs(mouseX - _rollStartX) > 6f)
                    {
                        _isCandidateRolling = false;
                        float ledgeY = _rollStartY;
                        IntPtr attachWin = IntPtr.Zero;
                        WindowInfo candidateWin = null;

                        if (winMgr != null)
                        {
                            List<WindowInfo> windows = winMgr.ActiveWindows;
                            for (int w = 0; w < windows.Count; w++)
                            {
                                WindowInfo win = windows[w];
                                Rectangle b = win.Bounds;
                                if (mouseX >= b.Left && mouseX <= b.Right && mouseY >= b.Top - 25 && mouseY <= b.Top + 15)
                                {
                                    ledgeY = b.Top;
                                    attachWin = win.Handle;
                                    candidateWin = win;
                                    break;
                                }
                            }
                            if (attachWin == IntPtr.Zero && mouseY >= screenHeight - 50)
                            {
                                ledgeY = screenHeight;
                            }
                        }

                        Snowball newBall = new Snowball(mouseX, ledgeY - 8f, 8f);
                        newBall.IsRolling = true;
                        newBall.IsGrabbed = true;
                        newBall.AttachedWindow = attachWin;
                        if (candidateWin != null)
                        {
                            newBall.WindowOffsetX = newBall.X - candidateWin.Bounds.Left;
                            newBall.WindowOffsetY = candidateWin.Bounds.Top - newBall.Y;
                        }
                        _snowballs.Add(newBall);
                        _rollingBall = newBall;

                        if (onSnowPuff != null) onSnowPuff(newBall.X, newBall.Y, 5);
                    }
                }
                else if (_rollingBall != null)
                {
                    _rollingBall.X = mouseX;
                    _rollingBall.Angle += mouseDx * 0.15f;

                    // Grow snowball as it rolls through snow (up to 22px max base size)
                    if (_rollingBall.Radius < 22f)
                    {
                        _rollingBall.Radius += Math.Abs(mouseDx) * 0.14f;
                        _rollingBall.BaseRadius = _rollingBall.Radius;
                    }

                    // Keep resting on current surface ledge
                    if (winMgr != null)
                    {
                        List<WindowInfo> windows = winMgr.ActiveWindows;
                        bool onWin = false;
                        for (int w = 0; w < windows.Count; w++)
                        {
                            WindowInfo win = windows[w];
                            Rectangle b = win.Bounds;
                            if (mouseX >= b.Left && mouseX <= b.Right && mouseY >= b.Top - 35 && mouseY <= b.Top + 20)
                            {
                                _rollingBall.Y = b.Top - _rollingBall.Radius;
                                _rollingBall.AttachedWindow = win.Handle;
                                _rollingBall.WindowOffsetX = _rollingBall.X - b.Left;
                                _rollingBall.WindowOffsetY = b.Top - _rollingBall.Y;
                                onWin = true;

                                // Pack snow from ledge into ball!
                                if (win.SnowHeights != null && win.SnowHeights.Length > 1)
                                {
                                    float rel = (mouseX - (b.Left + 3f)) / (b.Width - 6f);
                                    int col = (int)(rel * (win.SnowHeights.Length - 1));
                                    if (col >= 0 && col < win.SnowHeights.Length)
                                    {
                                        win.SnowHeights[col] = Math.Max(0f, win.SnowHeights[col] - 3.5f);
                                        if (col > 0) win.SnowHeights[col - 1] = Math.Max(0f, win.SnowHeights[col - 1] - 2.0f);
                                        if (col < win.SnowHeights.Length - 1) win.SnowHeights[col + 1] = Math.Max(0f, win.SnowHeights[col + 1] - 2.0f);
                                    }
                                }
                                break;
                            }
                        }

                        if (!onWin && mouseY >= screenHeight - 50)
                        {
                            _rollingBall.Y = screenHeight - _rollingBall.Radius;
                            _rollingBall.AttachedWindow = IntPtr.Zero;
                            _rollingBall.WindowOffsetX = 0f;
                            _rollingBall.WindowOffsetY = 0f;

                            // Pack snow from floor into ball!
                            if (winMgr.FloorSnowPoints != null && winMgr.FloorSnowPoints.Length > 1)
                            {
                                float rel = (float)mouseX / (screenWidth - 1);
                                int col = (int)(rel * (winMgr.FloorSnowPoints.Length - 1));
                                if (col >= 0 && col < winMgr.FloorSnowPoints.Length)
                                {
                                    winMgr.FloorSnowPoints[col] = Math.Max(0f, winMgr.FloorSnowPoints[col] - 3.5f);
                                    if (col > 0) winMgr.FloorSnowPoints[col - 1] = Math.Max(0f, winMgr.FloorSnowPoints[col - 1] - 2.0f);
                                    if (col < winMgr.FloorSnowPoints.Length - 1) winMgr.FloorSnowPoints[col + 1] = Math.Max(0f, winMgr.FloorSnowPoints[col + 1] - 2.0f);
                                }
                            }
                        }
                    }

                    if (Math.Abs(mouseDx) > 3f && onSnowPuff != null && _rand.Next(3) == 0)
                    {
                        onSnowPuff(_rollingBall.X, _rollingBall.Y + _rollingBall.Radius * 0.7f, 2);
                    }
                }
            }

            // 3. Mouse Just Released
            if (mouseJustReleased)
            {
                _isCandidateRolling = false;

                if (_rollingBall != null)
                {
                    _rollingBall.IsRolling = false;
                    _rollingBall.IsGrabbed = false;

                    // Ensure WindowOffsetX and WindowOffsetY are strictly synchronized upon release
                    if (_rollingBall.AttachedWindow != IntPtr.Zero && winMgr != null)
                    {
                        Rectangle curB;
                        bool dummy;
                        if (winMgr.TryGetWindowCurrentBounds(_rollingBall.AttachedWindow, out curB, out dummy))
                        {
                            _rollingBall.WindowOffsetX = _rollingBall.X - curB.Left;
                            _rollingBall.WindowOffsetY = curB.Top - _rollingBall.Y;
                        }
                    }

                    CheckStacking(_rollingBall, winMgr, onSnowPuff);
                    _rollingBall = null;
                }

                if (_draggedBall != null)
                {
                    _draggedBall.IsGrabbed = false;
                    bool stacked = CheckStacking(_draggedBall, winMgr, onSnowPuff);

                    if (!stacked)
                    {
                        // Check if placed directly on or near a window ledge
                        bool placedOnWin = false;
                        if (winMgr != null)
                        {
                            List<WindowInfo> windows = winMgr.ActiveWindows;
                            for (int w = 0; w < windows.Count; w++)
                            {
                                WindowInfo win = windows[w];
                                Rectangle r = win.Bounds;
                                if (_draggedBall.X >= r.Left && _draggedBall.X <= r.Right &&
                                    _draggedBall.Y >= r.Top - _draggedBall.BaseRadius - 20f &&
                                    _draggedBall.Y <= r.Top + 15f)
                                {
                                    // Placed directly on window ledge!
                                    _draggedBall.Y = r.Top - _draggedBall.BaseRadius;
                                    _draggedBall.Vx = 0f;
                                    _draggedBall.Vy = 0f;
                                    _draggedBall.IsFalling = false;
                                    _draggedBall.AttachedWindow = win.Handle;
                                    _draggedBall.WindowOffsetX = _draggedBall.X - r.Left;
                                    _draggedBall.WindowOffsetY = r.Top - _draggedBall.Y;
                                    placedOnWin = true;
                                    if (onSnowPuff != null) onSnowPuff(_draggedBall.X, _draggedBall.Y + _draggedBall.BaseRadius, 6);
                                    break;
                                }
                            }
                        }

                        if (!placedOnWin)
                        {
                            // Released in the air: let it fly and fall with gravity!
                            _draggedBall.IsFalling = true;
                            _draggedBall.AttachedWindow = IntPtr.Zero;
                        }
                    }
                    _draggedBall = null;
                }
            }

            // 4. Update Physics & Falling for all snowballs
            for (int i = _snowballs.Count - 1; i >= 0; i--)
            {
                Snowball b = _snowballs[i];
                if (b.IsGrabbed || b.IsRolling) continue;

                if (b.IsFalling)
                {
                    b.Vy += 0.9f; // gravity
                    b.X += b.Vx;
                    b.Y += b.Vy;
                    b.Vx *= 0.98f;

                    // Check violent impact shatter
                    float currentSpeed = (float)Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);

                    // Check landing on window ledges
                    if (winMgr != null)
                    {
                        List<WindowInfo> windows = winMgr.ActiveWindows;
                        for (int w = 0; w < windows.Count; w++)
                        {
                            WindowInfo win = windows[w];
                            Rectangle r = win.Bounds;
                            if (b.X >= r.Left && b.X <= r.Right)
                            {
                                float ledgeY = r.Top;
                                if (b.Y >= ledgeY - b.BaseRadius && (b.Y - b.Vy) <= ledgeY + 12f)
                                {
                                    if (currentSpeed > 22.0f)
                                    {
                                        // Shatter into snow flurry upon hard throw!
                                        if (onSnowPuff != null) onSnowPuff(b.X, ledgeY, 32);
                                        _snowballs.RemoveAt(i);
                                        break;
                                    }

                                    // Land gracefully on window
                                    b.Y = ledgeY - b.BaseRadius;
                                    b.Vy = 0f;
                                    b.Vx = 0f;
                                    b.IsFalling = false;
                                    b.AttachedWindow = win.Handle;
                                    b.WindowOffsetX = b.X - win.Bounds.Left;
                                    b.WindowOffsetY = win.Bounds.Top - b.Y;

                                    if (onSnowPuff != null) onSnowPuff(b.X, b.Y + b.BaseRadius, 8);
                                    CheckStacking(b, winMgr, onSnowPuff);
                                    break;
                                }
                            }
                        }
                    }

                    // Check landing on floor / taskbar
                    if (b.IsFalling && b.Y >= screenHeight - b.BaseRadius - 2f)
                    {
                        if (currentSpeed > 22.0f)
                        {
                            if (onSnowPuff != null) onSnowPuff(b.X, screenHeight - 10f, 32);
                            _snowballs.RemoveAt(i);
                            continue;
                        }

                        b.Y = screenHeight - b.BaseRadius - 2f;
                        b.Vy = 0f;
                        b.Vx = 0f;
                        b.IsFalling = false;
                        b.AttachedWindow = IntPtr.Zero;

                        if (onSnowPuff != null) onSnowPuff(b.X, b.Y + b.BaseRadius, 8);
                        CheckStacking(b, winMgr, onSnowPuff);
                    }
                }
                else if (b.AttachedWindow != IntPtr.Zero && winMgr != null)
                {
                    // Track attached window movement
                    Rectangle curBounds;
                    bool hasMoved;
                    if (winMgr.TryGetWindowCurrentBounds(b.AttachedWindow, out curBounds, out hasMoved))
                    {
                        b.X = curBounds.Left + b.WindowOffsetX;
                        b.Y = curBounds.Top - b.WindowOffsetY;

                        // Check if window is shaken vigorously -> snowballs tumble off downwards!
                        for (int w = 0; w < winMgr.ActiveWindows.Count; w++)
                        {
                            WindowInfo win = winMgr.ActiveWindows[w];
                            if (win.Handle == b.AttachedWindow && win.Speed > 30f)
                            {
                                b.AttachedWindow = IntPtr.Zero;
                                b.IsFalling = true;
                                b.Vy = (float)(3.0 + _rand.NextDouble() * 3.5);
                                b.Vx = win.VelocityX * 0.4f;
                                if (onSnowPuff != null) onSnowPuff(b.X, b.Y, 10);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // Window closed/minimized -> fall
                        b.AttachedWindow = IntPtr.Zero;
                        b.IsFalling = true;
                    }
                }
            }

            _prevMouseDown = isMouseDown;
            _prevMouseX = mouseX;
            _prevMouseY = mouseY;
        }

        private bool CheckStacking(Snowball dropped, WindowManager winMgr, Action<float, float, int> onSnowPuff)
        {
            if (dropped == null || dropped.BallCount >= 3) return false;

            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball target = _snowballs[i];
                if (target == dropped || target.IsGrabbed) continue;

                // Check proximity for stacking (horizontal distance < target.BaseRadius + 16, and dropped is above or touching)
                float dx = Math.Abs(dropped.X - target.X);
                float dy = Math.Abs(dropped.Y - target.Y);

                if (dx <= target.BaseRadius + 16f && dy <= (target.BaseRadius + dropped.BaseRadius + 25f))
                {
                    if (target.BallCount == 1 && dropped.BallCount == 1)
                    {
                        // 1 Ball + 1 Ball = 2-Ball Stack!
                        target.BallCount = 2;
                        target.BaseRadius = Math.Max(target.BaseRadius, dropped.BaseRadius);
                        target.TorsoRadius = Math.Min(target.BaseRadius * 0.85f, Math.Max(11f, dropped.BaseRadius));
                        if (target.AttachedWindow != IntPtr.Zero && winMgr != null)
                        {
                            Rectangle r;
                            bool moved;
                            if (winMgr.TryGetWindowCurrentBounds(target.AttachedWindow, out r, out moved))
                            {
                                target.Y = r.Top - target.BaseRadius;
                                target.WindowOffsetY = r.Top - target.Y;
                            }
                        }
                        _snowballs.Remove(dropped);

                        if (onSnowPuff != null) onSnowPuff(target.X, target.Y - target.BaseRadius, 12);
                        return true;
                    }
                    else if (target.BallCount == 2 && dropped.BallCount == 1)
                    {
                        // 2 Balls + 1 Ball = 3-Ball FULL SNOWMAN!
                        target.BallCount = 3;
                        target.HeadRadius = Math.Min(target.TorsoRadius * 0.78f, Math.Max(9f, dropped.BaseRadius));
                        target.IsSnowman = true;
                        if (target.AttachedWindow != IntPtr.Zero && winMgr != null)
                        {
                            Rectangle r;
                            bool moved;
                            if (winMgr.TryGetWindowCurrentBounds(target.AttachedWindow, out r, out moved))
                            {
                                target.Y = r.Top - target.BaseRadius;
                                target.WindowOffsetY = r.Top - target.Y;
                            }
                        }
                        _snowballs.Remove(dropped);

                        // Happy celebratory burst!
                        if (onSnowPuff != null) onSnowPuff(target.X, target.Y - target.BaseRadius - target.TorsoRadius, 25);
                        return true;
                    }
                }
            }
            return false;
        }

        public void Draw(Graphics g)
        {
            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball s = _snowballs[i];

                if (s.BallCount == 1)
                {
                    DrawSnowballSphere(g, s.X, s.Y, s.BaseRadius, s.Angle);
                }
                else if (s.BallCount == 2)
                {
                    float r1 = s.BaseRadius;
                    float r2 = s.TorsoRadius;
                    float y1 = s.Y;
                    float y2 = y1 - r1 - r2 + 4f;

                    DrawSnowballSphere(g, s.X, y1, r1, 0f);
                    DrawSnowballSphere(g, s.X, y2, r2, 0f);
                }
                else if (s.BallCount >= 3)
                {
                    DrawFullSnowman(g, s);
                }
            }
        }

        private void DrawSnowballSphere(Graphics g, float cx, float cy, float radius, float angle)
        {
            if (radius <= 2f) return;

            RectangleF rect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

            // 1. Soft contact shadow under the sphere
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 15, 30, 50)))
            {
                g.FillEllipse(shadowBrush, cx - radius * 0.85f, cy + radius - 3.5f, radius * 1.7f, 6.5f);
            }

            // 2. Volumetric 3D gradient sphere (KoiArt / Snowy aesthetic)
            using (GraphicsPath spherePath = new GraphicsPath())
            {
                spherePath.AddEllipse(rect);
                using (LinearGradientBrush grad = new LinearGradientBrush(
                    new PointF(cx - radius * 0.65f, cy - radius * 0.65f),
                    new PointF(cx + radius * 0.65f, cy + radius * 0.65f),
                    Color.FromArgb(255, 255, 255, 255),
                    Color.FromArgb(255, 218, 232, 246)))
                {
                    g.FillPath(grad, spherePath);
                }

                // 3. Specular gloss highlight on top-left
                float hlRadius = radius * 0.45f;
                float hlX = cx - radius * 0.30f;
                float hlY = cy - radius * 0.30f;
                using (SolidBrush hlBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                {
                    g.FillEllipse(hlBrush, hlX - hlRadius, hlY - hlRadius, hlRadius * 2f, hlRadius * 2f);
                }

                // 4. Subtle cool rim outline
                using (Pen rimPen = new Pen(Color.FromArgb(55, 135, 165, 195), 1.0f))
                {
                    g.DrawEllipse(rimPen, rect);
                }
            }
        }

        private void DrawFullSnowman(Graphics g, Snowball s)
        {
            float r1 = s.BaseRadius;
            float r2 = s.TorsoRadius;
            float r3 = s.HeadRadius;
            float y1 = s.Y;
            float y2 = y1 - r1 - r2 + 4f;
            float y3 = y2 - r2 - r3 + 4f;

            // 1. Wooden Twig Arms (branching from Torso sides)
            using (Pen armPen = new Pen(Color.FromArgb(255, 95, 65, 45), 2.2f))
            {
                armPen.StartCap = LineCap.Round;
                armPen.EndCap = LineCap.Round;

                // Left arm
                g.DrawLine(armPen, s.X - r2 + 3f, y2 - 2f, s.X - r2 - 16f, y2 - 10f);
                g.DrawLine(armPen, s.X - r2 - 9f, y2 - 6f, s.X - r2 - 12f, y2 - 14f);

                // Right arm
                g.DrawLine(armPen, s.X + r2 - 3f, y2 - 2f, s.X + r2 + 16f, y2 - 10f);
                g.DrawLine(armPen, s.X + r2 + 9f, y2 - 6f, s.X + r2 + 12f, y2 - 14f);
            }

            // 2. Three Snow Spheres (Base, Torso, Head)
            DrawSnowballSphere(g, s.X, y1, r1, 0f);
            DrawSnowballSphere(g, s.X, y2, r2, 0f);
            DrawSnowballSphere(g, s.X, y3, r3, 0f);

            // 3. Festive Red Scarf (between Head and Torso)
            float neckY = y3 + r3 - 2f;
            using (SolidBrush scarfBrush = new SolidBrush(Color.FromArgb(255, 230, 42, 42)))
            {
                // Collar ellipse around neck
                g.FillEllipse(scarfBrush, s.X - r3 * 1.15f, neckY - 3f, r3 * 2.3f, 7f);

                // Hanging tail over torso
                PointF[] tail = new PointF[]
                {
                    new PointF(s.X + 2f, neckY + 1f),
                    new PointF(s.X + 9f, neckY + 1f),
                    new PointF(s.X + 8f, neckY + r2 * 0.9f),
                    new PointF(s.X + 1f, neckY + r2 * 0.9f)
                };
                g.FillPolygon(scarfBrush, tail);
            }

            // Scarf white fringe lines
            using (Pen fringePen = new Pen(Color.White, 1.2f))
            {
                float tailBot = neckY + r2 * 0.9f;
                g.DrawLine(fringePen, s.X + 2.5f, tailBot - 2f, s.X + 2.5f, tailBot + 2f);
                g.DrawLine(fringePen, s.X + 5.0f, tailBot - 2f, s.X + 5.0f, tailBot + 2f);
                g.DrawLine(fringePen, s.X + 7.5f, tailBot - 2f, s.X + 7.5f, tailBot + 2f);
            }

            // 4. Coal Buttons down Torso
            using (SolidBrush coalBrush = new SolidBrush(Color.FromArgb(255, 35, 40, 45)))
            using (SolidBrush gleamBrush = new SolidBrush(Color.White))
            {
                float[] btnOffsets = new float[] { -r2 * 0.25f, r2 * 0.15f, r2 * 0.55f };
                for (int b = 0; b < btnOffsets.Length; b++)
                {
                    float by = y2 + btnOffsets[b];
                    g.FillEllipse(coalBrush, s.X - 2.0f, by - 2.0f, 4.0f, 4.0f);
                    g.FillEllipse(gleamBrush, s.X - 1.0f, by - 1.5f, 1.2f, 1.2f);
                }

                // 5. Face (Eyes, Smile)
                float eyeDist = r3 * 0.35f;
                g.FillEllipse(coalBrush, s.X - eyeDist - 2.0f, y3 - 3.0f, 4.0f, 4.0f);
                g.FillEllipse(gleamBrush, s.X - eyeDist - 1.0f, y3 - 2.5f, 1.2f, 1.2f);

                g.FillEllipse(coalBrush, s.X + eyeDist - 2.0f, y3 - 3.0f, 4.0f, 4.0f);
                g.FillEllipse(gleamBrush, s.X + eyeDist - 1.0f, y3 - 2.5f, 1.2f, 1.2f);

                // Smile
                float[] smX = new float[] { -4.5f, -1.5f, 1.5f, 4.5f };
                float[] smY = new float[] { 4.0f, 5.5f, 5.5f, 4.0f };
                for (int sm = 0; sm < 4; sm++)
                {
                    g.FillEllipse(coalBrush, s.X + smX[sm] - 1.0f, y3 + smY[sm] - 1.0f, 2.0f, 2.0f);
                }
            }

            // 6. Bright Orange Carrot Nose (pointing right)
            using (SolidBrush carrotBrush = new SolidBrush(Color.FromArgb(255, 255, 115, 0)))
            {
                PointF[] nose = new PointF[]
                {
                    new PointF(s.X - 1.0f, y3 - 1.5f),
                    new PointF(s.X - 1.0f, y3 + 2.0f),
                    new PointF(s.X + 11.0f, y3 + 0.5f)
                };
                g.FillPolygon(carrotBrush, nose);
            }
        }
    }
}
