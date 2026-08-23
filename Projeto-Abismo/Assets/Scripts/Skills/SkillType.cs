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
    Dash,

    // =====================================
    // MODOS DO LAMPIÃO
    // =====================================
    // Estas habilidades controlam quais modos do Lampião
    // estão desbloqueados em cada fase. Integrado com o
    // sistema existente de PlayerAbilities / ScenePlayerAbilities.
    // =====================================

    /// <summary>
    /// Modo Afastar inimigos - o Lampião afasta inimigos próximos (Fase 2).
    /// </summary>
    LampiaoAfastar,

    /// <summary>
    /// Modo Atrair inimigos - o Lampião puxa inimigos próximos (Fase 3).
    /// </summary>
    LampiaoAtrair,

    /// <summary>
    /// Modo Paralisar inimigos - o Lampião paralisa inimigos próximos (Fase 4).
    /// </summary>
    LampiaoParalisar
}
