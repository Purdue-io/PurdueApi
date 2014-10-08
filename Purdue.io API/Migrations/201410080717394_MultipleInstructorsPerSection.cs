namespace PurdueIo.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MultipleInstructorsPerSection : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Sections", "Instructor_InstructorId", "dbo.Instructors");
            DropIndex("dbo.Sections", new[] { "Instructor_InstructorId" });
            CreateTable(
                "dbo.InstructorSections",
                c => new
                    {
                        Instructor_InstructorId = c.Guid(nullable: false),
                        Section_SectionId = c.Guid(nullable: false),
                    })
                .PrimaryKey(t => new { t.Instructor_InstructorId, t.Section_SectionId })
                .ForeignKey("dbo.Instructors", t => t.Instructor_InstructorId, cascadeDelete: true)
                .ForeignKey("dbo.Sections", t => t.Section_SectionId, cascadeDelete: true)
                .Index(t => t.Instructor_InstructorId)
                .Index(t => t.Section_SectionId);
            
            DropColumn("dbo.Sections", "Instructor_InstructorId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Sections", "Instructor_InstructorId", c => c.Guid());
            DropForeignKey("dbo.InstructorSections", "Section_SectionId", "dbo.Sections");
            DropForeignKey("dbo.InstructorSections", "Instructor_InstructorId", "dbo.Instructors");
            DropIndex("dbo.InstructorSections", new[] { "Section_SectionId" });
            DropIndex("dbo.InstructorSections", new[] { "Instructor_InstructorId" });
            DropTable("dbo.InstructorSections");
            CreateIndex("dbo.Sections", "Instructor_InstructorId");
            AddForeignKey("dbo.Sections", "Instructor_InstructorId", "dbo.Instructors", "InstructorId");
        }
    }
}
