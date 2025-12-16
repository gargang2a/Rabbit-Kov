using UnityEngine;
using UnityEngine.UI;

public class WeaponPreviewSlot : MonoBehaviour
{
    [Header("UI 연결")]
    public RawImage targetRawImage;

    // ------------------------------------------------
    // 내부 변수
    // ------------------------------------------------
    private const int WEAPON_LAYER = 29;
    private GameObject _currentWeapon;
    private RenderTexture _renderTexture;
    private Transform _studioRoot;
    private GameObject _camObj;

    private void OnDestroy()
    {
        if (_renderTexture != null) _renderTexture.Release();
        if (_studioRoot != null) Destroy(_studioRoot.gameObject);
    }

    /// <summary>
    /// 무기를 설정하고, 위치/회전/크기 보정값을 적용합니다.
    /// </summary>
    public void SetWeapon(GameObject prefab, Vector3 posOffset, Vector3 rotOffset, float scale)
    {
        if (_studioRoot == null) CreateStudio();
        if (_currentWeapon != null) Destroy(_currentWeapon);

        if (prefab == null)
        {
            targetRawImage.enabled = false;
            return;
        }

        targetRawImage.enabled = true;

        // 1. 생성
        _currentWeapon = Instantiate(prefab, _studioRoot);

        // 2. ★ 보정값 적용 (여기가 핵심)
        _currentWeapon.transform.localPosition = posOffset;
        _currentWeapon.transform.localRotation = Quaternion.Euler(rotOffset);
        _currentWeapon.transform.localScale = Vector3.one * scale;

        // 3. 레이어 및 물리 정리
        SetLayerAndCleanup(_currentWeapon);
    }

    private void Update()
    {
        // 무기를 Y축으로 계속 회전시킴 (전시 효과)
        // 초기 회전값(rotOffset)을 유지하면서 돌리기 위해 Space.World 사용
        if (_currentWeapon != null)
        {
            _currentWeapon.transform.Rotate(Vector3.up * 30f * Time.deltaTime, Space.World);
        }
    }

    private void CreateStudio()
    {
        float offset = transform.GetSiblingIndex() * 100f;
        _studioRoot = new GameObject($"Studio_{gameObject.name}").transform;
        _studioRoot.position = new Vector3(offset, -1000f, 0f);

        _renderTexture = new RenderTexture(256, 256, 16);
        targetRawImage.texture = _renderTexture;

        _camObj = new GameObject("CCTV_Camera");
        _camObj.transform.SetParent(_studioRoot);
        _camObj.transform.localPosition = new Vector3(0, 0, -2f); // 카메라는 고정
        _camObj.transform.LookAt(_studioRoot);

        Camera cam = _camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0, 0, 0, 0);
        cam.cullingMask = 1 << WEAPON_LAYER;
        cam.targetTexture = _renderTexture;

        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(_studioRoot);
        lightObj.transform.localPosition = new Vector3(0, 2f, -2f);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.5f;
        l.cullingMask = 1 << WEAPON_LAYER;
    }

    private void SetLayerAndCleanup(GameObject obj)
    {
        obj.layer = WEAPON_LAYER;
        if (obj.TryGetComponent(out Rigidbody rb)) Destroy(rb);
        foreach (var col in obj.GetComponents<Collider>()) Destroy(col);
        foreach (Transform child in obj.transform) SetLayerAndCleanup(child.gameObject);
    }
}