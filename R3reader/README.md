# R3 Reader

Industrial RFID laundry table reader app for the R3 desktop reader. Java + Material Design, built to match the R3 Android UI design system.

## Features (UI)

- Login / sign-up screen
- Bluetooth connection management
- Workflow selection (sorting, shipment, quarantine, return)
- Live RFID scan session with pulse animation and EPC feed
- Item details, verification, damage marking
- Assignment, delivery preparation, sync queue, history, settings
- Bottom navigation + dark mode

## Requirements

- Android Studio Hedgehog or newer
- JDK 17
- minSdk 27, targetSdk 34

## Build

```bash
cd R3reader
./gradlew assembleDebug
```

## RFID SDK

BLE/UHF integration uses the Chainway/RSCJA `RFIDWithUHFBLE` SDK (see `uhf-ble-demo`). Copy vendor `*.aar` / `*.jar` into `app/libs` and wire `ReaderManager` when ready.

## Design tokens

| Token | Value |
|-------|-------|
| Primary | `#1976D2` |
| Success | `#2E7D32` |
| Warning | `#F9A825` |
| Error | `#D32F2F` |
| Button height | 52dp |
| Card radius | 18dp |
