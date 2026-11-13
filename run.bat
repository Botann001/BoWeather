@echo off
echo Menghentikan proses BoWeather / KoiWeather lama jika sedang berjalan...
taskkill /F /IM BoWeather*.exe >nul 2>&1
taskkill /F /IM KoiWeather*.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

if not exist "BoWeather.exe" (
    echo Mengompilasi BoWeather.exe...
    call build.bat nopause
)

start "" "BoWeather.exe"
echo.
echo ========================================================
echo   [SUKSES] BoWeather berhasil dijalankan!
echo ========================================================
echo Ikon aplikasi dapat ditemukan di System Tray (dekat jam).
echo Fitur yang aktif:
echo - Nama resmi: BoWeather
echo - Pemulihan salju taskbar dan jendela super cepat (~15-20 detik).
echo - Gulung bola salju (Snowball) dengan kursor mouse.
echo - Lempar jendela dengan cepat untuk menghamburkan salju.
echo - Auto-Pause saat Main Game / Fullscreen (0.0%% CPU ^& 0.0%% GPU).
timeout /t 3 /nobreak >nul 2>&1
