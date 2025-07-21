duck := "src/Serilog.Sinks.DuckDB"
duckproject := duck + "/Serilog.Sinks.DuckDB.csproj"


default:
    just --list

clean:
    #!/bin/sh
    if [ -d "./artifacts" ]; then
        echo "build: Cleaning ./artifacts"
        rm -rf ./artifacts
    fi 
    dotnet clean {{duckproject}}

restore:
    #!/bin/sh
    dotnet restore {{duckproject}} --no-cache

pack: clean restore
    #!/bin/sh
    branch=$(git symbolic-ref --short -q HEAD)
    commitHash=$(git rev-parse --short HEAD)
    tag=$(git describe --tags --abbrev=0)
    buildSuffix="${branch}-${commitHash}-${tag}"
    dotnet build {{duckproject}} -c Release --version-suffix=$buildSuffix -p:EnableSourceLink=true
    if [ -n "$suffix" ]; then
        dotnet pack {{duckproject}} -c Release -o ./artifacts --version-suffix=$suffix --no-build
    else
        dotnet pack {{duckproject}} -c Release -o ./artifacts --no-build
    fi