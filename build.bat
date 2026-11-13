@echo off
setlocal
echo ========================================================
echo        Building BoWeather Desktop Application...
echo ========================================================

echo Menghentikan BoWeather jika sedang berjalan...
taskkill /F /IM BoWeather*.exe >nul 2>&1
taskkill /F /IM KoiWeather*.exe >nul 2>&1
timeout /t 1 /nobreak >nul 2>&1

set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo Error: C# compiler csc.exe tidak ditemukan!
    if "%1" neq "nopause" pause
    exit /b 1
)

echo Mengompilasi source code dari folder src...
"%CSC%" /nologo /optimize /target:winexe /win32manifest:app.manifest /out:BoWeather.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /recurse:src\*.cs

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [SUKSES] BoWeather.exe berhasil dikompilasi!
    if exist "..\BoWeather" (
        copy /y BoWeather.exe "..\BoWeather\BoWeather.exe" >nul 2>&1
        echo [SYNC] BoWeather.exe otomatis disinkronkan ke folder BoWeather.
    )
    echo Silakan jalankan run.bat atau klik ganda BoWeather.exe
) else (
    echo.
    echo [GAGAL] Terjadi kesalahan saat kompilasi.
)

if "%1" neq "nopause" pause
