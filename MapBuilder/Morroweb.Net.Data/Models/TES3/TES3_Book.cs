﻿namespace Morroweb.Net.Data.Models.TES3
{
    public class TES3_Book
    {
        public string type { get; set; }
        public string flags { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string script { get; set; }
        public string mesh { get; set; }
        public string icon { get; set; }
        public string enchanting { get; set; }
        public string text { get; set; }
        public TES3_Book_Data data { get; set; }
    }

}
