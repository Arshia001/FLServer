#!/bin/bash

echo Checking dotnet version...

if ! dotnet --version  2> /dev/null
then

  echo dotnet was not found in PATH. Install .NET Core SDK and try again.
  exit 1

fi

echo Checking nodejs version...

if ! node --version  2> /dev/null
then

  echo node was not found in PATH. Install Node.js and try again.
  exit 1

fi

echo Checking yarn version...

if ! yarn --version  2> /dev/null
then

  echo yarn was not found in PATH. Install it and try again.
  exit 1

fi

echo Downloading tools...
dotnet tool restore

echo Restoring packages...
dotnet paket install

echo Done. Run \"dotnet fake build\" to build and \"dotnet fake build -t run\" to run the code.
