#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool para criar automaticamente toda a estrutura do Boss Fase 3.
/// Menu: Tools > Abismo > Setup Boss Fase 3
/// </summary>
public class BossFase3Setup : EditorWindow
{
    // Configurações da arena
    private float arenaWidth = 30f;
    private float arenaHeight = 15f;
    private float bossXOffset = 2f; // Distância da borda direita
    private float spawnXOffset = 3f; // Distância da borda direita para spawn
    private float leftLimitXOffset = 2f; // Distância da borda esquerda

    [MenuItem("Tools/Abismo/Setup Boss Fase 3")]
    public static void ShowWindow()
    {
        GetWindow<BossFase3Setup>("Boss Fase 3 Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Boss Fase 3 - Auto Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("Configurações da Arena", EditorStyles.label);
        arenaWidth = EditorGUILayout.FloatField("Largura da Arena", arenaWidth);
        arenaHeight = EditorGUILayout.FloatField("Altura da Arena", arenaHeight);
        bossXOffset = EditorGUILayout.FloatField("Boss - Offset da Borda Direita", bossXOffset);
        spawnXOffset = EditorGUILayout.FloatField("Spawn Ondas - Offset da Borda Direita", spawnXOffset);
        leftLimitXOffset = EditorGUILayout.FloatField("Limite Esquerdo - Offset da Borda Esquerda", leftLimitXOffset);

        EditorGUILayout.Space();

        if (GUILayout.Button("Criar Boss Fase 3 Completo", GUILayout.Height(40)))
        {
            CreateBossFase3();
        }

        if (GUILayout.Button("Criar Apenas Prefabs das Ondas", GUILayout.Height(30)))
        {
            CreateWavePrefabs();
        }

        if (GUILayout.Button("Limpar Tudo (Boss Fase 3)", GUILayout.Height(30)))
        {
            CleanupBossFase3();
        }
    }

    private void CreateBossFase3()
    {
        // Inicia operação undo
        Undo.SetCurrentGroupName("Create Boss Fase 3");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            // 1. Encontra ou cria a arena (parent)
            Transform arenaParent = FindOrCreateArenaParent();

            // 2. Calcula posições baseadas na arena
            Vector3 arenaCenter = arenaParent.position;
            float halfWidth = arenaWidth / 2f;
            float halfHeight = arenaHeight / 2f;

            float rightEdge = arenaCenter.x + halfWidth;
            float leftEdge = arenaCenter.x - halfWidth;
            float topEdge = arenaCenter.y + halfHeight;
            float bottomEdge = arenaCenter.y - halfHeight;
            float middleY = arenaCenter.y;

            // Posições calculadas
            Vector3 posicaoBossPos = new Vector3(rightEdge - bossXOffset, middleY, 0);
            Vector3 spawnOndasPos = new Vector3(rightEdge - spawnXOffset, middleY, 0);
            Vector3 limiteEsquerdoPos = new Vector3(leftEdge + leftLimitXOffset, middleY, 0);
            Vector3 centroArenaPos = arenaCenter;

            // 3. Cria os pontos de referência (Transforms vazios)
            Transform posicaoBoss = CreateTransformPoint(arenaParent, "PosicaoBoss", posicaoBossPos, Color.red, 1.5f);
            Transform spawnOndas = CreateTransformPoint(arenaParent, "SpawnOndas", spawnOndasPos, Color.cyan, 0.5f);
            Transform limiteEsquerdo = CreateTransformPoint(arenaParent, "LimiteEsquerdoArena", limiteEsquerdoPos, Color.green, 0.5f);
            Transform centroArena = CreateTransformPoint(arenaParent, "CentroArena", centroArenaPos, Color.yellow, 1f);

            // 4. Cria os prefabs das ondas
            GameObject ondaComumPrefab = CreateWavePrefab("OndaComumPrefab", false);
            GameObject ondaRoxaPrefab = CreateWavePrefab("OndaRoxaPrefab", true);

            // 5. Cria o GameObject do Boss
            GameObject bossObj = CreateBossGameObject(arenaParent, "BossFase3", posicaoBossPos);

            // 6. Configura o script BossFase3
            var bossScript = bossObj.GetComponent<BossFase3>();
            if (bossScript != null)
            {
                Undo.RecordObject(bossScript, "Configure BossFase3");

                // Usa SerializedObject para setar campos privados
                var so = new SerializedObject(bossScript);
                so.FindProperty("posicaoBoss").objectReferenceValue = posicaoBoss;
                so.FindProperty("spawnOndas").objectReferenceValue = spawnOndas;
                so.FindProperty("limiteEsquerdoArena").objectReferenceValue = limiteEsquerdo;
                so.FindProperty("centroArena").objectReferenceValue = centroArena;
                so.FindProperty("ondaComumPrefab").objectReferenceValue = ondaComumPrefab;
                so.FindProperty("ondaRoxaPrefab").objectReferenceValue = ondaRoxaPrefab;

                // Configurações padrão balanceadas
                so.FindProperty("velocidadeOndaComum").floatValue = 7f;
                so.FindProperty("danoOndaComum").intValue = 1;
                so.FindProperty("quantidadeOndasComuns").intValue = 3;
                so.FindProperty("intervaloOndasComuns").floatValue = 0.5f;
                so.FindProperty("alturaOndaComum").floatValue = 1.8f;

                so.FindProperty("velocidadeOndaRoxa").floatValue = 6f;
                so.FindProperty("danoOndaRoxa").intValue = 2;
                so.FindProperty("quantidadeOndasRoxas").intValue = 2;
                so.FindProperty("intervaloOndasRoxas").floatValue = 0.8f;
                so.FindProperty("alturaOndaRoxa").floatValue = 5f;

                so.FindProperty("ondasComunsAntesRoxa").intValue = 2;
                so.FindProperty("tempoEntreSequencias").floatValue = 2.5f;
                so.FindProperty("tempoPreparacaoOnda").floatValue = 1f;

                so.FindProperty("forcaEmpurraoConstante").floatValue = 4f;
                so.FindProperty("usarForcaFisica").boolValue = true;

                so.FindProperty("vidaMaxima").intValue = 20;
                so.FindProperty("danoParaStun").intValue = 6;
                so.FindProperty("duracaoStun").floatValue = 4f;
                so.FindProperty("tempoInvulneravelStun").floatValue = 0.5f;

                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(bossScript);
            }

            // 7. Adiciona collider grande no boss (para ser "enorme")
            var bossCol = bossObj.GetComponent<BoxCollider2D>();
            if (bossCol == null) bossCol = bossObj.AddComponent<BoxCollider2D>();
            bossCol.size = new Vector2(4f, 6f);
            bossCol.offset = new Vector2(0f, 0f);

            // 8. Seleciona o boss no final
            Selection.activeGameObject = bossObj;

            Debug.Log("[BossFase3Setup] Boss Fase 3 criado com sucesso!");
            Debug.Log($"  - Boss em: {posicaoBossPos}");
            Debug.Log($"  - Spawn Ondas em: {spawnOndasPos}");
            Debug.Log($"  - Limite Esquerdo em: {limiteEsquerdoPos}");
            Debug.Log($"  - Centro Arena em: {centroArenaPos}");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private Transform FindOrCreateArenaParent()
    {
        // Procura por objeto "Arena" ou "Fase3Arena"
        GameObject arena = GameObject.Find("Arena");
        if (arena == null) arena = GameObject.Find("Fase3Arena");
        if (arena == null) arena = GameObject.Find("BossArena");

        if (arena == null)
        {
            arena = new GameObject("Fase3Arena");
            Undo.RegisterCreatedObjectUndo(arena, "Create Arena Parent");
            arena.transform.position = Vector3.zero;
        }

        return arena.transform;
    }

    private Transform CreateTransformPoint(Transform parent, string name, Vector3 position, Color gizmoColor, float gizmoSize)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent);
        go.transform.position = position;

        // Adiciona componente para gizmo visual no editor
        var gizmo = go.AddComponent<TransformGizmo>();
        gizmo.gizmoColor = gizmoColor;
        gizmo.gizmoSize = gizmoSize;
        gizmo.label = name;

        return go.transform;
    }

