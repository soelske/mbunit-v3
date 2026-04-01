!include LogicLib.nsh
!addincludedir "$%XX_SETUP%"

!include "TextFunc.nsh"
!include "xx_string_functions.nsh"
!include "xx_acad_macros.nsh"

; Enkel 64 bit versies maken
!define X64

RequestExecutionLevel admin

; Basis defines om de productnaam, -locatie en -versie op te geven.
; OUTPUTFOLDER en VERSION worden in compile-setup.bat vastgelegd.
!define PRODUCT_NAME "Icarus"
!define PRODUCT_VERSION "$VERSION$"

!define PRODUCT_DIR "$OUTPUTFOLDER$"

; MbUnit plugin output — ingevuld door compile-setup.bat
!define MBUNIT_CORE_DIR  "$MBUNIT_CORE_DIR$"

; AutoCAD plugin directories — ingevuld door compile-setup.bat
!define ACAD_CORE_DIR    "$ACAD_CORE_DIR$"
!define ACAD_UI_CORE_DIR "$ACAD_UI_CORE_DIR$"
!define ACAD_PLUGIN_DIR  "$ACAD_PLUGIN_DIR$"

; Gallio.Reports.Core output — ingevuld door compile-setup.bat
!define REPORTS_DIR      "$REPORTS_DIR$"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "${PRODUCT_NAME} ${PRODUCT_VERSION}.exe"

Icon "${PRODUCT_DIR}\Resources\Gallio.Icarus.ico"

!ifndef INSTALL_LOCATION
!define INSTALL_LOCATION "C:\iTs\${PRODUCT_NAME}\"
!endif

InstallDir "${INSTALL_LOCATION}"

!define PRODUCT_UN_INST_KEY_NAME "${PRODUCT_NAME}"

SectionGroup /e "Icarus"

