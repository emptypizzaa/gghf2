using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 절차적 플레이스홀더 활(prop) — 얇은 큐브로 활 림(아크 근사) + 시위.
    /// 활 에셋이 없어 임시로 코드 생성. **조잡 = 플레이스홀더 전제**:
    /// 토도의 복셀 활 glb가 오면 CombatantData.weaponPrefab에 할당해 교체(코드 0).
    /// UnitSpawner.ApplyWeaponSocket이 손 소켓에 부착·배치한다.
    /// </summary>
    public static class BowProp
    {
        static readonly Color Wood = new Color(0.45f, 0.28f, 0.13f);   // 활대(나무)
        static readonly Color StringCol = new Color(0.88f, 0.85f, 0.72f); // 시위

        /// <summary>세로로 선 활(Y축 길이, 아크는 +Z로 볼록, 시위는 -Z). 부착·스케일은 호출측에서.</summary>
        public static GameObject Create()
        {
            var root = new GameObject("BowProp");

            var wood = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wood.SetColor("_BaseColor", Wood);
            var str = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            str.SetColor("_BaseColor", StringCol);

            // 활대: 위 림 / 아래 림(바깥으로 기울여 아크) + 가운데 그립(앞으로 볼록)
            Bar(root.transform, wood, new Vector3(0f, 0.34f, 0.10f), new Vector3(0f, 0f, 28f), new Vector3(0.06f, 0.40f, 0.06f));
            Bar(root.transform, wood, new Vector3(0f, -0.34f, 0.10f), new Vector3(0f, 0f, -28f), new Vector3(0.06f, 0.40f, 0.06f));
            Bar(root.transform, wood, new Vector3(0f, 0f, 0.16f), Vector3.zero, new Vector3(0.07f, 0.28f, 0.07f));
            // 시위: 직선(세로)
            Bar(root.transform, str, new Vector3(0f, 0f, -0.04f), Vector3.zero, new Vector3(0.015f, 0.92f, 0.015f));

            return root;
        }

        private static void Bar(Transform parent, Material mat, Vector3 pos, Vector3 euler, Vector3 scale)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "BowBar";
            var col = b.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            b.transform.SetParent(parent, false);
            b.transform.localPosition = pos;
            b.transform.localRotation = Quaternion.Euler(euler);
            b.transform.localScale = scale;
            var r = b.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }
    }
}
