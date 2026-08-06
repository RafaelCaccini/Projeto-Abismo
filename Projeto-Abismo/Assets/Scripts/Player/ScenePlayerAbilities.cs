using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ScenePlayerAbilities : MonoBehaviour
{
    [Header("Player Abilities for this Scene")]
    [SerializeField]
    private List<AbilityData> abilities = new List<AbilityData>();

    // Expose as read-only for other systems
    public IReadOnlyList<AbilityData> Abilities => abilities.AsReadOnly();
}
