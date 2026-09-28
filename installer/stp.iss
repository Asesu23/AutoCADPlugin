#ifndef AppVersion
  #define AppVersion "4.0.0.0"
#endif

[Setup]
AppId={{A6D2C8E4-1234-4567-89AB-CDEF12345678}}
AppName=AutoCAD Plugin (Mikhail Nekrasov)
AppVersion={#AppVersion}
; Устанавливаем в общую папку плагинов Autodesk
DefaultDirName={commonappdata}\Autodesk\ApplicationPlugins\AutoCADPlugin.bundle
DisableDirPage=yes
DefaultGroupName=AutoCAD Plugin
OutputBaseFilename=AutoCADPlugin_Setup
Compression=lzma
SolidCompression=yes
; Инсталлятор сам попросит закрыть AutoCAD, если он запущен
CloseApplications=yes

[Files]
; 1. Копируем файл-манифест в корень бандла
Source: "PackageContents.xml"; DestDir: "{app}"; Flags: ignoreversion
; 2. Копируем всё содержимое вашей чистой папки bin в подпапку Contents
Source: "..\AutoCADPlugin\bin\x64\Release\net48\*"; DestDir: "{app}\Contents"; Flags: recursesubdirs createallsubdirs ignoreversion

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Run]
Filename: "{code:GetAutoCADPath}"; \
    Description: "{cm:LaunchProgram,AutoCAD}"; \
    Flags: postinstall nowait skipifsilent; \
    Check: ShouldRunAutoCAD
    
[Code]

var
  FoundAutoCADPaths: TArrayOfString;      // массив найденных путей к acad.exe
  SelectedAutoCADPath: string;             // путь, выбранный пользователем
  SelectAutoCADPage: TInputOptionWizardPage; // страница для выбора версии

// ------------------------------------------------------------------
// Поиск всех установленных версий AutoCAD в реестре
// ------------------------------------------------------------------
procedure FindAllAutoCADVersions();
var
  I, J: Integer;
  Releases, SubVersions: TArrayOfString;
  ReleaseKey, SubKey, AcadPath: string;
  PathsCount: Integer;
  RegRoot: Integer;
begin
  SetArrayLength(FoundAutoCADPaths, 0);
  PathsCount := 0;

  if IsWin64 then
    RegRoot := HKLM64
  else
    RegRoot := HKEY_LOCAL_MACHINE;

  Log('Начинаем поиск AutoCAD в реестре...');

  if RegGetSubkeyNames(RegRoot, 'SOFTWARE\Autodesk\AutoCAD', Releases) then
  begin
    for I := 0 to GetArrayLength(Releases) - 1 do
    begin
      ReleaseKey := 'SOFTWARE\Autodesk\AutoCAD\' + Releases[I];
      Log('Проверяем релиз: ' + ReleaseKey);

      if RegGetSubkeyNames(RegRoot, ReleaseKey, SubVersions) then
      begin
        for J := 0 to GetArrayLength(SubVersions) - 1 do
        begin
          SubKey := ReleaseKey + '\' + SubVersions[J];
          Log('  Проверяем подраздел: ' + SubKey);

          // Сначала пробуем AcadLocation (современные версии)
          if RegQueryStringValue(RegRoot, SubKey, 'AcadLocation', AcadPath) then
          begin
            if (AcadPath <> '') and FileExists(AcadPath + '\acad.exe') then
            begin
              Log('  НАЙДЕН AutoCAD: ' + AcadPath);
              SetArrayLength(FoundAutoCADPaths, PathsCount + 1);
              FoundAutoCADPaths[PathsCount] := AcadPath + '\acad.exe';
              PathsCount := PathsCount + 1;
            end;
          end
          else
          // Если нет AcadLocation, пробуем Location (старые версии)
          if RegQueryStringValue(RegRoot, SubKey, 'Location', AcadPath) then
          begin
            if (AcadPath <> '') and FileExists(AcadPath + '\acad.exe') then
            begin
              Log('  НАЙДЕН AutoCAD (через Location): ' + AcadPath);
              SetArrayLength(FoundAutoCADPaths, PathsCount + 1);
              FoundAutoCADPaths[PathsCount] := AcadPath + '\acad.exe';
              PathsCount := PathsCount + 1;
            end;
          end;
        end;
      end;
    end;
  end;

  Log('Поиск завершен. Найдено версий: ' + IntToStr(GetArrayLength(FoundAutoCADPaths)));
end;

// ------------------------------------------------------------------
// Создание страницы выбора, если найдено несколько версий
// ------------------------------------------------------------------
procedure InitializeWizard();
var
  I: Integer;
begin
  FindAllAutoCADVersions();

  if GetArrayLength(FoundAutoCADPaths) > 1 then
  begin
    SelectAutoCADPage := CreateInputOptionPage(wpSelectTasks,
      'Выберите версию AutoCAD',
      'На вашем компьютере найдено несколько версий AutoCAD.',
      'Пожалуйста, выберите версию, для которой вы хотите установить плагин:',
      True, False);

    for I := 0 to GetArrayLength(FoundAutoCADPaths) - 1 do
    begin
      SelectAutoCADPage.Add(ExtractFilePath(FoundAutoCADPaths[I]));
    end;

    SelectAutoCADPage.SelectedValueIndex := 0; // предвыбор первого
  end;
end;

// ------------------------------------------------------------------
// Функция возвращает путь к acad.exe для секции [Run]
// ------------------------------------------------------------------
function GetAutoCADPath(Param: string): string;
begin
  // Если пользователь уже выбрал (на странице)
  if SelectedAutoCADPath <> '' then
  begin
    Result := SelectedAutoCADPath;
    Exit;
  end;

  // Если найдена ровно одна версия
  if GetArrayLength(FoundAutoCADPaths) = 1 then
  begin
    Result := FoundAutoCADPaths[0];
    Exit;
  end;

  // Если несколько версий и страница существует, берём выбранный пункт
  if (GetArrayLength(FoundAutoCADPaths) > 1) and Assigned(SelectAutoCADPage) then
  begin
    if SelectAutoCADPage.SelectedValueIndex >= 0 then
    begin
      SelectedAutoCADPath := FoundAutoCADPaths[SelectAutoCADPage.SelectedValueIndex];
      Result := SelectedAutoCADPath;
      Exit;
    end;
  end;

  // Ничего не найдено
  Result := '';
end;

// ------------------------------------------------------------------
// Проверка, нужно ли показывать пункт запуска AutoCAD
// ------------------------------------------------------------------
function ShouldRunAutoCAD: Boolean;
var
  Path: string;
begin
  Path := GetAutoCADPath('');
  Result := (Path <> '') and FileExists(Path);
end;