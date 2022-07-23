#!/bin/sh
dotnet publish chipeur.csproj -c Release -r linux-x64 -p:PublishSingleFile=true -p:DebugType=None --self-contained false
dotnet publish chipeur.csproj -c Release -r win-x64 -p:PublishSingleFile=true -p:DebugType=None --self-contained false
find . -type f -path '*publish*/*' -name "*.dll.config" -delete