namespace Morroweb.Net.Data.Models.Data
{
    public class MwRaceStat
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string RaceId { get; set; }
        public virtual MwRace Race { get; set; }
        public float Male { get; set; }
        public float Female { get; set; }
    }
}