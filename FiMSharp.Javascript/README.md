# FiMSharp.Javascript

Convert FiM++ code into valid Javascript!

Based on [this proposal by Kyli Rogue](https://fimpp.fandom.com/wiki/FiM%2B%2B_Wiki:Proposals/Compiler#.JS_file).

# Usage

```csharp
FiMReport report = new FiMReport( lines );

string[] javascript_lines = FiMJavascript.Parse( lines );
```