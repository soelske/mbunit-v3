IF NOT EXIST "bin\Plugins" mkdir "bin\Plugins"

set "pluginDir=%cd%\bin\Plugins\"

set "autoCadDir=%cd%\bin\Plugins\AutoCad"

IF NOT EXIST "bin\Plugins\lib\Plugins\AutoCad" mkdir "bin\Plugins\AutoCad"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD\bin\Gallio.AutoCAD.dll" "bin\Plugins\AutoCad"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD.Plugin\bin\v23.0\Gallio.AutoCAD.Plugin230.dll" "bin\Plugins\AutoCad"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD.UI\bin\Gallio.AutoCAD.UI.dll" "bin\Plugins\AutoCad"

IF NOT EXIST "bin\Plugins\lib\Plugins\AutoCad\Resources" mkdir "bin\Plugins\AutoCad\Resources"
XCOPY /Y /R "%cd%\..\..\..\AutoCad\Gallio.AutoCAD\Resources\Gallio.AutoCAD.ico" "bin\Plugins\AutoCad\Resources"

cd %cd%\..\..\..\..\

FOR /R %%f IN ("*.plugin") DO (
  XCOPY /Y /R "%%f" "%pluginDir%"
)

cd %pluginDir%

FOR /R %%f IN ("*AutoCad*.plugin") DO (
  MOVE "%%f" "%autoCadDir%"
)

exit /b