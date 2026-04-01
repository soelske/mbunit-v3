@echo off
SETLOCAL

REM === Configuratie ===
REM Optioneel eerste argument: configuratienaam (default: Release)
IF "%~1"=="" (SET CONFIGURATION=Release) ELSE (SET CONFIGURATION=%~1)
SET FRAMEWORK=net8.0-windows

REM Build output van Gallio.Icarus.Core
SET OUTPUTFOLDER=%~d0%~p0..\bin\%CONFIGURATION%\%FRAMEWORK%

REM Gallio.Reports.Core output
SET REPORTS_DIR=%~d0%~p0..\..\..\..\Gallio\Gallio.Reports.Core\bin\%CONFIGURATION%\%FRAMEWORK%

REM MbUnit plugin output
SET MBUNIT_CORE_DIR=%~d0%~p0..\..\..\..\MbUnit\MbUnit.Core\bin\%CONFIGURATION%\%FRAMEWORK%

REM AutoCAD plugin outputs
SET ACAD_CORE_DIR=%~d0%~p0..\..\..\..\Extensions\AutoCAD\Gallio.AutoCAD.Core\bin\%CONFIGURATION%\%FRAMEWORK%
SET ACAD_UI_CORE_DIR=%~d0%~p0..\..\..\..\Extensions\AutoCAD\Gallio.AutoCAD.UI.Core\bin\%CONFIGURATION%\%FRAMEWORK%
SET ACAD_PLUGIN_DIR=%~d0%~p0..\..\..\..\Extensions\AutoCAD\Gallio.AutoCAD.Plugin\bin\v25.0\%FRAMEWORK%

SET NSISOURCE=%~d0%~p0Icarus.Core.nsi
SET NSIDEST=%~d0%~p0bin\Icarus.Core.nsi

IF NOT EXIST "%~d0%~p0bin" mkdir "%~d0%~p0bin"

COPY %NSISOURCE% %NSIDEST%

REM Versienummer opvragen van de executable
%XX_PROGS%\xx_tools\exec\xx_tools.exe getversioninfo "%OUTPUTFOLDER%\Gallio.Icarus.exe" "FileVersion" > %TMP%\version.tmp
SET /p VERSION="" < %TMP%\version.tmp

REM Placeholders vervangen in de .nsi
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" OUTPUTFOLDER "%OUTPUTFOLDER%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" VERSION %VERSION%
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" ACAD_CORE_DIR "%ACAD_CORE_DIR%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" ACAD_UI_CORE_DIR "%ACAD_UI_CORE_DIR%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" ACAD_PLUGIN_DIR "%ACAD_PLUGIN_DIR%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" MBUNIT_CORE_DIR "%MBUNIT_CORE_DIR%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" REPORTS_DIR "%REPORTS_DIR%"

ECHO Building setup version: %VERSION%
ECHO NSIDEST:     %NSIDEST%
ECHO OUTPUTFOLDER: %OUTPUTFOLDER%

IF NOT EXIST "%ProgramFiles(x86)%\NSIS\makensis.exe" goto nsis_notinstalled

CALL "%ProgramFiles(x86)%\NSIS\makensis.exe" "%NSIDEST%"

IF ERRORLEVEL 1 goto error_code

goto end

:error_code
ECHO Setup build error
exit /b 1

:nsis_notinstalled
ECHO NSIS niet gevonden op %ProgramFiles(x86)%\NSIS\makensis.exe
exit /b 1

:end
ENDLOCAL
