setlocal
@echo off

set CSC_EXE=F:\Windows\Microsoft.NET\Framework64\v3.5\csc
set ACAD_HOME=%ProgramFiles%\AutoCAD 2010
set ACMGD_DLL=%ACAD_HOME%\acmgd.dll
set ACDBMGD_DLL=%ACAD_HOME%\acdbmgd.dll

"%CSC_EXE%" /t:library /r:"%ACMGD_DLL%" /r:"%ACDBMGD_DLL%" lmc.cs