# AspTree

A quick and dirty program that does file-level analysis of a classic ASP project, to generate digraphs of file dependencies.

## Why?

Classic ASP is a mess of a framework; albeit a legacy framework. Almost all tools available for Classic ASP are windows-only or are extinct. This is intended to be platform agnostic, as many modern developers prefer to work on Unix derived systems.

## How to use

With dotnet core >= 2.0 installed, run:

```sh
$ dotnet run -- [Folder] (File)
```

where:
- `Folder` is the **absolute path** to the folder that contains your Classic ASP project,
- and `File` is the **relative path** from the specified `Folder` to the file you wan't to generate a dependency graph of.

If `file` isn't specified, then it will determined dependencies of **all ASP** files in `Folder`.

This will generate an `out.txt` which contains a simple [DOT language](https://www.graphviz.org/doc/info/lang.html) digraph - compilable with tools like [graphviz](https://www.graphviz.org/) or [webgraphviz](http://www.webgraphviz.com/).

## NB

This tool is, as noted, quick and dirty. We needed something fast and simple to use on my team for a brief period of time, and this worked.

A file dependency doesnt necessarily mean that any classes, variants, or methods in that file are actually being used. Thus, I would like to integrate this tooling with some proper static analysis.
