namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MeetingsAdded : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.InstructorSections", "Instructor_InstructorId", "dbo.Instructors");
            DropForeignKey("dbo.InstructorSections", "Section_SectionId", "dbo.Sections");
            DropForeignKey("dbo.Sections", "Room_RoomId", "dbo.Rooms");
            DropIndex("dbo.Sections", new[] { "Room_RoomId" });
            DropIndex("dbo.InstructorSections", new[] { "Instructor_InstructorId" });
            DropIndex("dbo.InstructorSections", new[] { "Section_SectionId" });
            CreateTable(
                "dbo.Meetings",
                c => new
                    {
                        MeetingId = c.Guid(nullable: false),
                        Type = c.String(),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        StartTime = c.DateTimeOffset(nullable: false, precision: 7),
                        Duration = c.Time(nullable: false, precision: 7),
                        Room_RoomId = c.Guid(),
                        Section_SectionId = c.Guid(),
                    })
                .PrimaryKey(t => t.MeetingId, clustered: false)
                .ForeignKey("dbo.Rooms", t => t.Room_RoomId)
                .ForeignKey("dbo.Sections", t => t.Section_SectionId)
                .Index(t => t.Room_RoomId)
                .Index(t => t.Section_SectionId);
            
            CreateTable(
                "dbo.InstructorMeetings",
                c => new
                    {
                        Instructor_InstructorId = c.Guid(nullable: false),
                        Meeting_MeetingId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.Instructor_InstructorId, t.Meeting_MeetingId }, clustered: false)
                .ForeignKey("dbo.Instructors", t => t.Instructor_InstructorId, cascadeDelete: true)
                .ForeignKey("dbo.Meetings", t => t.Meeting_MeetingId, cascadeDelete: true)
                .Index(t => t.Instructor_InstructorId)
                .Index(t => t.Meeting_MeetingId);
            
            DropColumn("dbo.Sections", "StartTime");
            DropColumn("dbo.Sections", "Duration");
            DropColumn("dbo.Sections", "Room_RoomId");
            DropTable("dbo.InstructorSections");
        }
        
        public override void Down()
        {
            CreateTable(
                "dbo.InstructorSections",
                c => new
                    {
                        Instructor_InstructorId = c.Guid(nullable: false),
                        Section_SectionId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.Instructor_InstructorId, t.Section_SectionId });
            
            AddColumn("dbo.Sections", "Room_RoomId", c => c.Guid());
            AddColumn("dbo.Sections", "Duration", c => c.Time(nullable: false, precision: 7));
            AddColumn("dbo.Sections", "StartTime", c => c.DateTimeOffset(nullable: false, precision: 7));
            DropForeignKey("dbo.Meetings", "Section_SectionId", "dbo.Sections");
            DropForeignKey("dbo.Meetings", "Room_RoomId", "dbo.Rooms");
            DropForeignKey("dbo.InstructorMeetings", "Meeting_MeetingId", "dbo.Meetings");
            DropForeignKey("dbo.InstructorMeetings", "Instructor_InstructorId", "dbo.Instructors");
            DropIndex("dbo.InstructorMeetings", new[] { "Meeting_MeetingId" });
            DropIndex("dbo.InstructorMeetings", new[] { "Instructor_InstructorId" });
            DropIndex("dbo.Meetings", new[] { "Section_SectionId" });
            DropIndex("dbo.Meetings", new[] { "Room_RoomId" });
            DropTable("dbo.InstructorMeetings");
            DropTable("dbo.Meetings");
            CreateIndex("dbo.InstructorSections", "Section_SectionId");
            CreateIndex("dbo.InstructorSections", "Instructor_InstructorId");
            CreateIndex("dbo.Sections", "Room_RoomId");
            AddForeignKey("dbo.Sections", "Room_RoomId", "dbo.Rooms", "RoomId");
            AddForeignKey("dbo.InstructorSections", "Section_SectionId", "dbo.Sections", "SectionId", cascadeDelete: true);
            AddForeignKey("dbo.InstructorSections", "Instructor_InstructorId", "dbo.Instructors", "InstructorId", cascadeDelete: true);
        }
    }
}
