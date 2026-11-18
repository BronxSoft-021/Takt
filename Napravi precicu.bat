@echo off
rem Precica "BronxSoft Takt" na desktopu -> dist\Takt.exe
cd /d "%~dp0"
if not exist "dist\Takt.exe" (
  echo Prvo pokreni "Napravi exe.bat".
  pause
  exit /b 1
)
powershell -NoProfile -Command "$d=[Environment]::GetFolderPath('Desktop'); $e=(Resolve-Path 'dist\Takt.exe').Path; $o=Join-Path $d 'BronxSoft Takt.lnk'; if (Test-Path $o) { Remove-Item $o }; $s=(New-Object -ComObject WScript.Shell).CreateShortcut($o); $s.TargetPath=$e; $s.WorkingDirectory=(Split-Path $e); $s.IconLocation=$e + ',0'; $s.Description='BronxSoft Takt'; $s.Save(); 'Precica napravljena: ' + $o"
