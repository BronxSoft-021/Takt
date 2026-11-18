@echo off
rem Pravi dist\Takt.exe (pre toga zatvori program: Podesavanja -> izlaz iz prozora)
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet publish Takt.csproj -c Release -nologo -v q -o dist
if errorlevel 1 (
  echo.
  echo Pakovanje nije uspelo.
  pause
  exit /b 1
)
echo.
echo Gotovo: dist\Takt.exe
