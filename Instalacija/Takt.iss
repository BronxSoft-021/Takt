; BronxSoft Takt - pravi Windows Setup (Inno Setup 7).
; Pravi se sa "Napravi instalaciju.bat": prvo dist-prenos (sa .NET-om u sebi), pa ova skripta.
; Setup nosi sve sto treba: na drugom racunaru ne treba ni .NET ni bilo sta drugo osim PawnIO drajvera.

#define Ver "0.39.91"

[Setup]
AppId={{8E4C1D62-77A5-4B21-9E3C-5A1B7D0F2C64}
AppName=BronxSoft Takt
AppVersion={#Ver}
AppVerName=BronxSoft Takt {#Ver}
AppPublisher=BronxSoft
AppPublisherURL=https://github.com/BronxSoft-021/Takt
DefaultDirName={autopf}\BronxSoft Takt
DefaultGroupName=BronxSoft Takt
DisableProgramGroupPage=yes
UninstallDisplayName=BronxSoft Takt
UninstallDisplayIcon={app}\Takt.exe
OutputDir=..\setup
OutputBaseFilename=Takt-Setup-{#Ver}
SetupIconFile=..\assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#Ver}
VersionInfoCompany=BronxSoft
VersionInfoDescription=BronxSoft Takt - instalacija
VersionInfoProductName=BronxSoft Takt

[Languages]
Name: "sr"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "usluga"; Description: "Nadzor u pozadini (usluga) - cita senzore i kad je prozor zatvoren"; GroupDescription: "Sta da se uradi:"; Flags: checkedonce
Name: "desktopicon"; Description: "Precica na radnoj povrsini"; GroupDescription: "Precice:"; Flags: unchecked

[Files]
Source: "..\dist-prenos\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\BronxSoft Takt"; Filename: "{app}\Takt.exe"; WorkingDir: "{app}"
Name: "{group}\Ukloni BronxSoft Takt"; Filename: "{uninstallexe}"
Name: "{autodesktop}\BronxSoft Takt"; Filename: "{app}\Takt.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
; The service is registered by the program itself (sc create, ACLs on its data folder, the user's settings path).
Filename: "{app}\TaktSluzba.exe"; Parameters: "--instaliraj --korisnik ""{userappdata}\BronxSoft Takt"""; StatusMsg: "Registrujem uslugu nadzora..."; Flags: runhidden waituntilterminated; Tasks: usluga
; PawnIO is a separate, free download and is not ours to hand out: the Setup only opens the page, and only if it
; is missing. Without it the program still runs, with fewer sensors, and says so.
Filename: "https://pawnio.eu"; Description: "Otvori pawnio.eu - PawnIO drajver za temperaturu procesora i ventilatore"; Flags: postinstall shellexec nowait skipifsilent unchecked; Check: not ImaPawnIo
Filename: "{app}\Takt.exe"; Description: "Pokreni BronxSoft Takt"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Stops and removes the service, deletes the shortcuts that point into {app}; the history and the user's settings stay.
Filename: "{app}\TaktSluzba.exe"; Parameters: "--ukloni"; RunOnceId: "UkloniUslugu"; Flags: runhidden waituntilterminated

[UninstallDelete]
; --ukloni deletes the folder itself; the uninstaller then recreates it for its own log and would leave it empty.
Type: dirifempty; Name: "{app}"

[Code]
function ImaPawnIo(): Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\PawnIO')
         or RegKeyExists(HKLM32, 'SYSTEM\CurrentControlSet\Services\PawnIO');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Kod: Integer;
begin
  if CurStep <> ssInstall then Exit;

  { An older Takt and its service hold their files open: stop them before Setup replaces anything, or the copy
    fails on TaktSluzba.exe (a running service is not something Inno's CloseApplications can close). The service
    is registered again after the copy. }
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Takt.exe', '', SW_HIDE, ewWaitUntilTerminated, Kod);
  Exec(ExpandConstant('{sys}\sc.exe'), 'stop BronxSoftTakt', '', SW_HIDE, ewWaitUntilTerminated, Kod);
  Sleep(2000);
  Exec(ExpandConstant('{sys}\sc.exe'), 'delete BronxSoftTakt', '', SW_HIDE, ewWaitUntilTerminated, Kod);
  Sleep(500);
end;
