# The Game of Go

A Windows Store (UWP) implementation of the ancient Chinese game of Go.

The Game of Go is available on the Windows Store as a Windows 10 compatible Universal Windows Platform app.  The original versions ran the Fuego AI on Windows Azure, but as of version 1.3 (April 2016), the AI is compiled in as a Windows Runtime Component.  In mid 2017, The Game of Go 2 was released with new analysis tools to help with understanding area and dead stones, and scoring.  Multiplayer was also added!

It is implemented with C# and C++ (AI code borrowed from the Fuego project) using Visual Studio 2017.  .Net Standard 2.0 must be installed as a target.  The code should compile with no other dependencies.
