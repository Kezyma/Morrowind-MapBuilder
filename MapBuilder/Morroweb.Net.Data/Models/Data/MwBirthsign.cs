namespace Morroweb.Net.Data.Models.Data
{
    public class MwBirthsign
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Texture { get; set; }
        public string Description { get; set; }
        public virtual ICollection<MwBirthsignSpell> Spells { get; set; }
    }
}
