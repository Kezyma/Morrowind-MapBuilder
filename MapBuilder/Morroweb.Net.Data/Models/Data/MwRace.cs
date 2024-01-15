using System.ComponentModel.DataAnnotations.Schema;

namespace Morroweb.Net.Data.Models.Data
{
    public class MwRace
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Flags { get; set; }
        public virtual ICollection<MwRaceSpell> Spells { get; set; }
        public virtual ICollection<MwRaceAttribute> Attributes { get; set; }
        public virtual ICollection<MwRaceSkillBonus> SkillBonuses { get; set; }
        public virtual ICollection<MwRaceStat> Stats { get; set; }

        [NotMapped]
        public virtual MwRaceStat Weight => Stats.FirstOrDefault(x => x.Type == nameof(Weight));
        [NotMapped]
        public virtual MwRaceStat Height => Stats.FirstOrDefault(x => x.Type == nameof(Height));
    }
}
