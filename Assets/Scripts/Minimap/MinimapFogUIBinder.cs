using UnityEngine;
using UnityEngine.UI;

namespace WildTamer
{
    /// <summary>
    /// 런타임 FoW RenderTexture를 MinimapFogOverlay RawImage에 할당합니다.
    /// MinimapFogOverlay 게임 오브젝트에 부착하십시오.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class MinimapFogUIBinder : MonoBehaviour
    {
        private void Start()
        {
            var rt = FowController.Instance?.FowRT;
            if (rt == null)
            {
                Debug.LogWarning("[MinimapFogUIBinder] FowController가 준비되지 않았습니다.");
                return;
            }

            var img = GetComponent<RawImage>();
            img.texture = rt;

            // 셰이더 프로퍼티 이름이 명시적으로 유지되도록 머티리얼의 _FogMaskTex 슬롯에도 전달합니다.
            if (img.material != null)
                img.material.SetTexture("_FogMaskTex", rt);
        }
    }
}