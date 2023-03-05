using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public class ScreenMaskTexturePainter : MonoBehaviour
{
    [SerializeField][Min(0)] private int _brushRadius = 25;
    [SerializeField][Min(0)] private float _drawStepDistance = 25;
    [SerializeField][Range(0, 1)] private float _brushSmoothness = 0.8f;

    [SerializeField] private ComputeShader _painterShader;
    [SerializeField] private Material _outputMaskMaterial;

    private float ScreenScaleFactor => _canvas.scaleFactor;
    private int NormalizedBrushRadius => (int)(_brushRadius * ScreenScaleFactor);
    private float NormalizedDrawStepDistance => _drawStepDistance * ScreenScaleFactor;

    private Canvas _canvas;
    private RenderTexture _maskTexture;
    private Vector2? _cachedMousePosition;

    #region ComputeShaderPoperties

    private readonly int _drawKernalId = 0;
    private readonly int _clearTextureKernalId = 1;

    private readonly string _shBrushRadius = "BrushRadius";
    private readonly string _shBrushSmoothness = "BrushSmoothness";
    private readonly string _shBrushPosition = "BrushPosition";
    private readonly string _shResult = "Result";

    #endregion

    private void Awake()
    {
        Init();
        CreateTexture();
    }

    private void Init()
    {
        _canvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        ProcessInput();
    }

    private void ProcessInput()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            HoldBrush(mousePosition);
            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            ReleaseBrush();
        }
    }

    private void HoldBrush(Vector2 mousePosition)
    {
        Vector2Int screenMousePosition = Vector2Int.CeilToInt(mousePosition);

        if (screenMousePosition == _cachedMousePosition) return;

        float mousePositionDelta = _cachedMousePosition == null ? 
            0 : Vector2.Distance(_cachedMousePosition.Value, mousePosition);

        if (mousePositionDelta > NormalizedDrawStepDistance)
        {
            Vector2 normalizedMouseDirection = (mousePosition - _cachedMousePosition.Value).normalized;

            for (float i = 0; i <= mousePositionDelta; i += NormalizedDrawStepDistance)
            {
                Vector2 intermediateDrawPosition = _cachedMousePosition.Value + normalizedMouseDirection * i;
                Draw(Vector2Int.CeilToInt(intermediateDrawPosition));
            }
        }

        Draw(screenMousePosition);

        _cachedMousePosition = screenMousePosition;
    }

    private void ReleaseBrush()
    {
        ClearTexture();
        _cachedMousePosition = null;
    }

    private void CreateTexture()
    {
        Vector2Int textureSize = new Vector2Int(Screen.width, Screen.height);
        _maskTexture = new RenderTexture(textureSize.x, textureSize.y, 32)
        {
            enableRandomWrite = true,
        };

        _outputMaskMaterial.SetTexture("_MaskTex", _maskTexture);
        ClearTexture();
    }

    private void ClearTexture()
    {
        _painterShader.SetTexture(_clearTextureKernalId, _shResult, _maskTexture);
        CalculateComputeThreadsForKernal(_clearTextureKernalId, out var threadsX, out var threadsY);
        _painterShader.Dispatch(_clearTextureKernalId, threadsX, threadsY, 1);
    }
    
    private void Draw(Vector2Int screenPosition)
    {
        _painterShader.SetTexture(_drawKernalId, _shResult, _maskTexture);
        _painterShader.SetInt(_shBrushRadius, NormalizedBrushRadius);
        _painterShader.SetFloat(_shBrushSmoothness, _brushSmoothness);
        _painterShader.SetInts(_shBrushPosition, screenPosition.x, screenPosition.y);

        CalculateComputeThreadsForKernal(_drawKernalId, out var threadsX, out var threadsY);
        _painterShader.Dispatch(_drawKernalId, threadsX, threadsY, 1);
    }

    private void CalculateComputeThreadsForKernal(int kernalId, out int threadsX, out int threadsY)
    {
        _painterShader.GetKernelThreadGroupSizes(kernalId, out var x, out var y, out _);

        threadsX = _maskTexture.width / (int)x;
        threadsY = _maskTexture.height / (int)y;
    }
}