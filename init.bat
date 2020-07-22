@echo OFF

echo Checking dotnet version...
dotnet --version
if ERRORLEVEL 1 (
    echo dotnet was not found in PATH. Install .NET Core SDK and try again.
    GOTO Quit
)

echo Downloading tools...
dotnet tool restore

echo Restoring packages...
dotnet paket install

echo Done. Run "dotnet fake build" to build and "dotnet fake build -t run" to run the code.

:Quit
