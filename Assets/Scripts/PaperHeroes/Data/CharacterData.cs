using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// Shared data for an ally or enemy archetype. All tuning lives here (no magic
    /// numbers in code). Designers edit these .asset files; code never changes.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData", menuName = "PaperHeroes/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        public Role role = Role.MeleeDealer;

        [Header("Combat")]
        public float maxHP = 100f;
        public float attackPower = 10f;
        public float attackInterval = 1f;
        public float attackRange = 1f;
        public float moveSpeed = 1.5f;

        [Header("Ally only (enemies ignore)")]
        public int cost = 50;
        public float spawnCooldown = 3f;

        [Header("Healer only (STRETCH)")]
        public float healPower = 0f;
        public float healInterval = 1.5f;
        public float healRange = 3f;

        [Header("Art (placeholder until prefab swap)")]
        public Color colorTag = Color.white;
        public GameObject prefab; // optional; if null a primitive is spawned
    }
}
