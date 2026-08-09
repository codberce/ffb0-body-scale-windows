# FI2319 Clinic Scale for Windows

A privacy-first Windows desktop application for FI2319WB-D Bluetooth body-composition scales. It provides a clinic-oriented Romanian interface for patient profiles, measurement history, body-composition ranges, search, and native Windows print/PDF reports.

## Features

- Connects directly to an FI2319WB-D scale over Bluetooth Low Energy.
- Stores patient records locally in `%LOCALAPPDATA%\FI2319 Clinic`.
- Searches previous measurements by patient name or date.
- Suggests existing patients while entering a name.
- Displays BMI, body fat, water, muscle, protein, visceral fat, bone mass, BMR, metabolic age, body score, subcutaneous fat, heart rate, and impedance.
- Shows color-coded reference bands and the measurement position for every supported metric.
- Uses the native Microsoft print dialog with an A4 report preview, physical printers, and Microsoft Print to PDF.
- Does not require Fitdays+ or a cloud account.

## Requirements

- Windows 11 x64.
- Bluetooth Low Energy support.
- FI2319WB-D-compatible scale exposing the FFB0 service.
- .NET 8 SDK to build from source.

## Build

```powershell
dotnet restore .\CantarClinica.csproj
dotnet publish .\CantarClinica.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Alternatively, run `build.ps1`. The self-contained executable is created in `publish` and does not require a separately installed .NET runtime on the destination PC.

## Scale protocol

The application scans for `MY_SCALE` or BLE service `0000ffb0-0000-1000-8000-00805f9b34fb`, subscribes to FFB2/FFB3 notifications, and writes the measurement profile through FFB1. Scale firmware variants may use different packet layouts; diagnostic logs are available from the application.

## Privacy and medical notice

All profiles and measurements stay on the local PC unless the user explicitly exports or prints them. Body-composition calculations are wellness estimates and are not a medical diagnosis. Validate the calculations and reference ranges for your intended clinical or regulatory context before use.

## Branding

This public edition intentionally contains no clinic logo or clinic-specific identity. The generic header can be customized in `MainWindow.xaml` and `BuildPrintDocument` in `Program.cs`.

## License

MIT. Third-party notices are listed in `THIRD_PARTY_LICENSES.txt`.
