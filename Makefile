all: build win win32 linux linuxarm darwin

build:
	dotnet build FiMSharp -c Release -o bin/
	dotnet build FiMSharp.Javascript -c Release -o bin/

win:	
	dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/win" --runtime win-x64

win32:
	dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/win32" --runtime win-x86

linux:
	dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/linux" --runtime linux-x64

linuxarm:
	dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/linuxarm" --runtime linux-arm

darwin:
	dotnet publish FiMSharp.Test --self-contained=true -p:PublishSingleFile=True -c Release -o "bin/darwin" --runtime osx-x64

nuget:
	dotnet pack -o Nuget/ -c Release