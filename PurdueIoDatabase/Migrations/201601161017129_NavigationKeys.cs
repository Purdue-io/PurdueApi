namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class NavigationKeys : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Buildings", "Campus_CampusId", "dbo.Campus");
            DropForeignKey("dbo.Rooms", "Building_BuildingId", "dbo.Buildings");
            DropForeignKey("dbo.Meetings", "Section_SectionId", "dbo.Sections");
            DropForeignKey("dbo.Sections", "Class_ClassId", "dbo.Classes");
            DropForeignKey("dbo.Classes", "Campus_CampusId", "dbo.Campus");
            DropForeignKey("dbo.Classes", "Course_CourseId", "dbo.Courses");
            DropForeignKey("dbo.Classes", "Term_TermId", "dbo.Terms");
            DropForeignKey("dbo.Courses", "Subject_SubjectId", "dbo.Subjects");
            DropIndex("dbo.Buildings", new[] { "Campus_CampusId" });
            DropIndex("dbo.Rooms", new[] { "Building_BuildingId" });
            DropIndex("dbo.Meetings", new[] { "Section_SectionId" });
            DropIndex("dbo.Sections", new[] { "Class_ClassId" });
            DropIndex("dbo.Classes", new[] { "Campus_CampusId" });
            DropIndex("dbo.Classes", new[] { "Course_CourseId" });
            DropIndex("dbo.Classes", new[] { "Term_TermId" });
            DropIndex("dbo.Courses", new[] { "Subject_SubjectId" });
            RenameColumn(table: "dbo.Buildings", name: "Campus_CampusId", newName: "CampusId");
            RenameColumn(table: "dbo.Rooms", name: "Building_BuildingId", newName: "BuildingId");
            RenameColumn(table: "dbo.Meetings", name: "Room_RoomId", newName: "RoomId");
            RenameColumn(table: "dbo.Meetings", name: "Section_SectionId", newName: "SectionId");
            RenameColumn(table: "dbo.Sections", name: "Class_ClassId", newName: "ClassId");
            RenameColumn(table: "dbo.Classes", name: "Campus_CampusId", newName: "CampusId");
            RenameColumn(table: "dbo.Classes", name: "Course_CourseId", newName: "CourseId");
            RenameColumn(table: "dbo.Classes", name: "Term_TermId", newName: "TermId");
            RenameColumn(table: "dbo.Courses", name: "Subject_SubjectId", newName: "SubjectId");
            RenameIndex(table: "dbo.Meetings", name: "IX_Room_RoomId", newName: "IX_RoomId");
            AlterColumn("dbo.Buildings", "CampusId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Rooms", "BuildingId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Meetings", "SectionId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Sections", "ClassId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Classes", "CampusId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Classes", "CourseId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Classes", "TermId", c => c.Guid(nullable: false));
            AlterColumn("dbo.Courses", "SubjectId", c => c.Guid(nullable: false));
            CreateIndex("dbo.Buildings", "CampusId");
            CreateIndex("dbo.Rooms", "BuildingId");
            CreateIndex("dbo.Meetings", "SectionId");
            CreateIndex("dbo.Sections", "ClassId");
            CreateIndex("dbo.Classes", "CourseId");
            CreateIndex("dbo.Classes", "TermId");
            CreateIndex("dbo.Classes", "CampusId");
            CreateIndex("dbo.Courses", "SubjectId");
            AddForeignKey("dbo.Buildings", "CampusId", "dbo.Campus", "CampusId", cascadeDelete: true);
            AddForeignKey("dbo.Rooms", "BuildingId", "dbo.Buildings", "BuildingId", cascadeDelete: true);
            AddForeignKey("dbo.Meetings", "SectionId", "dbo.Sections", "SectionId", cascadeDelete: true);
            AddForeignKey("dbo.Sections", "ClassId", "dbo.Classes", "ClassId", cascadeDelete: true);
            AddForeignKey("dbo.Classes", "CampusId", "dbo.Campus", "CampusId", cascadeDelete: true);
            AddForeignKey("dbo.Classes", "CourseId", "dbo.Courses", "CourseId", cascadeDelete: true);
            AddForeignKey("dbo.Classes", "TermId", "dbo.Terms", "TermId", cascadeDelete: true);
            AddForeignKey("dbo.Courses", "SubjectId", "dbo.Subjects", "SubjectId", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Courses", "SubjectId", "dbo.Subjects");
            DropForeignKey("dbo.Classes", "TermId", "dbo.Terms");
            DropForeignKey("dbo.Classes", "CourseId", "dbo.Courses");
            DropForeignKey("dbo.Classes", "CampusId", "dbo.Campus");
            DropForeignKey("dbo.Sections", "ClassId", "dbo.Classes");
            DropForeignKey("dbo.Meetings", "SectionId", "dbo.Sections");
            DropForeignKey("dbo.Rooms", "BuildingId", "dbo.Buildings");
            DropForeignKey("dbo.Buildings", "CampusId", "dbo.Campus");
            DropIndex("dbo.Courses", new[] { "SubjectId" });
            DropIndex("dbo.Classes", new[] { "CampusId" });
            DropIndex("dbo.Classes", new[] { "TermId" });
            DropIndex("dbo.Classes", new[] { "CourseId" });
            DropIndex("dbo.Sections", new[] { "ClassId" });
            DropIndex("dbo.Meetings", new[] { "SectionId" });
            DropIndex("dbo.Rooms", new[] { "BuildingId" });
            DropIndex("dbo.Buildings", new[] { "CampusId" });
            AlterColumn("dbo.Courses", "SubjectId", c => c.Guid());
            AlterColumn("dbo.Classes", "TermId", c => c.Guid());
            AlterColumn("dbo.Classes", "CourseId", c => c.Guid());
            AlterColumn("dbo.Classes", "CampusId", c => c.Guid());
            AlterColumn("dbo.Sections", "ClassId", c => c.Guid());
            AlterColumn("dbo.Meetings", "SectionId", c => c.Guid());
            AlterColumn("dbo.Rooms", "BuildingId", c => c.Guid());
            AlterColumn("dbo.Buildings", "CampusId", c => c.Guid());
            RenameIndex(table: "dbo.Meetings", name: "IX_RoomId", newName: "IX_Room_RoomId");
            RenameColumn(table: "dbo.Courses", name: "SubjectId", newName: "Subject_SubjectId");
            RenameColumn(table: "dbo.Classes", name: "TermId", newName: "Term_TermId");
            RenameColumn(table: "dbo.Classes", name: "CourseId", newName: "Course_CourseId");
            RenameColumn(table: "dbo.Classes", name: "CampusId", newName: "Campus_CampusId");
            RenameColumn(table: "dbo.Sections", name: "ClassId", newName: "Class_ClassId");
            RenameColumn(table: "dbo.Meetings", name: "SectionId", newName: "Section_SectionId");
            RenameColumn(table: "dbo.Meetings", name: "RoomId", newName: "Room_RoomId");
            RenameColumn(table: "dbo.Rooms", name: "BuildingId", newName: "Building_BuildingId");
            RenameColumn(table: "dbo.Buildings", name: "CampusId", newName: "Campus_CampusId");
            CreateIndex("dbo.Courses", "Subject_SubjectId");
            CreateIndex("dbo.Classes", "Term_TermId");
            CreateIndex("dbo.Classes", "Course_CourseId");
            CreateIndex("dbo.Classes", "Campus_CampusId");
            CreateIndex("dbo.Sections", "Class_ClassId");
            CreateIndex("dbo.Meetings", "Section_SectionId");
            CreateIndex("dbo.Rooms", "Building_BuildingId");
            CreateIndex("dbo.Buildings", "Campus_CampusId");
            AddForeignKey("dbo.Courses", "Subject_SubjectId", "dbo.Subjects", "SubjectId");
            AddForeignKey("dbo.Classes", "Term_TermId", "dbo.Terms", "TermId");
            AddForeignKey("dbo.Classes", "Course_CourseId", "dbo.Courses", "CourseId");
            AddForeignKey("dbo.Classes", "Campus_CampusId", "dbo.Campus", "CampusId");
            AddForeignKey("dbo.Sections", "Class_ClassId", "dbo.Classes", "ClassId");
            AddForeignKey("dbo.Meetings", "Section_SectionId", "dbo.Sections", "SectionId");
            AddForeignKey("dbo.Rooms", "Building_BuildingId", "dbo.Buildings", "BuildingId");
            AddForeignKey("dbo.Buildings", "Campus_CampusId", "dbo.Campus", "CampusId");
        }
    }
}
