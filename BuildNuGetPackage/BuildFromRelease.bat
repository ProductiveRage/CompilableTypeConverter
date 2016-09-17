@echo off

%~d0
cd "%~p0"

del *.nu*
del *.dll

copy ..\CompilableTypeConverterQueryableExtensions\bin\release\*.dll > nul
copy ..\CompilableTypeConverterQueryableExtensions\bin\release\*.xml > nul

copy ..\ProductiveRage.CompilableTypeConverter.nuspec > nul
..\packages\NuGet.CommandLine.3.4.3\tools\nuget pack -NoPackageAnalysis ProductiveRage.CompilableTypeConverter.nuspec