# 🐎 FiMSharp

![GitHub release (latest by date)](https://img.shields.io/github/v/release/Jaezmien/FiMSharp?style=flat-square) ![Nuget](https://img.shields.io/nuget/v/FiMSharp?style=flat-square)

FiMSharp is a [FiM++](https://esolangs.org/wiki/FiM%2B%2B) interpreter library written in C#.

# 🖥 Usage

To use the library, simply include the library to your project:

```csharp
using FiMSharp;
```

And create a `FiMReport` with the lines of your report!

```csharp
FiMReport report = new FiMReport( lines );

// Run the main paragraph!
if( report.MainParagraph != null ) report.MainParagraph.Execute();
```

You can also build and use `FiMSharp.Test`.

# 🔃 Compiling

You'll need [.NET SDK](https://dotnet.microsoft.com/download) to compile the solution from source.

Most of the work can be done by the included makefile.

Included runtimes are:

-   `win32` - Windows x86-64
-   `win` - Windows x64
-   `linux` - Linux-x64
-   `linuxarm` - Linux ARM
-   `darwin` - OSX-64

You can use this command to build the Win32 executable of `FiMSharp.Test`

```
make win32
```

# 🏃‍♀️ Running (FiMSharp.Test)

The releases page should include a `bin.zip` which contains:

-   A pre-built .dll for both `FiMSharp` and `FiMSharp.Javascript`
-   Pre-built executables for different platforms listed in the release.

# 🐳 Running from Docker

FiMSharp is also available on Docker as [Docker container](https://hub.docker.com/r/jaezmien/fimsharp). While it makes it easy to setup FiMSharp at many machine, it comes with a large filesize.

```
docker pull jaezmien/fimsharp:0.3.3
```

Example usage:

```
docker run jaezmien/fimsharp:0.3.3 Reports/hello.fim
```

For reports which require user inputs, you'll need to add the `-i` flag.

```
docker run -i jaezmien/fimsharp:0.3.3 Reports/input.fim
```

# 📚 External Resources

[Esolangs Page](https://esolangs.org/wiki/FiM%2B%2B)

[FiM++ Fandom](https://fimpp.fandom.com)

[Language Specification](https://docs.google.com/document/d/1gU-ZROmZu0Xitw_pfC1ktCDvJH5rM85TxxQf5pg_xmg/edit#)

[Original EQD Post](https://www.equestriadaily.com/2012/10/editorial-fim-pony-programming-language.html)

[Online Interpreter using Blazor](https://fimsharp.web.app)

[Docker Container](https://hub.docker.com/r/jaezmien/fimsharp)

# 📝 Notes

-   FiMSharp is just a personal hobby project, seeing as FiM++ has never been updated for quite some time now.

-   The syntax used here follows the proposed `Sparkle 1.0` syntax, unlike what [fimpp](https://github.com/KarolS/fimpp) uses. You can see the difference in readability in the Brainfuck Interpreter example:

    -   [FiMSharp](https://github.com/Jaezmien/FiMSharp/blob/master/Reports/brainfuck.fim)

    -   [fimpp](https://github.com/KarolS/fimpp/blob/master/examples/bf.fimpp)

-   This is my first semi-compilcated README.md, please yell at me on the issues page if I did something wrong 🙏
