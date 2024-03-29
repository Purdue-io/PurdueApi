﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PurdueIo.Database;

#nullable disable

namespace Database.Migrations.Npgsql.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20231112213223_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0-rc.2.23480.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("PurdueIo.Database.Models.Building", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CampusId")
                        .HasColumnType("uuid");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("ShortCode")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.HasIndex("ShortCode");

                    b.HasIndex("CampusId", "ShortCode")
                        .IsUnique();

                    b.ToTable("Buildings");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Campus", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Code")
                        .HasMaxLength(12)
                        .HasColumnType("character varying(12)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("ZipCode")
                        .HasMaxLength(5)
                        .HasColumnType("character varying(5)");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Campuses");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Class", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("CampusId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("CourseId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("TermId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("CampusId");

                    b.HasIndex("CourseId");

                    b.HasIndex("TermId");

                    b.ToTable("Classes");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Course", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<double>("CreditHours")
                        .HasColumnType("double precision");

                    b.Property<string>("Description")
                        .HasColumnType("text");

                    b.Property<string>("Number")
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)");

                    b.Property<Guid>("SubjectId")
                        .HasColumnType("uuid");

                    b.Property<string>("Title")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Number");

                    b.HasIndex("SubjectId");

                    b.HasIndex("Title");

                    b.ToTable("Courses");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Instructor", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Email")
                        .HasMaxLength(254)
                        .HasColumnType("character varying(254)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Email");

                    b.HasIndex("Name");

                    b.ToTable("Instructors");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Meeting", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<byte>("DaysOfWeek")
                        .HasColumnType("smallint");

                    b.Property<TimeSpan>("Duration")
                        .HasColumnType("interval");

                    b.Property<DateOnly?>("EndDate")
                        .HasColumnType("date");

                    b.Property<Guid?>("RoomId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("SectionId")
                        .HasColumnType("uuid");

                    b.Property<DateOnly?>("StartDate")
                        .HasColumnType("date");

                    b.Property<TimeOnly?>("StartTime")
                        .HasColumnType("time without time zone");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("RoomId");

                    b.HasIndex("SectionId");

                    b.ToTable("Meetings");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.MeetingInstructor", b =>
                {
                    b.Property<Guid>("MeetingId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("InstructorId")
                        .HasColumnType("uuid");

                    b.HasKey("MeetingId", "InstructorId");

                    b.HasIndex("InstructorId");

                    b.ToTable("MeetingInstructor");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Room", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("BuildingId")
                        .HasColumnType("uuid");

                    b.Property<string>("Number")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("BuildingId");

                    b.HasIndex("Number");

                    b.ToTable("Rooms");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Section", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ClassId")
                        .HasColumnType("uuid");

                    b.Property<string>("Crn")
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)");

                    b.Property<DateOnly?>("EndDate")
                        .HasColumnType("date");

                    b.Property<DateOnly?>("StartDate")
                        .HasColumnType("date");

                    b.Property<string>("Type")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ClassId");

                    b.HasIndex("Crn");

                    b.ToTable("Sections");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Subject", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Abbreviation")
                        .HasMaxLength(6)
                        .HasColumnType("character varying(6)");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("Abbreviation")
                        .IsUnique();

                    b.HasIndex("Name");

                    b.ToTable("Subjects");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Term", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Code")
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)");

                    b.Property<DateOnly?>("EndDate")
                        .HasColumnType("date");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<DateOnly?>("StartDate")
                        .HasColumnType("date");

                    b.HasKey("Id");

                    b.HasIndex("Code")
                        .IsUnique();

                    b.HasIndex("Name")
                        .IsUnique();

                    b.ToTable("Terms");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Building", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Campus", "Campus")
                        .WithMany("Buildings")
                        .HasForeignKey("CampusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Campus");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Class", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Campus", "Campus")
                        .WithMany("Classes")
                        .HasForeignKey("CampusId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PurdueIo.Database.Models.Course", "Course")
                        .WithMany("Classes")
                        .HasForeignKey("CourseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PurdueIo.Database.Models.Term", "Term")
                        .WithMany("Classes")
                        .HasForeignKey("TermId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Campus");

                    b.Navigation("Course");

                    b.Navigation("Term");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Course", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Subject", "Subject")
                        .WithMany("Courses")
                        .HasForeignKey("SubjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subject");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Meeting", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Room", "Room")
                        .WithMany()
                        .HasForeignKey("RoomId");

                    b.HasOne("PurdueIo.Database.Models.Section", "Section")
                        .WithMany("Meetings")
                        .HasForeignKey("SectionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Room");

                    b.Navigation("Section");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.MeetingInstructor", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Instructor", "Instructor")
                        .WithMany()
                        .HasForeignKey("InstructorId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("PurdueIo.Database.Models.Meeting", "Meeting")
                        .WithMany()
                        .HasForeignKey("MeetingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Instructor");

                    b.Navigation("Meeting");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Room", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Building", "Building")
                        .WithMany("Rooms")
                        .HasForeignKey("BuildingId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Building");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Section", b =>
                {
                    b.HasOne("PurdueIo.Database.Models.Class", "Class")
                        .WithMany("Sections")
                        .HasForeignKey("ClassId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Class");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Building", b =>
                {
                    b.Navigation("Rooms");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Campus", b =>
                {
                    b.Navigation("Buildings");

                    b.Navigation("Classes");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Class", b =>
                {
                    b.Navigation("Sections");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Course", b =>
                {
                    b.Navigation("Classes");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Section", b =>
                {
                    b.Navigation("Meetings");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Subject", b =>
                {
                    b.Navigation("Courses");
                });

            modelBuilder.Entity("PurdueIo.Database.Models.Term", b =>
                {
                    b.Navigation("Classes");
                });
#pragma warning restore 612, 618
        }
    }
}
