﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Morroweb.Net.Data.Context;

#nullable disable

namespace Morroweb.Net.Migrations
{
    [DbContext(typeof(MorrowebDbContext))]
    [Migration("20240109130605_DbUpdate_MagicEffects")]
    partial class DbUpdate_MagicEffects
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwAttribute", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Attributes");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwClass", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Attribute1Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Attribute2Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Flags")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("MajorSkill1Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MajorSkill2Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MajorSkill3Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MajorSkill4Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MajorSkill5Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MinorSkill1Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MinorSkill2Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MinorSkill3Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MinorSkill4Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("MinorSkill5Id")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Services")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SpecialisationId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("Attribute1Id");

                    b.HasIndex("Attribute2Id");

                    b.HasIndex("MajorSkill1Id");

                    b.HasIndex("MajorSkill2Id");

                    b.HasIndex("MajorSkill3Id");

                    b.HasIndex("MajorSkill4Id");

                    b.HasIndex("MajorSkill5Id");

                    b.HasIndex("MinorSkill1Id");

                    b.HasIndex("MinorSkill2Id");

                    b.HasIndex("MinorSkill3Id");

                    b.HasIndex("MinorSkill4Id");

                    b.HasIndex("MinorSkill5Id");

                    b.HasIndex("SpecialisationId");

                    b.ToTable("Classes");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwGMST", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("GMSTs");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwGlobal", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<float>("Value")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.ToTable("Globals");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwMagicEffect", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<float>("BaseCost")
                        .HasColumnType("real");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Flags")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Icon")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SchoolId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<float>("Size")
                        .HasColumnType("real");

                    b.Property<float>("SizeCap")
                        .HasColumnType("real");

                    b.Property<float>("Speed")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.HasIndex("SchoolId");

                    b.ToTable("MagicEffects");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwScript", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Scripts");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwSkill", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("AttributeId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("SpecialisationId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("AttributeId");

                    b.HasIndex("SpecialisationId");

                    b.ToTable("Skills");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwSpecialisation", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("nvarchar(450)");

                    b.Property<int>("Index")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Specialisations");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwClass", b =>
                {
                    b.HasOne("Morroweb.Net.Models.Data.MwAttribute", "Attribute1")
                        .WithMany()
                        .HasForeignKey("Attribute1Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwAttribute", "Attribute2")
                        .WithMany()
                        .HasForeignKey("Attribute2Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MajorSkill1")
                        .WithMany()
                        .HasForeignKey("MajorSkill1Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MajorSkill2")
                        .WithMany()
                        .HasForeignKey("MajorSkill2Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MajorSkill3")
                        .WithMany()
                        .HasForeignKey("MajorSkill3Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MajorSkill4")
                        .WithMany()
                        .HasForeignKey("MajorSkill4Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MajorSkill5")
                        .WithMany()
                        .HasForeignKey("MajorSkill5Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MinorSkill1")
                        .WithMany()
                        .HasForeignKey("MinorSkill1Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MinorSkill2")
                        .WithMany()
                        .HasForeignKey("MinorSkill2Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MinorSkill3")
                        .WithMany()
                        .HasForeignKey("MinorSkill3Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MinorSkill4")
                        .WithMany()
                        .HasForeignKey("MinorSkill4Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "MinorSkill5")
                        .WithMany()
                        .HasForeignKey("MinorSkill5Id")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSpecialisation", "Specialisation")
                        .WithMany()
                        .HasForeignKey("SpecialisationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Attribute1");

                    b.Navigation("Attribute2");

                    b.Navigation("MajorSkill1");

                    b.Navigation("MajorSkill2");

                    b.Navigation("MajorSkill3");

                    b.Navigation("MajorSkill4");

                    b.Navigation("MajorSkill5");

                    b.Navigation("MinorSkill1");

                    b.Navigation("MinorSkill2");

                    b.Navigation("MinorSkill3");

                    b.Navigation("MinorSkill4");

                    b.Navigation("MinorSkill5");

                    b.Navigation("Specialisation");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwMagicEffect", b =>
                {
                    b.HasOne("Morroweb.Net.Models.Data.MwSkill", "School")
                        .WithMany()
                        .HasForeignKey("SchoolId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("School");
                });

            modelBuilder.Entity("Morroweb.Net.Models.Data.MwSkill", b =>
                {
                    b.HasOne("Morroweb.Net.Models.Data.MwAttribute", "Attribute")
                        .WithMany()
                        .HasForeignKey("AttributeId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Morroweb.Net.Models.Data.MwSpecialisation", "Specialisation")
                        .WithMany()
                        .HasForeignKey("SpecialisationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Attribute");

                    b.Navigation("Specialisation");
                });
#pragma warning restore 612, 618
        }
    }
}
