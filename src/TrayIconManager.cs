using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using BoWeather.Particles;

namespace BoWeather
{
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly OverlayForm _overlay;
        private readonly WallpaperManager _wallpaperManager;

        private MenuItem _sakuraItem;
        private MenuItem _snowItem;
        private MenuItem _autumnItem;
        private MenuItem _rainItem;
        private MenuItem _firefliesItem;
        private MenuItem _physicsItem;
        private MenuItem _pauseItem;

        public TrayIconManager(OverlayForm overlay, WallpaperManager wallpaperManager)
        {
            _overlay = overlay;
            _wallpaperManager = wallpaperManager;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "BoWeather - Interactive Desktop Effects";
            _notifyIcon.Icon = GenerateTrayIcon();
            _notifyIcon.Visible = true;

            BuildContextMenu();
        }

        private Icon GenerateTrayIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Circular background with gradient
                using (LinearGradientBrush brush = new LinearGradientBrush(new Point(0, 0), new Point(32, 32),
                    Color.FromArgb(255, 255, 120, 150), Color.FromArgb(255, 120, 140, 255)))
                {
                    g.FillEllipse(brush, 2, 2, 28, 28);
                }

                // Inner white petal / leaf symbol
                using (SolidBrush white = new SolidBrush(Color.White))
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddBezier(16, 24, 8, 16, 8, 8, 16, 6);
                    path.AddBezier(16, 6, 24, 8, 24, 16, 16, 24);
                    path.CloseFigure();
                    g.FillPath(white, path);
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }

        private void BuildContextMenu()
        {
            ContextMenu menu = new ContextMenu();

            // Weather Effects Submenu / Items
            _sakuraItem = new MenuItem("🌸 Sakura (Cherry Blossom)", delegate { SetEffect(EffectType.Sakura); });
            _snowItem = new MenuItem("❄️ Snow (Salju)", delegate { SetEffect(EffectType.Snow); });
            _autumnItem = new MenuItem("🍂 Autumn Leaves (Daun Gugur)", delegate { SetEffect(EffectType.AutumnLeaves); });
            _rainItem = new MenuItem("🌧️ Rain (Hujan)", delegate { SetEffect(EffectType.Rain); });
            _firefliesItem = new MenuItem("✨ Fireflies (Kunang-Kunang)", delegate { SetEffect(EffectType.Fireflies); });

            UpdateEffectChecks();

            menu.MenuItems.Add(_sakuraItem);
            menu.MenuItems.Add(_snowItem);
            menu.MenuItems.Add(_autumnItem);
            menu.MenuItems.Add(_rainItem);
            menu.MenuItems.Add(_firefliesItem);

            menu.MenuItems.Add("-");

            // Window Physics toggle
            _physicsItem = new MenuItem("🪟 Window Physics (Partikel Bertengger di Window)", delegate
            {
                _overlay.Particles.EnableWindowPhysics = !_overlay.Particles.EnableWindowPhysics;
                _physicsItem.Checked = _overlay.Particles.EnableWindowPhysics;
            });
            _physicsItem.Checked = _overlay.Particles.EnableWindowPhysics;
            menu.MenuItems.Add(_physicsItem);

            // Wind Menu
            MenuItem windMenu = new MenuItem("💨 Kekuatan Angin");
            MenuItem windCalm = new MenuItem("Tenang (Calm)", delegate { SetWind(WindLevel.Calm, windMenu); });
            MenuItem windBreeze = new MenuItem("Sepoi-sepoi (Breeze)", delegate { SetWind(WindLevel.Breeze, windMenu); });
            MenuItem windWindy = new MenuItem("Kencang (Windy)", delegate { SetWind(WindLevel.Windy, windMenu); });
            windBreeze.Checked = true;
            windMenu.MenuItems.Add(windCalm);
            windMenu.MenuItems.Add(windBreeze);
            windMenu.MenuItems.Add(windWindy);
            menu.MenuItems.Add(windMenu);

            // Density Menu
            MenuItem densityMenu = new MenuItem("🔢 Kepadatan Partikel");
            MenuItem densLow = new MenuItem("Sedikit (Low)", delegate { SetDensity(DensityLevel.Low, densityMenu); });
            MenuItem densMed = new MenuItem("Sedang (Medium)", delegate { SetDensity(DensityLevel.Medium, densityMenu); });
            MenuItem densHigh = new MenuItem("Banyak (High)", delegate { SetDensity(DensityLevel.High, densityMenu); });
            densMed.Checked = true;
            densityMenu.MenuItems.Add(densLow);
            densityMenu.MenuItems.Add(densMed);
            densityMenu.MenuItems.Add(densHigh);
            menu.MenuItems.Add(densityMenu);

            menu.MenuItems.Add("-");

            // Wallpaper options
            MenuItem applyWallpaper = new MenuItem("🖼️ Terapkan Wallpaper Tema Ini", delegate
            {
                _wallpaperManager.ApplyThemeWallpaper(_overlay.Particles.CurrentEffect);
            });
            menu.MenuItems.Add(applyWallpaper);

            MenuItem customWallpaper = new MenuItem("📁 Pilih Wallpaper Sendiri...", delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Pilih Gambar Wallpaper";
                    ofd.Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        _wallpaperManager.SetWallpaper(ofd.FileName);
                    }
                }
            });
            menu.MenuItems.Add(customWallpaper);

            menu.MenuItems.Add("-");

            // Auto-pause toggle for games / fullscreen
            MenuItem autoPauseItem = new MenuItem("🎮 Auto-Pause saat Main Game / Fullscreen", delegate(object s, EventArgs e)
            {
                _overlay.AutoPauseOnFullscreenGame = !_overlay.AutoPauseOnFullscreenGame;
                ((MenuItem)s).Checked = _overlay.AutoPauseOnFullscreenGame;
            });
            autoPauseItem.Checked = _overlay.AutoPauseOnFullscreenGame;
            menu.MenuItems.Add(autoPauseItem);

            // Pause toggle
            _pauseItem = new MenuItem("⏸️ Jeda Animasi Manual", delegate
            {
                _overlay.IsPaused = !_overlay.IsPaused;
                _pauseItem.Checked = _overlay.IsPaused;
            });
            menu.MenuItems.Add(_pauseItem);

            // Exit
            MenuItem exitItem = new MenuItem("❌ Keluar", delegate
            {
                _notifyIcon.Visible = false;
                Application.Exit();
            });
            menu.MenuItems.Add(exitItem);

            _notifyIcon.ContextMenu = menu;
        }

        private void SetEffect(EffectType effect)
        {
            _overlay.Particles.CurrentEffect = effect;
            UpdateEffectChecks();
        }

        private void UpdateEffectChecks()
        {
            EffectType cur = _overlay.Particles.CurrentEffect;
            _sakuraItem.Checked = (cur == EffectType.Sakura);
            _snowItem.Checked = (cur == EffectType.Snow);
            _autumnItem.Checked = (cur == EffectType.AutumnLeaves);
            _rainItem.Checked = (cur == EffectType.Rain);
            _firefliesItem.Checked = (cur == EffectType.Fireflies);
        }

        private void SetWind(WindLevel wind, MenuItem parent)
        {
            _overlay.Particles.Wind = wind;
            for (int i = 0; i < parent.MenuItems.Count; i++)
            {
                parent.MenuItems[i].Checked = false;
            }
            if (wind == WindLevel.Calm) parent.MenuItems[0].Checked = true;
            else if (wind == WindLevel.Breeze) parent.MenuItems[1].Checked = true;
            else if (wind == WindLevel.Windy) parent.MenuItems[2].Checked = true;
        }

        private void SetDensity(DensityLevel density, MenuItem parent)
        {
            _overlay.Particles.Density = density;
            for (int i = 0; i < parent.MenuItems.Count; i++)
            {
                parent.MenuItems[i].Checked = false;
            }
            if (density == DensityLevel.Low) parent.MenuItems[0].Checked = true;
            else if (density == DensityLevel.Medium) parent.MenuItems[1].Checked = true;
            else if (density == DensityLevel.High) parent.MenuItems[2].Checked = true;
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}

