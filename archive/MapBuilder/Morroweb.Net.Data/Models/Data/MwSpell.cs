namespace Morroweb.Net.Data.Models.Data
{
    public class MwSpell
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<MwMagicEffectInstance> MagicEffectInstances { get; set; }
        public string Type { get; set; }
        public int Cost { get; set; }
        public string Flags { get; set; }

    }
}