Section -"Common" SEC_COMMON

  SetOverwrite ifnewer

  SetOutPath "$INSTDIR"

  ; Uitvoerbare bestanden
  File "${PRODUCT_DIR}\*.exe"

  ; Assemblies
  File "${PRODUCT_DIR}\*.dll"

  ; .NET 8 runtime configuratie
  File "${PRODUCT_DIR}\*.json"

  ; Plugin descriptors in Plugins\ subfolder — individueel opgelijst zodat elke plugin
  ; in zijn eigen submap terechtkomt (geen wildcard die ze dubbel plaatst)
  SetOutPath "$INSTDIR\Plugins"
  File "${PRODUCT_DIR}\Gallio.plugin"
  File "${PRODUCT_DIR}\Gallio.UI.plugin"
  File "${PRODUCT_DIR}\Gallio.Icarus.plugin"

  ; Gallio core resources — plugin://Gallio/Resources/... resolveert naar Plugins\Resources\...
  ; BaseDirectory van Gallio.plugin = Plugins\, dus iconen moeten hier staan (niet in de root Resources\)
  SetOutPath "$INSTDIR\Plugins\Resources"
  File "${PRODUCT_DIR}\Resources\Assembly.ico"
  File "${PRODUCT_DIR}\Resources\Container.ico"
  File "${PRODUCT_DIR}\Resources\Fixture.ico"
  File "${PRODUCT_DIR}\Resources\Gallio.ico"
  File "${PRODUCT_DIR}\Resources\Gallio.Icarus.ico"
  File "${PRODUCT_DIR}\Resources\Test.ico"
  File "${PRODUCT_DIR}\Resources\Unsupported.ico"

  ; MbUnit plugin — zelfstandig in eigen submap
  ; plugin://MbUnit/Resources/MbUnit.ico resolveert naar Plugins\MbUnit\Resources\MbUnit.ico
  SetOutPath "$INSTDIR\Plugins\MbUnit"
  File "${MBUNIT_CORE_DIR}\MbUnit.dll"
  File "${MBUNIT_CORE_DIR}\MbUnit.plugin"
  SetOutPath "$INSTDIR\Plugins\MbUnit\Resources"
  File "${MBUNIT_CORE_DIR}\Resources\MbUnit.ico"

  ; Gallio.Reports plugin — zelfstandig in eigen submap
  SetOutPath "$INSTDIR\Plugins\Reports"
  File "${REPORTS_DIR}\Gallio.Reports.dll"
  File "${REPORTS_DIR}\NVelocity.dll"
  File "${REPORTS_DIR}\Gallio.Reports.plugin"

  ; Reports resources — enkel de subdirectories; root-level iconen (Assembly.ico etc.) worden weggelaten
  ; (die komen van Gallio.Core via ProjectReference en horen bij plugin://Gallio, niet bij Gallio.Reports)
  SetOutPath "$INSTDIR\Plugins\Reports\Resources"
  File /r "${REPORTS_DIR}\Resources\css"
  File /r "${REPORTS_DIR}\Resources\img"
  File /r "${REPORTS_DIR}\Resources\js"
  File /r "${REPORTS_DIR}\Resources\vm"
  File /r "${REPORTS_DIR}\Resources\xsl"

  ; AutoCAD plugin — zelfstandig in eigen submap, zoals de oude Icarus installer
  ; AcadPluginLocator zoekt Gallio.AutoCAD.Plugin*.dll in dezelfde map als Gallio.AutoCAD.dll
  SetOutPath "$INSTDIR\Plugins\AutoCad"
  File "${ACAD_CORE_DIR}\Gallio.AutoCAD.dll"
  File "${ACAD_UI_CORE_DIR}\Gallio.AutoCAD.UI.dll"
  File "${ACAD_PLUGIN_DIR}\Gallio.AutoCAD.Plugin250.dll"
  File "${ACAD_CORE_DIR}\Gallio.AutoCAD.plugin"
  File "${ACAD_UI_CORE_DIR}\Gallio.AutoCAD.UI.plugin"


  ; AutoCAD resources — alleen Gallio.AutoCAD.ico (enige resource gerefereerd door het plugin descriptor)
  ; plugin://Gallio.AutoCAD/Resources/Gallio.AutoCAD.ico resolveert naar Plugins\AutoCad\Resources\Gallio.AutoCAD.ico
  ; Generieke Gallio-iconen (Assembly.ico etc.) worden niet gekopieerd: die staan in Plugins\ via Gallio.plugin
  SetOutPath "$INSTDIR\Plugins\AutoCad\Resources"
  File "${ACAD_CORE_DIR}\Resources\Gallio.AutoCAD.ico"

  ; Resources: Icarus iconen + Gallio.Reports css/js/img/vm/xsl
  SetOutPath "$INSTDIR\Resources"
  File /r "${PRODUCT_DIR}\Resources\*.*"

  ; .NET 8 native runtime dependencies (runtimes\win\...)
  SetOutPath "$INSTDIR\runtimes"
  File /r "${PRODUCT_DIR}\runtimes\*.*"

  CreateShortCut "$%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\${PRODUCT_NAME}.lnk" "$INSTDIR\Gallio.Icarus.exe"
  CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\Gallio.Icarus.exe"

SectionEnd

SectionGroupEnd

!include "xx_convert_database.nsh"

Section un.Icarus

  ; Programma bestanden verwijderen
  Delete "$INSTDIR\*.exe"
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\*.plugin"
  Delete "$INSTDIR\*.json"
  Delete "$INSTDIR\*.config"
  Delete "$INSTDIR\*.ico"
  Delete "$INSTDIR\*.bmp"

  Delete "$%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\${PRODUCT_NAME}.lnk"
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"

  RmDir /r "$INSTDIR\Plugins"
  RmDir /r "$INSTDIR\Resources"
  RmDir /r "$INSTDIR\runtimes"

SectionEnd

Section -un.Icarus.Final
  ; Alle overgebleven bestanden en de hoofdmap verwijderen
  Delete "$INSTDIR\*.*"
  RmDir /r "$INSTDIR"
SectionEnd

!define XX_CONFIGNAME "Icarus"
!define XX_CONFIGDIR "$INSTDIR\"

Function .onInstSuccess

!ifdef NSIS_ACCESSCONTROL
  ; Geeft BUILTIN\Users volledige toegang op de installatiemap.
  ; Vereist de AccessControl NSIS-plugin (https://nsis.sourceforge.io/AccessControl_plug-in).
  AccessControl::GrantOnFile "$INSTDIR" "(BU)" "FullAccess"
!endif

  SetOutPath "$INSTDIR\"
  WriteUninstaller "$INSTDIR\uninst-re.exe"

FunctionEnd
