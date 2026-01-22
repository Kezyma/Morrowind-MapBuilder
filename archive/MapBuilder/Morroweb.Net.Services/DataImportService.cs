using Microsoft.EntityFrameworkCore;
using Morroweb.Net.Data.Context;
using Morroweb.Net.Data.Models.Data;
using Morroweb.Net.Data.Models.TES3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Morroweb.Net.Services
{
    public class DataImportService
    {
        public DataImportService(MorrowebDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private readonly MorrowebDbContext _dbContext;

        public void ImportRecords(string jsonPath)
        {
            if (Directory.Exists(jsonPath))
            {
                // Find all the json files.
                var files = Directory.GetFiles(jsonPath);
                var jsonFiles = files.Where(x => x.EndsWith(".json")).Select(x => Path.GetFileNameWithoutExtension(x)).ToList();

                // Calculate a load order.
                var forceOrder = new[] { "Morrowind", "Tribunal", "Bloodmoon" };
                var finalOrder = forceOrder.ToList();
                finalOrder.AddRange(jsonFiles.Where(x => !forceOrder.Contains(x)));

                // Import any enums that will be needed later.
                ImportEnums();

                // Add or update each record to match the load order.
                foreach (var key in finalOrder)
                {
                    // Deserialize the export into a dynamic.
                    dynamic jsonData = JsonConvert.DeserializeObject(File.ReadAllText(Path.Combine(jsonPath, $"{key}.json")));
                    string currentDialogueTopic = string.Empty, currentDialogueType = string.Empty;
                    int currentSkillIndex = 0;
                    // Iterate over all the records.
                    foreach (var record in (JArray)jsonData)
                    {
                        // Convert the game record to a usable class.
                        var objectType = record.Value<string>("type");
                        var className = $"TES3_{objectType}";
                        var classType = System.Reflection.Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == className);
                        var converted = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(record), classType);

                        // Import each record.
                        switch (objectType)
                        {
                            case "GameSetting":
                                ImportGMST(converted as TES3_GameSetting);
                                break;
                            case "GlobalVariable":
                                ImportGlobal(converted as TES3_GlobalVariable);
                                break;
                            case "Script":
                                ImportScript(converted as TES3_Script);
                                break;
                            case "Skill":
                                ImportSkill(converted as TES3_Skill, currentSkillIndex);
                                currentSkillIndex++;
                                break;
                            case "Class":
                                ImportClass(converted as TES3_Class);
                                break;
                            case "MagicEffect":
                                ImportMagicEffect(converted as TES3_MagicEffect);
                                break;
                            case "Spell":
                                ImportSpell(converted as TES3_Spell);
                                break;
                            case "Race":
                                ImportRace(converted as TES3_Race);
                                break;
                            case "Birthsign":
                                ImportBirthsign(converted as TES3_Birthsign);
                                break;
                            case "Faction":
                                var faction = (converted as TES3_Faction);
                                break;
                            case "Enchanting":
                                var enchanting = (converted as TES3_Enchanting);
                                break;
                            case "MiscItem":
                                var miscItem = (converted as TES3_MiscItem);
                                break;
                            case "Weapon":
                                var weapon = (converted as TES3_Weapon);
                                break;
                            case "Book":
                                var book = (converted as TES3_Book);
                                break;
                            case "Alchemy":
                                var alchemy = (converted as TES3_Alchemy);
                                break;
                            case "Apparatus":
                                var apparatus = (converted as TES3_Apparatus);
                                break;
                            case "Armor":
                                var armour = (converted as TES3_Armor);
                                break;
                            case "Clothing":
                                var clothing = (converted as TES3_Clothing);
                                break;
                            case "Ingredient":
                                var ingredient = (converted as TES3_Ingredient);
                                break;
                            case "Lockpick":
                                var lockpick = (converted as TES3_Lockpick);
                                break;
                            case "Probe":
                                var probe = (converted as TES3_Probe);
                                break;
                            case "RepairItem":
                                var repairItem = (converted as TES3_RepairItem);
                                break;
                            case "Container":
                                var container = (converted as TES3_Container);
                                break;
                            case "Creature":
                                var creature = (converted as TES3_Creature);
                                break;
                            case "Npc":
                                var npc = (converted as TES3_Npc);
                                break;
                            case "LeveledCreature":
                                var leveledCreature = (converted as TES3_LeveledCreature);
                                break;
                            case "LeveledItem":
                                var leveledItem = (converted as TES3_LeveledItem);
                                break;
                            case "Region":
                                var region = (converted as TES3_Region);
                                break;
                            case "Cell":
                                var cell = (converted as TES3_Cell);
                                var cellId = "";
                                if (!string.IsNullOrWhiteSpace(cell.region)) cellId += $"{cell.region}_{cell.data.grid[0]}_{cell.data.grid[1]}";
                                else cellId = $"{cell.name}";
                                break;
                            case "Dialogue":
                                var dialogue = (converted as TES3_Dialogue);
                                currentDialogueTopic = dialogue.id;
                                break;
                            case "DialogueInfo":
                                var dialoguInfo = (converted as TES3_DialogueInfo);
                                break;
                            default: break;
                        }
                    }
                }
            }
        }

        public void ImportEnums()
        {
            if (!_dbContext.Attributes.Any())
            {
                _dbContext.AddRange(new List<MwAttribute>
                {
                    new() { Id = "strength", Name = "Strength", Index = 0 },
                    new() { Id = "intelligence", Name = "Intelligence", Index = 1 },
                    new() { Id = "willpower", Name = "Willpower", Index = 2 },
                    new() { Id = "agility", Name = "Agility", Index = 3 },
                    new() { Id = "speed", Name = "Speed", Index = 4 },
                    new() { Id = "endurance", Name = "Endurance", Index = 5},
                    new() { Id = "personality", Name = "Personality", Index = 6 },
                    new() { Id = "luck", Name = "Luck", Index = 7 }
                });
                _dbContext.SaveChanges();
            }

            if (!_dbContext.Specialisations.Any())
            {
                _dbContext.AddRange(new List<MwSpecialisation>
                {
                    new() { Id = "invalid", Name = "Invalid", Index = -1 },
                    new() { Id = "combat", Name = "Combat", Index = 0 },
                    new() { Id = "magic", Name = "Magic", Index = 1 },
                    new() { Id = "stealth", Name = "Stealth", Index = 2 }
                });
                _dbContext.SaveChanges();
            }
        }

        public void ImportGMST(TES3_GameSetting gmst)
        {
            var currentGmst = _dbContext.GMSTs.FirstOrDefault(x => x.Id == gmst.id);
            if (currentGmst == null)
            {
                currentGmst = new MwGMST { Id = gmst.id };
                _dbContext.Add(currentGmst);
            }
            currentGmst.Value = gmst.value.data.ToString();
            _dbContext.SaveChanges();
        }
        public void ImportGlobal(TES3_GlobalVariable global)
        {
            var currentGlobal = _dbContext.Globals.FirstOrDefault(x => x.Id == global.id);
            if (currentGlobal == null)
            {
                currentGlobal = new MwGlobal { Id = global.id };
                _dbContext.Add(currentGlobal);
            }
            currentGlobal.Value = global.value;
            _dbContext.SaveChanges();
        }
        public void ImportScript(TES3_Script script)
        {
            var currentScript = _dbContext.Scripts.FirstOrDefault(x => x.Id == script.id);
            if (currentScript == null)
            {
                currentScript = new MwScript { Id = script.id };
                _dbContext.Add(currentScript);
            }
            currentScript.Text = script.text;
            _dbContext.SaveChanges();
        }
        public void ImportSkill(TES3_Skill skill, int currentSkillIndex)
        {
            var currentSkill = _dbContext.Skills.FirstOrDefault(x => x.Id == skill.skill_id);
            if (currentSkill == null)
            {
                currentSkill = new MwSkill { Id = skill.skill_id };
                _dbContext.Add(currentSkill);
            }
            currentSkill.Description = skill.description;
            currentSkill.Name = _dbContext.GMSTs.First(x => x.Id.ToLower() == $"sSkill{skill.skill_id}".ToLower()).Value;
            currentSkill.AttributeId = _dbContext.Attributes.First(x => x.Index == skill.data.governing_attribute).Id;
            currentSkill.SpecialisationId = _dbContext.Specialisations.First(x => x.Index == skill.data.specialization).Id;
            currentSkill.Index = currentSkillIndex;
            _dbContext.SaveChanges();
        }
        public void ImportClass(TES3_Class classObj)
        {
            var currentClass = _dbContext.Classes.FirstOrDefault(x => x.Id == classObj.id);
            if (currentClass == null)
            {
                currentClass = new MwClass { Id = classObj.id };
                _dbContext.Add(currentClass);
            }
            currentClass.Name = classObj.name;
            currentClass.Description = classObj.description;
            currentClass.Attribute1Id = classObj.data.attribute1;
            currentClass.Attribute2Id = classObj.data.attribute2;
            currentClass.SpecialisationId = classObj.data.specialization;
            currentClass.MajorSkill1Id = classObj.data.major1;
            currentClass.MajorSkill2Id = classObj.data.major2;
            currentClass.MajorSkill3Id = classObj.data.major3;
            currentClass.MajorSkill4Id = classObj.data.major4;
            currentClass.MajorSkill5Id = classObj.data.major5;
            currentClass.MinorSkill1Id = classObj.data.minor1;
            currentClass.MinorSkill2Id = classObj.data.minor2;
            currentClass.MinorSkill3Id = classObj.data.minor3;
            currentClass.MinorSkill4Id = classObj.data.minor4;
            currentClass.MinorSkill5Id = classObj.data.minor5;
            currentClass.Flags = classObj.data.flags;
            currentClass.Services = classObj.data.services;
            _dbContext.SaveChanges();
        }
        public void ImportMagicEffect(TES3_MagicEffect magicEffect)
        {
            var currentEffect = _dbContext.MagicEffects.FirstOrDefault(x => x.Id == magicEffect.effect_id);
            if (currentEffect == null)
            {
                currentEffect = new MwMagicEffect { Id = magicEffect.effect_id };
                _dbContext.Add(currentEffect);
            }
            currentEffect.Name = _dbContext.GMSTs.FirstOrDefault(x => x.Id.ToLower() == $"sEffect{magicEffect.effect_id}".ToLower())?.Value ?? null;
            if (currentEffect.Name == null)
            {
                var lookupKeys = new[]
                {
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("magicka", "spellpoints"),
                                        $"sEffect{magicEffect.effect_id}Disease".ToLower(),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("ghost", "ancestralghost"),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("skeleton", "skeletalminion"),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("twilight", "wingedtwilight"),
                                        $"sEffect{magicEffect.effect_id}s".ToLower(),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("corprus", "corpus"),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("wolf", "creature01"),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("bear", "creature02"),
                                        $"sEffect{magicEffect.effect_id}".ToLower().Replace("bonewolf", "creature03"),
                                    };
                currentEffect.Name = _dbContext.GMSTs.FirstOrDefault(x => lookupKeys.Contains(x.Id.ToLower()))?.Value ?? null;
            }
            currentEffect.Description = magicEffect.description;
            currentEffect.Icon = magicEffect.icon.Split("\\").Last().Split(".").First();
            currentEffect.Size = magicEffect.data.size;
            currentEffect.SizeCap = magicEffect.data.size_cap;
            currentEffect.SchoolId = magicEffect.data.school;
            currentEffect.BaseCost = magicEffect.data.base_cost;
            currentEffect.Flags = magicEffect.data.flags;
            _dbContext.SaveChanges();
        }
        public void ImportSpell(TES3_Spell spell)
        {
            var currentSpell = _dbContext.Spells.Include(x => x.MagicEffectInstances).FirstOrDefault(x => x.Id == spell.id);
            if (currentSpell == null)
            {
                currentSpell = new MwSpell { Id = spell.id, MagicEffectInstances = new List<MwMagicEffectInstance>() };
                _dbContext.Add(currentSpell);
            }
            currentSpell.Name = spell.name;
            currentSpell.Type = spell.data.spell_type;
            currentSpell.Cost = spell.data.cost;
            currentSpell.Flags = spell.data.flags;
            for (int sei = 0; sei < spell.effects.Length; sei++)
            {
                var spellEffect = spell.effects[sei];
                var spellEffectId = $"{spell.id}[{sei}]";
                var currentSpellEffect = currentSpell.MagicEffectInstances.FirstOrDefault(x => x.Id == spellEffectId);
                if (currentSpellEffect == null)
                {
                    currentSpellEffect = new MwMagicEffectInstance { Id = spellEffectId, SpellId = spell.id };
                    currentSpell.MagicEffectInstances.Add(currentSpellEffect);
                }
                currentSpellEffect.MagicEffectId = spellEffect.magic_effect;
                currentSpellEffect.SkillId = !string.IsNullOrWhiteSpace(spellEffect.skill) && spellEffect.skill != "None" ? spellEffect.skill : null;
                currentSpellEffect.AttributeId = !string.IsNullOrWhiteSpace(spellEffect.attribute) && spellEffect.attribute != "None" ? spellEffect.attribute : null;
                currentSpellEffect.Range = spellEffect.range;
                currentSpellEffect.Area = spellEffect.area;
                currentSpellEffect.Duration = spellEffect.duration;
                currentSpellEffect.MinMagnitude = spellEffect.min_magnitude;
                currentSpellEffect.MaxMagnitude = spellEffect.max_magnitude;
            }
            _dbContext.SaveChanges();
        }
        public void ImportRace(TES3_Race race)
        {
            var currentRace = _dbContext.Races
                                    .Include(x => x.SkillBonuses)
                                    .Include(x => x.Spells)
                                    .Include(x => x.Attributes)
                                    .Include(x => x.Stats)
                                    .FirstOrDefault(x => x.Id == race.id);
            if (currentRace == null)
            {
                currentRace = new MwRace
                {
                    Id = race.id,
                    SkillBonuses = [],
                    Spells = [],
                    Attributes = [],
                    Stats = new[]
                    {
                                            new MwRaceStat { Id = $"{race.id}[height]", Type = nameof(MwRace.Height) },
                                            new MwRaceStat { Id = $"{race.id}[weight]", Type = nameof(MwRace.Weight) }
                                        }
                };
                _dbContext.Add(currentRace);
            }
            currentRace.Name = race.name;
            currentRace.Description = race.description;
            currentRace.Flags = race.data.flags;
            currentRace.Weight.Male = race.data.weight[0];
            currentRace.Weight.Female = race.data.weight[1];
            currentRace.Height.Male = race.data.height[0];
            currentRace.Height.Female = race.data.height[1];
            for (int rsi = 0; rsi < race.spells.Length; rsi++)
            {
                var raceSpell = race.spells[rsi];
                var raceSpellId = $"{race.id}[{rsi}]";
                var currentRaceSpell = currentRace.Spells.FirstOrDefault(x => x.Id == raceSpellId);
                if (currentRaceSpell == null)
                {
                    currentRaceSpell = new MwRaceSpell { Id = raceSpellId, RaceId = race.id };
                    currentRace.Spells.Add(currentRaceSpell);
                }
                currentRaceSpell.SpellId = raceSpell;
            }
            var attr = new[] { "strength", "intelligence", "willpower", "agility", "speed", "endurance", "personality", "luck" };
            foreach (var a in attr)
            {
                var attrVal = (int[])typeof(TES3_Race_Data).GetProperty(a).GetValue(race.data);
                var raceAttrId = $"{race.id}[{a}]";
                var currentRaceAttr = currentRace.Attributes.FirstOrDefault(x => x.Id == raceAttrId);
                if (currentRaceAttr == null)
                {
                    currentRaceAttr = new MwRaceAttribute { Id = raceAttrId, RaceId = race.id, AttributeId = a };
                    currentRace.Attributes.Add(currentRaceAttr);
                }
                currentRaceAttr.Male = attrVal[0];
                currentRaceAttr.Female = attrVal[1];
            }
            for (int rski = 0; rski < 6; rski++)
            {
                var bonusId = $"{race.id}[{rski}]";
                var bonusSkillId = (string)typeof(TES3_Race_Skill_Bonuses).GetProperty($"skill_{rski}").GetValue(race.data.skill_bonuses);
                if (bonusSkillId != "None")
                {
                    var currentBonus = currentRace.SkillBonuses.FirstOrDefault(x => x.Id == bonusId);
                    if (currentBonus == null)
                    {
                        currentBonus = new MwRaceSkillBonus { Id = bonusId, RaceId = race.id };
                        currentRace.SkillBonuses.Add(currentBonus);
                    }
                    currentBonus.SkillId = bonusSkillId;
                    currentBonus.Bonus = (int)typeof(TES3_Race_Skill_Bonuses).GetProperty($"bonus_{rski}").GetValue(race.data.skill_bonuses);
                }
            }
            _dbContext.SaveChanges();
        }
        public void ImportBirthsign(TES3_Birthsign birthsign)
        {
            var currentBs = _dbContext.Birthsigns.Include(x => x.Spells).FirstOrDefault(x => x.Id == birthsign.id);
            if (currentBs == null)
            {
                currentBs = new MwBirthsign { Id = birthsign.id, Spells = [] };
                _dbContext.Add(currentBs);
            }
            currentBs.Texture = birthsign.texture.Split("\\").Last().Split(".").First();
            currentBs.Description = birthsign.description;
            currentBs.Name = birthsign.name;
            for (int bsi = 0; bsi < birthsign.spells.Length; bsi++)
            {
                var bsSpell = birthsign.spells[bsi];
                var bsSpellId = $"{birthsign.id}[{bsi}]";
                var currentBsSpell = currentBs.Spells.FirstOrDefault(x => x.Id == bsSpellId);
                if (currentBsSpell == null)
                {
                    currentBsSpell = new MwBirthsignSpell { Id = bsSpellId, BirthsignId = birthsign.id };
                    currentBs.Spells.Add(currentBsSpell);
                }
                currentBsSpell.SpellId = bsSpell;
            }
            _dbContext.SaveChanges();
        }
    }
}
