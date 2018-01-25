#!/bin/bash
rm -r bin
rm -r obj
dotnet clean
dotnet restore
dotnet publish -c Release -r ubuntu.16.04-x64
dotnet publish -c Release -r win10-x64
