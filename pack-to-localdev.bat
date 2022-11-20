@echo off

dotnet new tool-manifest --force
dotnet tool install inedo.extensionpackager

cd Bitbucket\InedoExtension
dotnet inedoxpack pack . C:\LocalDev\BuildMaster\Extensions\Bitbucket.upack --build=Debug -o
cd ..\..