using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace BoWeather.Particles
{
    /// <summary>
    /// Manages the animated yellow tracked snowplow vehicle that drives along the taskbar edge,
    /// scraping accumulated snow, kicking up snow spray, and pushing loose snowballs.
    /// </summary>
    public class SnowplowManager
    {
        public float X;
        public float Y;
        public int Direction = 1; // 1 = driving right, -1 = driving left
        public bool IsActive = false;
        public bool IsTurbo = false;
        public bool AutoPatrolEnabled = true;

        private float _wheelAngle = 0f;
        private float _beaconTick = 0f;
        private float _pushedSnow = 0f; // 0..1 snow wedge buildup in front of blade
        private float _patrolCooldown = 480f; // ~8s after startup for first automatic appearance
        private int _nextSpawnDir = 1;
        private int _sprayCooldown = 0;
        private readonly Random _rand = new Random();

        // Truck geometry constants (local coords centered at X, ground at Y)
        private const float BaseSpeed = 1.85f;
        private const float TurboSpeed = 3.6f;
        private const float BladeTipOffsetX = 46f;
        private const float TruckHalfWidth = 48f;
        private const float TruckHeight = 46f;

        /// <summary>
        /// Immediately sends the snowplow onto the screen (or boosts/reverses if already active).
        /// </summary>
        public void Summon(int screenWidth, int floorY)
        {
            if (IsActive)
            {
                IsTurbo = !IsTurbo;
                return;
            }

            SpawnTruck(screenWidth, floorY, _nextSpawnDir);
            _nextSpawnDir = -_nextSpawnDir;
        }

        private void SpawnTruck(int screenWidth, int floorY, int dir)
        {
            Direction = dir;
            IsActive = true;
            IsTurbo = false;
            _pushedSnow = 0f;
            Y = floorY;
            X = (dir > 0) ? -75f : (screenWidth + 75f);
        }

        /// <summary>
        /// Reverses driving direction on the spot.
        /// </summary>
        public void ReverseDirection()
        {
            if (!IsActive) return;
            Direction = -Direction;
            _pushedSnow *= 0.25f;
        }

        /// <summary>
        /// Checks whether a screen point hits the snowplow body/blade.
        /// </summary>
        public bool HitTest(float px, float py)
        {
            if (!IsActive) return false;
            return px >= (X - TruckHalfWidth) && px <= (X + TruckHalfWidth) &&
                   py >= (Y - TruckHeight) && py <= (Y + 6f);
        }

        /// <summary>
        /// Updates snowplow movement, auto-patrol timer, floor snow clearing, and snowball pushing.
        /// </summary>
        public void Update(WindowManager winMgr, SnowballManager snowballs, EffectType currentEffect,
            bool enableWindowPhysics, int screenWidth, int floorY, Action<float, float, int> onSnowPuff)
        {
            _beaconTick += 0.22f;

            if (!IsActive)
            {
                if (!AutoPatrolEnabled || currentEffect != EffectType.Snow || !enableWindowPhysics || winMgr == null || winMgr.FloorSnowPoints == null)
                {
                    return;
                }

                _patrolCooldown -= 1f;
                if (_patrolCooldown <= 0f)
                {
                    // Check if taskbar snow is deep enough to warrant plowing
                    float sum = 0f;
                    float[] pts = winMgr.FloorSnowPoints;
                    for (int i = 0; i < pts.Length; i++) sum += pts[i];
                    float avg = (pts.Length > 0) ? (sum / pts.Length) : 0f;

                    if (avg >= 7.5f)
                    {
                        SpawnTruck(screenWidth, floorY, _nextSpawnDir);
                        _nextSpawnDir = -_nextSpawnDir;
                        _patrolCooldown = 2700f + _rand.Next(0, 900); // Next patrol ~45-60s after finishing
                    }
                    else
                    {
                        _patrolCooldown = 300f; // Check again in ~5s
                    }
                }
                return;
            }

            // Active driving update
            Y = floorY;
            float speed = (IsTurbo ? TurboSpeed : BaseSpeed) * Direction;
            X += speed;
            _wheelAngle += speed * 0.14f;

            // Leave screen cleanly on the far side
            if ((Direction > 0 && X > screenWidth + 95f) || (Direction < 0 && X < -95f))
            {
                IsActive = false;
                IsTurbo = false;
                _pushedSnow = 0f;
                return;
            }

            // Scrape taskbar/floor snow along the blade path
            float bladeX = X + Direction * BladeTipOffsetX;
            float scoopedAmount = 0f;

            if (winMgr != null && winMgr.FloorSnowPoints != null && screenWidth > 1)
            {
                float[] pts = winMgr.FloorSnowPoints;
                float step = (float)screenWidth / (pts.Length - 1);
                float clearBackX = X - Direction * 12f; // Track & chassis footprint behind blade
                float minX = Math.Min(clearBackX, bladeX);
                float maxX = Math.Max(clearBackX, bladeX);

                for (int i = 0; i < pts.Length; i++)
                {
                    float colX = i * step;

                    if (colX >= minX && colX <= maxX)
                    {
                        // Scrape cleanly down to a thin frost sheen (1.2px)
                        float residual = 1.2f;
                        if (pts[i] > residual)
                        {
                            scoopedAmount += (pts[i] - residual);
                            pts[i] = residual;
                        }
                    }
                    else
                    {
                        // Taper snow edge gently just ahead of the blade tip
                        float distAhead = (colX - bladeX) * Direction;
                        if (distAhead > 0f && distAhead < 22f)
                        {
                            float ratio = distAhead / 22f;
                            float maxRamp = winMgr.MaxFloorSnowPoints != null
                                ? winMgr.MaxFloorSnowPoints[i] * ratio + 1.5f
                                : 18f * ratio;
                            if (pts[i] > maxRamp)
                            {
                                scoopedAmount += (pts[i] - maxRamp) * 0.5f;
                                pts[i] = maxRamp;
                            }
                        }
                    }
                }
            }

            // Build up or decay snow wedge in front of the blade
            if (scoopedAmount > 0.25f)
            {
                _pushedSnow = Math.Min(1.0f, _pushedSnow + scoopedAmount * 0.045f);
                if (_sprayCooldown <= 0 && onSnowPuff != null)
                {
                    onSnowPuff(bladeX + Direction * 4f, Y - 7f, IsTurbo ? 3 : 2);
                    _sprayCooldown = IsTurbo ? 2 : 4;
                }
            }
            else
            {
                _pushedSnow = Math.Max(0f, _pushedSnow - 0.025f);
            }

            if (_sprayCooldown > 0) _sprayCooldown--;

            // Push loose floor snowballs gently along in front of the blade (snowmen remain untouched!)
            if (snowballs != null)
            {
                List<Snowball> balls = snowballs.Snowballs;
                for (int i = 0; i < balls.Count; i++)
                {
                    Snowball b = balls[i];
                    if (b.IsSnowman || b.IsGrabbed || b.IsFalling || b.IsMelting) continue;
                    if (b.Y + b.BaseRadius < floorY - 28f) continue;

                    float distToBlade = (b.X - bladeX) * Direction;
                    if (distToBlade >= -12f && distToBlade <= b.BaseRadius + 10f)
                    {
                        b.X = bladeX + Direction * (b.BaseRadius + 10f);
                        b.Vx = speed * 1.08f;
                        b.Angle += speed * 0.12f;
                    }
                }
            }
        }

        /// <summary>
        /// Renders the yellow tracked snowplow with spinning spoked wheels, flashing beacon, and front blade.
        /// </summary>
        public void Draw(Graphics g)
        {
            if (!IsActive) return;

            GraphicsState state = g.Save();
            g.TranslateTransform(X, Y);
            if (Direction < 0)
            {
                g.ScaleTransform(-1f, 1f);
            }

            // 1. Front hydraulic support strut connecting chassis to blade
            using (Pen strutPen = new Pen(Color.FromArgb(255, 100, 116, 139), 3.2f))
            {
                strutPen.StartCap = LineCap.Round;
                strutPen.EndCap = LineCap.Round;
                g.DrawLine(strutPen, 16f, -12f, 36f, -10f);
                g.DrawLine(strutPen, 18f, -16f, 34f, -18f);
            }

            // 2. Pushed snow wedge cupped directly in front of the curved blade
            if (_pushedSnow > 0.05f)
            {
                float pileH = 7f + 11f * _pushedSnow;
                float pileW = 14f + 14f * _pushedSnow;
                using (GraphicsPath snowPath = new GraphicsPath())
                {
                    snowPath.AddBezier(
                        38f, -1.5f,
                        40f, -pileH * 1.1f,
                        44f + pileW * 0.48f, -pileH * 0.85f,
                        46f + pileW, 0f);
                    snowPath.AddLine(46f + pileW, 0f, 38f, 0f);
                    snowPath.CloseFigure();

                    using (LinearGradientBrush snowBrush = new LinearGradientBrush(
                        new PointF(38f, -pileH), new PointF(38f, 1f),
                        Color.FromArgb(255, 255, 255, 255),
                        Color.FromArgb(255, 218, 232, 248)))
                    {
                        g.FillPath(snowBrush, snowPath);
                    }
                }
            }

            // 3. Curved silver snowplow blade + red cutting bottom edge
            using (GraphicsPath bladePath = new GraphicsPath())
            {
                bladePath.AddBezier(36f, -24f, 32f, -14f, 35f, -4f, 44f, -1f);
                bladePath.AddLine(44f, -1f, 46f, -2.5f);
                bladePath.AddBezier(46f, -2.5f, 38f, -6f, 36f, -14f, 40f, -23f);
                bladePath.CloseFigure();

                using (LinearGradientBrush bladeBrush = new LinearGradientBrush(
                    new PointF(32f, -24f), new PointF(46f, -1f),
                    Color.FromArgb(255, 241, 245, 249),
                    Color.FromArgb(255, 148, 163, 184)))
                {
                    g.FillPath(bladeBrush, bladePath);
                }
                using (Pen bladeEdge = new Pen(Color.FromArgb(200, 100, 116, 139), 1.0f))
                {
                    g.DrawPath(bladeEdge, bladePath);
                }
                // Red bottom edge accent (matches reference clip)
                using (Pen redTrim = new Pen(Color.FromArgb(255, 239, 68, 68), 2.2f))
                {
                    redTrim.StartCap = LineCap.Round;
                    redTrim.EndCap = LineCap.Round;
                    g.DrawLine(redTrim, 38f, -1.3f, 45f, -1.3f);
                }
            }

            // 4. Exhaust pipe at the back of the hood
            using (SolidBrush pipeBrush = new SolidBrush(Color.FromArgb(255, 71, 85, 105)))
            {
                g.FillRectangle(pipeBrush, -25f, -29f, 3.2f, 8f);
            }

            // 5. Yellow Tractor Body & Cab (cab first so chassis blends cleanly in front)
            // Cab structure
            using (GraphicsPath cabPath = CreateRoundedRectPath(-20f, -38f, 28f, 18f, 4f))
            using (LinearGradientBrush cabBrush = new LinearGradientBrush(
                new PointF(0f, -38f), new PointF(0f, -20f),
                Color.FromArgb(255, 253, 224, 71),
                Color.FromArgb(255, 250, 204, 21)))
            using (Pen cabBorder = new Pen(Color.FromArgb(255, 180, 83, 9), 1.1f))
            {
                g.FillPath(cabBrush, cabPath);
                g.DrawPath(cabBorder, cabPath);
            }

            // Main lower chassis + front engine hood
            using (GraphicsPath bodyPath = CreateRoundedRectPath(-30f, -23f, 54f, 14f, 3.5f))
            using (LinearGradientBrush bodyBrush = new LinearGradientBrush(
                new PointF(0f, -23f), new PointF(0f, -9f),
                Color.FromArgb(255, 250, 204, 21),
                Color.FromArgb(255, 234, 179, 8)))
            using (Pen bodyBorder = new Pen(Color.FromArgb(255, 180, 83, 9), 1.1f))
            {
                g.FillPath(bodyBrush, bodyPath);
                g.DrawPath(bodyBorder, bodyPath);
            }

            // Roof lip cap
            using (GraphicsPath roofPath = CreateRoundedRectPath(-22f, -40f, 32f, 4.2f, 2f))
            using (SolidBrush roofBrush = new SolidBrush(Color.FromArgb(255, 250, 204, 21)))
            using (Pen roofBorder = new Pen(Color.FromArgb(255, 180, 83, 9), 1.0f))
            {
                g.FillPath(roofBrush, roofPath);
                g.DrawPath(roofBorder, roofPath);
            }

            // Icy-blue windshield window with diagonal reflection
            using (GraphicsPath winPath = CreateRoundedRectPath(-16f, -35f, 20f, 12f, 2.5f))
            {
                using (LinearGradientBrush winBrush = new LinearGradientBrush(
                    new PointF(-16f, -35f), new PointF(4f, -23f),
                    Color.FromArgb(255, 224, 242, 254),
                    Color.FromArgb(255, 96, 165, 250)))
                {
                    g.FillPath(winBrush, winPath);
                }
                using (Pen winBorder = new Pen(Color.FromArgb(200, 148, 163, 184), 1.0f))
                {
                    g.DrawPath(winBorder, winPath);
                }
                // Subtle glass reflection
                using (Pen glint = new Pen(Color.FromArgb(140, 255, 255, 255), 2.0f))
                {
                    g.DrawLine(glint, -13f, -26f, -6f, -33f);
                }
            }

            // 6. Flashing Orange Beacon Light on roof
            float flash = (float)(0.5 + 0.5 * Math.Sin(_beaconTick * (IsTurbo ? 2.2f : 1.0f)));
            int glowA = (int)(35 + flash * 105);
            using (SolidBrush haloOuter = new SolidBrush(Color.FromArgb(glowA / 2, 251, 146, 60)))
            using (SolidBrush haloInner = new SolidBrush(Color.FromArgb(glowA, 253, 186, 116)))
            {
                g.FillEllipse(haloOuter, -13f, -51f, 18f, 18f);
                g.FillEllipse(haloInner, -9.5f, -47.5f, 11f, 11f);
            }
            using (SolidBrush beaconBase = new SolidBrush(Color.FromArgb(255, 55, 65, 81)))
            using (SolidBrush beaconDome = new SolidBrush(Color.FromArgb(255, 249, (int)(115 + flash * 80), (int)(22 + flash * 50))))
            {
                g.FillRectangle(beaconBase, -7.5f, -42f, 7f, 2f);
                g.FillPie(beaconDome, -7f, -46f, 6f, 8f, 180f, 180f);
            }

            // 7. Caterpillar Tracks & 4 Spinning Spoked Wheels (flush with floor Y = 0)
            using (GraphicsPath trackPath = CreateRoundedRectPath(-31f, -12.5f, 56f, 12.5f, 6.2f))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(255, 30, 35, 43)))
            using (Pen trackOutline = new Pen(Color.FromArgb(255, 15, 23, 42), 1.2f))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(trackOutline, trackPath);
            }

            // Subtle track tread notches moving along the top & bottom edges
            using (Pen notchPen = new Pen(Color.FromArgb(160, 71, 85, 105), 1.4f))
            {
                float offset = ((_wheelAngle * 4.5f) % 8f + 8f) % 8f;
                for (float tx = -24f + offset; tx <= 18f; tx += 8f)
                {
                    g.DrawLine(notchPen, tx, -12.5f, tx, -11f);
                    g.DrawLine(notchPen, tx, -1.5f, tx, 0f);
                }
            }

            // 4 spinning silver wheels inside the track
            float[] wheelCentersX = new float[] { -23.5f, -9.5f, 4.5f, 18.5f };
            float wheelCy = -6.2f;
            float wheelR = 4.6f;

            using (SolidBrush wheelBrush = new SolidBrush(Color.FromArgb(255, 226, 232, 240)))
            using (Pen hubPen = new Pen(Color.FromArgb(255, 71, 85, 105), 1.1f))
            using (SolidBrush centerDot = new SolidBrush(Color.FromArgb(255, 51, 65, 85)))
            {
                for (int w = 0; w < 4; w++)
                {
                    float wx = wheelCentersX[w];
                    g.FillEllipse(wheelBrush, wx - wheelR, wheelCy - wheelR, wheelR * 2f, wheelR * 2f);
                    g.DrawEllipse(hubPen, wx - wheelR, wheelCy - wheelR, wheelR * 2f, wheelR * 2f);

                    // 3 rotating spokes
                    for (int s = 0; s < 3; s++)
                    {
                        float ang = _wheelAngle + s * (float)(Math.PI * 2.0 / 3.0);
                        float sx = wx + (float)Math.Cos(ang) * (wheelR - 0.8f);
                        float sy = wheelCy + (float)Math.Sin(ang) * (wheelR - 0.8f);
                        g.DrawLine(hubPen, wx, wheelCy, sx, sy);
                    }
                    g.FillEllipse(centerDot, wx - 1.3f, wheelCy - 1.3f, 2.6f, 2.6f);
                }
            }

            g.Restore(state);
        }

        private static GraphicsPath CreateRoundedRectPath(float x, float y, float w, float h, float r)
        {
            GraphicsPath path = new GraphicsPath();
            float d = r * 2f;
            if (d > w) d = w;
            if (d > h) d = h;
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + w - d, y, d, d, 270, 90);
            path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
            path.AddArc(x, y + h - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
