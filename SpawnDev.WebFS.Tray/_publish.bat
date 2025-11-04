@echo off
SETLOCAL EnableDelayedExpansion
REM deleting publish folder
rmdir /Q /S "%~dp0bin\Publish\win-any"
dotnet publish --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly --configuration Release --output "%~dp0bin\Publish\win-any"

set "EXE_PATH=%~dp0bin\Publish\win-any\SpawnDev.WebFS.Tray.exe"

REM Use PowerShell to get the File Version and store it in a temporary variable
FOR /F "tokens=*" %%V IN ('powershell -Command "(Get-Command '!EXE_PATH!').FileVersionInfo.FileVersion"') DO (
    SET "FULL_VERSION=%%V"
)

REM Remove everything after and including the plus symbol
SET "CLEAN_VERSION=!FULL_VERSION:+=&REM;"!
FOR /F "delims=" %%C IN ("!CLEAN_VERSION!") DO (
    SET "FINAL_VERSION=%%C"
)

REM Set the final environment variable
SET "APP_VERSION=!FINAL_VERSION!"

REM Display the result
ECHO Version !APP_VERSION! built. Creating setup.

:: Path to Inno Setup Compiler (adjust if necessary)
set ISCC_PATH="C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

:: Create output directory if it doesn't exist
:: if not exist %OUTPUT_DIR% mkdir %OUTPUT_DIR%
%ISCC_PATH% "/DMyAppVersion=%APP_VERSION%" /Q _setupConfig.iss

if %errorlevel% equ 0 (
    echo Setup compilation successful!
) else (
    echo Setup compilation failed.
)

ENDLOCAL
pause


