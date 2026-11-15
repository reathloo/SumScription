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
- Eight visible free calculations before the paywall parody appears on the ninth
- Three deliberately annoying dismissal stages before calculator input is restored
- Local demo state with no user account or network access

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- macOS, Linux, or Windows

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

The paywall counter is stored as a small JSON file in the platform-specific application data directory. Calculation history is kept in memory only and is discarded when the app exits.
