using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System;
using System.Collections.Generic;

[Serializable]
public class MapeamentoAcao
{
    public string nomeAcao;

    [Header("Teclado")]
    public KeyCode teclado = KeyCode.None;

    [Header("Controle")]
    public GamepadButton botaoControle = GamepadButton.South;

    // Nome raw detectado no remapeamento (ex: "trigger", "button6")
    // Tem prioridade sobre botaoControle em runtime
    [HideInInspector] public string nomeRawControle = "";

    [HideInInspector] public bool aguardandoRemapeamento = false;

    // Clique de mouse (opcional). Quando useMouse=true, AcaoDown verifica
    // também Input.GetMouseButtonDown(mouseButton).
    // Isso EVITA que o clique do mouse se perca em refatorações de input:
    // toda ação que precisar de mouse deve habilitar aqui em vez de
    // checar inline no PlayerController.
    [Header("Mouse (opcional)")]
    public bool useMouse = false;
    public int mouseButton = 0; // 0 = esquerdo, 1 = direito, 2 = meio
}

public enum GamepadButton
{
    South, North, East, West,
    R1, L1, R2, L2, R3, L3,
    DpadUp, DpadDown, DpadLeft, DpadRight,
    Start, Select
}

public class PlayerInputHandler : MonoBehaviour
{
    // =============================================
    // MAPEAMENTOS
    // =============================================

    [Header("Mapeamentos de A��o")]
    public MapeamentoAcao pular = new MapeamentoAcao { nomeAcao = "Pular", teclado = KeyCode.Space, botaoControle = GamepadButton.South };
    // ATENÇÃO: teclado=F e useMouse=true mantém ataque funcionando por tecla F + clique.
    // Editável via inspector. useMouse/mouseButton também editáveis.
    // Nunca remover isso durante refatorações — é a "fonte única" de input do mouse.
    public MapeamentoAcao atacar = new MapeamentoAcao { nomeAcao = "Atacar", teclado = KeyCode.F, botaoControle = GamepadButton.West, useMouse = true, mouseButton = 0 };
    public MapeamentoAcao dash = new MapeamentoAcao { nomeAcao = "Dash", teclado = KeyCode.LeftShift, botaoControle = GamepadButton.R1 };
    public MapeamentoAcao pogo = new MapeamentoAcao { nomeAcao = "Pogo", teclado = KeyCode.S, botaoControle = GamepadButton.South };
    public MapeamentoAcao lampiao = new MapeamentoAcao { nomeAcao = "Lâmpiao", teclado = KeyCode.L, botaoControle = GamepadButton.North };

    // Modos do Lampião: alternar entre Normal/Afastar/Atrair
    public MapeamentoAcao alternarModo = new MapeamentoAcao { nomeAcao = "Alternar", teclado = KeyCode.Tab, botaoControle = GamepadButton.East };

    // Modo Paralisar: ativa/desativa paralisação de inimigos
    public MapeamentoAcao paralisar = new MapeamentoAcao { nomeAcao = "Paralisar", teclado = KeyCode.Q, botaoControle = GamepadButton.L2 };

    [Header("Controle")]
    [SerializeField] private bool usarControle = true;
    [SerializeField] private float deadZoneAnalogico = 0.15f;

    // =============================================
    // SINGLETON
    // =============================================

    public static PlayerInputHandler Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // =============================================
    // DISPOSITIVO ATIVO
    // =============================================

    // Retorna o dispositivo conectado (Gamepad ou Joystick)
    private InputDevice DispositivoAtivo()
    {
        if (Gamepad.current != null) return Gamepad.current;
        if (Joystick.current != null) return Joystick.current;
        return null;
    }

    // =============================================
    // LEITURA DE EIXO
    // =============================================

    public float Horizontal()
    {
        float teclado = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(teclado) > 0.01f) return teclado;

        if (!usarControle) return 0f;

        // Gamepad
        if (Gamepad.current != null)
        {
            float stick = Gamepad.current.leftStick.x.ReadValue();
            float dpad = Gamepad.current.dpad.x.ReadValue();
            float val = Mathf.Abs(stick) > deadZoneAnalogico ? stick : dpad;
            if (Mathf.Abs(val) > deadZoneAnalogico) return val;
        }

