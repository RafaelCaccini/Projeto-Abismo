using System;

[Serializable]
public class AbilityData
{
    public SkillType ability;
    public bool unlocked;

    public AbilityData() { }

    public AbilityData(SkillType ability, bool unlocked)
    {
        this.ability = ability;
        this.unlocked = unlocked;
    }
}
