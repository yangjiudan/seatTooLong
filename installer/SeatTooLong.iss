#define MyAppName "SeatTooLong"
#define MyAppExeName "SeatTooLong.App.exe"
#define MyAppPublisher "SeatTooLong"
#ifndef MyAppVersion
	#define MyAppVersion "0.0.0"
#endif
#ifndef MyOutputBaseFilename
	#define MyOutputBaseFilename "SeatTooLong-Setup-x64-" + MyAppVersion
#endif
#define MyAppId "{{8E7C5C88-8D31-4C4F-8D0F-4F0A9C64F63E}"
#define MyAppUserModelId "SeatTooLong.SeatTooLong"
#define SourceDir "..\artifacts\publish\win-x64"
#define OutputDir "..\artifacts\installer"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename={#MyOutputBaseFilename}
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
SetupIconFile=seattoolong-installer.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl,..\artifacts\installer-languages\ChineseSimplified.generated.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; AppUserModelID: "{#MyAppUserModelId}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon; AppUserModelID: "{#MyAppUserModelId}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
