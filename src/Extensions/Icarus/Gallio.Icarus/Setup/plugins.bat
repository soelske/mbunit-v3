IF NOT EXIST "bin\Plugins" mkdir "bin\Plugins"

set "pluginDir=%cd%\bin\Plugins\"

set "autoCadDir=%cd%\bin\Plugins\AutoCad"

set "reportsDir=%cd%\bin\Plugins\Reports"

IF NOT EXIST "bin\Plugins\AutoCad" mkdir "bin\Plugins\AutoCad"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD\bin\Gallio.AutoCAD.dll" "bin\Plugins\AutoCad"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD.Plugin\bin\v23.0\Gallio.AutoCAD.Plugin230.dll" "bin\Plugins\AutoCad"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD.UI\bin\Gallio.AutoCAD.UI.dll" "bin\Plugins\AutoCad"

IF NOT EXIST "bin\Plugins\lib\Plugins\AutoCad\Resources" mkdir "bin\Plugins\AutoCad\Resources"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD\Resources\Gallio.AutoCAD.ico" "bin\Plugins\AutoCad\Resources"

IF NOT EXIST "bin\Plugins\Reports" mkdir "bin\Plugins\Reports"
XCOPY /Y /R "%cd%\..\..\..\..\Gallio\Gallio.Reports\bin\Gallio.Reports.dll" "bin\Plugins\Reports"
XCOPY /Y /R "%cd%\..\..\..\..\Gallio\Gallio.Reports\bin\NVelocity.dll" "bin\Plugins\Reports"

IF NOT EXIST "bin\Plugins\Reports\Resources" mkdir "bin\Plugins\Reports\Resources"
XCOPY /s "%cd%\..\..\..\..\Gallio\Gallio.Reports\Resources" "bin\Plugins\Reports\Resources"

cd %cd%\..\..\..\..\

FOR /R %%f IN ("*.plugin") DO (
  XCOPY /Y /R "%%f" "%pluginDir%"
)

rem cd %cd%\Gallio\Gallio.Reports\Resources\

rem FOR /R %%f IN ("*.*") DO (
rem   XCOPY /Y /R "%%f" "%reportsDir%\Resources"
rem )

cd %pluginDir%

FOR /R %%f IN ("*AutoCad*.plugin") DO (
  MOVE "%%f" "%autoCadDir%"
)

FOR /R %%f IN ("*Reports.plugin") DO (
  MOVE "%%f" "%reportsDir%"
)

cd %~d0%~p0

exit /b