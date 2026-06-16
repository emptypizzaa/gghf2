using UnityEngine;

namespace PaperHeroes
{
    /// <summary>
    /// 원거리 공격용 발사체(화살). 슈터에서 생성되어 타겟까지 호밍 비행, 명중 시 데미지.
    /// 물리 없이 수동 이동. 데미지는 발사 시점에 캡처해 슈터가 비행 중 죽어도 보존한다.
    ///
    /// 핵심: IDamageable은 인터페이스라 `== null`이 Unity의 파괴-감지 ==를 안 탄다.
    /// 그래서 타겟을 Component로도 들고(`_targetComponent`) Transform null로 파괴를 감지한다.
    /// 타겟이 죽거나 사라지면 "빗나감"(데미지 없이 마지막 위치로 코스트 후 소멸).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private IDamageable _target;
        private Component _targetComponent;
        private float _damage;
        private float _speed;
        private float _hitRadius;
        private float _arcHeight;
        private float _maxLifetime;

        private Vector3 _startPos;
        private Vector3 _lastKnownPos;
        private float _totalDist;
        private float _age;
        private bool _missMode;

        public static Projectile Spawn(Vector3 muzzle, IDamageable target, CombatantData data, Faction faction)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // 물리 충돌 불필요 — 거리로 명중 판정

            go.transform.position = muzzle;
            go.transform.localScale = new Vector3(0.18f, 0.18f, 0.5f); // 화살처럼 길쭉

            // Emission으로 URP Bloom 글로우(_BaseColor만으론 안 빛남)
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color c = data.projectileColor;
                if (c.maxColorComponent <= 0.01f) c = data.prototypeColor; // 미설정 폴백
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.SetColor("_BaseColor", c);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", c * 4f);
                renderer.sharedMaterial = mat;

                // 트레일 — 비행 가시성("쏘는 손맛"). Bloom과 함께 글로우.
                var trail = go.AddComponent<TrailRenderer>();
                trail.time = 0.22f;
                trail.startWidth = 0.16f;
                trail.endWidth = 0f;
                trail.numCapVertices = 2;
                trail.material = mat;
                trail.startColor = c;
                trail.endColor = new Color(c.r, c.g, c.b, 0f);
            }

            var p = go.AddComponent<Projectile>();
            p._target = target;
            p._targetComponent = target as Component;
            p._damage = data.attackDamage;        // 발사 시점 캡처
            p._speed = data.projectileSpeed;
            p._hitRadius = data.projectileHitRadius;
            p._arcHeight = data.projectileArcHeight;
            p._maxLifetime = data.projectileMaxLifetime;
            p._startPos = muzzle;
            p._lastKnownPos = p._targetComponent != null ? p._targetComponent.transform.position : muzzle;
            p._totalDist = Mathf.Max(0.01f,
                Vector2.Distance(new Vector2(muzzle.x, muzzle.z), new Vector2(p._lastKnownPos.x, p._lastKnownPos.z)));
            return p;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_age >= _maxLifetime) { Destroy(gameObject); return; }

            // 타겟 유효성: Transform null(파괴) / IsDead(거점·동프레임 사망) → 빗나감 모드
            Transform tf = _targetComponent != null ? _targetComponent.transform : null;
            bool valid = (tf != null) && !_target.IsDead;
            if (valid) _lastKnownPos = tf.position;
            else _missMode = true;

            Vector3 goal = _lastKnownPos;

            // 수평 이동(X·Z) — 일정 속도
            Vector3 cur = transform.position;
            Vector2 hc = new Vector2(cur.x, cur.z);
            Vector2 hg = new Vector2(goal.x, goal.z);
            Vector2 hn = Vector2.MoveTowards(hc, hg, _speed * Time.deltaTime);
            float remaining = Vector2.Distance(hn, hg);

            // 아크: 수평 진척도 t로 Y를 띄움(전선 뒤 슈터 → 아군 위로)
            float t = 1f - Mathf.Clamp01(remaining / _totalDist);
            float baseY = Mathf.Lerp(_startPos.y, goal.y, t);
            float y = baseY + (_arcHeight > 0f ? Mathf.Sin(t * Mathf.PI) * _arcHeight : 0f);

            Vector3 next = new Vector3(hn.x, y, hn.y);
            Vector3 delta = next - transform.position;
            if (delta.sqrMagnitude > 1e-6f) transform.rotation = Quaternion.LookRotation(delta.normalized);
            transform.position = next;

            // 명중/도달
            if (remaining <= _hitRadius)
            {
                if (!_missMode && valid) _target.TakeDamage(_damage);
                Destroy(gameObject);
            }
        }
    }
}
