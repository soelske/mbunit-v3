IF NOT EXIST "bin\Resources" mkdir "bin\Resources"

set "resourcesDir=%cd%\bin\Resources\"

cd %cd%\..\..\..\..\Gallio\Gallio\Resources\

FOR /R %%f IN ("*.*") DO (
  XCOPY /Y /R "%%f" "%resourcesDir%"
)

cd %~d0%~p0

exit /b