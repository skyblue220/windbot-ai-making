@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
 echo .NET Framework C# compiler not found. Use Visual Studio MSBuild with RyzealProbe.csproj.
 exit /b 1
)
"%CSC%" /nologo /target:library /out:RyzealResearchProbe.dll /reference:..\..\ExecutorBase.dll /reference:System.Core.dll RyzealProbe.cs
if errorlevel 1 exit /b 1
echo Build completed: RyzealResearchProbe.dll
