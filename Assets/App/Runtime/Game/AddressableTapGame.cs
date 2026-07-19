using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JunkineeringTest.Runtime.Addressables;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JunkineeringTest.Runtime.Game
{
    public sealed class AddressableTapGame : MonoBehaviour
    {
        private const string LoadingStatus = "Loading...";
        private const string TapStatus = "Tap the object!";

        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Addressables")]
        [SerializeField] private AssetReferenceGameObject targetPrefab;
        [SerializeField] private AssetReferenceTexture fallbackTexture;
        [SerializeField] private List<AssetReferenceTexture> roundTextures = new();

        [Header("Scene")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private Vector3 spawnPosition = Vector3.zero;

        [Header("UI")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text errorText;

        [Header("Feedback")]
        [SerializeField] private Color missTint = Color.red;
        [SerializeField] private float missFlashSeconds = 0.2f;

        private readonly IAddressableService _addressables = new AddressableService();

        private MaterialPropertyBlock _block;
        private CancellationTokenSource _lifetime;
        private CancellationTokenSource _round;
        private CancellationTokenSource _flash;
        private AddressableAsset<GameObject> _targetPrefabAsset;
        private AddressableAsset<Texture> _fallbackTextureAsset;
        private AddressableAsset<Texture> _currentTexture;
        private GameObject _targetInstance;
        private Renderer _targetRenderer;
        private int _textureIndex;
        private int _version;
        private bool _isLoading;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _lifetime = new CancellationTokenSource();
            inputCamera ??= Camera.main;
            errorText.gameObject.SetActive(false);
            statusText.text = LoadingStatus;
        }

        private async void Start()
        {
            try
            {
                _targetPrefabAsset = await _addressables.LoadAsync<GameObject>(targetPrefab.RuntimeKey, _lifetime.Token);
                _fallbackTextureAsset = await _addressables.LoadAsync<Texture>(fallbackTexture.RuntimeKey, _lifetime.Token);

                _targetInstance = Instantiate(_targetPrefabAsset.Asset, spawnPosition, Quaternion.identity);
                _targetInstance.name = $"{_targetPrefabAsset.Asset.name} (Runtime)";
                _targetRenderer = _targetInstance.GetComponentInChildren<Renderer>();

                await LoadNextTextureAsync();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                statusText.text = "Setup failed";
                ShowError("Addressables setup failed.");
            }
        }

        private void Update()
        {
            if (_isLoading || _targetInstance == null || !TryGetPointerDown(out var screenPosition))
            {
                return;
            }

            var ray = inputCamera.ScreenPointToRay(screenPosition);
            var correct = Physics.Raycast(ray, out var hit) && hit.transform.IsChildOf(_targetInstance.transform);

            if (!correct)
            {
                ShowError("Miss! Try again.");
                _ = FlashMissAsync();
                return;
            }

            errorText.gameObject.SetActive(false);
            _ = LoadNextTextureAsync();
        }

        private async void OnDestroy()
        {
            try
            {
                _lifetime?.Cancel();
                _round?.Cancel();
                _flash?.Cancel();

                if (_targetInstance != null)
                {
                    Destroy(_targetInstance);
                }

                await ReleaseAsync(_currentTexture);
                await ReleaseAsync(_fallbackTextureAsset);
                await ReleaseAsync(_targetPrefabAsset);
                await _addressables.ReleaseAllAsync(CancellationToken.None);

                _round?.Dispose();
                _flash?.Dispose();
                _lifetime?.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private async Task LoadNextTextureAsync()
        {
            _round?.Cancel();
            _round?.Dispose();
            _round = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            var token = _round.Token;
            var version = ++_version;
            AddressableAsset<Texture> loadedTexture = null;

            try
            {
                _isLoading = true;
                statusText.text = LoadingStatus;
                errorText.gameObject.SetActive(false);

                var texture = roundTextures[_textureIndex++ % roundTextures.Count];
                loadedTexture = await _addressables.LoadAsync<Texture>(texture.RuntimeKey, token);
                token.ThrowIfCancellationRequested();

                if (version != _version)
                {
                    throw new OperationCanceledException(token);
                }

                ApplyTexture(loadedTexture.Asset);

                var previousTexture = _currentTexture;
                _currentTexture = loadedTexture;
                loadedTexture = null;
                await ReleaseAsync(previousTexture);

                statusText.text = TapStatus;
            }
            catch (OperationCanceledException)
            {
                await ReleaseAsync(loadedTexture);
            }
            catch (Exception exception)
            {
                await ReleaseAsync(loadedTexture);
                Debug.LogWarning($"Round texture load failed. Applying fallback texture. Reason: {exception.Message}", this);
                ApplyTexture(_fallbackTextureAsset.Asset);
                await ReleaseAsync(_currentTexture);
                _currentTexture = null;
                statusText.text = TapStatus;
                ShowError("Image failed. Fallback applied.");
            }
            finally
            {
                if (version == _version)
                {
                    _isLoading = false;
                }
            }
        }

        private async Task FlashMissAsync()
        {
            _flash?.Cancel();
            _flash?.Dispose();
            _flash = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);

            try
            {
                SetTint(missTint);
                await Task.Delay(TimeSpan.FromSeconds(missFlashSeconds), _flash.Token);
                SetTint(Color.white);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ApplyTexture(Texture texture)
        {
            _targetRenderer.GetPropertyBlock(_block);
            _block.SetTexture(MainTextureId, texture);
            _block.SetTexture(BaseMapId, texture);
            _block.SetColor(ColorId, Color.white);
            _block.SetColor(BaseColorId, Color.white);
            _targetRenderer.SetPropertyBlock(_block);
        }

        private void SetTint(Color color)
        {
            _targetRenderer.GetPropertyBlock(_block);
            _block.SetColor(ColorId, color);
            _block.SetColor(BaseColorId, color);
            _targetRenderer.SetPropertyBlock(_block);
        }

        private void ShowError(string message)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }

        private Task ReleaseAsync<T>(AddressableAsset<T> asset) where T : UnityEngine.Object
        {
            return asset == null ? Task.CompletedTask : _addressables.ReleaseAsync(asset, CancellationToken.None);
        }

        private bool TryGetPointerDown(out Vector2 screenPosition)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.press.wasPressedThisFrame)
                    {
                        screenPosition = touch.position.ReadValue();
                        return true;
                    }
                }
            }

            screenPosition = default;
            return false;
        }
    }
}
