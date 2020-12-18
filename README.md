# FiMSharp

FiMSharp is a C# interpreter library for the esoteric language, [FiM++](https://esolangs.org/wiki/FiM%2B%2B)

# Usage

To use the library, simply include the library to your project:

```csharp
using FiMSharp;
```

And create a `FiMReport` with the lines of your report!

```csharp
FiMReport report = new FiMReport( lines );

// Run the main paragraph!
if (!string.IsNullOrEmpty(report.MainParagraph))
    report.Paragraphs[report.MainParagraph].Execute(report);
```

You can also build and use project `FiMSharp.Test`, you'll need a `Report` directory on its executable file.

# TODO

- Logo

- REPL

# External Resources

[Esolangs Page](https://esolangs.org/wiki/FiM%2B%2B)

[FiM++ Fandom](https://fimpp.fandom.com)

[Language Specification](https://docs.google.com/document/d/1gU-ZROmZu0Xitw_pfC1ktCDvJH5rM85TxxQf5pg_xmg/edit#)

# Notes

- FiMSharp is just a personal hobby project, seeing as FiM++ has never been updated for quite some time now.

- The syntax here is different from what [fimpp](https://github.com/KarolS/fimpp) does. You can see the difference in readability in the Brainfuck Interpreter example:

    - [FiMSharp](https://github.com/Jaezmien/FiMSharp/blob/master/.Reports/brainfuck.fim)

    - [fimpp](https://github.com/KarolS/fimpp/blob/master/examples/bf.fimpp)
