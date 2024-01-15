namespace Morroweb.Net.Data.Models.Data
{
    public class MwRaceAttribute
    {
        public string Id { get; set; }
        public string RaceId { get; set; }
        public virtual MwRace Race { get; set; }
        public string AttributeId { get; set; }
        public virtual MwAttribute Attribute { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
    }
}