@echo off
rem Slike svih ekrana u folder "slike" (probni podaci, program se sam zatvori).
cd /d "%~dp0"
if not exist "bin\Debug\net10.0-windows10.0.19041.0\win-x64\Takt.exe" (
  echo Prvo pokreni: dotnet build Takt.csproj
  pause
  exit /b 1
)
start "" /wait "bin\Debug\net10.0-windows10.0.19041.0\win-x64\Takt.exe" --snimak "%~dp0slike"
echo.
echo Gotovo: slike\
