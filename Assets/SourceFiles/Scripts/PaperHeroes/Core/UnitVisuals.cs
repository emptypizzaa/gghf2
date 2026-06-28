using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 유닛 모델 비주얼 공통 헬퍼(전투 스폰 + 로비 디오라마 공용). UnitSpawner에서 추출 — Combatant 의존 없는
    /// 순수 변환 로직이라 전투/표시 양쪽에서 동일하게 쓴다(단일 진실원천). MonoBehaviour 아님 → Object.Instantiate 사용.
    /// </summary>
    public static class UnitVisuals
    {
        /// <summary>모델을 목표 높이로 스케일하고 바닥을 유닛 발(지면)에 맞춘다.</summary>
        public static void FitModel(Transform root, GameObject model, float targetHeight)
        {
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;

            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float h = b.size.y;
            if (h < 1e-4f) return;

            float parentY = Mathf.Max(1e-4f, root.lossyScale.y);
            model.transform.localScale = Vector3.one * (targetHeight / h / parentY);

            // 스케일 후 바운즈 재측정 → 모델 바닥을 유닛 발(지면)에 정렬.
            Bounds b2 = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b2.Encapsulate(rends[i].bounds);
            float footY = root.position.y - root.lossyScale.y;
            model.transform.position += new Vector3(0f, footY - b2.min.y, 0f);
        }

        /// <summary>
        /// 모델 텍스처를 Point 필터로 설정 — 복셀/픽셀아트(작은 텍스처)가 Bilinear 보간으로
        /// 뭉개지지 않고 또렷하게 보이도록(눈·나무 방패 등). aniso도 끔.
        /// </summary>
        public static void SharpenTextures(GameObject model)
        {
            var ids = new System.Collections.Generic.List<int>();
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                var mats = renderers[r].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (mat == null) continue;
                    ids.Clear();
                    mat.GetTexturePropertyNameIDs(ids);
                    for (int i = 0; i < ids.Count; i++)
                    {
                        var tex = mat.GetTexture(ids[i]) as Texture2D;
                        if (tex != null)
                        {
                            tex.filterMode = FilterMode.Point;
                            tex.anisoLevel = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 무기 소켓(선택): 모델의 특정 노드(예: 지팡이)를 숨기고, 손 노드에 무기 prop(활)을 부착한다.
        /// 전부 데이터 조건부 — 미설정 유닛은 no-op. art-agnostic: weaponPrefab(활 glb) 또는 절차적 플레이스홀더.
        /// </summary>
        public static void ApplyWeaponSocket(GameObject model, CombatantData data)
        {
            // 1) 숨길 노드(예: 지팡이 'Staff-Global')의 하위 Renderer 비활성.
            if (!string.IsNullOrEmpty(data.hideChildNode))
            {
                var hide = FindDeep(model, data.hideChildNode);
                if (hide != null)
                {
                    var rs = hide.GetComponentsInChildren<Renderer>(true);
                    for (int i = 0; i < rs.Length; i++) rs[i].enabled = false;
                }
            }

            // 2) 무기 prop 부착: weaponPrefab 우선, 없으면 절차적 플레이스홀더 활.
            GameObject prop = null;
            if (data.weaponPrefab != null) prop = Object.Instantiate(data.weaponPrefab);
            else if (data.usePlaceholderBow) prop = BowProp.Create();
            if (prop == null) return;

            Transform socket = FindDeep(model, data.weaponSocketNode);
            prop.transform.SetParent(socket != null ? socket : model.transform, false);
            prop.transform.localPosition = data.weaponLocalPos;
            prop.transform.localRotation = Quaternion.Euler(data.weaponLocalEuler);
            prop.transform.localScale = Vector3.one * data.weaponScale;
        }

        /// <summary>모델 하위에서 이름으로 노드를 찾는다(비활성 포함). 빈 이름이면 null.</summary>
        public static Transform FindDeep(GameObject root, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++) if (all[i].name == name) return all[i];
            return null;
        }
    }
}
