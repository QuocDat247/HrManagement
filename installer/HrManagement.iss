#define MyAppName "HR Management"
#define MyAppExeName "HrManagement.Desktop.exe"

#ifndef AppVersion
  #define AppVersion "1.0.0-rc2"
#endif

#ifndef AppFileVersion
  #define AppFileVersion "1.0.0.0"
#endif

#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif

#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

[Setup]
; IMPORTANT:
; Keep this AppId unchanged for all future versions.
; It is inherited from the original Preview installer.
AppId={{A07FA45C-8578-4C7A-9A2C-7F53E904145D}

AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}

DefaultDirName={autopf}\HR Management
DefaultGroupName=HR Management

OutputDir={#OutputDir}
OutputBaseFilename=HR-Management-Setup-{#AppVersion}

SetupArchitecture=x64
ArchitecturesAllowed=x64compatible

PrivilegesRequired=admin

Compression=lzma2
SolidCompression=yes
WizardStyle=modern

DisableProgramGroupPage=yes

CloseApplications=yes
RestartApplications=no

UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

VersionInfoVersion={#AppFileVersion}
VersionInfoProductName={#MyAppName}
VersionInfoDescription=HR Management Setup
VersionInfoProductVersion={#AppFileVersion}
VersionInfoProductTextVersion={#AppVersion}

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "Tạo biểu tượng ngoài Desktop"; GroupDescription: "Tùy chọn bổ sung:"; Flags: unchecked

[Icons]
Name: "{autoprograms}\HR Management"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"
Name: "{autodesktop}\HR Management"; Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Khởi động HR Management"; Flags: nowait postinstall skipifsilent