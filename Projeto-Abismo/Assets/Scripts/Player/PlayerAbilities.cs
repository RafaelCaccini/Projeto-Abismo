using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField]
    private List<AbilityData> abilities = new List<AbilityData>();

    // Checks if an ability is present and unlocked
    public bool Has(SkillType ability)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].ability == ability)
                return abilities[i].unlocked;
        }
        return false;
    }

    // Unlocks ability; if not present, adds it as unlocked
    public void Unlock(SkillType ability)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].ability == ability)
            {
                abilities[i].unlocked = true;
                return;
            }
        }

        abilities.Add(new AbilityData(ability, true));
    }

    // Locks ability; if not present, adds it as locked
    public void Lock(SkillType ability)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].ability == ability)
            {
                abilities[i].unlocked = false;
                return;
            }
        }

        abilities.Add(new AbilityData(ability, false));
    }

    // Optional: expose the list as read-only copy for other systems
    public IReadOnlyList<AbilityData> Abilities => abilities.AsReadOnly();

    // Replace internal abilities with provided list (deep copy)
    public void ReplaceAbilities(IEnumerable<AbilityData> newAbilities)
    {
        abilities.Clear();

        if (newAbilities == null)
            return;

        foreach (var a in newAbilities)
        {
            if (a == null) continue;
            abilities.Add(new AbilityData(a.ability, a.unlocked));
        }
    }
}
