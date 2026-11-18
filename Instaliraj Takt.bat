@echo off
rem Pravi Takt i uslugu u dist, pa ih instalira u "C:\Program Files\BronxSoft Takt" (Windows pita za dozvolu).
rem Posle instalacije precice na desktopu i u Start meniju otvaraju instalirani Takt, a usluga radi odande.
rem Ponovo pokrenuti posle svake izmene koda (program i usluga uvek idu zajedno, ista verzija).
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1

dotnet publish Takt.csproj -c Release -nologo -v q -o dist
if errorlevel 1 goto greska
dotnet publish Sluzba\TaktSluzba.csproj -c Release -nologo -v q -o dist
if errorlevel 1 goto greska

echo.
echo Instaliram (Windows ce pitati za dozvolu)...
powershell -NoProfile -Command "$p = Start-Process -FilePath '%~dp0dist\TaktSluzba.exe' -ArgumentList '--instaliraj' -Verb RunAs -Wait -PassThru; exit $p.ExitCode"
if errorlevel 1 (
  echo.
  echo Instalacija nije uspela ili je otkazana.
  pause
  exit /b 1
)

rem Pokrece se kao obican korisnik (ne iz administratorskog prozora), kao i sa precice.
start "" "%ProgramFiles%\BronxSoft Takt\Takt.exe"
echo Gotovo: Takt je instaliran i pokrenut.
exit /b 0

:greska
echo.
echo Pravljenje nije uspelo (da li je Takt iz dist otvoren?).
pause
exit /b 1
