<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# FrayedKnot

An implementation of a Rope data structure for large string manipulation.

This library is available on NuGet.org as **Sunlighter.FrayedKnot**.

## Purpose and Features

This library presents a class called `Rope`. It's designed for quick manipulation of relatively large volumes of text,
especially when such manipulation consists of splitting and concatenation, which the `Rope` can do in logarithmic
time. It uses `char` as its character type.

Ropes are immutable and based internally on balanced trees. The leaves of the tree contain short strings, rather than
individual characters.

This particular implementation is also indexed on newlines. The implementation considers `"\r"` or `"\n"` or `"\r\n"`
as a newline. Every node in the tree knows how many newlines are under it, making it possible to quickly find a line
by its line number.

Ropes are equatable and comparable (internally using `System.StringComparison.Ordinal`). This is provided by an
implementation of `ITypeTraits<Rope>` which works with the `Sunlighter.TypeTraitsLib` library. The type traits also
provide binary serialization, consistent with the traits for other types.

**Note:** It used to be necessary to use `Builder.Instance.AddTypeTraits` to register the rope&rsquo;s type traits
with the Type Traits library&rsquo;s builder, but since version 1.0.4, Sunlighter.TypeTraitsLib 1.1.2 is now used, and
the `Rope` class has a `ProvidesOwnTypeTraits` attribute, and registration is no longer necessary.

There are utility functions in a static `RopeUtility` class to read a `Rope` from a `System.IO.TextReader` or a file,
or to write a rope to a `System.IO.TextWriter` or a file.

## Serialization Formats

In version 2.0.0 a new serialization format was introduced and was made the default, but the old serialization format
is still available (in case you have binaries in the old format). The static property `Rope.SerializationMode` can be
set to either `RopeSerializationMode.Nodes` (the new format) or `RopeSerializationMode.Blocks` (the old format).

The block format simply divides the rope into blocks of 16k characters (the last block may be shorter). Duplication is
spelled out.

The nodes format saves the nodes as they are stored in memory. The nodes format has the advantage that, if you have
two or more ropes that share nodes (or even if you have a single rope that shares nodes internally, as might be
produced if you produce a Rope by concatenating copies of the same smaller Rope), the sharing is honored in the
serialized file. This makes it efficient to store &ldquo;undo&rdquo; histories, for example.

Note, however, that the shared nodes have to be detected in the same serialization call (e.g., `SerializeToBytes`),
which usually means you have to create the type traits for some complex data structure containing ropes, such as
`ImmutableList<Rope>` or the like. There is no memory *between* serialization calls.

## Rope Annotation Lists

Sometimes it is helpful to keep data associated with positions in the rope. If data is inserted into the middle of the
rope, the associated data must be kept with its text, even though the position of that text has changed.

For that purpose, in version 2.0.0 an immutable `RopeAnnotationList<T>` type was introduced. It keeps items of type
`T` at given positions, and it is possible to insert or delete space, causing items to be moved.

It is possible to keep a `Rope` synchronized with an instance of `RopeAnnotationList<T>` and thereby have data
associated with points in the `Rope`. (This must be done manually.)
