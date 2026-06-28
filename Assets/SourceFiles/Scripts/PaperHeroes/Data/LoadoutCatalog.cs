using System.Collections.Generic;
using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 캐릭터 커스터마이즈(분장 색 + 무장 프리셋)의 코드 정의 카탈로그(단일 진실원천).
    /// .asset 직접 작성 없이 정적 테이블로 두며, 추후 ScriptableObject로 그대로 이관 가능(필드 모양 동일).
    /// 무장 프리셋의 스탯 배수는 Combatant가 스폰 시 read-time으로 합성한다(StatMods).
    /// </summary>

    /// <summary>무장(무기) 프리셋이 유닛에 거는 스탯 보정. ★default(StatMods)는 전부 0이라 스탯을 0으로 만든다 — 절대 쓰지 말 것.
    /// 항상 Identity 또는 WeaponPreset.ToMods()로만 생성한다.</summary>
    public struct StatMods
    {
        public float damageMul, hpMul, rangeMul, intervalMul, moveMul, defenseBonus;

        public static StatMods Identity => new StatMods
        {
            damageMul = 1f, hpMul = 1f, rangeMul = 1f, intervalMul = 1f, moveMul = 1f, defenseBonus = 0f
        };
    }

    /// <summary>분장(외형) 색 견본. id=="none"은 "기본"(틴트 미적용 — 현재 외형 보존).</summary>
    public sealed class CosmeticSwatch
    {
        public readonly string id, displayName;
        public readonly Color tint;
        public CosmeticSwatch(string id, string displayName, Color tint)
        {
            this.id = id; this.displayName = displayName; this.tint = tint;
        }
    }

    /// <summary>무장(무기) 프리셋. 배수 필드는 기본 1f(보정 없음)이고 오브젝트 이니셜라이저로 일부만 덮어쓴다.</summary>
    public sealed class WeaponPreset
    {
        public readonly string id, displayName, flavor; // flavor = 한글 설명 줄(무장 설명)
        public readonly UnitRole[] roles;               // 장착 가능 역할
        public float damageMul = 1f, hpMul = 1f, rangeMul = 1f, intervalMul = 1f, moveMul = 1f, defenseBonus = 0f;

        public WeaponPreset(string id, string displayName, string flavor, UnitRole[] roles)
        {
            this.id = id; this.displayName = displayName; this.flavor = flavor; this.roles = roles;
        }

        public StatMods ToMods() => new StatMods
        {
            damageMul = damageMul, hpMul = hpMul, rangeMul = rangeMul,
            intervalMul = intervalMul, moveMul = moveMul, defenseBonus = defenseBonus
        };

        public bool AppliesTo(UnitRole r)
        {
            for (int i = 0; i < roles.Length; i++) if (roles[i] == r) return true;
            return false;
        }
    }

    public static class LoadoutCatalog
    {
        public const string DefaultCosmeticId = "none";
        public const string DefaultWeaponId = "stock";

        // 분장 색견본 8종. [0]="none"(기본, 틴트 미적용 — Cosmetics[0]이 fallback 기본값).
        public static readonly CosmeticSwatch[] Cosmetics =
        {
            new CosmeticSwatch("none",   "기본",          new Color(1f, 1f, 1f, 1f)),
            new CosmeticSwatch("red",    "정열의 빨강",   new Color(0.90f, 0.25f, 0.20f, 1f)),
            new CosmeticSwatch("blue",   "차분한 파랑",   new Color(0.25f, 0.45f, 0.90f, 1f)),
            new CosmeticSwatch("green",  "풀잎 초록",     new Color(0.30f, 0.75f, 0.35f, 1f)),
            new CosmeticSwatch("gold",   "황금",          new Color(0.95f, 0.78f, 0.25f, 1f)),
            new CosmeticSwatch("purple", "신비의 보라",   new Color(0.60f, 0.30f, 0.80f, 1f)),
            new CosmeticSwatch("black",  "그림자 검정",   new Color(0.15f, 0.15f, 0.18f, 1f)),
            new CosmeticSwatch("white",  "순백",          new Color(0.95f, 0.95f, 0.95f, 1f)),
        };

        // 무장 프리셋: 범용 stock(전 역할) + 역할별 2종씩. intervalMul<1 = 공격/회복 빨라짐.
        public static readonly WeaponPreset[] Weapons =
        {
            new WeaponPreset("stock",         "기본 장비",      "표준 장비",
                new[]{ UnitRole.Tank, UnitRole.Melee, UnitRole.Ranged, UnitRole.Healer }),

            // 근접(Melee)
            new WeaponPreset("rusty_blade",   "무딘 칼날",      "공속 +25%, 공격 -15%",
                new[]{ UnitRole.Melee }) { damageMul = 0.85f, intervalMul = 0.80f },
            new WeaponPreset("heavy_cleaver", "묵직한 식칼",    "공격 +40%, 공속 -23%, 이동 -10%",
                new[]{ UnitRole.Melee }) { damageMul = 1.40f, intervalMul = 1.30f, moveMul = 0.90f },

            // 원거리(Ranged)
            new WeaponPreset("long_bow",      "장궁",           "사거리 +35%, 공격 +10%, 공속 -13%",
                new[]{ UnitRole.Ranged }) { rangeMul = 1.35f, damageMul = 1.10f, intervalMul = 1.15f },
            new WeaponPreset("rapid_bow",     "속사 단궁",      "공속 +43%, 공격 -20%, 사거리 -10%",
                new[]{ UnitRole.Ranged }) { intervalMul = 0.70f, damageMul = 0.80f, rangeMul = 0.90f },

            // 탱커(Tank)
            new WeaponPreset("iron_shield",   "강철 방패",      "체력 +60%, 방어 +6, 공격 -15%, 이동 -15%",
                new[]{ UnitRole.Tank }) { hpMul = 1.60f, defenseBonus = 6f, damageMul = 0.85f, moveMul = 0.85f },
            new WeaponPreset("spiked_shield", "가시 방패",      "공격 +30%, 방어 +3",
                new[]{ UnitRole.Tank }) { damageMul = 1.30f, defenseBonus = 3f },

            // 힐러(Healer) — 회복량=AttackDamage, 회복 사거리=AttackRange, 회복 간격=AttackInterval 재사용.
            new WeaponPreset("blessed_staff", "축복의 지팡이",  "회복량 +40%, 사거리 +20%",
                new[]{ UnitRole.Healer }) { damageMul = 1.40f, rangeMul = 1.20f },
            new WeaponPreset("swift_staff",   "신속의 지팡이",  "회복 속도 +43%, 회복량 -15%",
                new[]{ UnitRole.Healer }) { intervalMul = 0.70f, damageMul = 0.85f },
        };

        /// <summary>id로 분장 견본 조회. 없으면 Cosmetics[0](기본).</summary>
        public static CosmeticSwatch CosmeticById(string id)
        {
            for (int i = 0; i < Cosmetics.Length; i++) if (Cosmetics[i].id == id) return Cosmetics[i];
            return Cosmetics[0];
        }

        /// <summary>id로 무장 프리셋 조회. 없으면 stock(Weapons[0]).</summary>
        public static WeaponPreset WeaponById(string id)
        {
            for (int i = 0; i < Weapons.Length; i++) if (Weapons[i].id == id) return Weapons[i];
            return Weapons[0];
        }

        /// <summary>해당 역할이 장착 가능한 무장 목록(항상 stock 포함). 로비 무장 리스트 UI용.</summary>
        public static List<WeaponPreset> WeaponsForRole(UnitRole r)
        {
            var list = new List<WeaponPreset>();
            for (int i = 0; i < Weapons.Length; i++) if (Weapons[i].AppliesTo(r)) list.Add(Weapons[i]);
            return list;
        }
    }
}
