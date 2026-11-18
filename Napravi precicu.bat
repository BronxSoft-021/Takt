@echo off
rem Precica "BronxSoft Takt" na desktopu: na instalirani Takt (Program Files) ako postoji, inace na dist\Takt.exe.
rem Instalacija ("Instaliraj Takt.bat") pravi precice sama; ovo je samo za rad bez instalacije.
cd /d "%~dp0"
set "EXE=%ProgramFiles%\BronxSoft Takt\Takt.exe"
if not exist "%EXE%" set "EXE=%~dp0dist\Takt.exe"
if not exist "%EXE%" (
  echo Prvo pokreni "Instaliraj Takt.bat" ili "Napravi exe.bat".
  pause
  exit /b 1
)
powershell -NoProfile -Command "$d=[Environment]::GetFolderPath('Desktop'); $e=$env:EXE; $o=Join-Path $d 'BronxSoft Takt.lnk'; if (Test-Path $o) { Remove-Item $o }; $s=(New-Object -ComObject WScript.Shell).CreateShortcut($o); $s.TargetPath=$e; $s.WorkingDirectory=(Split-Path $e); $s.IconLocation=$e + ',0'; $s.Description='BronxSoft Takt'; $s.Save(); 'Precica napravljena: ' + $o + ' -> ' + $e"
