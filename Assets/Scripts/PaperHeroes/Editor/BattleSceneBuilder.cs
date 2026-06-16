#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PaperHeroes.EditorTools
{
    /// <summary>
    /// Builds the Paper Heroes prototype from code: seeds the ScriptableObject data
    /// (spec §6) and assembles Battle.unity (bases, camera, light, UGUI HUD, managers)
    /// with all references wired. Idempotent; persists via SaveScene + SaveAssets.
    /// Invoke the two menu items in order via `unity-cli execute-menu`.
    /// </summary>
    public static class BattleSceneBuilder
    {
        const string DataDir = "Assets/Data";
        const string CharDir = "Assets/Data/Characters";
        const string StagePath = "Assets/Data/Stage1.asset";
        const string SceneDir = "Assets/Scenes";
        const string ScenePath = "Assets/Scenes/Battle.unity";

        static readonly Color Blue = new Color(0.25f, 0.45f, 1f);
        static readonly Color Green = new Color(0.30f, 0.80f, 0.35f);
        static readonly Color Cyan = new Color(0.25f, 0.85f, 0.90f);
        static readonly Color EnemyRed = new Color(0.90f, 0.25f, 0.22f);
        static readonly Color EnemyGray = new Color(0.35f, 0.35f, 0.38f);

        // ---------------------------------------------------------------- Data assets
        [MenuItem("PaperHeroes/1. Create Data Assets")]
        public static void CreateDataAssets()
        {
            EnsureFolder(DataDir);
            EnsureFolder(CharDir);

            var tank = Upsert(CharDir + "/ally_tank.asset", c => {
                c.id = "ally_tank"; c.displayName = "종이상자 탱커"; c.role = Role.Tank;
                c.maxHP = 600; c.attackPower = 20; c.attackInterval = 1.5f; c.attackRange = 1.0f; c.moveSpeed = 1.2f;
                c.cost = 80; c.spawnCooldown = 6; c.colorTag = Blue;
            });
            var melee = Upsert(CharDir + "/ally_melee.asset", c => {
                c.id = "ally_melee"; c.displayName = "종이칼 근접딜러"; c.role = Role.MeleeDealer;
                c.maxHP = 150; c.attackPower = 60; c.attackInterval = 1.0f; c.attackRange = 1.0f; c.moveSpeed = 2.0f;
                c.cost = 50; c.spawnCooldown = 3; c.colorTag = Green;
            });
            var ranged = Upsert(CharDir + "/ally_ranged.asset", c => {
                c.id = "ally_ranged"; c.displayName = "종이활 원거리딜러"; c.role = Role.RangedDealer;
                c.maxHP = 100; c.attackPower = 50; c.attackInterval = 1.2f; c.attackRange = 4.0f; c.moveSpeed = 1.5f;
                c.cost = 90; c.spawnCooldown = 5; c.colorTag = Cyan;
            });
            var mob = Upsert(CharDir + "/enemy_mob.asset", c => {
                c.id = "enemy_mob"; c.displayName = "졸개"; c.role = Role.MeleeDealer;
                c.maxHP = 200; c.attackPower = 30; c.attackInterval = 1.0f; c.attackRange = 1.0f; c.moveSpeed = 1.5f;
                c.colorTag = EnemyRed;
            });
            var bushman = Upsert(CharDir + "/enemy_bushman.asset", c => {
                c.id = "enemy_bushman"; c.displayName = "부시맨"; c.role = Role.MeleeDealer;
                c.maxHP = 800; c.attackPower = 80; c.attackInterval = 2.0f; c.attackRange = 1.0f; c.moveSpeed = 0.8f;
                c.colorTag = EnemyGray;
            });

            var stage = AssetDatabase.LoadAssetAtPath<StageData>(StagePath);
            bool newStage = stage == null;
            if (newStage) stage = ScriptableObject.CreateInstance<StageData>();
            stage.playerBaseHP = 1000; stage.enemyBaseHP = 1500;
            stage.moneyStart = 50; stage.moneyPerSecond = 5; stage.moneyMax = 300;
            stage.laneLength = 18;
            stage.allyRoster = new List<CharacterData> { tank, melee, ranged };
            stage.waves = new List<WaveEntry>
            {
                new WaveEntry { time = 3,  enemy = mob,     count = 3, spacing = 2.0f },
                new WaveEntry { time = 18, enemy = mob,     count = 4, spacing = 1.5f },
                new WaveEntry { time = 30, enemy = bushman, count = 1, spacing = 0f },
                new WaveEntry { time = 45, enemy = mob,     count = 5, spacing = 1.2f },
                new WaveEntry { time = 60, enemy = bushman, count = 2, spacing = 4.0f },
            };
            if (newStage) AssetDatabase.CreateAsset(stage, StagePath);
            else EditorUtility.SetDirty(stage);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("PH_BUILD data assets created/updated");
        }

        static CharacterData Upsert(string path, Action<CharacterData> fill)
        {
            var a = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            bool isNew = a == null;
            if (isNew) a = ScriptableObject.CreateInstance<CharacterData>();
            fill(a);
            if (isNew) AssetDatabase.CreateAsset(a, path);
            else EditorUtility.SetDirty(a);
            return a;
        }

        // ---------------------------------------------------------------- Scene
        [MenuItem("PaperHeroes/2. Build Battle Scene")]
        public static void BuildBattleScene()
        {
            CreateDataAssets();
            var stage = AssetDatabase.LoadAssetAtPath<StageData>(StagePath);
            if (stage == null) { Debug.LogError("PH_ERROR no StageData at " + StagePath); return; }

            // Protect any unsaved active scene before we replace it.
            if (!EditorApplication.isPlaying)
            {
                var act = EditorSceneManager.GetActiveScene();
                if (act.isDirty && !string.IsNullOrEmpty(act.path))
                    EditorSceneManager.SaveScene(act);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            float lane = stage.laneLength;
            float mid = lane * 0.5f;

            // Camera (side framing of the lane)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(mid, 7.5f, -17f);
            camGo.transform.LookAt(new Vector3(mid, 0.5f, 0f));
            cam.fieldOfView = 42f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.86f, 0.89f, 0.92f);

            // Light (units/bases are Unlit, but harmless + future-proof)
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.0f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Ground strip
            MakeBlock("Ground", new Vector3(mid, -0.5f, 0f), new Vector3(lane + 6f, 0.2f, 3f), new Color(0.62f, 0.62f, 0.66f));

            // Bases
            var playerBaseGo = MakeBlock("PlayerBase", new Vector3(0f, 1f, 0f), new Vector3(1.4f, 2.2f, 2.2f), new Color(0.30f, 0.50f, 0.95f));
            var playerBase = playerBaseGo.AddComponent<Base>();
            var enemyBaseGo = MakeBlock("EnemyBase", new Vector3(lane, 1f, 0f), new Vector3(1.4f, 2.2f, 2.2f), new Color(0.90f, 0.40f, 0.40f));
            var enemyBase = enemyBaseGo.AddComponent<Base>();

            // Managers
            var managers = new GameObject("Managers");
            var money = managers.AddComponent<MoneyManager>();
            var allySpawner = managers.AddComponent<AllySpawner>();
            var waveSpawner = managers.AddComponent<WaveSpawner>();
            var battle = managers.AddComponent<BattleManager>();

            // Canvas + EventSystem (NEW Input System UI module — required)
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Money label (top center)
            var moneyLabel = MakeText(canvasGo.transform, "MoneyLabel", "0 / 0", 44, TextAlignmentOptions.Center, Color.black);
            Anchor(moneyLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(420, 72));
            var moneyUI = canvasGo.AddComponent<MoneyUI>();
            moneyUI.money = money; moneyUI.label = moneyLabel;

            // Base HP bars
            MakeHpBar(canvasGo.transform, "PlayerHPBar", new Vector2(0f, 1f), new Vector2(24, -24), new Color(0.30f, 0.50f, 0.95f), playerBase);
            MakeHpBar(canvasGo.transform, "EnemyHPBar", new Vector2(1f, 1f), new Vector2(-24, -24), new Color(0.90f, 0.40f, 0.40f), enemyBase);

            // Ally button bar (bottom, from roster)
            int n = stage.allyRoster.Count;
            const float bw = 220f, bh = 130f, gap = 24f;
            float totalW = n * bw + Mathf.Max(0, n - 1) * gap;
            for (int i = 0; i < n; i++)
            {
                var data = stage.allyRoster[i];
                if (data == null) continue;
                float x = -totalW * 0.5f + bw * 0.5f + i * (bw + gap);
                MakeUnitButton(canvasGo.transform, data, allySpawner, money, new Vector2(x, 24), new Vector2(bw, bh));
            }

            // Result panel (starts hidden)
            var resultPanel = MakeResultPanel(canvasGo.transform, battle);

            // Wiring
            battle.stage = stage;
            battle.playerBase = playerBase;
            battle.enemyBase = enemyBase;
            battle.money = money;
            battle.allySpawner = allySpawner;
            battle.waveSpawner = waveSpawner;
            battle.resultPanel = resultPanel;
            EditorUtility.SetDirty(battle);

            EnsureFolder(SceneDir);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("PH_BUILD scene built: " + ScenePath);
        }

        // ---------------------------------------------------------------- helpers
        static GameObject MakeBlock(string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            var col = go.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.DestroyImmediate(col);
            SetUrpColor(go, color);
            return go;
        }

        static void SetUrpColor(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            rend.sharedMaterial = mat;
        }

        static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.alignment = align; t.color = color;
            t.raycastTarget = false;
            return t;
        }

        static Image MakeImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        static void Stretch(RectTransform rt)
            => Anchor(rt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        static BaseHPBar MakeHpBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Color color, Base target)
        {
            var root = MakeImage(parent, name, new Color(0, 0, 0, 0.35f));
            Anchor(root.rectTransform, anchor, anchor, anchor, pos, new Vector2(360, 46));
            var fill = MakeImage(root.transform, "Fill", color);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left; fill.fillAmount = 1f;
            Stretch(fill.rectTransform);
            var label = MakeText(root.transform, "Label", "", 26, TextAlignmentOptions.Center, Color.white);
            Stretch(label.rectTransform);
            var bar = root.gameObject.AddComponent<BaseHPBar>();
            bar.target = target; bar.fill = fill; bar.label = label;
            return bar;
        }

        static UnitButton MakeUnitButton(Transform parent, CharacterData data, AllySpawner spawner, MoneyManager money, Vector2 pos, Vector2 size)
        {
            var img = MakeImage(parent, "Btn_" + data.id, new Color(0.95f, 0.95f, 0.95f, 0.95f));
            Anchor(img.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;

            var swatch = MakeImage(img.transform, "Swatch", data.colorTag);
            Anchor(swatch.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 28));
            swatch.raycastTarget = false;

            var nameLabel = MakeText(img.transform, "Name", data.displayName, 26, TextAlignmentOptions.Center, Color.black);
            Anchor(nameLabel.rectTransform, new Vector2(0, 0.45f), new Vector2(1, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var costLabel = MakeText(img.transform, "Cost", "$" + data.cost, 30, TextAlignmentOptions.Center, new Color(0.1f, 0.4f, 0.1f));
            Anchor(costLabel.rectTransform, new Vector2(0, 0.05f), new Vector2(1, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            var sweep = MakeImage(img.transform, "CooldownSweep", new Color(0, 0, 0, 0.55f));
            sweep.type = Image.Type.Filled; sweep.fillMethod = Image.FillMethod.Vertical;
            sweep.fillOrigin = (int)Image.OriginVertical.Top; sweep.fillAmount = 0f;
            sweep.raycastTarget = false;
            Stretch(sweep.rectTransform);

            var ub = img.gameObject.AddComponent<UnitButton>();
            ub.data = data; ub.spawner = spawner; ub.money = money;
            ub.button = button; ub.cooldownSweep = sweep; ub.nameLabel = nameLabel; ub.costLabel = costLabel;
            return ub;
        }

        static ResultPanel MakeResultPanel(Transform parent, BattleManager battle)
        {
            var holder = new GameObject("ResultUI", typeof(RectTransform));
            holder.transform.SetParent(parent, false);
            Stretch(holder.GetComponent<RectTransform>());
            var rp = holder.AddComponent<ResultPanel>();

            var panel = MakeImage(holder.transform, "Panel", new Color(0, 0, 0, 0.7f));
            Stretch(panel.rectTransform);

            var resultText = MakeText(panel.transform, "ResultText", "WIN", 120, TextAlignmentOptions.Center, Color.white);
            Anchor(resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(800, 200));

            var restartImg = MakeImage(panel.transform, "RestartButton", new Color(0.95f, 0.95f, 0.95f, 1f));
            Anchor(restartImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(360, 100));
            var restartButton = restartImg.gameObject.AddComponent<Button>();
            restartButton.targetGraphic = restartImg;
            var restartLabel = MakeText(restartImg.transform, "Label", "RESTART", 40, TextAlignmentOptions.Center, Color.black);
            Stretch(restartLabel.rectTransform);

            rp.battle = battle; rp.panelRoot = panel.gameObject; rp.resultText = resultText; rp.restartButton = restartButton;
            panel.gameObject.SetActive(false);
            return rp;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace("\\", "/");
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes) if (s.path == scenePath) return;
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
