# Junkineering Addressables Tap Test

Unity test project focused on Addressables lifecycle, async loading, cancellation, fallback handling, and remote content delivery.

## Addressables Flow

```text
Scene starts
  -> AddressableTapGame initializes AddressableService
  -> target prefab is loaded once from Addressables
  -> fallback texture is loaded once from Addressables
  -> target cube is instantiated
  -> round texture is requested asynchronously
  -> loaded texture is applied to the cube renderer
  -> correct tap starts the next async texture request
  -> previous round request is cancelled/ignored if it finishes late
  -> failed remote texture load applies the already-loaded fallback texture
  -> teardown releases loaded Addressables handles
```

`AddressableService` is separated behind `IAddressableService`, so in a real production project it can be registered through DI, for example with VContainer. For this test task I kept the runtime setup dependency-free and instantiate the service directly from the simple gameplay MonoBehaviour to avoid pulling extra packages that are not required for the evaluated behavior.

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

## Main Files

- `Assets/Scenes/AddressablesTapGame.unity`
- `Assets/App/Runtime/Game/AddressableTapGame.cs`
- `Assets/App/Runtime/Addressables/`
- `Assets/AddressableAssetsData/`