        // Joystick gen�rico
        if (Joystick.current != null)
        {
            float stick = Joystick.current.stick.x.ReadValue();
            if (Mathf.Abs(stick) > deadZoneAnalogico) return stick;
        }

        return 0f;
    }

    public float Vertical()
    {
        float teclado = Input.GetAxisRaw("Vertical");
        if (Mathf.Abs(teclado) > 0.01f) return teclado;

        if (!usarControle) return 0f;

        if (Gamepad.current != null)
        {
            float stick = Gamepad.current.leftStick.y.ReadValue();
            float dpad = Gamepad.current.dpad.y.ReadValue();
            float val = Mathf.Abs(stick) > deadZoneAnalogico ? stick : dpad;
            if (Mathf.Abs(val) > deadZoneAnalogico) return val;
        }

        if (Joystick.current != null)
        {
            float stick = Joystick.current.stick.y.ReadValue();
            if (Mathf.Abs(stick) > deadZoneAnalogico) return stick;
        }

        return 0f;
    }

    // =============================================
    // A��ES P�BLICAS
    // =============================================

    public bool PularDown() => AcaoDown(pular);
    public bool PularHeld() => AcaoHeld(pular);
    public bool PularUp() => AcaoUp(pular);
    public bool AtacarDown() => AcaoDown(atacar);
    public bool DashDown() => AcaoDown(dash);
    public bool PogoDown() => AcaoDown(pogo);
    public bool LampiaoDown() => AcaoDown(lampiao);
    public bool AlternarModoDown() => AcaoDown(alternarModo);
    public bool ParalisarDown() => AcaoDown(paralisar);

    // =============================================
    // HELPERS DE A��O
    // =============================================

    bool AcaoDown(MapeamentoAcao acao)
    {
        if (Input.GetKeyDown(acao.teclado)) return true;
        // Clique de mouse: checa aqui (fonte única) para não perder em refatorações
        if (acao.useMouse && Input.GetMouseButtonDown(acao.mouseButton)) return true;
        if (usarControle) return BotaoDown(acao);
        return false;
    }

    bool AcaoHeld(MapeamentoAcao acao)
    {
        if (Input.GetKey(acao.teclado)) return true;
        if (usarControle) return BotaoHeld(acao);
        return false;
    }

    bool AcaoUp(MapeamentoAcao acao)
    {
        if (Input.GetKeyUp(acao.teclado)) return true;
        if (usarControle) return BotaoUp(acao);
        return false;
    }

    // =============================================
    // LEITURA DE BOT�O � GAMEPAD E JOYSTICK
    // =============================================

    // Converte nosso enum para o nome do controle no Input System
    string EnumParaNome(GamepadButton btn)
    {
        return btn switch
        {
            GamepadButton.South => "buttonSouth",
            GamepadButton.North => "buttonNorth",
            GamepadButton.East => "buttonEast",
            GamepadButton.West => "buttonWest",
            GamepadButton.R1 => "rightShoulder",
            GamepadButton.L1 => "leftShoulder",
            GamepadButton.R2 => "rightTrigger",
            GamepadButton.L2 => "leftTrigger",
            GamepadButton.R3 => "rightStickButton",
            GamepadButton.L3 => "leftStickButton",
            GamepadButton.DpadUp => "dpad/up",
            GamepadButton.DpadDown => "dpad/down",
            GamepadButton.DpadLeft => "dpad/left",
            GamepadButton.DpadRight => "dpad/right",
            GamepadButton.Start => "start",
            GamepadButton.Select => "select",
            _ => ""
        };
    }

    // Nomes alternativos para joystick gen�rico
    string EnumParaNomeJoystick(GamepadButton btn)
    {
        return btn switch
        {
            GamepadButton.South => "trigger",
            GamepadButton.East => "button2",
            GamepadButton.North => "button3",
            GamepadButton.West => "button4",
            GamepadButton.L1 => "button5",
            GamepadButton.R1 => "button6",
            GamepadButton.L2 => "button7",
            GamepadButton.R2 => "button8",
            GamepadButton.Select => "button9",
            GamepadButton.Start => "button10",
            GamepadButton.L3 => "button11",
            GamepadButton.R3 => "button12",
            _ => ""
        };
    }

    ButtonControl ObterBotao(MapeamentoAcao acao)
    {
        // Se tem nome raw salvo, usa ele diretamente em todos os dispositivos
        if (!string.IsNullOrEmpty(acao.nomeRawControle))
        {
            foreach (var device in InputSystem.devices)
            {
                try
                {
                    var control = device[acao.nomeRawControle] as ButtonControl;
                    if (control != null) return control;
                }
                catch { }
            }
        }

        // Fallback: usa enum convertido
        if (Gamepad.current != null)
        {
            string nome = EnumParaNome(acao.botaoControle);
            if (!string.IsNullOrEmpty(nome))
            {
                var control = Gamepad.current[nome] as ButtonControl;
                if (control != null) return control;
            }
        }

        if (Joystick.current != null)
        {
            string nome = EnumParaNomeJoystick(acao.botaoControle);
            if (!string.IsNullOrEmpty(nome))
            {
                var control = Joystick.current[nome] as ButtonControl;
                if (control != null) return control;
            }
        }

        return null;
    }

    bool BotaoDown(MapeamentoAcao acao)
    {
        var control = ObterBotao(acao);
        return control != null && control.wasPressedThisFrame;
    }

    bool BotaoHeld(MapeamentoAcao acao)
    {
        var control = ObterBotao(acao);
        return control != null && control.isPressed;
    }

    bool BotaoUp(MapeamentoAcao acao)
    {
        var control = ObterBotao(acao);
        return control != null && control.wasReleasedThisFrame;
    }

    // =============================================
    // DETEC��O PARA REMAPEAMENTO
    // =============================================

    // Chamado pelo Editor para detectar bot�o pressionado agora
    public bool DetectarBotaoPressionadoRuntime(out GamepadButton resultado, out string nomeDetectado)
    {
        resultado = GamepadButton.South;
        nomeDetectado = "";

        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                if (!(control is ButtonControl btn)) continue;
                if (!btn.isPressed) continue;

                nomeDetectado = btn.name;

                GamepadButton? mapeado = NomeParaEnum(btn.name);
                if (mapeado.HasValue)
                {
                    resultado = mapeado.Value;
                    return true;
                }
            }
        }

        return false;
    }

    public static GamepadButton? NomeParaEnum(string nome)
    {
        return nome switch
        {
            "buttonSouth" => GamepadButton.South,
            "buttonNorth" => GamepadButton.North,
            "buttonEast" => GamepadButton.East,
            "buttonWest" => GamepadButton.West,
            "rightShoulder" => GamepadButton.R1,
            "leftShoulder" => GamepadButton.L1,
            "rightTrigger" => GamepadButton.R2,
            "leftTrigger" => GamepadButton.L2,
            "rightStickButton" => GamepadButton.R3,
            "leftStickButton" => GamepadButton.L3,
            "dpad/up" => GamepadButton.DpadUp,
            "dpad/down" => GamepadButton.DpadDown,
            "dpad/left" => GamepadButton.DpadLeft,
            "dpad/right" => GamepadButton.DpadRight,
            "start" => GamepadButton.Start,
            "select" => GamepadButton.Select,
            "trigger" => GamepadButton.South,
            "button2" => GamepadButton.East,
            "button3" => GamepadButton.North,
            "button4" => GamepadButton.West,
            "button5" => GamepadButton.L1,
            "button6" => GamepadButton.R1,
            "button7" => GamepadButton.L2,
            "button8" => GamepadButton.R2,
            "button9" => GamepadButton.Select,
            "button10" => GamepadButton.Start,
            "button11" => GamepadButton.L3,
            "button12" => GamepadButton.R3,
            _ => null
        };
    }

    public bool UsarControle => usarControle;
}