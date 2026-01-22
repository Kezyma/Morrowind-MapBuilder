namespace Morroweb.Net.Data.Models.Data
{
    public class MwMagicEffectInstance
    {
        public string Id { get; set; }
        public string? SpellId { get; set; }
        public virtual MwSpell Spell { get; set; }

        public string MagicEffectId { get; set; }
        public virtual MwMagicEffect MagicEffect { get; set; }
        public string? SkillId { get; set; }
        public virtual MwSkill Skill { get; set; }
        public string? AttributeId { get; set; }
        public virtual MwAttribute Attribute { get; set; }

        public string Range { get; set; }
        public int Area { get; set; }
        public int Duration { get; set; }
        public int MinMagnitude { get; set; }
        public int MaxMagnitude { get; set; }
    }
}
