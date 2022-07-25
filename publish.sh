#!/bin/sh
if [ ${#@} -eq 0 ]; then
    echo "Missing platform argument: linux|windows"
else
    if [ $1 = "linux" ]; then
        dotnet publish chipeur.csproj -c Release -r linux-x64 -p:Os=Linux -p:PublishSingleFile=true -p:DebugType=None --self-contained false
    elif [ $1 = "windows" ]; then
        dotnet publish chipeur.csproj -c Release -r win-x64 -p:Os=Windows -p:PublishSingleFile=true -p:DebugType=None --self-contained false
    fi
    find . -type f -path '*publish*/*' -name "*.dll.config" -delete
fi