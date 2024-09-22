﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProcessTrackerService.Infrastructure.Data;

#nullable disable

namespace ProcessTrackerService.Infrastructure.Migrations
{
    [DbContext(typeof(PTServiceContext))]
    [Migration("20240831190703_TagSessionSummary")]
    partial class TagSessionSummary
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.7");

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.Filter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("FieldType")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<string>("FieldValue")
                        .HasMaxLength(500)
                        .HasColumnType("TEXT");

                    b.Property<string>("FilterType")
                        .HasMaxLength(50)
                        .HasColumnType("TEXT");

                    b.Property<bool>("Inactive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(false);

                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TagId");

                    b.ToTable("Filters");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.Setting", b =>
                {
                    b.Property<string>("SettingName")
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("TEXT");

                    b.HasKey("SettingName");

                    b.ToTable("Settings");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.Tag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Inactive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasDefaultValue(false);

                    b.Property<string>("Name")
                        .HasMaxLength(200)
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.TagSession", b =>
                {
                    b.Property<int>("SessionId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("EndTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastUpdateTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("StartTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("SessionId");

                    b.HasIndex("TagId");

                    b.ToTable("TagSessions");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.TagSessionSummary", b =>
                {
                    b.Property<int>("SummaryId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("CreationDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Day")
                        .HasColumnType("TEXT");

                    b.Property<double>("Seconds")
                        .HasColumnType("REAL");

                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("SummaryId");

                    b.HasIndex("TagId");

                    b.ToTable("TagSessionSummary");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.Filter", b =>
                {
                    b.HasOne("ProcessTrackerService.Core.Entities.Tag", "Tag")
                        .WithMany("Filters")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.TagSession", b =>
                {
                    b.HasOne("ProcessTrackerService.Core.Entities.Tag", "Tag")
                        .WithMany("TagSessions")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.TagSessionSummary", b =>
                {
                    b.HasOne("ProcessTrackerService.Core.Entities.Tag", "Tag")
                        .WithMany("TagSessionSummaries")
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("ProcessTrackerService.Core.Entities.Tag", b =>
                {
                    b.Navigation("Filters");

                    b.Navigation("TagSessionSummaries");

                    b.Navigation("TagSessions");
                });
#pragma warning restore 612, 618
        }
    }
}
