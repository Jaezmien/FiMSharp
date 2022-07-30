<div align="center">
  
# 🐎 FiMSharp

> FiMSharp is a [FiM++](https://esolangs.org/wiki/FiM%2B%2B) interpreter library written in C#.

<br>

<div>
	<a href="https://github.com/Jaezmien/FiMSharp">
		<img
			alt="GitHub release (latest by date)"
			src="https://img.shields.io/github/v/release/Jaezmien/FiMSharp?label=Latest%20Release&style=for-the-badge"
		>
	</a>
	<a href="https://www.nuget.org/packages/FiMSharp/">
		<img
			alt="Nuget"
			src="https://img.shields.io/nuget/v/FiMSharp?style=for-the-badge"
		>
	</a>
</div>

</div>

# 🖥 Usage

## Library

```csharp
using FiMSharp;

FiMReport report = new FiMReport(@"Dear Princess Celestia: Hello World!
Today I learned how to say hello world.
	I said ""Hello World!"".
That's all about how to say hello world.
Your faithful student, Twilight Sparkle."); // You can also use FiMReport.FromFile(string path); to use a path instead.

if( report.MainParagraph != null ) {
	report.MainParagraph.Execute(); // Outputs "Hello World!" into the console.
}
```

## CLI

```bash
$ ./fim Reports/hello.fim
Hello World!
```

See the [reports folder](./FiMSharp.Test/Reports/) for sample reports you can run on FiMSharp.

# 🚧 Supported Platforms

-   [FiMSharp](./FiMSharp) - **.NET Standard 2.0** at minimum.

-   [FiMSharp.CLI](./FiMSharp.CLI) - **.NET Core 3.1** at minimum.

# 📚 External Resources

-   [Original Equestria Daily Post](https://www.equestriadaily.com/2012/10/editorial-fim-pony-programming-language.html)

-   [Esolangs Page](https://esolangs.org/wiki/FiM%2B%2B)

-   [Language Specification](https://docs.google.com/document/d/1gU-ZROmZu0Xitw_pfC1ktCDvJH5rM85TxxQf5pg_xmg/edit#)

-   [FiM++ Fandom](https://fimpp.fandom.com)

-   [Online Interpretator using Blazor](https://fimsharp.netlify.app)

# 📝 Notes

-   FiMSharp is just a personal hobby project, seeing as FiM++ has never been updated for quite some time now.

-   The syntax used here follows a modified `Sparkle 1.0` syntax, unlike what [fimpp](https://github.com/KarolS/fimpp) uses. Please refer to the sample reports to see the differences.

-   ⚠🐛 There is still a bug regarding combining statements into one line. Thus, it's still recommended to have statements be on their own line.
