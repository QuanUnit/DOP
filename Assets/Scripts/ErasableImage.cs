using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ErasableImage : MonoBehaviour
{
    [SerializeField] private Material _imageMaskMaterial;

    private Image _image;

    private void OnValidate()
    {
        if (_imageMaskMaterial == null) return;

        _image = GetComponent<Image>();
        _image.material = _imageMaskMaterial;
    }
}
