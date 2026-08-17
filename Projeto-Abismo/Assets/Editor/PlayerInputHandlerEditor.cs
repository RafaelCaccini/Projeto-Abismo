#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System.Collections.Generic;

[CustomEditor(typeof(PlayerInputHandler))]
public class PlayerInputHandlerEditor : Editor
{
    private MapeamentoAcao acaoAguardando = null;
    private string nomeAguardando = "";
    private Dictionary<string, bool> estadoAnterior = new Dictionary<string, bool>();

    private void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
        acaoAguardando = null;
        nomeAguardando = "";
    }

    private void EditorUpdate()
    {
        if (acaoAguardando == null) return;

        InputSystem.Update();

        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                if (!(control is ButtonControl btn)) continue;

                string chave = $"{device.deviceId}_{btn.name}";
                bool pressionadoAgora = btn.isPressed;
                bool estavaPressionado = estadoAnterior.ContainsKey(chave) && estadoAnterior[chave];

                if (pressionadoAgora && !estavaPressionado)
                {
                    Debug.Log($"[Remapeamento] Dispositivo: {device.displayName} | Botão: {btn.name}");

                    serializedObject.Update();
                    acaoAguardando.botaoControle = PlayerInputHandler.NomeParaEnum(btn.name) ?? acaoAguardando.botaoControle;
                    acaoAguardando.nomeRawControle = btn.name;
                    EditorUtility.SetDirty(target);
                    serializedObject.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();

                    Debug.Log($"[Remapeamento] {nomeAguardando} → raw:'{btn.name}' enum:{acaoAguardando.botaoControle} ✅");

                    acaoAguardando = null;
                    nomeAguardando = "";
                    estadoAnterior.Clear();

                    Repaint();
                    return;
                }

                estadoAnterior[chave] = pressionadoAgora;
            }
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("── Remapeamento de Controle ──", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        PlayerInputHandler handler = (PlayerInputHandler)target;

        DesenharBotaoRemapear(handler.pular, "Pular");
        DesenharBotaoRemapear(handler.atacar, "Atacar");
        DesenharBotaoRemapear(handler.dash, "Dash");
        DesenharBotaoRemapear(handler.pogo, "Pogo");
        DesenharBotaoRemapear(handler.lampiao, "Lampião");

        if (acaoAguardando != null)
        {
            EditorGUILayout.Space(6);

            string dispositivos = "";
            foreach (var d in InputSystem.devices)
                dispositivos += $"\n• {d.displayName}";

            EditorGUILayout.HelpBox(
                $"⏳ Aguardando botão para: {nomeAguardando}\n" +
                $"Dispositivos:{dispositivos}\n\n" +
                "Aperte qualquer botão...",
                MessageType.Warning
            );
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DesenharBotaoRemapear(MapeamentoAcao acao, string nome)
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(nome, GUILayout.Width(80));

        // Mostra nome raw se disponível, senão mostra o enum
        string labelControle = string.IsNullOrEmpty(acao.nomeRawControle)
            ? $"→ {acao.botaoControle}"
            : $"→ {acao.nomeRawControle}";

        EditorGUILayout.LabelField(labelControle, GUILayout.Width(160));

        bool estaAguardando = acaoAguardando == acao;
        GUI.backgroundColor = estaAguardando ? Color.yellow : Color.white;

        if (GUILayout.Button(estaAguardando ? "⏳ Aguardando..." : "🎮 Remapear", GUILayout.Width(130)))
        {
            if (estaAguardando)
            {
                acaoAguardando = null;
                nomeAguardando = "";
                estadoAnterior.Clear();
            }
            else
            {
                estadoAnterior.Clear();
                acaoAguardando = acao;
                nomeAguardando = nome;
            }
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
    }
}
#endif