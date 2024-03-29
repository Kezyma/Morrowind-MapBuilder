﻿namespace Morroweb.Net.Data.Models.TES3
{
    public class TES3_Cell_Reference
    {
        public int mast_index { get; set; }
        public int refr_index { get; set; }
        public string id { get; set; }
        public bool temporary { get; set; }
        public float?[] translation { get; set; }
        public float?[] rotation { get; set; }
        public float? scale { get; set; }
        public TES3_Cell_Destination destination { get; set; }
        public string owner { get; set; }
        public int health_left { get; set; }
        public int object_count { get; set; }
        public int lock_level { get; set; }
        public string trap { get; set; }
        public string key { get; set; }
        public string owner_faction { get; set; }
        public long owner_faction_rank { get; set; }
        public string soul { get; set; }
        public string owner_global { get; set; }
        public int charge_left { get; set; }
        public int blocked { get; set; }
    }

}
