<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# FrayedKnot

An implementation of a Rope data structure for large string manipulation

## Purpose and Features

This library presents a class called `Rope`. It's designed for quick manipulation of relatively large volumes of text,
especially when such manipulation consists of splitting and concatenation, which it can do in logarithmic time. It
uses `char` as its character type.

Ropes are immutable and based internally on balanced trees. The leaves of the tree contain short strings, rather than
individual characters.

This particular implementation is also indexed on newlines. The implementation considers `"\r"` or `"\n"` or `"\r\n"`
as a newline. Every node in the tree knows how many newline characters are under it, making it possible to quickly
find a line by its line number.

Ropes are equatable and comparable (internally using `System.StringComparison.Ordinal`). This is provided by an
implementation of `ITypeTraits<Rope>` which works with the `Sunlighter.TypeTraitsLib` library. The type traits also
provide binary serialization, consistent with the traits for other types.

There are utility functions in a static `RopeUtility` class to read a `Rope` from a `System.IO.TextReader` or a file,
or to write a rope to a `System.IO.TextWriter` or a file.
