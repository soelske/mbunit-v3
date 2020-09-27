IF NOT EXIST "nuget" mkdir "nuget"

set "nugetDir=%cd%\nuget\"

XCOPY /Y /R "%cd%\..\Nuget\nuget.exe" "nuget"
XCOPY /Y /R "%cd%\..\Nuget\Package.nuspec" "nuget"

IF NOT EXIST "nuget\lib" mkdir "nuget\lib"

XCOPY /Y /R "Gallio.Echo.exe" "nuget\lib"
XCOPY /Y /R "Gallio.dll" "nuget\lib"
XCOPY /Y /R "Mono.Cecil.dll" "nuget\lib"
XCOPY /Y /R "Microsoft.Cci.*.dll" "nuget\lib"
XCOPY /Y /R "%cd%\..\..\..\..\Gallio\Gallio.Host\bin\Gallio.Host.exe" "nuget\lib"
XCOPY /Y /R "%cd%\..\..\..\..\MbUnit\MbUnit\bin\MbUnit.dll" "nuget\lib"

IF NOT EXIST "nuget\lib\Plugins" mkdir "nuget\lib\Plugins"

set "pluginDir=%cd%\nuget\lib\Plugins"

cd %cd%\..\..\..\..\

FOR /R %%f IN ("*.plugin") DO (
  XCOPY /Y /R "%%f" "%pluginDir%"
)

cd %nugetDir%

nuget.exe pack package.nuspec