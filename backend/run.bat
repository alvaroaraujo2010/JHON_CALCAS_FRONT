@echo off
cd /d "%~dp0"
echo Iniciando backend (salida silenciada)...
dotnet run > backend-log.txt 2>&1
