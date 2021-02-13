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

You can also build and use project `FiMSharp.Test`, you'll need a `Report` directory on its executable file.

# 🔃 Compiling

You'll need [.NET SDK](https://dotnet.microsoft.com/download) to compile the solution from source.

Most of the work can be done by the included makefile. You can look inside the file if you want to find the arguments needed to build the optional `FiMSharp.Test` project.

Included runtimes are:

- `win32` - Windows x86-64
- `win` - Windows x64
- `linux` - Linux-x64
- `linuxarm` - Linux ARM
- `darwin` - Osx-64

Example: `make win32` builds the .dll and the Win32 executable of `FiMSharp.Test`

# 🏃‍♀️ Running (FiMSharp.Test)

The releases page should include a `bin.zip` which contains:
- A pre-built .dll for both `FiMSharp` and `FiMSharp.Javascript`
- Pre-built executables for different platforms listed in the release.

About the program:

- Running the program directly will give you a menu in which you can run reports in the `Report` directory. Type in `.help` to list all the commands
- Supplying in arguments:
    - `-report [report name]` - Runs the report
    - `-js` - Builds the report into a Javascript file instead.

# 📚 External Resources

[Esolangs Page](https://esolangs.org/wiki/FiM%2B%2B)

[FiM++ Fandom](https://fimpp.fandom.com)

[Language Specification](https://docs.google.com/document/d/1gU-ZROmZu0Xitw_pfC1ktCDvJH5rM85TxxQf5pg_xmg/edit#)

[Original EQD Post](https://www.equestriadaily.com/2012/10/editorial-fim-pony-programming-language.html)

[Online Interpreter using Blazor](https://fimsharp.web.app)

# 📝 Notes

- FiMSharp is just a personal hobby project, seeing as FiM++ has never been updated for quite some time now.

- The syntax here is different from what [fimpp](https://github.com/KarolS/fimpp) does. You can see the difference in readability in the Brainfuck Interpreter example:

    - [FiMSharp](https://github.com/Jaezmien/FiMSharp/blob/master/.Reports/brainfuck.fim)

    - [fimpp](https://github.com/KarolS/fimpp/blob/master/examples/bf.fimpp)

- This is my first semi-compilcated README.md, please yell at me on the issues page if I did something wrong 🙏
