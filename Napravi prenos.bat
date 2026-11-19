@echo off
rem Pravi dist-prenos: isti program, ali sa .NET-om u sebi - radi na racunaru na kome nista nije
rem instalirano (ni .NET 10). Folder je velik (~190 MB); za svoj racunar koristi "Napravi exe.bat".
cd /d "%~dp0"
set DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet publish Takt.csproj -c Release -r win-x64 --self-contained true -nologo -v q -o dist-prenos
if errorlevel 1 goto greska
dotnet publish Sluzba\TaktSluzba.csproj -c Release -r win-x64 --self-contained true -nologo -v q -o dist-prenos
if errorlevel 1 goto greska
echo.
echo Gotovo: dist-prenos (ceo folder se prenosi; Takt.exe radi bez instalacije .NET-a).
exit /b 0

:greska
echo.
echo Pakovanje nije uspelo.
pause
exit /b 1
