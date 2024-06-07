@echo off
setlocal enabledelayedexpansion

REM Check if API key is provided as a parameter
if "%~1"=="" (
    REM Ask for API key
    set /p "APIKEY=Please enter your GitHub API key: "
) else (
    set "APIKEY=%~1"
)

REM Define the source directory for the packages
set "source_dir=E:\Nugets"

REM Define the file mask for the packages
set "file_mask=*DrawnUi.Maui*.1.2.2.8.nupkg"

REM Loop through each package file in the source directory
for %%f in ("%source_dir%\%file_mask%") do (
    echo Uploading %%f to GitHub with API key.
    dotnet nuget push --source github --api-key %APIKEY% "%%f"
    if errorlevel 1 (
        echo An error occurred while uploading %%f.
    ) else (
        echo %%f uploaded successfully.
    )
)

pause
endlocal
