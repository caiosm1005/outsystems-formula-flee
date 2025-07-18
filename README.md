# Formula
Custom implementation of Flee (Fast Lightweight Expression Evaluator) v2.0.0 for the
[Formula (v5) OutSystems Forge component](https://www.outsystems.com/forge/component-overview/3227/formula-o11).

## Compiling Grammar File
In order to compile the grammar file, download the
[latest version of Grammatica](https://github.com/cederberg/grammatica/releases) and run the command below:

```
java -jar grammatica-x.x.jar path/to/src/Flee/Parsing/Expression.grammar --csoutput ./output --csnamespace Flee.Parsing
```

This will generate an output folder with 4 files:
- ExpressionAnalyzer.cs
- ExpressionConstants.cs
- ExpressionParser.cs
- ExpressionTokenizer.cs

Replace the existing files in the `src/Flee/Parsing` folder and make changes as needed. Notice that you might need to
remove the line that contains `using PerCederberg.Grammatica.Runtime;` in each file to avoid reference errors.

## License
Formula is licensed under the [BSD-3 license](https://opensource.org/licenses/BSD-3-Clause). You are free to use,
modify, and distribute this software in source and binary forms, with or without modification, under the terms of the
license.
Flee is licensed under the LGPL. This means that as long as you dynamically link (ie: add a reference) to the officially
released assemblies, you can use it in commercial and non-commercial applications.
