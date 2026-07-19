# Junkineering Addressables Tap Test

Unity test project focused on Addressables lifecycle, async loading, cancellation, fallback handling, and remote content delivery.

Unity version: `6000.5.2f1`

The implementation intentionally avoids extra runtime dependencies such as UniTask or a DI container. In a production project, `IAddressableService` could be registered through DI, for example with VContainer, and async helpers could be standardized through UniTask. For this test task, the goal is to keep the project small and show the required Addressables behavior using plain C# `async/await`.

## What Was Implemented

- `AddressablesTapGame` scene with one runtime-spawned target cube.
- Target cube prefab is loaded through Addressables.
- Round textures are loaded asynchronously through Addressables with `async/await`.
- Previous round texture loads are cancelled/ignored so older requests cannot overwrite newer results.
- Fallback texture is also an Addressable asset and is loaded once at startup.
- Minimal uGUI status/error feedback.
- Incorrect taps keep the current image and flash the target red.
- Addressable assets are released during teardown.
- Editor Play Mode is configured to use the existing Addressables build.
- Remote texture bundle delivery is configured through Netlify.

## Remote Addressables

For the simplest possible remote-delivery demonstration, I used Netlify as a free static HTTPS host for the built Addressables catalog and remote texture bundle.

Remote load path:

```text
https://precious-gumption-d57c06.netlify.app/[BuildTarget]
```

## WebGL Demo

https://glebbulah.github.io/JunkineeringTest/

## Main Files

- `Assets/Scenes/AddressablesTapGame.unity`
- `Assets/App/Runtime/Game/AddressableTapGame.cs`
- `Assets/App/Runtime/Addressables/`
- `Assets/AddressableAssetsData/`
