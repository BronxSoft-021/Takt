@echo off
rem Pravi dist\TaktSluzba.exe (usluga). Instalacija (jednom, kao administrator):
rem   dist\TaktSluzba.exe --instaliraj
rem   -> kopira se u "C:\Program Files\BronxSoft Takt\Usluga" i radi odande (ne iz dist, u koji svako piše);
rem      istorija i dnevnik usluge su u C:\ProgramData\BronxSoft Takt. Posle novog builda: ponovo --instaliraj.
rem Uklanjanje:
rem   dist\TaktSluzba.exe --ukloni   (briše i kopiju u Program Files; istorija ostaje)
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet publish Sluzba\TaktSluzba.csproj -c Release -nologo -v q -o dist
if errorlevel 1 (
  echo.
  echo Pakovanje usluge nije uspelo.
  pause
  exit /b 1
)
echo.
echo Gotovo: dist\TaktSluzba.exe
echo Instalacija: dist\TaktSluzba.exe --instaliraj   (kao administrator; kopira se u Program Files)
