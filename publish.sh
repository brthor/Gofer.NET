#!/bin/bash
set -ev

package_name="$1"

if [ "${TRAVIS_PULL_REQUEST}" = "false" ] && [ "$TRAVIS_BRANCH" = "master" ]; then
    echo "Publishing ${package_name} $(printf %05d $TRAVIS_BUILD_NUMBER)"

    dotnet pack *.csproj -c Release --version-suffix $(printf %05d $TRAVIS_BUILD_NUMBER)
    dotnet nuget push ./bin/Release/${package_name}*.nupkg -s https://www.nuget.org/api/v2/package -k $NUGET_KEY
fi
