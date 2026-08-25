using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAbilities : MonoBehaviour
{
    [SerializeField]
    private List<AbilityData> abilities = new List<AbilityData>();

    public bool Has(SkillType ability)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null && abilities[i].ability == ability)
                return abilities[i].unlocked;
        }

        return false;
    }

    public void Unlock(SkillType ability)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null && abilities[i].ability == ability)
            {
                abilities[i].unlocked = true;
                return;
            }
        }

        abilities.Add(new AbilityData(ability, true));
    }

    public void Lock(SkillType ability)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i] != null && abilities[i].ability == ability)
            {
                abilities[i].unlocked = false;
                return;
            }
        }

        abilities.Add(new AbilityData(ability, false));
    }

    public IReadOnlyList<AbilityData> Abilities => abilities.AsReadOnly();

    public void ReplaceAbilities(IEnumerable<AbilityData> newAbilities)
    {
        abilities.Clear();

        if (newAbilities == null)
            return;

        foreach (var a in newAbilities)
        {
            if (a == null)
                continue;

            abilities.Add(new AbilityData(a.ability, a.unlocked));
        }
    }
}