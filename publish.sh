#!/bin/sh
dotnet publish chipeur.csproj -c Release -r linux-x64 -p:Os=Unix -p:PublishSingleFile=true -p:DebugType=None --self-contained false
