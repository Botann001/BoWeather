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
        public float Alpha;
        public bool IsMelting;

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
            Alpha = 1.0f;
            IsMelting = false;
        }

        public RectangleF GetBounds()
        {
            if (BallCount == 1)
            {
                return new RectangleF(X - BaseRadius, Y - BaseRadius, BaseRadius * 2f, BaseRadius * 2f);
            }
            else if (BallCount == 2)
            {
                float overlap12 = Math.Max(2f, Math.Min(BaseRadius, TorsoRadius) * 0.18f);
                float totalH = BaseRadius * 2f + TorsoRadius * 2f - overlap12;
                float maxR = Math.Max(BaseRadius, TorsoRadius);
                return new RectangleF(X - maxR, Y + BaseRadius - totalH, maxR * 2f, totalH);
            }
            else
            {
                float overlap12 = Math.Max(2f, Math.Min(BaseRadius, TorsoRadius) * 0.18f);
                float overlap23 = Math.Max(2f, Math.Min(TorsoRadius, HeadRadius) * 0.18f);
                float totalH = BaseRadius * 2f + TorsoRadius * 2f + HeadRadius * 2f - overlap12 - overlap23;
                float maxR = Math.Max(BaseRadius, Math.Max(TorsoRadius, HeadRadius));
                return new RectangleF(X - maxR, Y + BaseRadius - totalH, maxR * 2f, totalH);
            }
        }
    }

    public enum DebrisKind
    {
        Shockwave,
        SnowChunk,
        CarrotNose,
        SantaHat,
        Coal
    }

    public class ShatterDebris
    {
        public DebrisKind Kind;
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public float Gravity;
        public float Radius;
        public float Angle;
        public float AngularVelocity;
        public float Alpha;
        public float Decay;
        public float MaxRadius;
    }

    public class SnowballManager
    {
        public const float MaxSnowballRadius = 52.0f; // Exactly sized to match the user's reference screenshot (~104px diameter)
        private readonly List<Snowball> _snowballs = new List<Snowball>();
        private readonly List<ShatterDebris> _debrisList = new List<ShatterDebris>();
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
        public int Count { get { return _snowballs.Count + _debrisList.Count; } }
        public ChristmasManager Christmas { get; set; }

        public void Clear()
        {
            _snowballs.Clear();
            _debrisList.Clear();
            _draggedBall = null;
            _rollingBall = null;
            _isCandidateRolling = false;
        }

        public void StartMelting()
        {
            for (int i = 0; i < _snowballs.Count; i++)
            {
                _snowballs[i].IsMelting = true;
            }
            _draggedBall = null;
            _rollingBall = null;
            _isCandidateRolling = false;
        }

        public Snowball HitTest(float mouseX, float mouseY)
        {
            for (int i = _snowballs.Count - 1; i >= 0; i--)
            {
                Snowball b = _snowballs[i];
                RectangleF rect = b.GetBounds();
                rect.Inflate(12f, 12f);
                if (rect.Contains(mouseX, mouseY))
                {
                    return b;
                }
            }
            return null;
        }

        public void StartDrag(Snowball b, float mouseX, float mouseY)
        {
            if (b == null) return;
            _draggedBall = b;
            b.IsGrabbed = true;
            b.IsFalling = false;
            b.AttachedWindow = IntPtr.Zero;
            b.GrabOffsetX = mouseX - b.X;
            b.GrabOffsetY = mouseY - b.Y;
            b.Vx = 0f;
            b.Vy = 0f;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;
        }

        public void StartCandidateRoll(float mouseX, float mouseY)
        {
            _draggedBall = null;
            _rollingBall = null;
            _isCandidateRolling = true;
            _rollStartX = mouseX;
            _rollStartY = mouseY;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;
        }

        public void ShatterBall(Snowball b, Action<float, float, int> onSnowPuff)
        {
            if (b == null) return;
            SpawnShatterDebris(b);
            if (onSnowPuff != null)
            {
                int count = (b.BallCount >= 3) ? 55 : ((b.BallCount == 2) ? 35 : 20);
                onSnowPuff(b.X, b.Y, count);
            }
            _snowballs.Remove(b);
            if (_draggedBall == b) _draggedBall = null;
            if (_rollingBall == b) _rollingBall = null;
        }

        private void SpawnShatterDebris(Snowball b)
        {
            float cy = b.Y;
            if (b.BallCount == 2)
            {
                cy = b.Y - b.BaseRadius * 0.5f;
            }
            else if (b.BallCount >= 3)
            {
                cy = b.Y - b.BaseRadius - b.TorsoRadius * 0.4f;
            }

            // 1. Shockwave Blast Ring
            _debrisList.Add(new ShatterDebris
            {
                Kind = DebrisKind.Shockwave,
                X = b.X,
                Y = cy,
                Radius = 10f,
                MaxRadius = (b.BallCount >= 3) ? 125f : ((b.BallCount == 2) ? 90f : 60f),
                Alpha = 1.0f,
                Decay = 0.058f
            });

            // 2. Chunks of 3D Volumetric Snow
            int chunkCount = (b.BallCount >= 3) ? 36 : ((b.BallCount == 2) ? 22 : 12);
            for (int i = 0; i < chunkCount; i++)
            {
                float angle = (float)(_rand.NextDouble() * Math.PI * 2.0);
                float speed = (float)(5.0 + _rand.NextDouble() * 11.0);
                float maxR = (b.BallCount >= 3) ? 15f : ((b.BallCount == 2) ? 11f : 8f);
                float chunkR = (float)(3.5 + _rand.NextDouble() * (maxR - 3.5f));

                float vx = (float)Math.Cos(angle) * speed;
                // Explosive upward fountain bias
                float vy = (float)(Math.Sin(angle) * speed * 0.72 - (3.5 + _rand.NextDouble() * 7.0));

                _debrisList.Add(new ShatterDebris
                {
                    Kind = DebrisKind.SnowChunk,
                    X = b.X + (float)((_rand.NextDouble() - 0.5) * 16.0),
                    Y = cy + (float)((_rand.NextDouble() - 0.5) * 20.0),
                    Vx = vx,
                    Vy = vy,
                    Gravity = 0.42f,
                    Radius = chunkR,
                    Angle = (float)(_rand.NextDouble() * Math.PI * 2.0),
                    AngularVelocity = (float)((_rand.NextDouble() - 0.5) * 0.35),
                    Alpha = 1.0f,
                    Decay = (float)(0.016 + _rand.NextDouble() * 0.015)
                });
            }

            // 3. Snowman Accessories Flying Off
            if (b.BallCount >= 3 || b.IsSnowman)
            {
                float overlap12 = Math.Max(2f, Math.Min(b.BaseRadius, b.TorsoRadius) * 0.18f);
                float overlap23 = Math.Max(2f, Math.Min(b.TorsoRadius, b.HeadRadius) * 0.18f);
                float y2 = b.Y - b.BaseRadius - b.TorsoRadius + overlap12;
                float y3 = y2 - b.TorsoRadius - b.HeadRadius + overlap23;

                // Flying Carrot Nose
                _debrisList.Add(new ShatterDebris
                {
                    Kind = DebrisKind.CarrotNose,
                    X = b.X,
                    Y = y3 + b.HeadRadius * 0.06f,
                    Vx = (float)(5.5 + _rand.NextDouble() * 7.0),
                    Vy = (float)(-7.0 - _rand.NextDouble() * 6.0),
                    Gravity = 0.44f,
                    Radius = Math.Max(6f, b.HeadRadius * 0.45f),
                    Angle = 0f,
                    AngularVelocity = 0.38f,
                    Alpha = 1.0f,
                    Decay = 0.018f
                });

                // Flying Santa Hat
                if (Christmas != null && Christmas.ShowSantaHat)
                {
                    _debrisList.Add(new ShatterDebris
                    {
                        Kind = DebrisKind.SantaHat,
                        X = b.X,
                        Y = y3 - b.HeadRadius * 0.8f,
                        Vx = (float)((_rand.NextDouble() - 0.5) * 6.5),
                        Vy = (float)(-9.0 - _rand.NextDouble() * 6.0),
                        Gravity = 0.38f,
                        Radius = Math.Max(12f, b.HeadRadius * 0.85f),
                        Angle = 0f,
                        AngularVelocity = 0.16f,
                        Alpha = 1.0f,
                        Decay = 0.016f
                    });
                }

                // 7 Coal pieces scattering
                for (int c = 0; c < 7; c++)
                {
                    float cAngle = (float)(_rand.NextDouble() * Math.PI * 2.0);
                    float cSpeed = (float)(4.0 + _rand.NextDouble() * 8.0);
                    _debrisList.Add(new ShatterDebris
                    {
                        Kind = DebrisKind.Coal,
                        X = b.X + (float)((_rand.NextDouble() - 0.5) * 12.0),
                        Y = (c < 4) ? y3 : y2,
                        Vx = (float)Math.Cos(cAngle) * cSpeed,
                        Vy = (float)(Math.Sin(cAngle) * cSpeed * 0.65 - (2.5 + _rand.NextDouble() * 5.0)),
                        Gravity = 0.48f,
                        Radius = (float)(2.2 + _rand.NextDouble() * 1.6),
                        Angle = 0f,
                        AngularVelocity = (float)((_rand.NextDouble() - 0.5) * 0.4),
                        Alpha = 1.0f,
                        Decay = 0.017f
                    });
                }
            }
        }

        public void OnMouseMove(float mouseX, float mouseY, WindowManager winMgr, int screenWidth, int floorY, Action<float, float, int> onSnowPuff)
        {
            float mouseDx = (_prevMouseX >= 0) ? (mouseX - _prevMouseX) : 0f;
            float mouseDy = (_prevMouseY >= 0) ? (mouseY - _prevMouseY) : 0f;
            _prevMouseX = (int)mouseX;
            _prevMouseY = (int)mouseY;

            if (_draggedBall != null)
            {
                _draggedBall.X = mouseX - _draggedBall.GrabOffsetX;
                _draggedBall.Y = mouseY - _draggedBall.GrabOffsetY;
                _draggedBall.Vx = mouseDx * 0.85f;
                _draggedBall.Vy = mouseDy * 0.85f;
                _draggedBall.Angle += mouseDx * 0.12f;

                // A snowball that has already been rolled keeps its fixed size when dragged or thrown!
                if (Math.Abs(mouseDx) > 3.5f && onSnowPuff != null && _rand.Next(4) == 0)
                {
                    onSnowPuff(_draggedBall.X, _draggedBall.Y + _draggedBall.BaseRadius * 0.7f, 2);
                }
            }
            else if (_isCandidateRolling)
            {
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
                            if (mouseX >= b.Left && mouseX <= b.Right && mouseY >= b.Top - 35 && mouseY <= b.Top + 20)
                            {
                                if (win.HasSnow())
                                {
                                    ledgeY = b.Top;
                                    attachWin = win.Handle;
                                    candidateWin = win;
                                    break;
                                }
                            }
                        }
                        if (attachWin == IntPtr.Zero && mouseY >= floorY - 110)
                        {
                            ledgeY = floorY;
                        }
                    }

                    // Only roll snow if on a valid window ledge or on the floor snow
                    if (attachWin != IntPtr.Zero || mouseY >= floorY - 110)
                    {
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
            }
            else if (_rollingBall != null)
            {
                _rollingBall.X = mouseX;
                _rollingBall.Angle += mouseDx * 0.15f;

                // Max roll size capped to ~52px radius (~104px diameter) as referenced in user's screenshot
                if (_rollingBall.Radius < MaxSnowballRadius)
                {
                    float rollGrowth = Math.Abs(mouseDx) * Math.Max(0.035f, 0.16f / (1.0f + _rollingBall.Radius * 0.015f));
                    _rollingBall.Radius = Math.Min(MaxSnowballRadius, _rollingBall.Radius + rollGrowth);
                    _rollingBall.BaseRadius = _rollingBall.Radius;
                }

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

                    if (!onWin && _rollingBall.AttachedWindow != IntPtr.Zero)
                    {
                        // Rolled past the edge of the window! Detach and fall down naturally!
                        _rollingBall.AttachedWindow = IntPtr.Zero;
                        _rollingBall.IsRolling = false;
                        _rollingBall.IsGrabbed = false;
                        _rollingBall.IsFalling = true;
                        _rollingBall.Vy = 2.5f;
                        _rollingBall.Vx = mouseDx * 0.8f;
                        if (onSnowPuff != null) onSnowPuff(_rollingBall.X, _rollingBall.Y, 8);
                        _rollingBall = null;
                        return;
                    }

                    if (!onWin && mouseY >= floorY - 110)
                    {
                        _rollingBall.Y = floorY - _rollingBall.Radius;
                        _rollingBall.AttachedWindow = IntPtr.Zero;
                        _rollingBall.WindowOffsetX = 0f;
                        _rollingBall.WindowOffsetY = 0f;

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

        public void OnMouseUp(float mouseX, float mouseY, WindowManager winMgr, int floorY, Action<float, float, int> onSnowPuff)
        {
            _isCandidateRolling = false;

            if (_rollingBall != null)
            {
                _rollingBall.IsRolling = false;
                _rollingBall.IsGrabbed = false;

                if (_rollingBall.AttachedWindow != IntPtr.Zero && winMgr != null)
                {
                    Rectangle curB;
                    bool dummy;
                    if (winMgr.TryGetWindowCurrentBounds(_rollingBall.AttachedWindow, out curB, out dummy))
                    {
                        if (_rollingBall.X < curB.Left - 5f || _rollingBall.X > curB.Right + 5f)
                        {
                            _rollingBall.AttachedWindow = IntPtr.Zero;
                            _rollingBall.IsFalling = true;
                            _rollingBall.Vy = 2.5f;
                            _rollingBall.Vx = (float)((_rand.NextDouble() - 0.5) * 2.0);
                        }
                        else
                        {
                            _rollingBall.WindowOffsetX = _rollingBall.X - curB.Left;
                            _rollingBall.WindowOffsetY = curB.Top - _rollingBall.Y;
                        }
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
                    bool placed = false;
                    // Check landing on window ledge
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
                                _draggedBall.Y = r.Top - _draggedBall.BaseRadius;
                                _draggedBall.Vx = 0f;
                                _draggedBall.Vy = 0f;
                                _draggedBall.IsFalling = false;
                                _draggedBall.AttachedWindow = win.Handle;
                                _draggedBall.WindowOffsetX = _draggedBall.X - r.Left;
                                _draggedBall.WindowOffsetY = r.Top - _draggedBall.Y;
                                placed = true;
                                if (onSnowPuff != null) onSnowPuff(_draggedBall.X, _draggedBall.Y + _draggedBall.BaseRadius, 6);
                                break;
                            }
                        }
                    }

                    // Check landing on Taskbar
                    if (!placed && _draggedBall.Y >= floorY - _draggedBall.BaseRadius - 40f)
                    {
                        _draggedBall.Y = floorY - _draggedBall.BaseRadius;
                        _draggedBall.Vx = 0f;
                        _draggedBall.Vy = 0f;
                        _draggedBall.IsFalling = false;
                        _draggedBall.AttachedWindow = IntPtr.Zero;
                        placed = true;
                        if (onSnowPuff != null) onSnowPuff(_draggedBall.X, _draggedBall.Y + _draggedBall.BaseRadius, 6);
                    }

                    if (!placed)
                    {
                        _draggedBall.IsFalling = true;
                        _draggedBall.AttachedWindow = IntPtr.Zero;
                    }
                }
                _draggedBall = null;
            }
        }

        public void UpdatePhysics(WindowManager winMgr, int screenWidth, int screenHeight, int floorY, Action<float, float, int> onSnowPuff)
        {
            for (int i = _snowballs.Count - 1; i >= 0; i--)
            {
                Snowball b = _snowballs[i];

                if (b.IsMelting)
                {
                    b.Alpha -= 0.022f;
                    if (b.Alpha <= 0f)
                    {
                        _snowballs.RemoveAt(i);
                        continue;
                    }
                }

                if (b.IsGrabbed || b.IsRolling) continue;

                if (b.IsFalling)
                {
                    b.Vy += 0.9f;
                    b.X += b.Vx;
                    b.Y += b.Vy;
                    b.Vx *= 0.98f;

                    float currentSpeed = (float)Math.Sqrt(b.Vx * b.Vx + b.Vy * b.Vy);

                    // 0. Collision with Snowman! (Throwing a snowball at a snowman shatters both in a burst of snow)
                    Snowball hitSnowman = null;
                    for (int j = 0; j < _snowballs.Count; j++)
                    {
                        if (j == i) continue;
                        Snowball target = _snowballs[j];
                        if (target.IsGrabbed || target.IsRolling || target.IsMelting) continue;

                        if (target.BallCount >= 2 || target.IsSnowman)
                        {
                            RectangleF hitBox = target.GetBounds();
                            hitBox.Inflate(Math.Max(14f, b.BaseRadius * 0.85f), Math.Max(14f, b.BaseRadius * 0.85f));

                            PointF p1 = new PointF(b.X, b.Y);
                            PointF p2 = new PointF(b.X - b.Vx * 0.5f, b.Y - b.Vy * 0.5f);
                            PointF p3 = new PointF(b.X - b.Vx, b.Y - b.Vy);

                            if (hitBox.Contains(p1) || hitBox.Contains(p2) || hitBox.Contains(p3))
                            {
                                hitSnowman = target;
                                break;
                            }
                        }
                    }

                    if (hitSnowman != null)
                    {
                        float hitX = (b.X + hitSnowman.X) * 0.5f;
                        float hitY = (b.Y + hitSnowman.Y) * 0.5f;

                        // Shatter the snowman into a glorious flurry of snow
                        ShatterBall(hitSnowman, onSnowPuff);

                        // Shatter the thrown snowball
                        ShatterBall(b, onSnowPuff);

                        // Dramatic explosion of snow puff particles at the point of impact
                        if (onSnowPuff != null)
                        {
                            onSnowPuff(hitX, hitY, 35);
                        }

                        if (_snowballs.Count == 0) break;
                        if (i >= _snowballs.Count) i = _snowballs.Count;
                        continue;
                    }

                    // 1. Landing on window ledge
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
                                if (b.Y >= ledgeY - b.BaseRadius && (b.Y - b.Vy) <= ledgeY + 14f)
                                {
                                    b.Y = ledgeY - b.BaseRadius;
                                    b.Vy = 0f;
                                    b.Vx = 0f;
                                    b.IsFalling = false;
                                    b.AttachedWindow = win.Handle;
                                    b.WindowOffsetX = b.X - win.Bounds.Left;
                                    b.WindowOffsetY = win.Bounds.Top - b.Y;

                                    int puffCount = Math.Min(20, Math.Max(6, (int)(currentSpeed * 0.35f)));
                                    if (onSnowPuff != null) onSnowPuff(b.X, b.Y + b.BaseRadius, puffCount);
                                    CheckStacking(b, winMgr, onSnowPuff);
                                    break;
                                }
                            }
                        }
                    }

                    // 2. Landing on Taskbar
                    if (b.IsFalling && b.Y >= floorY - b.BaseRadius - 2f)
                    {
                        b.Y = floorY - b.BaseRadius;
                        b.Vy = 0f;
                        b.Vx = 0f;
                        b.IsFalling = false;
                        b.AttachedWindow = IntPtr.Zero;

                        int puffCount = Math.Min(24, Math.Max(8, (int)(currentSpeed * 0.45f)));
                        if (onSnowPuff != null) onSnowPuff(b.X, b.Y + b.BaseRadius, puffCount);
                        CheckStacking(b, winMgr, onSnowPuff);
                    }
                }
                else if (b.AttachedWindow != IntPtr.Zero && winMgr != null)
                {
                    Rectangle curBounds;
                    bool hasMoved;
                    if (winMgr.TryGetWindowCurrentBounds(b.AttachedWindow, out curBounds, out hasMoved))
                    {
                        if (b.X < curBounds.Left - 8f || b.X > curBounds.Right + 8f)
                        {
                            // Snowball is beyond the window ledge! Falls off naturally!
                            b.AttachedWindow = IntPtr.Zero;
                            b.IsFalling = true;
                            b.Vy = 2.5f;
                            b.Vx = (b.X < curBounds.Left) ? -1.5f : 1.5f;
                            if (onSnowPuff != null) onSnowPuff(b.X, b.Y, 8);
                            continue;
                        }

                        b.X = curBounds.Left + b.WindowOffsetX;
                        b.Y = curBounds.Top - b.WindowOffsetY;

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
                        b.AttachedWindow = IntPtr.Zero;
                        b.IsFalling = true;
                        b.Vy = 0f;
                    }
                }
            }

            UpdateDebris(floorY);
        }

        private void UpdateDebris(int floorY)
        {
            for (int i = _debrisList.Count - 1; i >= 0; i--)
            {
                ShatterDebris d = _debrisList[i];
                d.Alpha -= d.Decay;
                if (d.Alpha <= 0f)
                {
                    _debrisList.RemoveAt(i);
                    continue;
                }

                if (d.Kind == DebrisKind.Shockwave)
                {
                    d.Radius += (d.MaxRadius - d.Radius) * 0.22f + 2.0f;
                    continue;
                }

                d.X += d.Vx;
                d.Y += d.Vy;
                d.Vy += d.Gravity;
                d.Vx *= 0.985f;
                d.Angle += d.AngularVelocity;

                // Bounce on floor / taskbar with damping
                if (d.Y >= floorY - d.Radius)
                {
                    d.Y = floorY - d.Radius;
                    d.Vy = -d.Vy * 0.35f;
                    d.Vx *= 0.70f;
                    if (Math.Abs(d.Vy) < 1.0f) d.Vy = 0f;
                }
            }
        }

        private void DrawDebris(Graphics g)
        {
            if (_debrisList.Count == 0) return;

            // 1. Draw Shockwaves first (underneath debris)
            for (int i = 0; i < _debrisList.Count; i++)
            {
                ShatterDebris d = _debrisList[i];
                if (d.Kind != DebrisKind.Shockwave) continue;

                int ringAlpha = Math.Max(0, Math.Min(255, (int)(d.Alpha * 180f)));
                if (ringAlpha > 3)
                {
                    using (Pen glowPen = new Pen(Color.FromArgb(ringAlpha / 3, 180, 225, 255), 12.0f))
                    using (Pen ringPen = new Pen(Color.FromArgb(ringAlpha, 235, 248, 255), 4.0f))
                    {
                        g.DrawEllipse(glowPen, d.X - d.Radius, d.Y - d.Radius * 0.75f, d.Radius * 2f, d.Radius * 1.5f);
                        g.DrawEllipse(ringPen, d.X - d.Radius, d.Y - d.Radius * 0.75f, d.Radius * 2f, d.Radius * 1.5f);
                    }
                }
            }

            // 2. Draw Snow Chunks & Props
            for (int i = 0; i < _debrisList.Count; i++)
            {
                ShatterDebris d = _debrisList[i];
                if (d.Kind == DebrisKind.Shockwave) continue;

                int alpha = Math.Max(0, Math.Min(255, (int)(d.Alpha * 255f)));
                if (alpha <= 2) continue;

                if (d.Kind == DebrisKind.SnowChunk)
                {
                    RectangleF cRect = new RectangleF(d.X - d.Radius, d.Y - d.Radius, d.Radius * 2f, d.Radius * 2f);
                    using (GraphicsPath cp = new GraphicsPath())
                    {
                        cp.AddEllipse(cRect);
                        using (PathGradientBrush pgb = new PathGradientBrush(cp))
                        {
                            pgb.CenterPoint = new PointF(d.X - d.Radius * 0.35f, d.Y - d.Radius * 0.35f);
                            pgb.CenterColor = Color.FromArgb(alpha, 255, 255, 255);
                            pgb.SurroundColors = new Color[] { Color.FromArgb(alpha, 210, 226, 245) };
                            g.FillPath(pgb, cp);
                        }
                    }
                }
                else if (d.Kind == DebrisKind.CarrotNose)
                {
                    GraphicsState gs = g.Save();
                    g.TranslateTransform(d.X, d.Y);
                    g.RotateTransform((float)(d.Angle * 180.0 / Math.PI));
                    using (SolidBrush carrotB = new SolidBrush(Color.FromArgb(alpha, 255, 115, 0)))
                    {
                        PointF[] nosePts = new PointF[] {
                            new PointF(-4f, -5f),
                            new PointF(-4f, 5f),
                            new PointF(22f, 1f)
                        };
                        g.FillPolygon(carrotB, nosePts);
                    }
                    g.Restore(gs);
                }
                else if (d.Kind == DebrisKind.Coal)
                {
                    using (SolidBrush coalB = new SolidBrush(Color.FromArgb(alpha, 25, 28, 35)))
                    {
                        g.FillEllipse(coalB, d.X - d.Radius, d.Y - d.Radius, d.Radius * 2f, d.Radius * 2f);
                    }
                }
                else if (d.Kind == DebrisKind.SantaHat)
                {
                    GraphicsState hs = g.Save();
                    g.TranslateTransform(d.X, d.Y);
                    g.RotateTransform((float)(d.Angle * 180.0 / Math.PI));
                    PointF[] hatPoly = new PointF[] {
                        new PointF(-16f, 8f),
                        new PointF(16f, 8f),
                        new PointF(24f, -14f),
                        new PointF(0f, -26f)
                    };
                    using (SolidBrush hatB = new SolidBrush(Color.FromArgb(alpha, 235, 45, 55)))
                    using (SolidBrush whiteB = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                    {
                        g.FillPolygon(hatB, hatPoly);
                        g.FillEllipse(whiteB, -18f, 4f, 36f, 9f);  // brim
                        g.FillEllipse(whiteB, 20f, -18f, 9f, 9f); // pom-pom
                    }
                    g.Restore(hs);
                }
            }
        }

        public void Update(WindowManager winMgr, int mouseX, int mouseY, bool isMouseDown, int screenWidth, int screenHeight, Action<float, float, int> onSnowPuff)
        {
            Update(winMgr, mouseX, mouseY, isMouseDown, screenWidth, screenHeight, screenHeight, onSnowPuff);
        }

        public void Update(WindowManager winMgr, int mouseX, int mouseY, bool isMouseDown, int screenWidth, int screenHeight, int floorY, Action<float, float, int> onSnowPuff)
        {
            bool mouseJustPressed = isMouseDown && !_prevMouseDown;
            bool mouseJustReleased = !isMouseDown && _prevMouseDown;

            if (mouseJustPressed && mouseX >= 0 && mouseY >= 0)
            {
                Snowball hit = HitTest(mouseX, mouseY);
                if (hit != null)
                {
                    StartDrag(hit, mouseX, mouseY);
                }
                else
                {
                    StartCandidateRoll(mouseX, mouseY);
                }
            }

            if (isMouseDown && mouseX >= 0 && mouseY >= 0)
            {
                OnMouseMove(mouseX, mouseY, winMgr, screenWidth, floorY, onSnowPuff);
            }

            if (mouseJustReleased)
            {
                OnMouseUp(mouseX, mouseY, winMgr, floorY, onSnowPuff);
            }

            UpdatePhysics(winMgr, screenWidth, screenHeight, floorY, onSnowPuff);

            _prevMouseDown = isMouseDown;
            _prevMouseX = mouseX;
            _prevMouseY = mouseY;
        }

        private bool CheckStacking(Snowball dropped, WindowManager winMgr, Action<float, float, int> onSnowPuff)
        {
            if (dropped == null || dropped.BallCount >= 3) return false;

            RectangleF droppedBounds = dropped.GetBounds();

            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball target = _snowballs[i];
                if (target == dropped || target.IsGrabbed || target.BallCount >= 3) continue;

                // Two snowballs stack whenever they touch or overlap in 2D space (with generous snap margin)
                RectangleF targetBounds = target.GetBounds();
                RectangleF inflatedTarget = targetBounds;
                inflatedTarget.Inflate(36f, 40f);

                if (inflatedTarget.IntersectsWith(droppedBounds))
                {
                    // Case 1: 1 Ball + 1 Ball = 2-Ball Stack!
                    if (target.BallCount == 1 && dropped.BallCount == 1)
                    {
                        target.BallCount = 2;

                        // Preserve exact rolled sizes without forcing order!
                        // Whichever ball is physically lower (larger Y) is the base; the upper is torso.
                        if (target.Y >= dropped.Y - 5f)
                        {
                            // target is at the bottom, dropped is stacked on top
                            target.TorsoRadius = dropped.BaseRadius;
                        }
                        else
                        {
                            // dropped is at the bottom, target was placed on top
                            float topR = target.BaseRadius;
                            target.BaseRadius = dropped.BaseRadius;
                            target.TorsoRadius = topR;
                            target.Y = dropped.Y;
                            target.X = dropped.X;
                        }

                        if (target.AttachedWindow != IntPtr.Zero && winMgr != null)
                        {
                            Rectangle r;
                            bool moved;
                            if (winMgr.TryGetWindowCurrentBounds(target.AttachedWindow, out r, out moved))
                            {
                                target.Y = r.Top - target.BaseRadius;
                                target.WindowOffsetX = target.X - r.Left;
                                target.WindowOffsetY = r.Top - target.Y;
                            }
                        }
                        _snowballs.Remove(dropped);

                        if (onSnowPuff != null) onSnowPuff(target.X, target.Y - target.BaseRadius, 14);
                        return true;
                    }
                    // Case 2: (2 Balls + 1 Ball) or (1 Ball + 2 Balls) = 3-Ball FULL SNOWMAN!
                    // Snowballs do NOT need to be rolled in a specific order (e.g. small first, medium second, big third works perfectly)!
                    else if ((target.BallCount == 2 && dropped.BallCount == 1) || (target.BallCount == 1 && dropped.BallCount == 2))
                    {
                        if (target.BallCount == 2 && dropped.BallCount == 1)
                        {
                            // target already has base (level 1) and torso (level 2)
                            if (target.Y >= dropped.Y - 15f)
                            {
                                // dropped is added on top as head (level 3)
                                target.HeadRadius = dropped.BaseRadius;
                            }
                            else
                            {
                                // dropped was at the bottom, 2-ball stack was dropped onto it
                                float oldBase = target.BaseRadius;
                                float oldTorso = target.TorsoRadius;
                                target.BaseRadius = dropped.BaseRadius;
                                target.TorsoRadius = oldBase;
                                target.HeadRadius = oldTorso;
                                target.Y = dropped.Y;
                                target.X = dropped.X;
                            }
                        }
                        else // target.BallCount == 1 && dropped.BallCount == 2
                        {
                            float targetR = target.BaseRadius;
                            if (target.Y >= dropped.Y - 15f)
                            {
                                // target was at the bottom, 2-ball stack was dropped on top
                                target.BaseRadius = targetR;
                                target.TorsoRadius = dropped.BaseRadius;
                                target.HeadRadius = dropped.TorsoRadius;
                            }
                            else
                            {
                                // dropped was at the bottom, target was placed on top as head
                                target.BaseRadius = dropped.BaseRadius;
                                target.TorsoRadius = dropped.TorsoRadius;
                                target.HeadRadius = targetR;
                                target.Y = dropped.Y;
                                target.X = dropped.X;
                            }
                        }

                        target.BallCount = 3;
                        target.IsSnowman = true;

                        if (target.AttachedWindow == IntPtr.Zero && dropped.AttachedWindow != IntPtr.Zero)
                        {
                            target.AttachedWindow = dropped.AttachedWindow;
                        }

                        if (target.AttachedWindow != IntPtr.Zero && winMgr != null)
                        {
                            Rectangle r;
                            bool moved;
                            if (winMgr.TryGetWindowCurrentBounds(target.AttachedWindow, out r, out moved))
                            {
                                target.Y = r.Top - target.BaseRadius;
                                target.WindowOffsetX = target.X - r.Left;
                                target.WindowOffsetY = r.Top - target.Y;
                            }
                        }
                        _snowballs.Remove(dropped);

                        // Happy celebratory burst!
                        if (onSnowPuff != null)
                        {
                            onSnowPuff(target.X, target.Y - target.BaseRadius - target.TorsoRadius, 32);
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        public void Draw(Graphics g)
        {
            DrawDebris(g);

            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball s = _snowballs[i];

                if (s.BallCount == 1)
                {
                    DrawSnowballSphere(g, s.X, s.Y, s.BaseRadius, s.Angle, s.Alpha);
                }
                else if (s.BallCount == 2)
                {
                    float r1 = s.BaseRadius;
                    float r2 = s.TorsoRadius;
                    float y1 = s.Y;
                    float overlap12 = Math.Max(2f, Math.Min(r1, r2) * 0.18f);
                    float y2 = y1 - r1 - r2 + overlap12;

                    DrawSnowballSphere(g, s.X, y1, r1, 0f, s.Alpha);
                    DrawSnowballSphere(g, s.X, y2, r2, 0f, s.Alpha);
                }
                else if (s.BallCount >= 3)
                {
                    DrawFullSnowman(g, s);
                }
            }
        }

        private void DrawSnowballSphere(Graphics g, float cx, float cy, float radius, float angle, float alpha = 1.0f)
        {
            if (radius <= 2f) return;
            alpha = Math.Max(0f, Math.Min(1f, alpha));
            if (alpha <= 0.01f) return;

            RectangleF rect = new RectangleF(cx - radius, cy - radius, radius * 2f, radius * 2f);

            // 1. Soft contact shadow under the sphere (proportional to ball size)
            float shadowW = radius * 1.7f;
            float shadowH = Math.Max(4f, radius * 0.30f);
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb((int)(40 * alpha), 15, 30, 50)))
            {
                g.FillEllipse(shadowBrush, cx - shadowW * 0.5f, cy + radius - shadowH * 0.5f, shadowW, shadowH);
            }

            // 2. Volumetric 3D gradient sphere (KoiArt / Snowy aesthetic)
            using (GraphicsPath spherePath = new GraphicsPath())
            {
                spherePath.AddEllipse(rect);
                using (LinearGradientBrush grad = new LinearGradientBrush(
                    new PointF(cx - radius * 0.65f, cy - radius * 0.65f),
                    new PointF(cx + radius * 0.65f, cy + radius * 0.65f),
                    Color.FromArgb((int)(255 * alpha), 255, 255, 255),
                    Color.FromArgb((int)(255 * alpha), 218, 232, 246)))
                {
                    g.FillPath(grad, spherePath);
                }

                // 3. Specular gloss highlight on top-left
                float hlRadius = radius * 0.45f;
                float hlX = cx - radius * 0.30f;
                float hlY = cy - radius * 0.30f;
                using (SolidBrush hlBrush = new SolidBrush(Color.FromArgb((int)(180 * alpha), 255, 255, 255)))
                {
                    g.FillEllipse(hlBrush, hlX - hlRadius, hlY - hlRadius, hlRadius * 2f, hlRadius * 2f);
                }

                // 4. Subtle cool rim outline
                using (Pen rimPen = new Pen(Color.FromArgb((int)(55 * alpha), 135, 165, 195), 1.0f))
                {
                    g.DrawEllipse(rimPen, rect);
                }
            }
        }

        private void DrawFullSnowman(Graphics g, Snowball s)
        {
            float alpha = Math.Max(0f, Math.Min(1f, s.Alpha));
            if (alpha <= 0.01f) return;

            float r1 = s.BaseRadius;
            float r2 = s.TorsoRadius;
            float r3 = s.HeadRadius;
            float y1 = s.Y;
            float overlap12 = Math.Max(2f, Math.Min(r1, r2) * 0.18f);
            float overlap23 = Math.Max(2f, Math.Min(r2, r3) * 0.18f);
            float y2 = y1 - r1 - r2 + overlap12;
            float y3 = y2 - r2 - r3 + overlap23;

            // 1. Wooden Twig Arms (branching from Torso sides, dynamically scaled to torso size)
            float armPenW = Math.Max(1.6f, r2 * 0.10f);
            using (Pen armPen = new Pen(Color.FromArgb((int)(255 * alpha), 95, 65, 45), armPenW))
            {
                armPen.StartCap = LineCap.Round;
                armPen.EndCap = LineCap.Round;

                float armLen = r2 * 0.85f;
                float forkLen = armLen * 0.45f;

                // Left arm
                float lStartX = s.X - r2 + r2 * 0.12f;
                float lStartY = y2 - r2 * 0.10f;
                float lEndX = s.X - r2 - armLen;
                float lEndY = y2 - armLen * 0.45f;
                g.DrawLine(armPen, lStartX, lStartY, lEndX, lEndY);

                float lForkX = s.X - r2 - armLen * 0.50f;
                float lForkY = y2 - armLen * 0.25f;
                g.DrawLine(armPen, lForkX, lForkY, lForkX - forkLen * 0.5f, lForkY - forkLen * 0.75f);

                // Right arm
                float rStartX = s.X + r2 - r2 * 0.12f;
                float rStartY = y2 - r2 * 0.10f;
                float rEndX = s.X + r2 + armLen;
                float rEndY = y2 - armLen * 0.45f;
                g.DrawLine(armPen, rStartX, rStartY, rEndX, rEndY);

                float rForkX = s.X + r2 + armLen * 0.50f;
                float rForkY = y2 - armLen * 0.25f;
                g.DrawLine(armPen, rForkX, rForkY, rForkX + forkLen * 0.5f, rForkY - forkLen * 0.75f);
            }

            // 2. Three Snow Spheres (Base, Torso, Head)
            DrawSnowballSphere(g, s.X, y1, r1, 0f, alpha);
            DrawSnowballSphere(g, s.X, y2, r2, 0f, alpha);
            DrawSnowballSphere(g, s.X, y3, r3, 0f, alpha);

            // 3. Festive Red Scarf (between Head and Torso)
            float neckY = y3 + r3 - overlap23 * 0.4f;
            float collarW = Math.Min(r3 * 2.2f, (r2 + r3) * 1.15f);
            float collarH = Math.Max(4f, Math.Min(r2, r3) * 0.42f);
            using (SolidBrush scarfBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 230, 42, 42)))
            {
                // Collar ellipse around neck
                g.FillEllipse(scarfBrush, s.X - collarW * 0.5f, neckY - collarH * 0.45f, collarW, collarH);

                // Hanging tail over torso
                float tailW = Math.Max(4f, Math.Min(r2, r3) * 0.32f);
                float tailL = Math.Min(r2 * 0.95f, r3 * 1.25f);
                PointF[] tail = new PointF[]
                {
                    new PointF(s.X + r3 * 0.10f, neckY),
                    new PointF(s.X + r3 * 0.10f + tailW, neckY),
                    new PointF(s.X + r3 * 0.10f + tailW * 0.88f, neckY + tailL),
                    new PointF(s.X + r3 * 0.10f, neckY + tailL)
                };
                g.FillPolygon(scarfBrush, tail);

                // Scarf white fringe lines at bottom of tail
                float fringePenW = Math.Max(1.0f, r3 * 0.08f);
                using (Pen fringePen = new Pen(Color.FromArgb((int)(255 * alpha), 255, 255, 255), fringePenW))
                {
                    float tailBot = neckY + tailL;
                    float fringeH = Math.Max(2f, r3 * 0.15f);
                    float fStep = tailW * 0.28f;
                    g.DrawLine(fringePen, s.X + r3 * 0.10f + fStep * 0.5f, tailBot - fringeH * 0.5f, s.X + r3 * 0.10f + fStep * 0.5f, tailBot + fringeH * 0.5f);
                    g.DrawLine(fringePen, s.X + r3 * 0.10f + fStep * 1.5f, tailBot - fringeH * 0.5f, s.X + r3 * 0.10f + fStep * 1.5f, tailBot + fringeH * 0.5f);
                    g.DrawLine(fringePen, s.X + r3 * 0.10f + fStep * 2.5f, tailBot - fringeH * 0.5f, s.X + r3 * 0.10f + fStep * 2.5f, tailBot + fringeH * 0.5f);
                }
            }

            // 4. Coal Buttons down Torso
            float btnRadius = Math.Max(1.6f, r2 * 0.11f);
            using (SolidBrush coalBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 35, 40, 45)))
            using (SolidBrush gleamBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 255, 255, 255)))
            {
                float[] btnOffsets = new float[] { -r2 * 0.22f, r2 * 0.16f, r2 * 0.54f };
                for (int b = 0; b < btnOffsets.Length; b++)
                {
                    float by = y2 + btnOffsets[b];
                    g.FillEllipse(coalBrush, s.X - btnRadius, by - btnRadius, btnRadius * 2f, btnRadius * 2f);
                    g.FillEllipse(gleamBrush, s.X - btnRadius * 0.45f, by - btnRadius * 0.65f, btnRadius * 0.65f, btnRadius * 0.65f);
                }

                // 5. Face (Eyes, Smile)
                float eyeDist = r3 * 0.35f;
                float eyeRadius = Math.Max(1.4f, r3 * 0.15f);
                float eyeY = y3 - r3 * 0.12f;

                // Left eye
                g.FillEllipse(coalBrush, s.X - eyeDist - eyeRadius, eyeY - eyeRadius, eyeRadius * 2f, eyeRadius * 2f);
                g.FillEllipse(gleamBrush, s.X - eyeDist - eyeRadius * 0.45f, eyeY - eyeRadius * 0.65f, eyeRadius * 0.65f, eyeRadius * 0.65f);

                // Right eye
                g.FillEllipse(coalBrush, s.X + eyeDist - eyeRadius, eyeY - eyeRadius, eyeRadius * 2f, eyeRadius * 2f);
                g.FillEllipse(gleamBrush, s.X + eyeDist - eyeRadius * 0.45f, eyeY - eyeRadius * 0.65f, eyeRadius * 0.65f, eyeRadius * 0.65f);

                // Smile dots
                float smileRadius = Math.Max(1.0f, r3 * 0.07f);
                float smSpread = r3 * 0.45f;
                float smBaseY = y3 + r3 * 0.36f;
                float[] smXRel = new float[] { -smSpread, -smSpread * 0.38f, smSpread * 0.38f, smSpread };
                float[] smYRel = new float[] { -r3 * 0.08f, 0f, 0f, -r3 * 0.08f };
                for (int sm = 0; sm < 4; sm++)
                {
                    g.FillEllipse(coalBrush, s.X + smXRel[sm] - smileRadius, smBaseY + smYRel[sm] - smileRadius, smileRadius * 2f, smileRadius * 2f);
                }
            }

            // 6. Bright Orange Carrot Nose (pointing right)
            float carrotH = Math.Max(2.4f, r3 * 0.24f);
            float carrotL = Math.Max(6f, r3 * 0.72f);
            float noseX = s.X - r3 * 0.05f;
            float noseY = y3 + r3 * 0.06f;
            using (SolidBrush carrotBrush = new SolidBrush(Color.FromArgb((int)(255 * alpha), 255, 115, 0)))
            {
                PointF[] nose = new PointF[]
                {
                    new PointF(noseX, noseY - carrotH * 0.5f),
                    new PointF(noseX, noseY + carrotH * 0.5f),
                    new PointF(noseX + carrotL, noseY + carrotH * 0.08f)
                };
                g.FillPolygon(carrotBrush, nose);
            }

            // 7. Cozy Red Santa Hat (Optional Christmas Accessory)
            if (Christmas != null && Christmas.ShowSantaHat)
            {
                float brimW = r3 * 1.55f;
                float brimH = Math.Max(5f, r3 * 0.36f);
                float brimY = y3 - r3 * 0.82f;

                // Red velvet cap body gracefully folding over to the right
                float tipX = s.X + r3 * 1.15f;
                float tipY = brimY - r3 * 0.25f;

                using (GraphicsPath hatPath = new GraphicsPath())
                {
                    // Left curve up to peak
                    hatPath.AddBezier(
                        new PointF(s.X - brimW * 0.44f, brimY - 2f),
                        new PointF(s.X - brimW * 0.48f, brimY - r3 * 0.85f),
                        new PointF(s.X - r3 * 0.35f, brimY - r3 * 1.55f),
                        new PointF(s.X - r3 * 0.05f, brimY - r3 * 1.55f)
                    );

                    // Right curve drooping down to tip
                    hatPath.AddBezier(
                        new PointF(s.X - r3 * 0.05f, brimY - r3 * 1.55f),
                        new PointF(s.X + r3 * 0.55f, brimY - r3 * 1.55f),
                        new PointF(tipX + 8f, brimY - r3 * 0.95f),
                        new PointF(tipX, tipY)
                    );

                    // Inner fold curve going back to right base of brim
                    hatPath.AddBezier(
                        new PointF(tipX, tipY),
                        new PointF(tipX - 10f, tipY - 16f),
                        new PointF(s.X + r3 * 0.65f, brimY - r3 * 0.85f),
                        new PointF(s.X + brimW * 0.44f, brimY - 2f)
                    );
                    hatPath.CloseFigure();

                    RectangleF hatBounds = hatPath.GetBounds();
                    using (LinearGradientBrush hatGrad = new LinearGradientBrush(
                        new PointF(hatBounds.Left, hatBounds.Top),
                        new PointF(hatBounds.Right, hatBounds.Bottom),
                        Color.FromArgb((int)(255 * alpha), 235, 45, 55),
                        Color.FromArgb((int)(255 * alpha), 170, 20, 25)))
                    {
                        g.FillPath(hatGrad, hatPath);
                    }
                }

                // Highlight along hat fold
                using (GraphicsPath hlPath = new GraphicsPath())
                {
                    hlPath.AddBezier(
                        new PointF(s.X - brimW * 0.38f, brimY - 6f),
                        new PointF(s.X - brimW * 0.40f, brimY - r3 * 0.8f),
                        new PointF(s.X - r3 * 0.25f, brimY - r3 * 1.40f),
                        new PointF(s.X - r3 * 0.05f, brimY - r3 * 1.40f)
                    );
                    using (Pen hlPen = new Pen(Color.FromArgb((int)(65 * alpha), 255, 255, 255), Math.Max(2f, r3 * 0.08f)))
                    {
                        g.DrawPath(hlPen, hlPath);
                    }
                }

                // White plush brim (curved rounded rectangle)
                RectangleF brimRect = new RectangleF(s.X - brimW * 0.5f, brimY - brimH * 0.5f, brimW, brimH);
                using (GraphicsPath brimPath = VectorIcons.CreateRoundedRect(brimRect, brimH * 0.5f))
                using (LinearGradientBrush brimGrad = new LinearGradientBrush(
                    new PointF(s.X, brimY - brimH * 0.5f),
                    new PointF(s.X, brimY + brimH * 0.5f),
                    Color.FromArgb((int)(255 * alpha), 255, 255, 255),
                    Color.FromArgb((int)(255 * alpha), 225, 230, 240)))
                {
                    g.FillPath(brimGrad, brimPath);
                }

                // White fluffy pom-pom at tip
                float pomR = Math.Max(4f, r3 * 0.32f);
                using (LinearGradientBrush pomBrush = new LinearGradientBrush(
                    new PointF(tipX - pomR, tipY - pomR),
                    new PointF(tipX + pomR, tipY + pomR),
                    Color.FromArgb((int)(255 * alpha), 255, 255, 255),
                    Color.FromArgb((int)(255 * alpha), 215, 222, 235)))
                using (Pen pomPen = new Pen(Color.FromArgb((int)(50 * alpha), 180, 190, 200), 1f))
                {
                    g.FillEllipse(pomBrush, tipX - pomR, tipY - pomR, pomR * 2f, pomR * 2f);
                    g.DrawEllipse(pomPen, tipX - pomR, tipY - pomR, pomR * 2f, pomR * 2f);
                }
            }
        }
    }
}
