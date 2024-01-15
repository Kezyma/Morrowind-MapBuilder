using Microsoft.EntityFrameworkCore;
using Morroweb.Net.Data.Models.Data;

namespace Morroweb.Net.Data.Context
{
    public class MorrowebDbContext(DbContextOptions options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var relationship in builder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
            foreach (var property in builder.Model.GetEntityTypes().SelectMany(t => t.GetProperties()).Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetColumnType("decimal(18, 9)");
            }
        }

        // Preconfigured
        public virtual DbSet<MwSpecialisation> Specialisations { get; set; }
        public virtual DbSet<MwAttribute> Attributes { get; set; }

        // Imported
        public virtual DbSet<MwGMST> GMSTs { get; set; }
        public virtual DbSet<MwGlobal> Globals { get; set; }
        public virtual DbSet<MwScript> Scripts { get; set; }
        public virtual DbSet<MwSkill> Skills { get; set; }
        public virtual DbSet<MwClass> Classes { get; set; }
        public virtual DbSet<MwMagicEffect> MagicEffects { get; set; }
        public virtual DbSet<MwMagicEffectInstance> MagicEffectInstances { get; set; }
        public virtual DbSet<MwSpell> Spells { get; set; }
        public virtual DbSet<MwRace> Races { get; set; }
        public virtual DbSet<MwRaceSpell> RaceSpells { get; set; }
        public virtual DbSet<MwRaceAttribute> RaceAttributes { get; set; }
        public virtual DbSet<MwRaceStat> RaceStats { get; set; }
        public virtual DbSet<MwRaceSkillBonus> RaceSkillBonuses { get; set; }
        public virtual DbSet<MwBirthsign> Birthsigns { get; set; }
        public virtual DbSet<MwBirthsignSpell> BirthsignSpells { get; set; }
    }
}
