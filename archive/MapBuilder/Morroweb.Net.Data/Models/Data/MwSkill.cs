namespace Morroweb.Net.Data.Models.Data
{
    public class MwSkill
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Index { get; set; }
        public string Description { get; set; }
        public string AttributeId { get; set; }
        public virtual MwAttribute Attribute { get; set; }
        public string SpecialisationId { get; set; }
        public virtual MwSpecialisation Specialisation { get; set; }
    }
}
