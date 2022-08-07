all: build win linux linuxarm darwin

build:
	dotnet build FiMSharp -c Release -o bin/
	dotnet build FiMSharp.Changeling -c Release -o bin/

win:	
	dotnet publish FiMSharp.CLI --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/win" --runtime win-x86

linux:
	dotnet publish FiMSharp.CLI --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/linux" --runtime linux-x64

linuxarm:
	dotnet publish FiMSharp.CLI --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/linuxarm" --runtime linux-arm

darwin:
	dotnet publish FiMSharp.CLI --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/darwin" --runtime osx-x64

nuget:
	dotnet pack FiMSharp -o bin/ -c Release

test:
	dotnet run --project FiMSharp.Test -c Release