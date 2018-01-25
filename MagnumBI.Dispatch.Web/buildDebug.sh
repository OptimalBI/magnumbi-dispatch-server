#!/bin/bash
rm -r bin
rm -r obj
dotnet clean
dotnet restore
dotnet publish -c Debug -r ubuntu.16.04-x64
#dotnet publish -c Debug -r win10-x64
