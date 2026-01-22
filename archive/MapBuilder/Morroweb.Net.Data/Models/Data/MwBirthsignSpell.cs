namespace Morroweb.Net.Data.Models.Data
{
    public class MwBirthsignSpell
    {
        public string Id { get; set; }
        public string BirthsignId { get; set; }
        public virtual MwBirthsign Birthsign { get; set; }
        public string SpellId { get; set; }
        public virtual MwSpell Spell { get; set; }
    }
}
