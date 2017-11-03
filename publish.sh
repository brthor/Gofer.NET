#!/bin/bash
set -ev

package_name="Gofer.NET"

if [ "${TRAVIS_PULL_REQUEST}" = "false" ] && [ "$TRAVIS_BRANCH" = "master" ]; then
    echo "Publishing ${package_name} $(printf %05d $TRAVIS_BUILD_NUMBER)"

    dotnet pack -c Release --version-suffix $(printf %05d $TRAVIS_BUILD_NUMBER)
    dotnet nuget push ./bin/Release/${package_name}*.nupkg -k $NUGET_KEY
fi
