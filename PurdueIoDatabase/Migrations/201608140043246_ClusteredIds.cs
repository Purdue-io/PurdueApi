namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ClusteredIds : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Buildings", "BuildingClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Campus", "CampusClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Rooms", "RoomClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Meetings", "MeetingClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Instructors", "InstructorClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.InstructorMeetings", "InstructorMeetingClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Sections", "SectionClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Classes", "ClassClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Courses", "CourseClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Subjects", "SubjectClusterId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.Terms", "TermClusterId", c => c.Int(nullable: false, identity: true));
            CreateIndex("dbo.Buildings", "BuildingClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Campus", "CampusClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Rooms", "RoomClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Meetings", "MeetingClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Instructors", "InstructorClusterId", unique: true, clustered: true);
            CreateIndex("dbo.InstructorMeetings", "InstructorMeetingClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Sections", "SectionClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Classes", "ClassClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Courses", "CourseClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Subjects", "SubjectClusterId", unique: true, clustered: true);
            CreateIndex("dbo.Terms", "TermClusterId", unique: true, clustered: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Terms", new[] { "TermClusterId" });
            DropIndex("dbo.Subjects", new[] { "SubjectClusterId" });
            DropIndex("dbo.Courses", new[] { "CourseClusterId" });
            DropIndex("dbo.Classes", new[] { "ClassClusterId" });
            DropIndex("dbo.Sections", new[] { "SectionClusterId" });
            DropIndex("dbo.Instructors", new[] { "InstructorClusterId" });
            DropIndex("dbo.InstructorMeetings", new[] { "InstructorMeetingClusterId" });
            DropIndex("dbo.Meetings", new[] { "MeetingClusterId" });
            DropIndex("dbo.Rooms", new[] { "RoomClusterId" });
            DropIndex("dbo.Campus", new[] { "CampusClusterId" });
            DropIndex("dbo.Buildings", new[] { "BuildingClusterId" });
            DropColumn("dbo.Terms", "TermClusterId");
            DropColumn("dbo.Subjects", "SubjectClusterId");
            DropColumn("dbo.Courses", "CourseClusterId");
            DropColumn("dbo.Classes", "ClassClusterId");
            DropColumn("dbo.Sections", "SectionClusterId");
            DropColumn("dbo.Instructors", "InstructorClusterId");
            DropColumn("dbo.InstructorMeetings", "InstructorMeetingClusterId");
            DropColumn("dbo.Meetings", "MeetingClusterId");
            DropColumn("dbo.Rooms", "RoomClusterId");
            DropColumn("dbo.Campus", "CampusClusterId");
            DropColumn("dbo.Buildings", "BuildingClusterId");
        }
    }
}
