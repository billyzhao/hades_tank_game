# Game 1

A minimal Godot 4.7 .NET / C# 2D prototype.

## Build

```powershell
dotnet build "D:\my program\codex\godot\game1\Game1.csproj"
```

## Run

```powershell
godot-gui --path "D:\my program\codex\godot\game1"
```

The first editor launch may spend a few seconds scanning C# scripts and building the assembly.

If Vulkan pipeline errors appear, use the OpenGL compatibility launchers:

```powershell
.\run-editor-opengl.cmd
.\run-game-opengl.cmd
```

## Controls

- Move: WASD or arrow keys
- Goal: touch the yellow target to increase the score
