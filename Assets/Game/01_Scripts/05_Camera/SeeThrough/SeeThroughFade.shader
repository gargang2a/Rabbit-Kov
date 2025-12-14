Shader "Custom/SeeThroughFade"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _AlphaScale ("Alpha Scale", Range(0,1)) = 1.0
    }
    SubShader
    {
        // 1. 태그 유지 (투명도 처리를 위해 필요)
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        // 2. 깊이 기록 패스 유지 (Z-Sorting 문제 방지용)
        Pass
        {
            ZWrite On
            ColorMask 0
        }

        CGPROGRAM
        // -------------------------------------------------------------------------
        // [수정된 부분] 끝에 'addshadow' 키워드 추가
        // fullforwardshadows: 포인트 라이트 등 모든 조명 그림자 지원
        // alpha:fade: 반투명 페이드 모드
        // keepalpha: 알파 채널 값 보존
        // addshadow: 반투명 상태에서도 지오메트리 기반으로 그림자를 강제 생성하는 패스 추가
        // -------------------------------------------------------------------------
        #pragma surface surf Standard fullforwardshadows alpha:fade keepalpha addshadow
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _AlphaScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            
            // 투명도 적용
            o.Alpha = c.a * _AlphaScale;
        }
        ENDCG
    }
    // FallBack을 Legacy Shaders로 지정하면 그림자 처리가 더 안정적일 수 있음
    FallBack "Legacy Shaders/Transparent/Cutout/VertexLit"
}