# SumScription

A modern desktop calculator with a translucent liquid-glass interface and an intentionally over-the-top, entirely fictional paywall.

> [!IMPORTANT]
> The paywall is a parody. It never requests payment details, completes purchases, or charges money.

## Features

- Basic arithmetic, percentages, sign changes, and decimal numbers
- Standard operator precedence and a readable calculation history
- Full keyboard and numeric keypad support
- Dark and light themes with a scalable Avalonia interface
- Reduced motion through `SUMSCRIPTION_REDUCED_MOTION=1`
- A randomly chosen paywall trigger on the fourth through seventh successful calculation of each app run
- A monthly/annual offer switch: one result for €4.99 or one month for €74.99; one year for €499 or Annual Premium for €699 with 100 percentage calculations
- A premium percent key with fictional packs for 1, 10, or 100 calculations
- Three deliberately annoying dismissal stages before calculator input is restored
- A refusal button that continuously evades pointer input and a prominent fake-purchase confetti celebration
- A glowing premium shop icon on the unobstructed left edge, inspired by microtransaction-heavy mobile games
- Session-only fictional purchases with no user account or network access

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- macOS, Linux, or Windows

## Download

Ready-to-run builds are attached to each [GitHub release](https://github.com/reathloo/SumScription/releases/latest):

- macOS Apple Silicon (`arm64`)
- macOS Intel (`x64`)
- Windows 64-bit (`x64`)
- Windows ARM (`arm64`)

Extract the matching ZIP archive and launch SumScription. The packages include the .NET runtime, so the .NET SDK is not required. macOS builds are currently unsigned; on first launch, macOS may require **Open Anyway** in **System Settings → Privacy & Security**. Windows SmartScreen may similarly ask for confirmation because the binaries are not code-signed.

Each release also includes `SHA256SUMS.txt` for verifying the downloaded archives.

## Run the app

```bash
git clone https://github.com/reathloo/SumScription.git
cd SumScription
dotnet run --project src/SumScription.App/SumScription.App.csproj
```

The debug configuration may display the Avalonia diagnostics toggle. To run without debug tooling:

```bash
dotnet run --configuration Release --project src/SumScription.App/SumScription.App.csproj
```

## Build and test

```bash
dotnet restore SumScription.slnx
dotnet build SumScription.slnx --configuration Release --no-restore
dotnet test SumScription.slnx --configuration Release --no-build
```

## Project structure

```text
src/SumScription.App/        Avalonia UI and view model
src/SumScription.Core/       Calculator, history, and paywall logic
tests/                       xUnit tests
docs/design.md               Current design direction
```

The lifetime calculation counter is stored as a small JSON file in the platform-specific application data directory, but it does not consume the quota of a new app run. Purchases, unused premium percentage credits, and calculation history are session-only and reset when the app restarts. Every purchase flow is fictional: the app has no payment fields, payment provider, user account, or network access.
