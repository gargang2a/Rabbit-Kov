using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 고화질(Anti-Aliasing 적용) 아이콘 캡처 스크립트입니다.
/// 계단 현상을 제거하고 선명한 이미지를 생성합니다.
/// </summary>
public class HighQualityCapture : MonoBehaviour
{
    [Header("Capture Settings")]
    [SerializeField] private Camera _captureCamera;
    [SerializeField] private string _fileName = "HQ_Icon";

    [Tooltip("이미지 해상도입니다. 1024 이상을 권장합니다.")]
    [SerializeField] private int _resolution = 1024;

    [Tooltip("안티앨리어싱 강도 (1, 2, 4, 8). 높을수록 가장자리가 부드러워집니다.")]
    [Range(1, 8)]
    [SerializeField] private int _antiAliasing = 8;

    [Header("Save Path")]
    [SerializeField] private string _saveFolder = "Art/Icons/Generated";

    public void Capture()
    {
        if (_captureCamera == null) _captureCamera = GetComponent<Camera>();
        if (_captureCamera == null)
        {
            Debug.LogError("[HighQualityCapture] 카메라가 없습니다!");
            return;
        }

        // 1. 경로 설정
        string folderPath = Path.Combine(Application.dataPath, _saveFolder);
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        // 2. 카메라 설정 백업
        var prevClearFlags = _captureCamera.clearFlags;
        var prevBgColor = _captureCamera.backgroundColor;
        var prevTarget = _captureCamera.targetTexture;

        // 3. 투명 배경 설정
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;
        _captureCamera.backgroundColor = new Color(0, 0, 0, 0);

        // 4. 고화질 RenderTexture 생성 (핵심: AA 레벨 설정)
        // 포맷을 DefaultARGB나 ARGB32로 하여 투명도 보존
        RenderTexture rt = RenderTexture.GetTemporary(
            _resolution,
            _resolution,
            24,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB,
            _antiAliasing // 여기가 핵심! (1~8)
        );

        _captureCamera.targetTexture = rt;

        // 5. 렌더링
        Texture2D screenShot = new Texture2D(_resolution, _resolution, TextureFormat.RGBA32, false);
        _captureCamera.Render();

        // 6. 픽셀 읽기
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, _resolution, _resolution), 0, 0);
        screenShot.Apply();

        // 7. 저장
        byte[] bytes = screenShot.EncodeToPNG();
        string fullPath = Path.Combine(folderPath, $"{_fileName}.png");
        File.WriteAllBytes(fullPath, bytes);

        // 8. 정리
        _captureCamera.targetTexture = prevTarget;
        _captureCamera.clearFlags = prevClearFlags;
        _captureCamera.backgroundColor = prevBgColor;
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        Debug.Log($"[HighQualityCapture] Saved High-Res Icon: {fullPath}");

#if UNITY_EDITOR
        AssetDatabase.Refresh();
        string assetPath = "Assets/" + _saveFolder + "/" + _fileName + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            // 임포트 세팅 자동 최적화 (선택사항)
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HighQualityCapture))]
public class HighQualityCaptureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        HighQualityCapture script = (HighQualityCapture)target;

        GUILayout.Space(15);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 1f); // 예쁜 파란색 버튼
        if (GUILayout.Button("✨ Capture High-Quality Icon", GUILayout.Height(40)))
        {
            script.Capture();
        }
        GUI.backgroundColor = Color.white;
    }
}
#endif