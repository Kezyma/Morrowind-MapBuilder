﻿namespace Morroweb.Net.Data.Models.TES3
{
    public class TES3_Ingredient
    {
        public string type { get; set; }
        public string flags { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string script { get; set; }
        public string mesh { get; set; }
        public string icon { get; set; }
        public TES3_Ingredient_Data data { get; set; }
    }

}
