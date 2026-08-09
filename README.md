# FFB0 Body Scale for Windows

A privacy-first Windows desktop application for body-composition scales that implement the FFB0 Bluetooth Low Energy protocol. It provides a clinic-oriented Romanian interface for patient profiles, measurement history, body-composition ranges, search, and native Windows print/PDF reports.

## Features

- Connects directly to FFB0-compatible body-composition scales over Bluetooth Low Energy.
- Stores patient records locally in `%LOCALAPPDATA%\FFB0 Body Scale`.
- Searches previous measurements by patient name or date.
- Suggests existing patients while entering a name.
- Displays BMI, body fat, water, muscle, protein, visceral fat, bone mass, BMR, metabolic age, body score, subcutaneous fat, heart rate, and impedance.
- Shows color-coded reference bands and the measurement position for every supported metric.
- Uses the native Microsoft print dialog with an A4 report preview, physical printers, and Microsoft Print to PDF.
- Does not require Fitdays+ or a cloud account.

## Requirements

- Windows 11 x64.
- Bluetooth Low Energy support.
- A compatible scale exposing the FFB0 service and packet layout described below.
- .NET 8 SDK to build from source.

## Build

```powershell
dotnet restore .\FFB0BodyScale.csproj
dotnet publish .\FFB0BodyScale.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

Alternatively, run `build.ps1`. The self-contained executable is created in `publish` and does not require a separately installed .NET runtime on the destination PC.

## FFB0 protocol compatibility

This project targets the scale family that uses:

- Advertisement name `MY_SCALE`, or service UUID `0000ffb0-0000-1000-8000-00805f9b34fb`.
- FFB1 (`0000ffb1-...`) for profile and control writes.
- FFB2 (`0000ffb2-...`) for notifications.
- FFB3 (`0000ffb3-...`) for indications and completed BIA results.
- Fixed 20-byte frames with the checksum and A2/A3 measurement layout implemented in `ScaleBluetooth`.

The application is therefore branded for the protocol rather than one retail model. A scale is not automatically compatible merely because it works with Fitdays+: firmware variants may expose different UUIDs, commands, checksums, or packet layouts. Diagnostic logs are available to help validate additional devices. The FI2319WB-D is one known implementation of this FFB0 protocol.

## Privacy and medical notice

All profiles and measurements stay on the local PC unless the user explicitly exports or prints them. Body-composition calculations are wellness estimates and are not a medical diagnosis. Validate the calculations and reference ranges for your intended clinical or regulatory context before use.

## Branding

This public edition intentionally contains no clinic logo or clinic-specific identity. Its generic FFB0 branding can be customized in `MainWindow.xaml` and `BuildPrintDocument` in `Program.cs`.

## License

MIT. Third-party notices are listed in `THIRD_PARTY_LICENSES.txt`.
