<!-- -*- coding: utf-8; fill-column: 118 -*- -->

# FrayedKnot

An implementation of a Rope data structure for large string manipulation

## Purpose and Features

This library presents a single class called `Rope`. It's designed for quick manipulation of relatively large volumes
of text, especially when such manipulation consists of splitting and concatenation, which it can do in logarithmic
time. It uses `char` as its character type.

Ropes are immutable and based internally on balanced trees. The leaves of the tree contain short strings, and not
individual characters.

This particular implementation is also indexed on newlines. It considers `"\r"` or `"\n"` or `"\r\n"` as a
newline. Every node in the tree knows how many newline characters are under it, making it possible to quickly find a
line by its line number.
