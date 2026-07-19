# Junkineering Addressables Tap Test

Unity test project focused on Addressables lifecycle, async loading, cancellation, fallback handling, and remote content delivery.

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

Current remote load path:

```text
https://precious-gumption-d57c06.netlify.app/[BuildTarget]
```

Remote build output is in:

```text
ServerData/StandaloneWindows64
```

When Addressables content changes:

1. Build Addressables content.
2. Redeploy the full `ServerData` folder to Netlify.
3. Verify:

```text
https://precious-gumption-d57c06.netlify.app/StandaloneWindows64/catalog_0.1.0.hash
```

## Main Files

- `Assets/Scenes/AddressablesTapGame.unity`
- `Assets/App/Runtime/Game/AddressableTapGame.cs`
- `Assets/App/Runtime/Addressables/`
- `Assets/AddressableAssetsData/`
