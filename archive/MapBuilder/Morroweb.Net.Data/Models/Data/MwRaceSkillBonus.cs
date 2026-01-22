namespace Morroweb.Net.Data.Models.Data
{
    public class MwRaceSkillBonus
    {
        public string Id { get; set; }
        public string RaceId { get; set; }
        public virtual MwRace Race { get; set; }
        public string SkillId { get; set; }
        public virtual MwSkill Skill { get; set; }
        public int Bonus { get; set; }
    }
}
