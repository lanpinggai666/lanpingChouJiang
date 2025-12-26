; 脚本由 Inno Setup 脚本向导生成。
; 有关创建 Inno Setup 脚本文件的详细信息，请参阅帮助文档！

; ========== 动态参数定义（由 GitHub Actions 工作流传入）==========
; 这些参数将通过 GitHub Actions 工作流的 /D 参数传入
#ifndef MyAppVersion
  #define MyAppVersion "2.0.1"
#endif

#ifndef Configuration
  #define Configuration "Release"
#endif

#ifndef VariantName
  #define VariantName "PortableSingleFile"
#endif

#ifndef PublishDir
  #define PublishDir "..\Publish\PortableSingleFile"
#endif

#ifndef SelfContained
  #define SelfContained "false"
#endif

#ifndef PublishSingleFile
  #define PublishSingleFile "true"
#endif

; ========== 静态参数定义 ==========
#define MyAppName "蓝屏抽奖机"
#define MyAppPublisher "蓝屏钙，好喝的钙"
#define MyAppURL "https://blog.lanpinggai.top"
#define MyAppExeName "lanpingcj.exe"
#define MyAppAssocName MyAppName + "文件"
#define MyAppAssocExt ""
#define MyAppAssocKey StringChange(MyAppAssocName, " ", "") + MyAppAssocExt

; ========== 根据变体类型设置输出文件名后缀 ==========
#if VariantName == "PortableSingleFile"
  #define OutputSuffix "_便携单文件版"
#elif VariantName == "FullSingleFile"
  #define OutputSuffix "_单文件完整版"
#elif VariantName == "PortableFrameworkDependent"
  #define OutputSuffix "_便携框架版"
#elif VariantName == "FullFrameworkDependent"
  #define OutputSuffix "_完整框架版"
#else
  #define OutputSuffix ""
#endif

[Setup]
; 注意：AppId 的值唯一标识此应用程序。不要在其他应用程序的安装程序中使用相同的 AppId 值。
AppId={{A198484D-ED9B-48A2-98FC-96243826E981}
AppName={#MyAppName}{#OutputSuffix}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\lanpingCJ
UninstallDisplayIcon={app}\{#MyAppExeName}
; "ArchitecturesAllowed=x64compatible" 指定安装程序只能在 x64 和 Windows 11 on Arm 上运行。
ArchitecturesAllowed=x64compatible
; "ArchitecturesInstallIn64BitMode=x64compatible" 要求在 X64 或 Windows 11 on Arm 上以 "64-位模式" 进行安装
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=yes
DisableProgramGroupPage=yes
LicenseFile=LICENSE-2.0.txt
InfoBeforeFile=update.txt
; 取消注释以下行以在非管理安装模式下运行 (仅为当前用户安装)。
;PrivilegesRequired=lowest
; 注意：工作流中已经使用 /F 参数指定了输出文件名，这里可以保持固定或留空
OutputBaseFilename=蓝屏抽奖机{#OutputSuffix}
SetupIconFile=..\icon.ico
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; ========== 根据单文件/多文件模式动态包含文件 ==========
; 使用传入的 PublishDir 变量，这是工作流根据变体自动设置的
#if PublishSingleFile == "true"
  ; 单文件模式：只包含主程序文件
  Source: "{#PublishDir}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
  ; 如果单文件模式下有其他必要的配置文件，可以在这里添加
  ; Source: "{#PublishDir}\config.json"; DestDir: "{app}"; Flags: ignoreversion
#else
  ; 多文件模式：包含所有文件
  Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif

; ========== 条件包含.NET运行时文件（仅当自包含时为true）==========
#if SelfContained == "true"
  ; 注意：当使用 --self-contained true 时，运行时文件通常已经包含在 PublishDir 中
  ; 但如果您需要特殊处理运行时文件的位置，可以在这里添加额外规则
  ; 例如，将运行时文件放在特定的子目录中：
  ; Source: "{#PublishDir}\runtimes\*"; DestDir: "{app}\runtimes"; Flags: ignoreversion recursesubdirs createallsubdirs
#endif

; ========== 添加版本信息文件（可选）==========
; Source: "{#PublishDir}\version.txt"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocExt}\OpenWithProgids"; ValueType: string; ValueName: "{#MyAppAssocKey}"; ValueData: ""; Flags: uninsdeletevalue
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}"; ValueType: string; ValueName: ""; ValueData: "{#MyAppAssocName}"; Flags: uninsdeletekey
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#MyAppExeName},0"
Root: HKA; Subkey: "Software\Classes\{#MyAppAssocKey}\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#MyAppExeName}"" ""%1"""
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "testrun"; ValueData: "{app}\{#MyAppExeName}"

[Icons]
Name: "{autoprograms}\{#MyAppName}{#OutputSuffix}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}{#OutputSuffix}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

; ========== 自定义代码部分（可选）==========
[Code]
// 显示安装配置信息
procedure InitializeWizard();
begin
  // 在安装向导中添加一个信息页面或标签，显示当前变体信息
  #ifdef VariantName
    MsgBox('正在安装：' + '{#MyAppName}{#OutputSuffix}' + #13#10 +
           '版本：' + '{#MyAppVersion}' + #13#10 +
           '变体：' + '{#VariantName}' + #13#10 +
           '配置：单文件=' + '{#PublishSingleFile}' + ', 自包含=' + '{#SelfContained}',
           mbInformation, MB_OK);
  #endif
end;

// 安装完成后的处理
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 可以在这里添加安装后的自定义操作
    Log('安装完成：' + ExpandConstant('{app}'));
  end;
end;