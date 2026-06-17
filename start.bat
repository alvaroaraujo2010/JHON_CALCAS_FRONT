@echo off
cd /d "%~dp0"
echo ===========================================
echo  Iniciando JHON CALCAS - E-COMMERCE
echo ===========================================
echo.

echo [Backend] Iniciando API en http://localhost:5001 ...
start "Backend" cmd /c "cd /d backend && dotnet run > backend-log.txt 2>&1"

echo [Frontend] Iniciando Angular en http://localhost:4200 ...
start "Frontend" cmd /c "cd /d frontend && ng serve > frontend-log.txt 2>&1"

echo.
echo ===========================================
echo  AMBOS SERVIDORES INICIADOS
echo  Backend:  http://localhost:5001
echo  Frontend: http://localhost:4200
echo  Logs:     backend/backend-log.txt
echo            frontend/frontend-log.txt
echo ===========================================
echo.
echo  Para DETENER ambos, cierra las ventanas
echo  o ejecuta: taskkill /f /im ContaNexo.API.exe ^& taskkill /f /im node.exe
echo.
pause