    private GameObject CreateWavePrefab(string name, bool isRoxa)
    {
        // Verifica se já existe na pasta Resources/Prefabs
        string prefabPath = $"Assets/Resources/Prefabs/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existing != null)
        {
            Debug.Log($"[BossFase3Setup] Prefab {name} já existe em {prefabPath}");
            return existing;
        }

        // Cria o GameObject da onda
        GameObject wave = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(wave, "Create " + name);

        // Adiciona componentes necessários
        var rb = wave.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.bodyType = RigidbodyType2D.Dynamic;

        var col = wave.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, isRoxa ? 5f : 1.5f);

        var sr = wave.AddComponent<SpriteRenderer>();
        sr.sprite = CreateWaveSprite(isRoxa);
        if (isRoxa)
        {
            sr.color = new Color(0.7f, 0.2f, 0.9f, 0.95f);
            wave.transform.localScale = Vector3.one * 1.4f;
        }
        else
        {
            sr.color = new Color(0.2f, 0.7f, 1f, 0.7f);
        }
        sr.sortingOrder = 5;

        // Adiciona script OndaBoss2
        var ondaScript = wave.AddComponent<OndaBoss2>();

        // Salva como prefab
        string folderPath = "Assets/Resources/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Prefabs");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wave, prefabPath);
        DestroyImmediate(wave);

        Debug.Log($"[BossFase3Setup] Prefab criado: {prefabPath}");
        return prefab;
    }

    private Sprite CreateWaveSprite(bool isRoxa)
    {
        int width = 64;
        int height = isRoxa ? 128 : 64;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = (x - width / 2f) / (width / 2f);
                float cy = (y - height / 2f) / (height / 2f);
                float dist = Mathf.Sqrt(cx * cx + cy * cy);

                if (dist < 1f)
                {
                    float alpha = 1f - dist;
                    if (isRoxa)
                        pixels[y * width + x] = new Color(0.7f, 0.2f, 0.9f, alpha);
                    else
                        pixels[y * width + x] = new Color(0.2f, 0.7f, 1f, alpha * 0.7f);
                }
                else
                {
                    pixels[y * width + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private GameObject CreateBossGameObject(Transform parent, string name, Vector3 position)
    {
        GameObject boss = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(boss, "Create " + name);
        boss.transform.SetParent(parent);
        boss.transform.position = position;
        boss.transform.localScale = new Vector3(3f, 3f, 1f); // Boss "enorme"

        // Adiciona componentes necessários
        var rb = boss.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        var col = boss.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4f, 6f);

        var sr = boss.AddComponent<SpriteRenderer>();
        sr.sprite = CreateBossSprite();
        sr.color = Color.white;
        sr.sortingOrder = 10;

        var animator = boss.AddComponent<Animator>();
        // O animator controller deve ser configurado manualmente depois

        var audioSource = boss.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;

        // Adiciona script BossFase3
        boss.AddComponent<BossFase3>();

        // Tag
        boss.tag = "Boss";

        return boss;
    }

    private Sprite CreateBossSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = (x - size / 2f) / (size / 2f);
                float cy = (y - size / 2f) / (size / 2f);
                float dist = Mathf.Sqrt(cx * cx + cy * cy);

                if (dist < 1f)
                {
                    // Gradiente vermelho-escuro para boss
                    float alpha = 1f - dist * 0.5f;
                    pixels[y * size + x] = new Color(0.8f, 0.1f, 0.1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void CreateWavePrefabs()
    {
        Undo.SetCurrentGroupName("Create Wave Prefabs");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            CreateWavePrefab("OndaComumPrefab", false);
            CreateWavePrefab("OndaRoxaPrefab", true);
            Debug.Log("[BossFase3Setup] Prefabs das ondas criados/atualizados!");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    private void CleanupBossFase3()
    {
        if (!EditorUtility.DisplayDialog("Confirmar Limpeza",
            "Isso vai DELETAR:\n- BossFase3 GameObject\n- Todos os pontos de referência (PosicaoBoss, SpawnOndas, etc)\n- Prefabs em Assets/Resources/Prefabs/\n\nTem certeza?",
            "Sim, deletar tudo", "Cancelar"))
            return;

        // Deleta GameObjects na cena
        string[] names = { "BossFase3", "Fase3Arena", "PosicaoBoss", "SpawnOndas", "LimiteEsquerdoArena", "CentroArena" };
        foreach (string name in names)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) DestroyImmediate(go);
        }

        // Deleta prefabs
        AssetDatabase.DeleteAsset("Assets/Resources/Prefabs/OndaComumPrefab.prefab");
        AssetDatabase.DeleteAsset("Assets/Resources/Prefabs/OndaRoxaPrefab.prefab");

        AssetDatabase.Refresh();
        Debug.Log("[BossFase3Setup] Limpeza concluída!");
    }
}

// Componente auxiliar para desenhar gizmos nos pontos de referência
[ExecuteAlways]
public class TransformGizmo : MonoBehaviour
{
    public Color gizmoColor = Color.white;
    public float gizmoSize = 1f;
    public string label = "";

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        Gizmos.DrawIcon(transform.position, "d_UnityEditor.ConsoleWindow", true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, gizmoSize + 0.2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * (gizmoSize + 0.3f), label);
#endif
    }
}
#endif