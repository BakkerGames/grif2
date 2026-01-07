# GRIF

## Overview

GRIF is a game runner for interactive fiction games. It is designed to be a simple but extensible engine that can handle various styles of interactive fiction. Version 2 has a completely rewritten engine and adds additional features and improvements.

GRIF can be integrated into other applications to provide game support such as scripting, in-memory data storage, and save/restore functionality.

## Features

- Supports GRIF and JSON game formats
- Runs games in a terminal with a text-based interface
- Scripting language for game logic
- In-memory data storage for static and modified game data
- Automatic save and load functionality with a top-level overlay
- Allows for game modifications through separate files
- Supports localization to other languages using modification files
- Runs on Windows, Linux, and macOS

## Improvements over Version 1

- Can return interleaved results including media information
- Can handle system events such as sleep
- Can stack multiple GRIF files for modular game design
- Outchannel support can be customized for different output methods
- Handles 64-bit integers for larger data values
- Scripts can have local variables for internal processing
- @return command exits scripts immediately
- Self-contained GrifLib project (soon to be available as a NuGet package)
- GrifLib can be included in other front-end applications