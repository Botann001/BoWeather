using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BoWeather.Particles;

namespace BoWeather
{
    public enum AppLanguage
    {
        Indonesian,
        English
    }

    public class TrayIconManager : IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly NotifyIcon _notifyIcon;
        private readonly OverlayForm _overlay;
        private readonly WallpaperManager _wallpaperManager;
        private readonly string _configPath;
        private AppLanguage _currentLanguage = AppLanguage.Indonesian;
        private ControlCenterForm _controlCenter;
        private FloatingWidgetForm _widgetForm;

        public AppLanguage CurrentLanguage { get { return _currentLanguage; } }
        public bool IsAutoWallpaperEnabled { get { return _wallpaperTimer != null && _wallpaperTimer.Enabled; } }
        public bool IsDarkMode { get { return _controlCenter != null ? _controlCenter.IsDarkMode : true; } }
        public ControlCenterForm ControlCenter { get { return _controlCenter; } }
        public FloatingWidgetForm WidgetForm { get { return _widgetForm; } }

        public void OnThemeModeChanged()
        {
            if (_widgetForm != null && !_widgetForm.IsDisposed)
            {
                _widgetForm.UpdateAppearance();
            }
        }

        public void UpdatePauseState()
        {
            if (_pauseItem != null) _pauseItem.Checked = _overlay.IsPaused;
            if (_widgetForm != null && !_widgetForm.IsDisposed)
            {
                _widgetForm.UpdateEffect();
            }
        }

        private MenuItem _sakuraItem;
        private MenuItem _snowItem;
        private MenuItem _autumnItem;
        private MenuItem _rainItem;
        private MenuItem _firefliesItem;
        private MenuItem _physicsItem;
        private MenuItem _pauseItem;
        private MenuItem _autoWallpaperItem;

        // Auto wallpaper: re-checks once a minute and re-applies only when
        // the effect or the time-of-day slot changed.
        private System.Windows.Forms.Timer _wallpaperTimer;
        private string _lastWallpaperKey;

        public TrayIconManager(OverlayForm overlay, WallpaperManager wallpaperManager)
        {
            _overlay = overlay;
            _wallpaperManager = wallpaperManager;
            _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.txt");
            _currentLanguage = LoadLanguagePreference();

            _controlCenter = new ControlCenterForm(_overlay, _wallpaperManager, this);
            _widgetForm = new FloatingWidgetForm(_overlay, this);
            _overlay.TrayManager = this;

            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "BoWeather - Control Center";
            _notifyIcon.Icon = GenerateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.MouseClick += OnTrayIconMouseClick;
            _notifyIcon.DoubleClick += (s, e) => ToggleControlCenter();

            BuildContextMenu();
            if (_controlCenter != null && !_controlCenter.IsDisposed) _controlCenter.Invalidate();
            if (_widgetForm != null && !_widgetForm.IsDisposed) _widgetForm.Invalidate();

            _widgetForm.Show();
        }

        public void ToggleControlCenter(Rectangle? anchor = null)
        {
            if (_controlCenter != null)
            {
                _controlCenter.ToggleVisibility(anchor);
            }
        }

        public void OnFullscreenStateChanged(bool isFullscreen)
        {
            if (_widgetForm != null && !_widgetForm.IsDisposed)
            {
                if (isFullscreen && _widgetForm.Visible) _widgetForm.Hide();
                else if (!isFullscreen && !_widgetForm.Visible) _widgetForm.Show();
            }
        }

