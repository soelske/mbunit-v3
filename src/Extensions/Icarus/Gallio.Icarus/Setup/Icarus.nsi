!include LogicLib.nsh
!addincludedir "$%XX_SETUP%"

!include "TextFunc.nsh"
!include "xx_string_functions.nsh"
!include "xx_acad_macros.nsh"

; Enkel 64 bit versies maken
!define X64
!define VS2008

RequestExecutionLevel admin

; Basis defines om de productnaam, -locatie en -versie op te geven.
; EXECFOLDER wordt in compile-setup.bat vastgelegd
!define PRODUCT_NAME "Icarus"
!define PRODUCT_VERSION "$VERSION$"

!define PRODUCT_DIR "$OUTPUTFOLDER$"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "${PRODUCT_NAME} ${PRODUCT_VERSION}.exe"

Icon "${PRODUCT_DIR}\..\Resources\Gallio.Icarus.ico"

!ifndef INSTALL_LOCATION
!define INSTALL_LOCATION "C:\iTs\${PRODUCT_NAME}\"
!endif

InstallDir "${INSTALL_LOCATION}"

!define PRODUCT_UN_INST_KEY_NAME "${PRODUCT_NAME}"

;!define CONTENTS_SUBDIR "Contents\"
!define CONTENTS_SUBDIR ""

SectionGroup /e "Icarus"

Section -"Common" SEC_COMMON

  SetOverwrite ifnewer

  SetOutPath "$INSTDIR"
  
  ; Programma bestanden
  File "${PRODUCT_DIR}\*.exe"
  File "${PRODUCT_DIR}\*.config"
  File "${PRODUCT_DIR}\*.dll"
  File /nonfatal "${PRODUCT_DIR}\*.bmp"
  File /nonfatal "${PRODUCT_DIR}\*.ico"
  
  ;plugins 
  SetOutPath "$INSTDIR\Plugins"
  File /r "${PRODUCT_DIR}\..\Setup\bin\Plugins\*.*"
  
  ;resources 
  SetOutPath "$INSTDIR\Resources"
  File /r "${PRODUCT_DIR}\..\Setup\bin\Resources\*.*"
  
  ;Gallio.Reports.plugin, verplaatst.
  IfFileExists "$INSTDIR\Plugins\Gallio.Reports.plugin" 0 +2
  Delete "$INSTDIR\Plugins\Gallio.Reports.plugin"
  
  CreateShortCut "$%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\${PRODUCT_NAME}.lnk" "$INSTDIR\Gallio.Icarus.exe"
  CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\Gallio.Icarus.exe"
  
SectionEnd

SectionGroupEnd

!include "xx_convert_database.nsh"

Section un.Icarus
  
  ; Programma bestanden verwijderen
  Delete "$INSTDIR\*.exe"
  Delete "$INSTDIR\*.dll"
  Delete "$INSTDIR\*.config"
  Delete "$INSTDIR\*.bmp"
  Delete "$INSTDIR\*.ico"
  
  Delete "$%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\${PRODUCT_NAME}.lnk"
  Delete "$DESKTOP\${PRODUCT_NAME}.lnk"

  RmDir /r "$INSTDIR\Plugins"

SectionEnd

Section -un.Icarus.Final
; alle subdirectories en hoofddirectory nog eens proberen te verwijderen 
  Delete "$INSTDIR\*.*"
  RmDir /r "$INSTDIR"
SectionEnd

!define XX_CONFIGNAME "Icarus"
!define XX_CONFIGDIR "$INSTDIR\"

Function .onInstSuccess

!ifdef DEBUG	
	UserInfo::GetAccountType
	Pop $1
	MessageBox MB_OK "$1"
!endif
	
    Call IsUserAdmin
	Pop $R0   ; at this point $R0 is "true" or "false"
!ifdef DEBUG
	MessageBox MB_OK "$R0"
!endif
  
  AccessControl::GrantOnFile "$INSTDIR" "(BU)" "FullAccess"
  
    SetOutPath "$INSTDIR\"
    WriteUninstaller "$INSTDIR\uninst-re.exe"
  
FunctionEnd