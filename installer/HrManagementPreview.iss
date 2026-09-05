#define MyAppName "HR Management"
#define MyAppVersion "0.1.0-preview.1"
#define MyAppExeName "HrManagement.Desktop.exe"

[Setup]
AppId={{A07FA45C-8578-4C7A-9A2C-7F53E904145D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} Preview {#MyAppVersion}

DefaultDirName={autopf}\HR Management
DefaultGroupName=HR Management

OutputDir=..\artifacts\installer
OutputBaseFilename=Setup

Compression=lzma2
SolidCompression=yes

PrivilegesRequired=admin

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

DisableProgramGroupPage=yes
WizardStyle=modern

UninstallDisplayName={#MyAppName} Preview
UninstallDisplayIcon={app}\{#MyAppExeName}

VersionInfoVersion=0.1.0.0
VersionInfoProductName={#MyAppName}
VersionInfoDescription=HR Management Preview Setup
VersionInfoProductVersion=0.1.0.0
VersionInfoProductTextVersion={#MyAppVersion}

[Files]
Source: "..\artifacts\staging\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Icons]
Name: "{autoprograms}\HR Management"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\HR Management"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch HR Management"; Flags: nowait postinstall skipifsilent