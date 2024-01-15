namespace Morroweb.Net.Data.Models.Data
{
    public class MwMagicEffect
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Description { get; set; }
        public string SchoolId { get; set; }
        public virtual MwSkill School { get; set; }
        public float BaseCost { get; set; }
        public string Flags { get; set; }
        public float Speed { get; set; }
        public float Size { get; set; }
        public float SizeCap { get; set; }
    }
}