        private void OnTrayIconMouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ToggleControlCenter();
            }
        }

        private AppLanguage LoadLanguagePreference()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string text = File.ReadAllText(_configPath).Trim().ToLower();
                    if (text == "en") return AppLanguage.English;
                    if (text == "id") return AppLanguage.Indonesian;
                }
            }
            catch { }

            // Default: detect system OS language
            string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            return (sys == "id") ? AppLanguage.Indonesian : AppLanguage.English;
        }

        public void SetLanguage(AppLanguage lang)
        {
            if (_currentLanguage == lang) return;
            _currentLanguage = lang;
            try
            {
                File.WriteAllText(_configPath, (lang == AppLanguage.English) ? "en" : "id");
            }
            catch { }

            BuildContextMenu();
            if (_controlCenter != null && !_controlCenter.IsDisposed) _controlCenter.Invalidate();
            if (_widgetForm != null && !_widgetForm.IsDisposed) _widgetForm.Invalidate();
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
                Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
                DestroyIcon(hIcon);
                return icon;
            }
        }

        private void BuildContextMenu()
        {
            ContextMenu menu = new ContextMenu();
            bool isId = (_currentLanguage == AppLanguage.Indonesian);

            // Open Control Center as first item
            MenuItem openCC = new MenuItem(isId ? "⚙️ Buka Control Center" : "⚙️ Open Control Center", delegate { ToggleControlCenter(); });
            menu.MenuItems.Add(openCC);
            menu.MenuItems.Add("-");

            // Weather Effects Submenu / Items
            _sakuraItem = new MenuItem(isId ? "🌸 Sakura (Cherry Blossom)" : "🌸 Sakura (Cherry Blossom)", delegate { SetEffect(EffectType.Sakura); });
            _snowItem = new MenuItem(isId ? "❄️ Snow (Salju)" : "❄️ Snow", delegate { SetEffect(EffectType.Snow); });
            _autumnItem = new MenuItem(isId ? "🍂 Autumn Leaves (Daun Gugur)" : "🍂 Autumn Leaves", delegate { SetEffect(EffectType.AutumnLeaves); });
            _rainItem = new MenuItem(isId ? "🌧️ Rain (Hujan)" : "🌧️ Rain", delegate { SetEffect(EffectType.Rain); });
            _firefliesItem = new MenuItem(isId ? "✨ Fireflies (Kunang-Kunang)" : "✨ Fireflies", delegate { SetEffect(EffectType.Fireflies); });

            UpdateEffectChecks();

            menu.MenuItems.Add(_sakuraItem);
            menu.MenuItems.Add(_snowItem);
            menu.MenuItems.Add(_autumnItem);
            menu.MenuItems.Add(_rainItem);
            menu.MenuItems.Add(_firefliesItem);

            MenuItem christmasItem = new MenuItem(isId ? "🎄 Dekorasi & Aksesoris Natal…" : "🎄 Christmas Accessories…", delegate
            {
                if (_overlay != null && _overlay.Christmas != null)
                {
                    using (ChristmasDecorForm form = new ChristmasDecorForm(_overlay.Christmas, _overlay, true))
                    {
                        form.ShowDialog();
                    }
                }
            });
            menu.MenuItems.Add(christmasItem);

            menu.MenuItems.Add("-");

            // Window Physics toggle
            _physicsItem = new MenuItem(isId ? "🪟 Window Physics (Partikel Bertengger di Window)" : "🪟 Window Physics (Particles Rest on Windows)", delegate
            {
                _overlay.Particles.EnableWindowPhysics = !_overlay.Particles.EnableWindowPhysics;
                _physicsItem.Checked = _overlay.Particles.EnableWindowPhysics;
            });
            _physicsItem.Checked = _overlay.Particles.EnableWindowPhysics;
            menu.MenuItems.Add(_physicsItem);

            // Wind Menu
            MenuItem windMenu = new MenuItem(isId ? "💨 Kekuatan Angin" : "💨 Wind Strength");
            MenuItem windCalm = new MenuItem(isId ? "Tenang (Calm)" : "Calm", delegate { SetWind(WindLevel.Calm, windMenu); });
            MenuItem windBreeze = new MenuItem(isId ? "Sepoi-sepoi (Breeze)" : "Breeze", delegate { SetWind(WindLevel.Breeze, windMenu); });
            MenuItem windWindy = new MenuItem(isId ? "Kencang (Windy)" : "Windy", delegate { SetWind(WindLevel.Windy, windMenu); });
            WindLevel curWind = _overlay.Particles.Wind;
            windCalm.Checked = (curWind == WindLevel.Calm);
            windBreeze.Checked = (curWind == WindLevel.Breeze);
            windWindy.Checked = (curWind == WindLevel.Windy);
            windMenu.MenuItems.Add(windCalm);
            windMenu.MenuItems.Add(windBreeze);
            windMenu.MenuItems.Add(windWindy);
            menu.MenuItems.Add(windMenu);

            // Density Menu
            MenuItem densityMenu = new MenuItem(isId ? "🔢 Kepadatan Partikel" : "🔢 Particle Density");
            MenuItem densLow = new MenuItem(isId ? "Sedikit (Low)" : "Low", delegate { SetDensity(DensityLevel.Low, densityMenu); });
            MenuItem densMed = new MenuItem(isId ? "Sedang (Medium)" : "Medium", delegate { SetDensity(DensityLevel.Medium, densityMenu); });
            MenuItem densHigh = new MenuItem(isId ? "Banyak (High)" : "High", delegate { SetDensity(DensityLevel.High, densityMenu); });
            DensityLevel curDens = _overlay.Particles.Density;
            densLow.Checked = (curDens == DensityLevel.Low);
            densMed.Checked = (curDens == DensityLevel.Medium);
            densHigh.Checked = (curDens == DensityLevel.High);
            densityMenu.MenuItems.Add(densLow);
            densityMenu.MenuItems.Add(densMed);
            densityMenu.MenuItems.Add(densHigh);
            menu.MenuItems.Add(densityMenu);

            menu.MenuItems.Add("-");

            // Wallpaper options
            MenuItem applyWallpaper = new MenuItem(isId ? "🖼️ Terapkan Wallpaper Tema Ini" : "🖼️ Apply Theme Wallpaper", delegate
            {
                _wallpaperManager.ApplyThemeWallpaper(_overlay.Particles.CurrentEffect);
            });
            menu.MenuItems.Add(applyWallpaper);

            // Auto wallpaper: follows the 8 times of day (theme_<effect>_<time>.png)
            _autoWallpaperItem = new MenuItem(isId ? "🕐 Wallpaper Otomatis Ikut Waktu (8 Waktu)" : "🕐 Auto-switch Wallpaper by Time (8 Slots)", delegate(object s, EventArgs e)
            {
                ToggleAutoWallpaper();
                ((MenuItem)s).Checked = (_wallpaperTimer != null && _wallpaperTimer.Enabled);
            });
            _autoWallpaperItem.Checked = (_wallpaperTimer != null && _wallpaperTimer.Enabled);
            menu.MenuItems.Add(_autoWallpaperItem);

            MenuItem customWallpaper = new MenuItem(isId ? "📁 Pilih Wallpaper Sendiri..." : "📁 Choose Custom Wallpaper...", delegate
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = isId ? "Pilih Gambar Wallpaper" : "Select Wallpaper Image";
                    ofd.Filter = isId ? "File Gambar (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp" : "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        _wallpaperManager.SetWallpaper(ofd.FileName);
                    }
                }
            });
            menu.MenuItems.Add(customWallpaper);

            menu.MenuItems.Add("-");

            // Auto-pause toggle for games / fullscreen
            MenuItem autoPauseItem = new MenuItem(isId ? "🎮 Auto-Pause saat Main Game / Fullscreen" : "🎮 Auto-Pause during Games / Fullscreen", delegate(object s, EventArgs e)
            {
                _overlay.AutoPauseOnFullscreenGame = !_overlay.AutoPauseOnFullscreenGame;
                ((MenuItem)s).Checked = _overlay.AutoPauseOnFullscreenGame;
            });
            autoPauseItem.Checked = _overlay.AutoPauseOnFullscreenGame;
            menu.MenuItems.Add(autoPauseItem);

            // Pause toggle
            _pauseItem = new MenuItem(isId ? "⏸️ Jeda Animasi Manual" : "⏸️ Pause Animation", delegate
            {
                _overlay.IsPaused = !_overlay.IsPaused;
                _pauseItem.Checked = _overlay.IsPaused;
            });
            _pauseItem.Checked = _overlay.IsPaused;
            menu.MenuItems.Add(_pauseItem);

            menu.MenuItems.Add("-");

            // Language submenu
            MenuItem langMenu = new MenuItem(isId ? "🌐 Bahasa / Language" : "🌐 Language / Bahasa");
            MenuItem langId = new MenuItem("🇮🇩 Bahasa Indonesia", delegate { SetLanguage(AppLanguage.Indonesian); });
            MenuItem langEn = new MenuItem("🇬🇧 English", delegate { SetLanguage(AppLanguage.English); });
            langId.Checked = isId;
            langEn.Checked = !isId;
            langMenu.MenuItems.Add(langId);
            langMenu.MenuItems.Add(langEn);
            menu.MenuItems.Add(langMenu);

            // Exit
            MenuItem exitItem = new MenuItem(isId ? "❌ Keluar" : "❌ Exit", delegate
            {
                _notifyIcon.Visible = false;
                Application.Exit();
            });
            menu.MenuItems.Add(exitItem);

            _notifyIcon.ContextMenu = menu;
        }

        public void SetEffect(EffectType effect)
        {
            _overlay.Particles.CurrentEffect = effect;
            UpdateEffectChecks();
            if (_widgetForm != null && !_widgetForm.IsDisposed) _widgetForm.UpdateEffect();
            if (_controlCenter != null && !_controlCenter.IsDisposed) _controlCenter.NotifyThemeChanged();

            if (_wallpaperTimer != null && _wallpaperTimer.Enabled)
            {
                _lastWallpaperKey = CurrentWallpaperKey();
                _wallpaperManager.ApplyThemeWallpaper(effect);
            }
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

        public void SetWind(WindLevel wind)
        {
            _overlay.Particles.Wind = wind;
        }

        private void SetWind(WindLevel wind, MenuItem parent)
        {
            SetWind(wind);
            for (int i = 0; i < parent.MenuItems.Count; i++)
            {
                parent.MenuItems[i].Checked = false;
            }
            if (wind == WindLevel.Calm) parent.MenuItems[0].Checked = true;
            else if (wind == WindLevel.Breeze) parent.MenuItems[1].Checked = true;
            else if (wind == WindLevel.Windy) parent.MenuItems[2].Checked = true;
        }

        public void SetDensity(DensityLevel density)
        {
            _overlay.Particles.Density = density;
        }

        private void SetDensity(DensityLevel density, MenuItem parent)
        {
            SetDensity(density);
            for (int i = 0; i < parent.MenuItems.Count; i++)
            {
                parent.MenuItems[i].Checked = false;
            }
            if (density == DensityLevel.Low) parent.MenuItems[0].Checked = true;
            else if (density == DensityLevel.Medium) parent.MenuItems[1].Checked = true;
            else if (density == DensityLevel.High) parent.MenuItems[2].Checked = true;
        }

        public void ToggleAutoWallpaper()
        {
            if (_wallpaperTimer != null && _wallpaperTimer.Enabled)
            {
                _wallpaperTimer.Stop();
                _wallpaperTimer.Dispose();
                _wallpaperTimer = null;
                _lastWallpaperKey = null;
                return;
            }

            _wallpaperTimer = new System.Windows.Forms.Timer();
            _wallpaperTimer.Interval = 60000; // check once a minute
            _wallpaperTimer.Tick += WallpaperTimer_Tick;
            _wallpaperTimer.Start();

            // Apply immediately, then let the timer handle later changes
            _lastWallpaperKey = CurrentWallpaperKey();
            _wallpaperManager.ApplyThemeWallpaper(_overlay.Particles.CurrentEffect);
        }

        public void ApplyCurrentWallpaper()
        {
            _wallpaperManager.ApplyThemeWallpaper(_overlay.Particles.CurrentEffect);
        }

        public void BrowseCustomWallpaper()
        {
            bool isId = (_currentLanguage == AppLanguage.Indonesian);
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = isId ? "Pilih Gambar Wallpaper" : "Select Wallpaper Image";
                ofd.Filter = isId ? "File Gambar (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp" : "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _wallpaperManager.SetWallpaper(ofd.FileName);
                }
            }
        }

        public void ExitApp()
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private string CurrentWallpaperKey()
        {
            return _overlay.Particles.CurrentEffect.ToString() + "|" + _wallpaperManager.GetCurrentTimeSlot();
        }

        private void WallpaperTimer_Tick(object sender, EventArgs e)
        {
            string key = CurrentWallpaperKey();
            if (key != _lastWallpaperKey)
            {
                _lastWallpaperKey = key;
                _wallpaperManager.ApplyThemeWallpaper(_overlay.Particles.CurrentEffect);
            }
        }

        public void Dispose()
        {
            if (_widgetForm != null)
            {
                _widgetForm.Dispose();
                _widgetForm = null;
            }

            if (_controlCenter != null)
            {
                _controlCenter.Dispose();
                _controlCenter = null;
            }

            if (_wallpaperTimer != null)
            {
                _wallpaperTimer.Stop();
                _wallpaperTimer.Dispose();
                _wallpaperTimer = null;
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}

