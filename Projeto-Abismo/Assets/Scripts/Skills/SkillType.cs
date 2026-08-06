using UnityEngine;

/// <summary>
/// Identificadores de habilidades do jogador.
/// Cada valor representa uma habilidade que pode ser
/// desbloqueada durante a progressão do jogo.
/// </summary>
public enum SkillType
{
    /// <summary>
    /// Pulo normal - sempre disponível (Fase 1).
    /// </summary>
    Jump,

    /// <summary>
    /// Pulo Pressionado - segurar o botão aumenta a altura do salto (Fase 2).
    /// </summary>
    ChargedJump,

    /// <summary>
    /// Dash - esquiva/carga rápida em uma direção (Fase 3).
    /// </summary>
    Dash
}
