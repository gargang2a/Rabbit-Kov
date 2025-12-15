using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Jongjun_Weapon))]
public class Jongjun_WeaponEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        Jongjun_Weapon weapon = (Jongjun_Weapon)target;

        // 기본 필드
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_type"));
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("_damage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_coolTime"));

        // 근접 무기 선택 시 Meele Collider만 노출
        if (weapon.Type == Jongjun_Weapon.WeaponType.Melee)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Melee Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_meleeCollider"));
        }
        
        // 원거리 무기 선택 시 총구/총알 관련 노출
        if (weapon.Type == Jongjun_Weapon.WeaponType.Range)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gun & Bullet", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_fireForce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_curAmmo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxAmmo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_reloadTime"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_firePointPos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_bulletCasePos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_bulletPrefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_bulletCasePrefab"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}