# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Microsoft Teams AI Agent (bot) application that integrates OpenAI function calling to launch the classic Doom game within a Teams dialog. Built with .NET 9.0 and the Microsoft Teams AI SDK v2.0.

## Development Commands

### Build and Run
```bash
# Build the project
dotnet build My.Doom.Agent/My.Doom.Agent.csproj

# Run the application (listens on http://localhost:3978)
dotnet run --project My.Doom.Agent/My.Doom.Agent.csproj

# Access dev tools at: http://localhost:3978/devtools
```

### Debug in Visual Studio
1. Create a public dev tunnel: Debug > Dev Tunnels > Create A Tunnel (set authentication type to Public)
2. Right-click 'TeamsApp' project > M365 Agents Toolkit > Select Microsoft 365 Account
3. Set Launch profile to `Microsoft Teams (Browser)`
4. Press F5 to start debugging

**Important:** Update the `devTunnelUrl` constant in `MainController.cs:21` to match your dev tunnel URL before debugging.

## Architecture

### Core Components

**Program.cs** - Application entry point that:
- Registers `MainController` as transient service
- Configures Teams AI SDK with OpenAI integration via `AddOpenAI<DoomPrompt>()`
- Sets up Teams middleware and tab routing for embedded web content
- Maps the "dialog-form" tab to embedded Web resources

**MainController.cs** - Main Teams controller (`[TeamsController("main")]`) handling:
- Message activities with AI streaming responses
- Task module fetch/submit for dialog-based UI
- Controls game launch flow via `playDoom` flag and streaming cancellation

**DoomPrompt** - OpenAI function calling prompt class that:
- Defines the AI assistant's behavior and instructions
- Exposes `StartDoom()` function that cancels AI streaming and triggers game dialog
- Uses `[Prompt]` and `[Function]` annotations for Teams AI SDK

### Key Patterns

1. **AI Streaming with Cancellation:** Messages stream OpenAI responses using linked cancellation tokens. When `StartDoom()` is called, it cancels the stream and sets `playDoom = true`, causing an Adaptive Card with game launcher to be sent.

2. **Task Modules for Dialog UI:** The `TaskFetch` handler returns a dialog pointing to an embedded webpage (`Web/dialog-form/index.html`) that hosts the Doom game. This webpage must:
   - Be publicly accessible via dev tunnel
   - Initialize Teams JS SDK (`@microsoft/teams-js`)
   - Be registered in the Teams manifest validDomains

3. **Embedded Web Resources:** Files in `Web/**` are embedded as resources (see `.csproj:16-18`) and served via `app.AddTab()` routing.

## Configuration

### appsettings.json
- Logging configuration for ASP.NET Core and Teams SDK
- Set Teams logging level via `Microsoft.Teams.Level`

### Dev Tunnel URL
Must be updated in two locations:
- `MainController.cs:21` - `devTunnelUrl` constant
- TeamsApp manifest's `validDomains` (handled by M365 Agents Toolkit)

## Teams Manifest

Located at `TeamsApp/appPackage/manifest.json`. Defines:
- Bot capabilities and scopes (personal, team, groupChat)
- Valid domains for embedded content
- Static tabs and web application info

Variables like `${{TEAMS_APP_ID}}`, `${{BOT_ID}}`, `${{BOT_DOMAIN}}` are replaced during deployment by M365 Agents Toolkit.
