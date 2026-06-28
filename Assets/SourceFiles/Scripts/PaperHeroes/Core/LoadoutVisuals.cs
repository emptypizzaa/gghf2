using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 분장(외형) 틴트를 스폰된 유닛에 per-instance(MaterialPropertyBlock _BaseColor)로 적용한다.
    /// 공유 머티리얼(모델 프리팹)을 절대 건드리지 않아 같은 모델의 다른 인스턴스에 번지지 않는다.
    /// 기본 견본("none")은 no-op이라 현재 외형을 그대로 보존한다.
    /// URP Lit은 _BaseColor를 _BaseMap에 곱하므로 텍스처 모델은 의상 틴트가, 프리미티브는 단색이 된다.
    /// </summary>
    public static class LoadoutVisuals
    {
        // 베이스 색 프로퍼티: URP Lit(프리미티브 폴백)=_BaseColor, glTFast(.glb 모델)=baseColorFactor.
        // ★로스터 모델은 glb(glTFast)라 _BaseColor가 없어 무시된다 → baseColorFactor도 함께 세팅해야 실제로 틴트된다.
        // 셰이더에 없는 프로퍼티는 MPB가 조용히 무시하므로 둘 다 세팅해도 안전(모델/프리미티브 모두 커버).
        static readonly int _idBaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int _idBaseColorFactor = Shader.PropertyToID("baseColorFactor");

        public static void ApplyTint(GameObject visualRoot, CosmeticSwatch swatch)
        {
            if (visualRoot == null || swatch == null || swatch.id == LoadoutCatalog.DefaultCosmeticId) return;

            var mpb = new MaterialPropertyBlock();
            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(mpb);
                mpb.SetColor(_idBaseColor, swatch.tint);
                mpb.SetColor(_idBaseColorFactor, swatch.tint);
                renderers[i].SetPropertyBlock(mpb);
            }
        }

        /// <summary>
        /// per-instance 틴트 제거 — 빈 MaterialPropertyBlock으로 덮어 머티리얼 원래 색을 복원한다.
        /// 로비에서 분장을 "기본(none)"으로 되돌릴 때 사용(ApplyTint는 none에서 early-return이라 이전 틴트가 남음).
        /// 전투 스폰은 유닛이 매번 새로 생겨 틴트 누적이 없으므로 필요 없다.
        /// </summary>
        public static void ClearTint(GameObject visualRoot)
        {
            if (visualRoot == null) return;
            var empty = new MaterialPropertyBlock();
            var renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].SetPropertyBlock(empty);
        }
    }
}
