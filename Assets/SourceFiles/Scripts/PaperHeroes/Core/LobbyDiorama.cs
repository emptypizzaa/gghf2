using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 로비 대기실 디오라마 — 로스터 영웅 모델을 화면 밖 전용 리그(레이어 30 @ x=5000)에 배치하고
    /// 전용 카메라로 RenderTexture에 렌더해 로비 RawImage로 교실 배경 위에 합성한다(불투명 오버레이라 월드를 가리므로).
    /// 선택 영웅은 앞/크게 강조, 나머지는 줄서 대기, 일부는 누워서 잠. 각 모델은 저장된 분장 색 반영.
    /// idle 모션은 LobbyHeroIdle이 unscaled로 구동(로비 timeScale=0). RT 수명은 CinematicPlayer 패턴(명시 Release).
    /// </summary>
    public sealed class LobbyDiorama : MonoBehaviour
    {
        const int RigLayer = 30;                          // TagManager 유저 레이어 8~31 미사용 — 30 사용
        static readonly Vector3 RigOrigin = new Vector3(5000f, 0f, 0f); // 메인 카메라 프러스텀 밖

        // 대기 라인업 X(채움 순서), 자는 슬롯 위치(리그 로컬). 카메라가 size 3.0이라 가시 X≈[-5.3,5.3].
        static readonly float[] WaitX = { -2.6f, 2.6f, -3.9f, 3.9f };
        static readonly Vector3[] SleepPos = { new Vector3(4.2f, 0f, -0.2f), new Vector3(-4.2f, 0f, -0.2f) };

        RenderTexture _rt;
        Camera _cam;
        Transform _rig;
        int _selected;

        sealed class Slot
        {
            public UnitData data;
            public Transform slot;
            public GameObject model;
            public LobbyHeroIdle idle;
            public Quaternion standRot;
            public Vector3 roleScale;
            public bool isModel;
        }
        readonly List<Slot> _slots = new List<Slot>();

        public RenderTexture Texture => _rt;

        public void Build(UnitData[] roster, int selectedIndex)
        {
            roster = roster ?? new UnitData[0];

            _rt = new RenderTexture(1024, 576, 16, RenderTextureFormat.ARGB32) { name = "DioramaRT" };
            _rt.Create();

            var rigGo = new GameObject("DioramaRig");
            _rig = rigGo.transform;
            _rig.position = RigOrigin;

            var camGo = new GameObject("DioramaCamera");
            camGo.transform.SetParent(_rig, false);
            camGo.transform.localPosition = new Vector3(0f, 2.2f, 7.5f);
            camGo.transform.localRotation = Quaternion.Euler(10f, 180f, 0f); // -Z(라인업) 향해 약간 내려봄
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.orthographicSize = 3.0f;
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 30f;
            _cam.cullingMask = 1 << RigLayer;                 // 리그만 렌더(전투 화면 안 보임)
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f); // 투명 → 교실 배경 비침
            _cam.targetTexture = _rt;

            for (int i = 0; i < roster.Length; i++) BuildSlot(roster[i], i);

            SetSelected(selectedIndex);
            SyncTints();
        }

        void BuildSlot(UnitData data, int index)
        {
            var slotGo = new GameObject("Slot_" + index);
            slotGo.transform.SetParent(_rig, false);
            var rec = new Slot { data = data, slot = slotGo.transform, isModel = data != null && data.visualPrefab != null };

            GameObject model;
            if (rec.isModel)
            {
                model = Instantiate(data.visualPrefab, slotGo.transform);
                model.transform.localPosition = Vector3.zero;
                rec.standRot = Quaternion.LookRotation(Vector3.forward, Vector3.up) * Quaternion.Euler(0f, data.modelYawOffset, 0f);
                model.transform.localRotation = rec.standRot;
                UnitVisuals.SharpenTextures(model);
                UnitVisuals.ApplyWeaponSocket(model, data); // 기본 무기 외형(예: Ranged 활)
                var anim = data.idleClip != null ? model.AddComponent<Animation>() : null;
                rec.idle = model.AddComponent<LobbyHeroIdle>();
                rec.idle.Init(anim, data.idleClip, LobbyHeroIdle.Mode.Standing, 1f);
            }
            else
            {
                // 모델 없는 유닛(Tank): 프리미티브 캡슐 + 역할 스케일 + URP/Lit prototypeColor.
                model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                var col = model.GetComponent<Collider>();
                if (col != null) Destroy(col);
                model.transform.SetParent(slotGo.transform, false);
                model.transform.localPosition = Vector3.zero;
                rec.standRot = Quaternion.identity;
                model.transform.localRotation = rec.standRot;
                rec.roleScale = RoleScale(data);
                var rend = model.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (data != null) mat.SetColor("_BaseColor", data.prototypeColor);
                    rend.sharedMaterial = mat;
                }
                rec.idle = model.AddComponent<LobbyHeroIdle>();
                rec.idle.Init(null, null, LobbyHeroIdle.Mode.Standing, 1f);
            }

            rec.model = model;
            SetLayerRecursive(slotGo, RigLayer); // 모델 자식 + 무기 prop 생성 후 일괄
            _slots.Add(rec);
        }

        /// <summary>선택 영웅 강조 + 대기 라인업 + 자는 포즈 재배치. 재인스턴스화 없이 슬롯만 갱신.</summary>
        public void SetSelected(int index)
        {
            if (_slots.Count == 0) return;
            _selected = Mathf.Clamp(index, 0, _slots.Count - 1);

            int sleeperCount = _slots.Count >= 5 ? 2 : 1;
            var sleepers = new HashSet<int>();
            for (int i = _slots.Count - 1; i >= 0 && sleepers.Count < sleeperCount; i--)
                if (i != _selected) sleepers.Add(i);

            int waitFill = 0, sleepFill = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                var rec = _slots[i];
                bool emph = i == _selected;
                bool sleeping = sleepers.Contains(i);

                Vector3 slotPos; float mul;
                if (emph) { slotPos = new Vector3(0f, 0f, 1.3f); mul = 1.25f; }
                else if (sleeping) { slotPos = sleepFill < SleepPos.Length ? SleepPos[sleepFill++] : new Vector3(4.2f, 0f, -0.2f); mul = 1f; }
                else { slotPos = new Vector3(waitFill < WaitX.Length ? WaitX[waitFill++] : 0f, 0f, -0.4f); mul = 1f; }

                rec.slot.localPosition = slotPos;
                rec.slot.localRotation = Quaternion.identity;
                rec.model.transform.localRotation = rec.standRot; // 눕힘 해제(서기 기준으로 리셋)

                ScaleModel(rec, mul);
                float floorY = rec.slot.position.y;            // 리그 바닥(월드 y)
                PinBottom(rec.model, floorY);

                if (sleeping)
                {
                    rec.model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f) * rec.standRot; // 옆으로 눕힘
                    PinBottom(rec.model, floorY);
                }

                rec.idle.SetMode(sleeping ? LobbyHeroIdle.Mode.Sleeping : LobbyHeroIdle.Mode.Standing);
                rec.idle.CaptureBase();
            }
        }

        /// <summary>저장된 분장 색을 모든 모델에 재적용(기본이면 틴트 제거 → 원래 색 복원).</summary>
        public void SyncTints()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var rec = _slots[i];
                if (rec.data == null || rec.model == null) continue;
                string cosId = LoadoutStore.GetCosmeticId(LoadoutStore.UnitId(rec.data));
                if (cosId == LoadoutCatalog.DefaultCosmeticId) LoadoutVisuals.ClearTint(rec.model);
                else LoadoutVisuals.ApplyTint(rec.model, LoadoutCatalog.CosmeticById(cosId));
            }
        }

        void ScaleModel(Slot rec, float mul)
        {
            if (rec.isModel)
            {
                rec.model.transform.localScale = Vector3.one;   // FitModel은 localScale=1 기준이라야 정확(슬롯 lossyScale=1)
                UnitVisuals.FitModel(rec.slot, rec.model, rec.data.visualHeight * mul);
            }
            else
            {
                rec.model.transform.localScale = rec.roleScale * mul;
            }
        }

        // 모델 바운즈 바닥을 floorWorldY에 고정(눕힘/스케일 변경 후 호출).
        static void PinBottom(GameObject model, float floorWorldY)
        {
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return;
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            model.transform.position += new Vector3(0f, floorWorldY - b.min.y, 0f);
        }

        static Vector3 RoleScale(UnitData d)
        {
            if (d == null) return Vector3.one;
            switch (d.role)
            {
                case UnitRole.Tank:   return new Vector3(1.3f, 1.15f, 1.3f);
                case UnitRole.Ranged: return new Vector3(0.72f, 1.18f, 0.72f);
                case UnitRole.Healer: return new Vector3(0.8f, 0.9f, 0.8f);
                default:              return Vector3.one;
            }
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++) SetLayerRecursive(t.GetChild(i).gameObject, layer);
        }

        void OnDestroy()
        {
            // RenderTexture는 GC가 회수 안 하는 GPU 리소스 → 명시 해제(CinematicPlayer 패턴).
            if (_cam != null) _cam.targetTexture = null;
            if (_rt != null) { _rt.Release(); Destroy(_rt); _rt = null; }
            if (_rig != null) { Destroy(_rig.gameObject); _rig = null; }
        }
    }
}
