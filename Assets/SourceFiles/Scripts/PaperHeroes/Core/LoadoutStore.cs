using UnityEngine;

namespace PaperHeroes
{
    /// <summary>한 유닛의 해석된 로드아웃(분장 견본 + 무장 프리셋).</summary>
    public readonly struct Loadout
    {
        public readonly CosmeticSwatch cosmetic;
        public readonly WeaponPreset weapon;
        public Loadout(CosmeticSwatch cosmetic, WeaponPreset weapon)
        {
            this.cosmetic = cosmetic; this.weapon = weapon;
        }
    }

    /// <summary>
    /// 로드아웃 영속화(PlayerPrefs) + 해석 서비스. 로비가 쓰고(SetCosmeticId/SetWeaponId),
    /// UnitSpawner가 스폰 시 읽는다(Resolve). UI 의존 없는 순수 로직.
    /// 키는 유닛별: CombatantData.id가 비면 에셋 이름으로 폴백(로스터 4종은 id가 비어 이름 사용).
    /// 저장된 id가 카탈로그에서 사라져도 LoadoutCatalog가 기본값으로 폴백 → 전후방 호환.
    /// </summary>
    public static class LoadoutStore
    {
        /// <summary>유닛 영속화 키(id 우선, 없으면 에셋 이름).</summary>
        public static string UnitId(CombatantData d)
            => d == null ? "" : (string.IsNullOrEmpty(d.id) ? d.name : d.id);

        static string CosmeticKey(string unitId) => "ph.loadout." + unitId + ".cosmetic";
        static string WeaponKey(string unitId) => "ph.loadout." + unitId + ".weapon";

        public static string GetCosmeticId(string unitId)
            => PlayerPrefs.GetString(CosmeticKey(unitId), LoadoutCatalog.DefaultCosmeticId);

        public static string GetWeaponId(string unitId)
            => PlayerPrefs.GetString(WeaponKey(unitId), LoadoutCatalog.DefaultWeaponId);

        public static void SetCosmeticId(string unitId, string id)
        {
            PlayerPrefs.SetString(CosmeticKey(unitId), id);
            PlayerPrefs.Save(); // save-on-write: 유닛 4종이라 비용 무시 가능, 앱 종료에도 보존.
        }

        public static void SetWeaponId(string unitId, string id)
        {
            PlayerPrefs.SetString(WeaponKey(unitId), id);
            PlayerPrefs.Save();
        }

        /// <summary>유닛의 저장된 분장/무장을 카탈로그 객체로 해석. 미설정/제거 시 기본값으로 폴백.</summary>
        public static Loadout Resolve(CombatantData data)
        {
            string u = UnitId(data);
            return new Loadout(
                LoadoutCatalog.CosmeticById(GetCosmeticId(u)),
                LoadoutCatalog.WeaponById(GetWeaponId(u)));
        }
    }
}
