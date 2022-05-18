#define MyAppName "DICOM Capacitor Warden"
#define MyAppMajorVersion "1.0"
#define MyAppPublisher "Flux Inc"
#define MyAppUrl "https://fluxinc.ca"
#define MyAppExeName "DICOMCapacitorWarden.exe"
#define MyAppVersion GetFileVersion('..\bin\x64\' + MyAppConfiguration + '\DICOMCapacitorWarden.dll')
[Setup]
AppId={{E96DFA0E-31CC-4406-87DB-51398C3B2140}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppMajorVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
ArchitecturesAllowed=x64 arm64
DefaultGroupName={#MyAppPublisher}\{#MyAppName}
CreateAppDir=yes
OutputBaseFilename=DICOMCapacitorWardenSetup-x64-{#MyAppVersion}-{#MyAppSetupSuffix}
DisableProgramGroupPage=true
DisableDirPage=false
Compression=lzma
SolidCompression=yes
Uninstallable=yes
DefaultDirName={commonpf64}\{#MyAppPublisher}\{#MyAppName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\bin\{#MyAppConfiguration}\*.dll"; DestDir: "{app}"; Flags: ignoreversion;
Source: "..\bin\{#MyAppConfiguration}\*.pdb"; DestDir: "{app}"; Flags: ignoreversion;
Source: "..\bin\{#MyAppConfiguration}\*.xml"; DestDir: "{app}"; Flags: ignoreversion;
Source: "..\bin\{#MyAppConfiguration}\DICOMCapacitorWarden.*"; DestDir: "{app}"; Flags: ignoreversion;

[Run]
Filename: "{sys}\sc.exe"; Parameters: "stop DICOMCapacitorWarden"; Description: Stopping service; WorkingDir: {app}; Check: IsServiceRunning('DICOMCapacitorWarden');
Filename: "{app}\DICOMCapacitorWarden.exe"; Parameters: "--install"; Description: Installing service; WorkingDir: {app}; Flags: runhidden;

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop DICOMCapacitorWarden"; Check: IsServiceRunning('DICOMCapacitorWarden')
Filename: "{app}\DICOMCapacitorWarden.exe"; Parameters: "--uninstall"; Flags: runhidden;

[Code]

#include "Include\Services.pas"