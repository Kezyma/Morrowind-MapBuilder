namespace Morroweb.Net.Data.Models.Data
{
    public class MwRaceSpell
    {
        public string Id { get; set; }
        public string RaceId { get; set; }
        public virtual MwRace Race { get; set; }
        public string SpellId { get; set; }
        public virtual MwSpell Spell { get; set; }
    }
}
