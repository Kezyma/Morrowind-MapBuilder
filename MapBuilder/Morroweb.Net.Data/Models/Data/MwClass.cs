using System.ComponentModel.DataAnnotations.Schema;

namespace Morroweb.Net.Data.Models.Data
{
    public class MwClass
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Services { get; set; }
        public string Flags { get; set; }

        public string SpecialisationId { get; set; }
        public virtual MwSpecialisation Specialisation { get; set; }

        public string Attribute1Id { get; set; }
        public virtual MwAttribute Attribute1 { get; set; }
        public string Attribute2Id { get; set; }
        public virtual MwAttribute Attribute2 { get; set; }

        [NotMapped]
        public List<MwAttribute> Attributes => new() { Attribute1, Attribute2 };

        public string MajorSkill1Id { get; set; }
        public virtual MwSkill MajorSkill1 { get; set; }
        public string MajorSkill2Id { get; set; }
        public virtual MwSkill MajorSkill2 { get; set; }
        public string MajorSkill3Id { get; set; }
        public virtual MwSkill MajorSkill3 { get; set; }
        public string MajorSkill4Id { get; set; }
        public virtual MwSkill MajorSkill4 { get; set; }
        public string MajorSkill5Id { get; set; }
        public virtual MwSkill MajorSkill5 { get; set; }

        [NotMapped]
        public List<MwSkill> MajorSkills => new() { MajorSkill1, MajorSkill2, MajorSkill3, MajorSkill4, MajorSkill5 };

        public string MinorSkill1Id { get; set; }
        public virtual MwSkill MinorSkill1 { get; set; }
        public string MinorSkill2Id { get; set; }
        public virtual MwSkill MinorSkill2 { get; set; }
        public string MinorSkill3Id { get; set; }
        public virtual MwSkill MinorSkill3 { get; set; }
        public string MinorSkill4Id { get; set; }
        public virtual MwSkill MinorSkill4 { get; set; }
        public string MinorSkill5Id { get; set; }
        public virtual MwSkill MinorSkill5 { get; set; }

        [NotMapped]
        public List<MwSkill> MinorSkills => new() { MinorSkill1, MinorSkill2, MinorSkill3, MinorSkill4, MinorSkill5 };
    }
}
