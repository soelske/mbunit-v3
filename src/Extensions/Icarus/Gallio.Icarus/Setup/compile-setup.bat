IF NOT EXIST "%~d0%~p0\bin" mkdir "%~d0%~p0\bin"
SET OUTPUTFOLDER=%~d0%~p0..\bin
SET NSISOURCE="%~d0%~p0\Icarus.nsi"
SET NSIDEST="%~d0%~p0\bin\Icarus.nsi"

COPY %NSISOURCE% %NSIDEST%

XCOPY /Y "%~d0%~p0*.nsh" "%~d0%~p0bin\"
XCOPY /Y "%~d0%~p0*.ini" "%~d0%~p0bin"

XCOPY /Y /R "%cd%\..\..\..\..\Gallio\Gallio.Host\bin\Gallio.Host.exe" "%OUTPUTFOLDER%"
XCOPY /Y /R "%cd%\..\..\..\..\MbUnit\MbUnit\bin\MbUnit.dll" "%OUTPUTFOLDER%"

CALL "%~d0%~p0resources.bat"
CALL "%~d0%~p0plugins.bat"

%XX_PROGS%\xx_tools\exec\xx_tools.exe getversioninfo "%OUTPUTFOLDER%\Gallio.Icarus.exe" "FileVersion" > %TMP%\version.tmp
REM Versienummer opvragen van executable
SET /p VERSION="" < %TMP%\version.tmp

%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIUSERINSTALL%" "%NSIUSERINSTALL%" OUTPUTFOLDER "%OUTPUTFOLDER%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIUSERINSTALL%" "%NSIUSERINSTALL%" PRODUCTPLATFORM x64
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIUSERINSTALL%" "%NSIUSERINSTALL%" PRODUCTNAME Icarus
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIUSERINSTALL%" "%NSIUSERINSTALL%" VERSION %VERSION%

%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" OUTPUTFOLDER "%OUTPUTFOLDER%"
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" PRODUCTPLATFORM x64
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" PRODUCTNAME Icarus
%XX_PROGS%\xx_tools\exec\xx_tools.exe kwreplace "%NSIDEST%" "%NSIDEST%" VERSION %VERSION%

REM XML PackageContents invullen
SET XML_CONTENTS=%OUTPUTFOLDER%\PackageContents.xml
COPY "%OUTPUTFOLDER%\..\PackageContentsX.xml" "%XML_CONTENTS%"

ECHO Building setup version: %VERSION%
ECHO NSIDEST: %NSIDEST%
ECHO OUTPUTFOLDER: %OUTPUTFOLDER%

IF NOT EXIST "%ProgramFiles(x86)%\NSIS\makensis.exe" goto nsis_notinstalled

CALL "%ProgramFiles(x86)%\NSIS\makensis.exe" "%NSIDEST%"

IF NOT errorlevel 0 goto error_code

del "%OUTPUTFOLDER%\Gallio.Host.exe"
del "%OUTPUTFOLDER%\MbUnit.dll"
rmdir /s /q "%~d0%~p0\bin\Plugins"
rmdir /s /q "%~d0%~p0\bin\Resources"

goto end

:error_code
ECHO "Setup build error"
exit 1

:nsis_notinstalled
ECHO "NSIS not properly installed"
exit 1

:end

