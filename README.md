# SumScription

A modern desktop calculator with a translucent liquid-glass interface and an intentionally over-the-top, entirely fictional paywall.

> [!IMPORTANT]
> The paywall is a parody. It never requests payment details, completes purchases, or charges money.

## Features, allegedly

- Does maths. Bold opening move for a calculator.
- Looks like expensive glass and behaves like it has investors to impress.
- The `%` key has recently discovered its market value.
- Includes a tiny shop for people who feel unsafe when an app is not selling them something.
- Offers several carefully engineered opportunities to reconsider your financial priorities.
- The **No** button respects your decision by leaving immediately.
- Successful business decisions may be rewarded with a tasteful and completely proportionate amount of confetti.
- Supports keyboard input, because clicking every number would be cruel even by our standards.
- Remembers your calculations just long enough to become emotionally significant.
- Forgets its imaginary purchases when closed. Our accounting department calls this “fiscal reincarnation.”
- Contains more pricing strategy than any calculator has a moral right to possess.

There are a few more surprises. Explaining them here would only help you prepare.

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

The joke stays local: there are no real payments, accounts, or network requests hiding behind the glass.
