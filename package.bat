@echo off
setlocal
echo ==============================================
echo   Packaging BoWeather Portable Release (.ZIP)
echo ==============================================

cd /d "%~dp0"

echo [1/3] Compiling BoWeather.exe...
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /nologo /optimize /target:winexe /win32manifest:app.manifest /out:BoWeather.exe /r:System.Windows.Forms.dll /r:System.Drawing.dll /recurse:src\*.cs
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Compilation failed!
    pause
    exit /b %ERRORLEVEL%
)

echo [2/3] Packaging into ZIP...
powershell -NoProfile -ExecutionPolicy Bypass -Command "$base = (Get-Item .).FullName; $dist = Join-Path $base 'dist'; if (!(Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }; python -c 'import zipfile, os; base = r\"'\"$base'\"'; dist = os.path.join(base, \"dist\"); zpath = os.path.join(dist, \"BoWeather-v1.0.0-Portable.zip\"); guide = os.path.join(dist, \"PANDUAN_MULAI.txt\"); z = zipfile.ZipFile(zpath, \"w\", zipfile.ZIP_DEFLATED); z.write(os.path.join(base, \"BoWeather.exe\"), \"BoWeather/BoWeather.exe\"); (z.write(guide, \"BoWeather/PANDUAN_MULAI.txt\") if os.path.exists(guide) else None); [z.write(os.path.join(r, f), os.path.join(\"BoWeather\", os.path.relpath(os.path.join(r, f), base)).replace(\"\\\\\", \"/\")) for r, _, fs in os.walk(os.path.join(base, \"wallpapers\")) for f in fs]; z.close(); print(f\"Finished! Size: {os.path.getsize(zpath) / (1024*1024):.2f} MB\")'"

echo [3/3] Selesai! File ZIP siap dibagikan ada di: dist\BoWeather-v1.0.0-Portable.zip
pause
