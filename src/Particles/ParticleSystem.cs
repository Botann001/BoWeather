using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BoWeather.Particles
{
    public class ParticleSystem
    {
        private readonly List<Particle> _particles = new List<Particle>();
        private readonly Random _rand = new Random();
        private EffectType _currentEffect = EffectType.Snow;
        private DensityLevel _density = DensityLevel.Medium;
        private WindLevel _wind = WindLevel.Breeze;
        private WindDirection _windDirection = WindDirection.West; // Default West (blowing left) as in Snowy!
        private bool _enableWindowPhysics = true;
        private int _screenWidth = 1920;
        private int _screenHeight = 1080;
        private float _tick = 0f;
        private int _prevMouseX = -1;
        private int _prevMouseY = -1;
        private readonly SnowballManager _snowballManager = new SnowballManager();

        public SnowballManager Snowballs { get { return _snowballManager; } }

        public EffectType CurrentEffect
        {
            get { return _currentEffect; }
            set
            {
                _currentEffect = value;
                if (value != EffectType.Snow) _snowballManager.Clear();
                RebuildParticles();
            }
        }

        public DensityLevel Density
        {
            get { return _density; }
            set
            {
                _density = value;
                RebuildParticles();
            }
        }

        public WindLevel Wind
        {
            get { return _wind; }
            set { _wind = value; }
        }

        public WindDirection Direction
        {
            get { return _windDirection; }
            set { _windDirection = value; }
        }

        public bool EnableWindowPhysics
        {
            get { return _enableWindowPhysics; }
            set { _enableWindowPhysics = value; }
        }

        public ParticleSystem(int screenWidth, int screenHeight)
        {
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            RebuildParticles();
        }

        public void SetScreenDimensions(int width, int height)
        {
            _screenWidth = width;
            _screenHeight = height;
            RebuildParticles();
        }

        public void RebuildParticles()
        {
            _particles.Clear();
            float scale = (_screenWidth * _screenHeight) / (1920f * 1080f);
            if (scale < 1.0f) scale = 1.0f;
            int count = (int)((int)_density * scale);

            for (int i = 0; i < count; i++)
            {
                Particle p = CreateParticle(true);
                _particles.Add(p);
            }
        }

        private Particle CreateParticle(bool randomizeY)
        {
            Particle p = new Particle();
            p.X = (float)(_rand.NextDouble() * (_screenWidth + 60) - 30);
            p.Y = randomizeY ? (float)(_rand.NextDouble() * _screenHeight) : (float)(-_rand.Next(10, 80));
            p.PrevY = p.Y;
            p.FlutterPhase = (float)(_rand.NextDouble() * Math.PI * 2);
            p.FlutterSpeed = (float)(0.02 + _rand.NextDouble() * 0.04);
            p.Angle = (float)(_rand.NextDouble() * 360);
            p.AngularSpeed = (float)((_rand.NextDouble() - 0.5) * 2.5);
            p.MaxRestTime = _rand.Next(200, 500);
            p.RestTimer = 0;
            p.SplashFrame = 0;
            p.Variant = _rand.Next(0, 4);
            p.Alpha = 1.0f;

            switch (_currentEffect)
            {
                case EffectType.Sakura:
                    p.Size = (float)(10 + _rand.NextDouble() * 10);
                    p.BaseVy = (float)(1.2 + _rand.NextDouble() * 1.5);
                    p.Vy = p.BaseVy;
                    p.Vx = (float)((_rand.NextDouble() - 0.5) * 1.2);
                    int pinkR = 250 + _rand.Next(0, 6);
                    int pinkG = 180 + _rand.Next(0, 30);
                    int pinkB = 200 + _rand.Next(0, 35);
                    p.Color = Color.FromArgb(215, pinkR, pinkG, pinkB);
                    break;

                case EffectType.Snow:
                    p.Size = (float)(2.8 + _rand.NextDouble() * 4.2);
                    p.BaseVy = (float)(1.2 + _rand.NextDouble() * 1.8);
                    p.Vy = p.BaseVy;
                    p.Vx = (float)((_rand.NextDouble() - 0.5) * 0.6);
                    int alpha = 190 + _rand.Next(0, 65);
                    p.Color = Color.FromArgb(alpha, 255, 255, 255);
                    break;

                case EffectType.AutumnLeaves:
                    p.Size = (float)(14 + _rand.NextDouble() * 12);
                    p.BaseVy = (float)(1.5 + _rand.NextDouble() * 1.8);
                    p.Vy = p.BaseVy;
                    p.Vx = (float)((_rand.NextDouble() - 0.5) * 2.0);
                    int palette = _rand.Next(0, 4);
                    if (palette == 0) p.Color = Color.FromArgb(220, 205, 55, 30);
                    else if (palette == 1) p.Color = Color.FromArgb(220, 235, 120, 25);
                    else if (palette == 2) p.Color = Color.FromArgb(220, 240, 175, 35);
                    else p.Color = Color.FromArgb(220, 180, 70, 35);
                    break;

                case EffectType.Rain:
                    p.Size = (float)(14 + _rand.NextDouble() * 10);
                    p.BaseVy = (float)(18.0 + _rand.NextDouble() * 9.0);
                    p.Vy = p.BaseVy;
                    p.Vx = 0f;
                    int rainAlpha = 140 + _rand.Next(0, 80);
                    p.Color = Color.FromArgb(rainAlpha, 210, 230, 255);
                    break;

                case EffectType.Fireflies:
                    p.Size = (float)(6 + _rand.NextDouble() * 5);
                    p.BaseVy = (float)((_rand.NextDouble() - 0.5) * 0.8);
                    p.Vy = p.BaseVy;
                    p.Vx = (float)((_rand.NextDouble() - 0.5) * 0.8);
                    p.Color = Color.FromArgb(240, 220, 255, 110);
                    break;
            }

            return p;
        }

        public void SpawnSnowPuff(float x, float y, int count)
        {
            if (_particles.Count == 0 || count <= 0) return;
            int n = Math.Min(count, _particles.Count);
            for (int i = 0; i < n; i++)
            {
                Particle p = _particles[i];
                p.X = x + (float)(_rand.NextDouble() - 0.5) * 14f;
                p.Y = y + (float)(_rand.NextDouble() - 0.5) * 14f;
                p.State = ParticleState.Falling;
                double angle = _rand.NextDouble() * Math.PI * 2.0;
                double speed = 2.0 + _rand.NextDouble() * 4.0;
                p.Vx = (float)(Math.Cos(angle) * speed);
                // Shower downwards naturally with gravity (positive Vy):
                p.Vy = (float)(Math.Abs(Math.Sin(angle)) * speed * 0.45 + 1.2 + _rand.NextDouble() * 2.5);
                p.Alpha = 1.0f;
            }
        }

        public void Update(WindowManager winMgr, int mouseX, int mouseY)
        {
            Update(winMgr, mouseX, mouseY, false);
        }

        public void Update(WindowManager winMgr, int mouseX, int mouseY, bool isMouseDown)
        {
            _tick += 0.03f;
            float baseWind = 0f;
            if (_wind == WindLevel.Breeze) baseWind = 1.0f;
            else if (_wind == WindLevel.Windy) baseWind = 2.4f;

            float dirSign = (_windDirection == WindDirection.West) ? -1.0f : 1.0f;
            float directionalWind = dirSign * baseWind;
            float dynamicWind = directionalWind + (float)Math.Sin(_tick * 0.5f) * (baseWind * 0.35f);

            // Handle window shake / fling: snow falls DOWN to taskbar and becomes snow pile!
            if (_enableWindowPhysics && winMgr != null)
            {
                List<WindowInfo> windows = winMgr.ActiveWindows;
                for (int w = 0; w < windows.Count; w++)
                {
                    WindowInfo win = windows[w];
                    if (win.IsFlung)
                    {
                        win.IsFlung = false;
                        ShakeOffWindowSnow(win, winMgr);
                    }
                }
            }

            UpdateMouseSnowSweep(winMgr, mouseX, mouseY);

            // Update interactive snowball rolling, dragging, stacking and snowmen!
            if (_currentEffect == EffectType.Snow)
            {
                _snowballManager.Update(winMgr, mouseX, mouseY, isMouseDown, _screenWidth, _screenHeight, SpawnSnowPuff);
            }

            for (int i = 0; i < _particles.Count; i++)
            {
                Particle p = _particles[i];

                if (p.State == ParticleState.Splashing)
                {
                    p.SplashFrame++;
                    if (p.SplashFrame > 8)
                    {
                        ResetParticle(p);
                    }
                    continue;
                }

                if (p.State == ParticleState.Resting)
                {
                    p.RestTimer++;

                    Rectangle winBounds;
                    bool hasMoved;
                    if (winMgr != null && _enableWindowPhysics && winMgr.TryGetWindowCurrentBounds(p.RestingWindowHandle, out winBounds, out hasMoved))
                    {
                        if (hasMoved)
                        {
                            p.State = ParticleState.Falling;
                            p.Vx = (float)((_rand.NextDouble() - 0.5) * 3.0);
                            p.Vy = (float)(2.0 + _rand.NextDouble() * 3.0); // drop downwards!
                        }
                        else
                        {
                            p.X = winBounds.Left + p.RestingOffsetX;
                            p.Y = winBounds.Top - p.Size * 0.4f;
                        }
                    }
                    else
                    {
                        p.State = ParticleState.Falling;
                        p.Vx = (float)((_rand.NextDouble() - 0.5) * 2.0);
                        p.Vy = (float)(1.5 + _rand.NextDouble() * 2.0);
                    }

                    if (p.RestTimer > p.MaxRestTime)
                    {
                        if (_currentEffect == EffectType.Snow)
                        {
                            p.Alpha -= 0.02f;
                            if (p.Alpha <= 0f) ResetParticle(p);
                        }
                        else
                        {
                            p.State = ParticleState.Falling;
                            p.Vx = (float)(2.0 + _rand.NextDouble() * 2.5);
                            p.Vy = (float)(0.5 + _rand.NextDouble() * 1.0);
                        }
                    }
                    continue;
                }

                p.PrevY = p.Y;
                p.FlutterPhase += p.FlutterSpeed;
                p.Angle += p.AngularSpeed;

                if (_currentEffect == EffectType.Fireflies)
                {
                    p.Vx = (float)Math.Cos(p.FlutterPhase * 0.7f) * 1.2f;
                    p.Vy = (float)Math.Sin(p.FlutterPhase * 0.5f) * 1.0f;
                    p.X += p.Vx;
                    p.Y += p.Vy;

                    if (p.X < -50) p.X = _screenWidth + 30;
                    if (p.X > _screenWidth + 50) p.X = -30;
                    if (p.Y < -50) p.Y = _screenHeight + 30;
                    if (p.Y > _screenHeight + 50) p.Y = -30;
                    continue;
                }

                if (_currentEffect == EffectType.Rain)
                {
                    p.X += dynamicWind * 1.8f;
                    p.Y += p.Vy;
                }
                else
                {
                    float sway = (float)Math.Sin(p.FlutterPhase) * (_currentEffect == EffectType.AutumnLeaves ? 2.2f : 0.8f);
                    p.X += sway + dynamicWind;
                    p.Y += p.Vy;
                }

                // Window collision on floating windows (top ledge)
                if (_enableWindowPhysics && winMgr != null)
                {
                    IntPtr hitHandle;
                    int winTop;
                    float hitOffsetX;

                    if (winMgr.CheckLedgeCollision(p.X, p.Y, p.PrevY, out hitHandle, out winTop, out hitOffsetX))
                    {
                        if (_currentEffect == EffectType.Rain)
                        {
                            p.State = ParticleState.Splashing;
                            p.Y = winTop - 2;
                            p.SplashFrame = 0;
                            continue;
                        }
                        else if (_currentEffect == EffectType.Snow)
                        {
                            ResetParticle(p);
                            continue;
                        }
                        else
                        {
                            p.State = ParticleState.Resting;
                            p.RestingWindowHandle = hitHandle;
                            p.RestingOffsetX = hitOffsetX;
                            p.Y = winTop - p.Size * 0.4f;
                            p.RestTimer = 0;
                            p.Alpha = 1.0f;
                            continue;
                        }
                    }
                }

                // Floor snow collision at bottom of screen
                if (p.Y > _screenHeight - 14)
                {
                    if (_currentEffect == EffectType.Snow && winMgr != null && winMgr.FloorSnowPoints != null)
                    {
                        float rel = p.X / _screenWidth;
                        if (rel >= 0f && rel <= 1f)
                        {
                            int col = (int)(rel * (winMgr.FloorSnowPoints.Length - 1));
                            if (col >= 0 && col < winMgr.FloorSnowPoints.Length)
                            {
                                AddSnowFloor(winMgr, col, 2.2f);
                                if (col > 0) AddSnowFloor(winMgr, col - 1, 1.5f);
                                if (col < winMgr.FloorSnowPoints.Length - 1) AddSnowFloor(winMgr, col + 1, 1.5f);
                                if (col > 1) AddSnowFloor(winMgr, col - 2, 0.8f);
                                if (col < winMgr.FloorSnowPoints.Length - 2) AddSnowFloor(winMgr, col + 2, 0.8f);
                            }
                        }
                    }
                    ResetParticle(p);
                }
                else if (p.X < -50)
                {
                    p.X = _screenWidth + 25;
                    p.Y = (float)(_rand.NextDouble() * _screenHeight * 0.7);
                }
                else if (p.X > _screenWidth + 50)
                {
                    p.X = -25;
                    p.Y = (float)(_rand.NextDouble() * _screenHeight * 0.7);
                }
            }

            // Soft natural snow settling during snow weather
            // Gradually restores swept areas over ~3-4 seconds
            if (_currentEffect == EffectType.Snow && winMgr != null)
            {
                if (winMgr.FloorSnowPoints != null && winMgr.MaxFloorSnowPoints != null)
                {
                    for (int p = 0; p < winMgr.FloorSnowPoints.Length; p++)
                    {
                        if (winMgr.FloorSnowPoints[p] < winMgr.MaxFloorSnowPoints[p])
                        {
                            winMgr.FloorSnowPoints[p] = Math.Min(winMgr.MaxFloorSnowPoints[p], winMgr.FloorSnowPoints[p] + 0.075f);
                        }
                    }
                }

                List<WindowInfo> windows = winMgr.ActiveWindows;
                for (int w = 0; w < windows.Count; w++)
                {
                    WindowInfo win = windows[w];
                    if (win.SnowHeights != null && win.MaxSnowHeights != null)
                    {
                        for (int p = 0; p < win.SnowHeights.Length; p++)
                        {
                            if (win.SnowHeights[p] < win.MaxSnowHeights[p])
                            {
                                win.SnowHeights[p] = Math.Min(win.MaxSnowHeights[p], win.SnowHeights[p] + 0.075f);
                            }
                        }
                    }
                }
            }

            _prevMouseX = mouseX;
            _prevMouseY = mouseY;
        }

        private static void AddSnowFloor(WindowManager winMgr, int col, float amount)
        {
            if (winMgr.FloorSnowPoints[col] < winMgr.MaxFloorSnowPoints[col])
            {
                winMgr.FloorSnowPoints[col] = Math.Min(winMgr.MaxFloorSnowPoints[col], winMgr.FloorSnowPoints[col] + amount);
            }
        }

        private void ShakeOffWindowSnow(WindowInfo win, WindowManager winMgr)
        {
            if (_currentEffect != EffectType.Snow || win == null || win.SnowHeights == null) return;

            float snowLeft = win.Bounds.Left + 3f;
            float snowWidth = win.Bounds.Width - 6f;
            if (snowWidth <= 0f) return;
            float step = snowWidth / (win.SnowHeights.Length - 1);

            float totalSnowFallen = 0f;

            // 1. Transfer window snow down to the taskbar/floor directly below!
            if (winMgr.FloorSnowPoints != null && winMgr.MaxFloorSnowPoints != null)
            {
                float floorStep = (float)_screenWidth / (winMgr.FloorSnowPoints.Length - 1);

                for (int p = 0; p < win.SnowHeights.Length; p++)
                {
                    float h = win.SnowHeights[p];
                    if (h > 0.5f)
                    {
                        totalSnowFallen += h;
                        float colX = snowLeft + p * step;
                        if (colX >= 0 && colX < _screenWidth)
                        {
                            int floorCol = (int)(colX / floorStep);
                            if (floorCol >= 0 && floorCol < winMgr.FloorSnowPoints.Length)
                            {
                                // Pile up on taskbar/floor below!
                                winMgr.FloorSnowPoints[floorCol] = Math.Min(winMgr.MaxFloorSnowPoints[floorCol] * 1.6f, winMgr.FloorSnowPoints[floorCol] + h * 0.7f);
                                if (floorCol > 0)
                                    winMgr.FloorSnowPoints[floorCol - 1] = Math.Min(winMgr.MaxFloorSnowPoints[floorCol - 1] * 1.6f, winMgr.FloorSnowPoints[floorCol - 1] + h * 0.4f);
                                if (floorCol < winMgr.FloorSnowPoints.Length - 1)
                                    winMgr.FloorSnowPoints[floorCol + 1] = Math.Min(winMgr.MaxFloorSnowPoints[floorCol + 1] * 1.6f, winMgr.FloorSnowPoints[floorCol + 1] + h * 0.4f);
                            }
                        }
                        win.SnowHeights[p] = 0f; // snow cleared from shaken window!
                    }
                }
            }

            // 2. Cascade falling snowflakes showering down from the window towards the floor!
            // Reusing existing particles from the fixed pool - NO dynamic allocations!
            if (totalSnowFallen > 2.0f && _particles.Count > 0)
            {
                int cascadeCount = Math.Min(25, _particles.Count / 3);
                for (int i = 0; i < cascadeCount; i++)
                {
                    Particle p = _particles[i];
                    p.X = win.Bounds.Left + 10f + (float)_rand.NextDouble() * Math.Max(20f, win.Bounds.Width - 20f);
                    p.Y = win.Bounds.Top + (float)_rand.NextDouble() * 4f;
                    p.State = ParticleState.Falling;
                    p.Vy = (float)(4.5 + _rand.NextDouble() * 3.5); // fast downward drop!
                    p.Vx = (float)((_rand.NextDouble() - 0.5) * 3.0);
                    p.Alpha = 1.0f;
                }
            }
        }

        private void UpdateMouseSnowSweep(WindowManager winMgr, int mouseX, int mouseY)
        {
            if (_currentEffect != EffectType.Snow || winMgr == null) return;
            if (mouseX < 0 || mouseY < 0) return;

            bool mouseMoved = (_prevMouseX >= 0) && (Math.Abs(mouseX - _prevMouseX) > 1 || Math.Abs(mouseY - _prevMouseY) > 1);
            if (!mouseMoved) return;

            float sweepRadius = 26.0f;

            // Sweep on active floating windows (snow is above the window: b.Top - 25 to b.Top + 10)
            List<WindowInfo> windows = winMgr.ActiveWindows;
            for (int w = 0; w < windows.Count; w++)
            {
                WindowInfo win = windows[w];
                if (win.SnowHeights == null) continue;
                Rectangle b = win.Bounds;
                if (mouseX >= b.Left - 15 && mouseX <= b.Right + 15 && mouseY >= b.Top - 25 && mouseY <= b.Top + 10)
                {
                    float snowLeft = b.Left + 3f;
                    float snowWidth = b.Width - 6f;
                    if (snowWidth <= 0f) continue;
                    float step = snowWidth / (win.SnowHeights.Length - 1);

                    for (int p = 0; p < win.SnowHeights.Length; p++)
                    {
                        float colX = snowLeft + p * step;
                        float dist = Math.Abs(mouseX - colX);
                        if (dist < sweepRadius)
                        {
                            float ratio = dist / sweepRadius;
                            // Smooth sine curve: scoops cleanly directly under cursor, leaves edge untouched
                            float factor = (float)Math.Sin(ratio * Math.PI * 0.5);
                            float targetH = win.MaxSnowHeights[p] * factor;
                            if (win.SnowHeights[p] > targetH)
                            {
                                win.SnowHeights[p] = targetH;
                            }
                        }
                    }
                }
            }

            // Sweep on taskbar / floor snow
            if (winMgr.FloorSnowPoints != null && mouseY >= _screenHeight - 50)
            {
                float step = (float)_screenWidth / (winMgr.FloorSnowPoints.Length - 1);
                for (int p = 0; p < winMgr.FloorSnowPoints.Length; p++)
                {
                    float colX = p * step;
                    float dist = Math.Abs(mouseX - colX);
                    if (dist < sweepRadius)
                    {
                        float ratio = dist / sweepRadius;
                        float factor = (float)Math.Sin(ratio * Math.PI * 0.5);
                        float targetH = winMgr.MaxFloorSnowPoints[p] * factor;
                        if (winMgr.FloorSnowPoints[p] > targetH)
                        {
                            winMgr.FloorSnowPoints[p] = targetH;
                        }
                    }
                }
            }
        }

        private void ResetParticle(Particle p)
        {
            p.X = (float)(_rand.NextDouble() * (_screenWidth + 60) - 30);
            p.Y = (float)(-_rand.Next(10, 60));
            p.PrevY = p.Y;
            p.State = ParticleState.Falling;
            p.RestTimer = 0;
            p.SplashFrame = 0;
            p.Alpha = 1.0f;
            p.MaxRestTime = _rand.Next(200, 500);
        }

        public void Draw(Graphics g, WindowManager winMgr)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Draw Puffy Snow Caps ONLY on Floating Windows and Floor
            if (_currentEffect == EffectType.Snow && _enableWindowPhysics && winMgr != null)
            {
                List<WindowInfo> windows = winMgr.ActiveWindows;
                for (int w = 0; w < windows.Count; w++)
                {
                    WindowInfo win = windows[w];
                    if (win.SnowHeights != null)
                    {
                        DrawPuffySnowCap(g, win.Bounds.Left, win.Bounds.Top + 0.5f, win.Bounds.Width, win.SnowHeights, true);
                    }
                }

                // Draw Floor Snow at bottom of screen
                if (winMgr.FloorSnowPoints != null)
                {
                    DrawPuffySnowCap(g, 0, _screenHeight, _screenWidth, winMgr.FloorSnowPoints, false);
                }

                // Draw Interactive Snowballs and Snowmen!
                _snowballManager.Draw(g);
            }

            // 2. Draw All Falling Particles across the entire screen
            for (int i = 0; i < _particles.Count; i++)
            {
                Particle p = _particles[i];

                if (p.State == ParticleState.Splashing)
                {
                    DrawRainSplash(g, p);
                    continue;
                }

                switch (_currentEffect)
                {
                    case EffectType.Sakura:
                        DrawSakuraPetal(g, p);
                        break;
                    case EffectType.Snow:
                        DrawSnowflake(g, p);
                        break;
                    case EffectType.AutumnLeaves:
                        DrawAutumnLeaf(g, p);
                        break;
                    case EffectType.Rain:
                        DrawRaindrop(g, p);
                        break;
                    case EffectType.Fireflies:
                        DrawFirefly(g, p);
                        break;
                }
            }
        }

        private void DrawPuffySnowCap(Graphics g, float left, float top, float width, float[] heights, bool isWindow)
        {
            if (heights == null || heights.Length < 2 || width <= 16f) return;

            int n = heights.Length;
            float snowLeft = isWindow ? (left + 3f) : left;
            float snowWidth = isWindow ? (width - 6f) : width;
            if (snowWidth <= 10f) return;
            float step = snowWidth / (n - 1);

            // 1. Check max height across the snow bank
            float maxH = 0f;
            for (int i = 0; i < n; i++)
            {
                if (heights[i] > maxH) maxH = heights[i];
            }
            if (maxH < 0.5f) return;

            // 2. Smooth heights using 3-point moving average to eliminate any discrete spikes
            float[] smoothH = new float[n];
            for (int i = 0; i < n; i++)
            {
                float prev = (i > 0) ? heights[i - 1] : heights[i];
                float next = (i < n - 1) ? heights[i + 1] : heights[i];
                smoothH[i] = (prev + heights[i] * 2.0f + next) * 0.25f;
            }

            // 3. Top contour: unbroken continuous crest rising UPWARDS above the window ledge
            List<PointF> topPts = new List<PointF>(n + 4);
            topPts.Add(new PointF(snowLeft, top));

            for (int i = 0; i < n; i++)
            {
                float x = snowLeft + i * step;
                float h = smoothH[i];

                if (isWindow)
                {
                    // Soft rounded corner fillet at window edges
                    float distEdge = Math.Min(x - snowLeft, (snowLeft + snowWidth) - x);
                    if (distEdge < 18f)
                    {
                        float u = distEdge / 18f;
                        h *= (float)Math.Sin(u * Math.PI * 0.5);
                    }
                }

                if (h < 0.1f) h = 0f;
                topPts.Add(new PointF(x, top - h));
            }

            topPts.Add(new PointF(snowLeft + snowWidth, top));

            // 4. Bottom contour: rests clean and flat on the window ledge, with soft rounded corner fillets
            List<PointF> botPts = new List<PointF>();
            int botSteps = Math.Max(12, (int)(snowWidth / 6.0f));
            float botStep = snowWidth / botSteps;

            for (int s = botSteps; s >= 0; s--)
            {
                float x = snowLeft + s * botStep;
                float droop = 0f;

                if (isWindow)
                {
                    // Clean flat baseline along window ledge, wrapping gently around corner radius
                    float distEdge = Math.Min(x - snowLeft, (snowLeft + snowWidth) - x);
                    if (distEdge < 14f)
                    {
                        float u = 1.0f - (distEdge / 14f);
                        droop = (float)Math.Sin(u * Math.PI * 0.5) * 2.0f;
                    }
                }
                else
                {
                    // Extend down past the bottom of the screen for floor snow
                    droop = 25f;
                }

                botPts.Add(new PointF(x, top + droop));
            }

            // 5. Fill volumetric gradient (Snowy / KoiArt aesthetic)
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddCurve(topPts.ToArray(), 0.45f);
                path.AddCurve(botPts.ToArray(), 0.45f);
                path.CloseFigure();

                float capTop = top - Math.Max(12.0f, maxH);
                float capBottom = top + (isWindow ? 2.0f : 25.0f);

                if (capBottom > capTop)
                {
                    using (LinearGradientBrush snowGrad = new LinearGradientBrush(
                        new PointF(0, capTop),
                        new PointF(0, capBottom),
                        Color.FromArgb(255, 255, 255, 255),
                        Color.FromArgb(255, 222, 235, 248)))
                    {
                        g.FillPath(snowGrad, path);
                    }
                }
                else
                {
                    using (SolidBrush solid = new SolidBrush(Color.White))
                    {
                        g.FillPath(solid, path);
                    }
                }

                // 6. Volumetric lighting lines
                if (isWindow)
                {
                    // Soft cool bottom shadow along the ledge
                    using (Pen shadowPen = new Pen(Color.FromArgb(65, 135, 165, 195), 1.0f))
                    {
                        g.DrawCurve(shadowPen, botPts.ToArray(), 0.45f);
                    }
                }

                // Crystalline crest highlight along top ridge
                using (Pen crestPen = new Pen(Color.FromArgb(240, 255, 255, 255), 1.0f))
                {
                    g.DrawCurve(crestPen, topPts.ToArray(), 0.45f);
                }
            }
        }

        private void DrawSakuraPetal(Graphics g, Particle p)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(p.X, p.Y);
            g.RotateTransform(p.Angle);

            float tumble = (p.State == ParticleState.Resting) ? 1.0f : (float)Math.Cos(p.FlutterPhase);
            if (Math.Abs(tumble) < 0.15f) tumble = (tumble < 0 ? -0.15f : 0.15f);
            g.ScaleTransform(tumble, 1.0f);

            int alpha = (int)(p.Color.A * p.Alpha);
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            Color drawCol = Color.FromArgb(alpha, p.Color.R, p.Color.G, p.Color.B);

            using (SolidBrush brush = new SolidBrush(drawCol))
            {
                float w = p.Size * 0.7f;
                float h = p.Size;

                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddBezier(0, h * 0.5f, -w * 0.6f, h * 0.1f, -w * 0.7f, -h * 0.3f, -w * 0.2f, -h * 0.5f);
                    path.AddLine(-w * 0.2f, -h * 0.5f, 0, -h * 0.35f);
                    path.AddLine(0, -h * 0.35f, w * 0.2f, -h * 0.5f);
                    path.AddBezier(w * 0.2f, -h * 0.5f, w * 0.7f, -h * 0.3f, w * 0.6f, h * 0.1f, 0, h * 0.5f);
                    path.CloseFigure();
                    g.FillPath(brush, path);
                }
            }

            g.Restore(state);
        }

        private void DrawSnowflake(Graphics g, Particle p)
        {
            int alpha = (int)(p.Color.A * p.Alpha);
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;

            float radius = p.Size * 0.5f;
            using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb((int)(alpha * 0.30f), 220, 240, 255)))
            {
                g.FillEllipse(glowBrush, p.X - radius * 1.5f, p.Y - radius * 1.5f, radius * 3.0f, radius * 3.0f);
            }
            using (SolidBrush coreBrush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
            {
                g.FillEllipse(coreBrush, p.X - radius, p.Y - radius, radius * 2f, radius * 2f);
            }
        }

        private void DrawAutumnLeaf(Graphics g, Particle p)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(p.X, p.Y);
            g.RotateTransform(p.Angle);

            float tumble = (p.State == ParticleState.Resting) ? 1.0f : (float)Math.Cos(p.FlutterPhase);
            if (Math.Abs(tumble) < 0.2f) tumble = (tumble < 0 ? -0.2f : 0.2f);
            g.ScaleTransform(tumble, 1.0f);

            int alpha = (int)(p.Color.A * p.Alpha);
            if (alpha < 0) alpha = 0;
            if (alpha > 255) alpha = 255;
            Color drawCol = Color.FromArgb(alpha, p.Color.R, p.Color.G, p.Color.B);

            using (SolidBrush brush = new SolidBrush(drawCol))
            {
                float s = p.Size;
                using (GraphicsPath path = new GraphicsPath())
                {
                    PointF[] points = new PointF[]
                    {
                        new PointF(0, -s * 0.6f),
                        new PointF(s * 0.25f, -s * 0.35f),
                        new PointF(s * 0.6f, -s * 0.4f),
                        new PointF(s * 0.35f, -s * 0.1f),
                        new PointF(s * 0.5f, s * 0.2f),
                        new PointF(s * 0.2f, s * 0.2f),
                        new PointF(0, s * 0.55f),
                        new PointF(-s * 0.2f, s * 0.2f),
                        new PointF(-s * 0.5f, s * 0.2f),
                        new PointF(-s * 0.35f, -s * 0.1f),
                        new PointF(-s * 0.6f, -s * 0.4f),
                        new PointF(-s * 0.25f, -s * 0.35f)
                    };
                    path.AddPolygon(points);
                    g.FillPath(brush, path);
                }
            }

            g.Restore(state);
        }

        private void DrawRaindrop(Graphics g, Particle p)
        {
            float dirSign = (_windDirection == WindDirection.West) ? -1.0f : 1.0f;
            float windAngle = dirSign * (_wind == WindLevel.Windy ? 0.35f : (_wind == WindLevel.Breeze ? 0.18f : 0.04f));
            float len = p.Size;
            float dx = (float)Math.Sin(windAngle) * len;
            float dy = (float)Math.Cos(windAngle) * len;

            using (Pen pen = new Pen(p.Color, 1.5f))
            {
                g.DrawLine(pen, p.X, p.Y, p.X + dx, p.Y + dy);
            }
        }

        private void DrawRainSplash(Graphics g, Particle p)
        {
            float progress = p.SplashFrame / 8.0f;
            float rx = 4.0f + progress * 14.0f;
            float ry = 2.0f + progress * 5.0f;
            int alpha = (int)((1.0f - progress) * 160);
            if (alpha > 0)
            {
                using (Pen pen = new Pen(Color.FromArgb(alpha, 210, 230, 255), 1.2f))
                {
                    g.DrawEllipse(pen, p.X - rx, p.Y - ry, rx * 2f, ry * 2f);
                }
            }
        }

        private void DrawFirefly(Graphics g, Particle p)
        {
            float pulse = (float)(0.6 + 0.4 * Math.Sin(p.FlutterPhase * 2.0f));
            float r = p.Size * pulse;

            using (SolidBrush glow = new SolidBrush(Color.FromArgb((int)(50 * pulse), p.Color.R, p.Color.G, p.Color.B)))
            {
                g.FillEllipse(glow, p.X - r * 2.5f, p.Y - r * 2.5f, r * 5.0f, r * 5.0f);
            }
            using (SolidBrush coreBrush = new SolidBrush(Color.FromArgb((int)(240 * pulse), 255, 255, 210)))
            {
                g.FillEllipse(coreBrush, p.X - r * 0.7f, p.Y - r * 0.7f, r * 1.4f, r * 1.4f);
            }
        }
    }
}
