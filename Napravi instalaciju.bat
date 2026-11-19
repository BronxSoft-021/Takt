@echo off
rem Pravi pravi Windows Setup: setup\Takt-Setup-<verzija>.exe
rem 1) dist-prenos (program + .NET u sebi, radi i tamo gde .NET nije instaliran)
rem 2) Inno Setup pravi jedan Setup.exe iz njega
rem Pre pokretanja zatvori program (ili pusti - Setup ga zatvori sam).
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
set ISCC="C:\Program Files\Inno Setup 7\ISCC.exe"
if not exist %ISCC% set ISCC="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist %ISCC% (
  echo.
  echo Nedostaje Inno Setup: instaliraj ga sa https://jrsoftware.org/isdl.php
  pause
  exit /b 1
)

echo Pakujem program (dist-prenos)...
dotnet publish Takt.csproj -c Release -r win-x64 --self-contained true -nologo -v q -o dist-prenos
if errorlevel 1 goto greska
dotnet publish Sluzba\TaktSluzba.csproj -c Release -r win-x64 --self-contained true -nologo -v q -o dist-prenos
if errorlevel 1 goto greska

echo Pravim Setup...
%ISCC% /Qp Instalacija\Takt.iss
if errorlevel 1 goto greska

echo.
echo Gotovo: setup\Takt-Setup-*.exe
exit /b 0

:greska
echo.
echo Pakovanje nije uspelo.
pause
exit /b 1
