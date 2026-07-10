# Sudoku Unity Game

Mobile portrait Sudoku game project built with Unity.

## Project

- Engine: Unity 6000.3.2f1
- Platform target: Android
- Orientation: Portrait
- Package name: `com.arcana.sudoku`
- Main scene: `Assets/Scenes/SudokuDemo.unity`

## Features

- Main screen
- Difficulty selection: Easy, Normal, Hard
- 10 rounds per difficulty
- Round indicator such as `Easy 1/10`
- Difficulty-based hints and mistakes
  - Easy: 3 hints, 3 mistakes
  - Normal: 2 hints, 2 mistakes
  - Hard: 1 hint, 1 mistake
- Randomized Sudoku board per round
- Notes mode with orange temporary numbers
- Hint button that fills one empty cell
- Game Over popup with Main Screen and Retry buttons
- Round Clear popup with Main Screen and Next Round buttons
- Light and dark mode settings
- Language menu
- Game rules screen
- Custom app icon
- Android AAB build helper
- Android launch orientation locked to portrait

## How To Open

1. Open this folder with Unity Hub.
2. Use Unity 6000.3.2f1 or a compatible Unity 6 version.
3. Open `Assets/Scenes/SudokuDemo.unity`.
4. Set the Game view to a portrait mobile ratio such as `1080 x 1920`.
5. Press Play.

## Android Build

The Android build helper is located at:

`Assets/Editor/SudokuAndroidBuild.cs`

The generated Android App Bundle uses:

- Application ID: `com.arcana.sudoku`
- Version name: `1.0.6`
- Version code: `8`
- App icon: `Assets/Icons/sudoku_icon_512.png`
- Launch orientation: portrait

## Store Listing Assets

Google Play listing images are backed up in `StoreAssets`.

- Feature graphic
- Phone screenshots
- Tablet screenshots
- PC Google Play Games images
- Chromebook screenshots
- Android XR screenshots

## Notes

Generated Unity folders such as `Library`, `Temp`, `Logs`, and Android signing files are intentionally excluded from GitHub.
