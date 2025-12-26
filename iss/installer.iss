; 脚本由 Inno Setup 脚本向导生成。
; 有关创建 Inno Setup 脚本文件的详细信息，请参阅帮助文档！

#define MyAppName "蓝屏抽奖机"
#define MyAppVersion "2.0.1"
#define MyAppPublisher "蓝屏钙，好喝的钙"
#define MyAppURL "https://blog.lanpinggai.top"
#define MyAppExeName "lanpingcj.exe"
#define MyAppAssocName MyAppName + "文件"
#define MyAppAssocExt ""
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

[Setup]
; 注意：AppId 的值唯一标识此应用程序。不要在其他应用程序的安装程序中使用相同的 AppId 值。
; (若要生成新的 GUID，请在 IDE 中单击 "工具|生成 GUID"。)
AppId={{A198484D-ED9B-48A2-98FC-96243826E981}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
;AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\lanpingCJ
UninstallDisplayIcon={app}\{#MyAppExeName}
; "ArchitecturesAllowed=x64compatible" 指定
; 安装程序只能在 x64 和 Windows 11 on Arm 上运行。
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" 要求
; 在 X64 或 Windows 11 on Arm 上以 "64-位模式" 进行安装，
; 这意味着它应该使用本地 64 位 Program Files 目录
; 和注册表的 64 位视图。
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
DisableProgramGroupPage=yes
LicenseFile=LICENSE-2.0.txt
InfoBeforeFile=update.txt
; 取消注释以下行以在非管理安装模式下运行 (仅为当前用户安装)。
;PrivilegesRequired=lowest
OutputBaseFilename=蓝屏抽奖机
SetupIconFile=icon.ico
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 根据 GitHub Actions 工作流配置调整路径
; 方法1: 使用编译时的定义来构建正确的路径
#ifndef Configuration
  #define Configuration "Release"
#endif

#ifndef ProjectName
  #define ProjectName "WpfApp2"
#endif

; 单文件发布模式（推荐）
Source: "Publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "Publish\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "Publish\*.json"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "Publish\*.config"; DestDir: "{app}"; Flags: ignoreversion
; 如果需要包含运行时文件
Source: "Publish\*.txt"; DestDir: "{app}"; Flags: ignoreversion

; 或者方法2: 使用相对路径（适用于调试和发布）
; Source: ".\WpfApp2\bin\{#Configuration}\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 或者方法3: 使用编译时定义的完整路径
; #define SourcePath ".\WpfApp2\bin\{#Configuration}\net8.0-windows"
; Source: "{#SourcePath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
; Source: "{#SourcePath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; 包含必要的资源文件
Source: "icon.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "LICENSE-2.0.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "update.txt"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "testrun"; ValueData: "{app}\{#MyAppExeName}"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent