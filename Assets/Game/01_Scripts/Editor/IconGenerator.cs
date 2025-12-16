using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 에디터 전용 툴: 선택한 3D 프리팹을 투명 배경의 2D PNG 이미지로 변환하여 저장합니다.
/// 사용법: 상단 메뉴 Tools > Duckov > Icon Generator 클릭
/// </summary>
public class IconGenerator : EditorWindow
{
    // 설정 변수
    private GameObject _targetPrefab;       // 캡처할 프리팹
    private int _imageSize = 512;           // 이미지 해상도 (512x512)
    private string _savePath = "Assets/Art/Icons/Generated"; // 저장 경로

    // 미리보기용 카메라 설정
    private Vector3 _cameraPosition = new Vector3(0, 1, -2);
    private Vector3 _cameraRotation = new Vector3(20, 0, 0);
    private Color _backgroundColor = new Color(0, 0, 0, 0); // 투명 배경

    [MenuItem("Tools/Duckov/Icon Generator")]
    public static void ShowWindow()
    {
        GetWindow<IconGenerator>("Icon Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("3D Prefab to PNG Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. 프리팹 선택
        _targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", _targetPrefab, typeof(GameObject), false);

        // 2. 해상도 설정
        _imageSize = EditorGUILayout.IntField("Image Size", _imageSize);

        // 3. 저장 경로 설정
        GUILayout.BeginHorizontal();
        _savePath = EditorGUILayout.TextField("Save Path", _savePath);
        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Save Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                // 절대 경로를 Assets 상대 경로로 변환
                if (path.StartsWith(Application.dataPath))
                {
                    _savePath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 4. 생성 버튼
        if (GUILayout.Button("Generate Icon", GUILayout.Height(40)))
        {
            if (_targetPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a Prefab first.", "OK");
                return;
            }

            CaptureAndSave();
        }
    }

    /// <summary>
    /// 실제 캡처 및 저장 로직
    /// </summary>
    private void CaptureAndSave()
    {
        // A. 임시 씬 오브젝트 생성 (아이템, 카메라, 조명)
        GameObject previewObject = Instantiate(_targetPrefab, Vector3.zero, Quaternion.identity);

        // 레이어 설정 (충돌 방지 등을 위해 임의의 레이어 사용 가능하나 여기선 기본값)

        // 카메라 생성
        GameObject camObj = new GameObject("TempCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.transform.position = previewObject.transform.position + _cameraPosition;
        cam.transform.rotation = Quaternion.Euler(_cameraRotation);

        // 카메라 세팅 (투명 배경 핵심)
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = _backgroundColor;
        cam.orthographic = false; // 원근감 필요 시 false, 쿼터뷰 느낌 원하면 true 고려

        // 조명 생성 (아이템이 검게 나오지 않게)
        GameObject lightObj = new GameObject("TempLight");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.5f;
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

        // B. 렌더링
        RenderTexture rt = new RenderTexture(_imageSize, _imageSize, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(_imageSize, _imageSize, TextureFormat.RGBA32, false);

        cam.Render();

        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, _imageSize, _imageSize), 0, 0);
        screenShot.Apply();

        // C. 파일 저장
        byte[] bytes = screenShot.EncodeToPNG();

        // 폴더가 없으면 생성
        if (!Directory.Exists(_savePath))
        {
            Directory.CreateDirectory(_savePath);
        }

        string fileName = $"{_targetPrefab.name}_Icon.png";
        string fullPath = Path.Combine(_savePath, fileName);

        File.WriteAllBytes(fullPath, bytes);

        // D. 정리 (Cleanup)
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);
        DestroyImmediate(camObj);
        DestroyImmediate(lightObj);
        DestroyImmediate(previewObject);

        // E. 에디터 갱신
        AssetDatabase.Refresh();

        // 생성된 파일 선택
        Object generatedFile = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
        EditorGUIUtility.PingObject(generatedFile);

        Debug.Log($"[IconGenerator] Saved to: {fullPath}");
    }
}