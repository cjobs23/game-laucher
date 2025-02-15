@echo off
SET ILMERGE="%USERPROFILE%\.nuget\packages\ilmerge\3.0.41\tools\net452\ILMerge.exe"
SET OUTPUT=rww3_merged.exe

%ILMERGE% /target:winexe /targetplatform:"v4,C:\Windows\Microsoft.NET\Framework\v4.0.30319" /out:%OUTPUT% rww3.exe rww3.dll

echo Merged files into %OUTPUT%
pause