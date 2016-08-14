namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCourseCatalog : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Buildings",
                c => new
                    {
                        BuildingId = c.Guid(nullable: false),
                        Name = c.String(),
                        ShortCode = c.String(maxLength: 8),
                        Campus_CampusId = c.Guid(),
                    })
                .PrimaryKey(t => t.BuildingId, clustered: false)
                .ForeignKey("dbo.Campus", t => t.Campus_CampusId)
                .Index(t => t.ShortCode)
                .Index(t => t.Campus_CampusId);
            
            CreateTable(
                "dbo.Campus",
                c => new
                    {
                        CampusId = c.Guid(nullable: false),
                        Name = c.String(),
                        ZipCode = c.String(maxLength: 5),
                    })
                .PrimaryKey(t => t.CampusId, clustered: false);
            
            CreateTable(
                "dbo.Rooms",
                c => new
                    {
                        RoomId = c.Guid(nullable: false),
                        Number = c.String(),
                        Building_BuildingId = c.Guid(),
                    })
                .PrimaryKey(t => t.RoomId, clustered: false)
                .ForeignKey("dbo.Buildings", t => t.Building_BuildingId)
                .Index(t => t.Building_BuildingId);
            
            CreateTable(
                "dbo.Sections",
                c => new
                    {
                        SectionId = c.Guid(nullable: false),
                        CRN = c.String(maxLength: 10),
                        RegistrationStatus = c.Int(nullable: false),
                        Type = c.String(),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                        StartTime = c.DateTimeOffset(nullable: false, precision: 7),
                        Duration = c.Time(nullable: false, precision: 7),
                        Capacity = c.Int(nullable: false),
                        Enrolled = c.Int(nullable: false),
                        RemainingSpace = c.Int(nullable: false),
                        WaitlistCapacity = c.Int(nullable: false),
                        WaitlistCount = c.Int(nullable: false),
                        WaitlistSpace = c.Int(nullable: false),
                        Class_ClassId = c.Guid(),
                        Instructor_InstructorId = c.Guid(),
                        Room_RoomId = c.Guid(),
                    })
                .PrimaryKey(t => t.SectionId, clustered: false)
                .ForeignKey("dbo.Classes", t => t.Class_ClassId)
                .ForeignKey("dbo.Instructors", t => t.Instructor_InstructorId)
                .ForeignKey("dbo.Rooms", t => t.Room_RoomId)
                .Index(t => t.CRN)
                .Index(t => t.Class_ClassId)
                .Index(t => t.Instructor_InstructorId)
                .Index(t => t.Room_RoomId);
            
            CreateTable(
                "dbo.Classes",
                c => new
                    {
                        ClassId = c.Guid(nullable: false),
                        Campus_CampusId = c.Guid(),
                        Course_CourseId = c.Guid(),
                        Term_TermId = c.Guid(),
                    })
                .PrimaryKey(t => t.ClassId, clustered: false)
                .ForeignKey("dbo.Campus", t => t.Campus_CampusId)
                .ForeignKey("dbo.Courses", t => t.Course_CourseId)
                .ForeignKey("dbo.Terms", t => t.Term_TermId)
                .Index(t => t.Campus_CampusId)
                .Index(t => t.Course_CourseId)
                .Index(t => t.Term_TermId);
            
            CreateTable(
                "dbo.Courses",
                c => new
                    {
                        CourseId = c.Guid(nullable: false),
                        Number = c.String(maxLength: 8),
                        Title = c.String(),
                        CreditHours = c.Double(nullable: false),
                        Description = c.String(),
                        Subject_SubjectId = c.Guid(),
                    })
                .PrimaryKey(t => t.CourseId, clustered: false)
                .ForeignKey("dbo.Subjects", t => t.Subject_SubjectId)
                .Index(t => t.Number)
                .Index(t => t.Subject_SubjectId);
            
            CreateTable(
                "dbo.Subjects",
                c => new
                    {
                        SubjectId = c.Guid(nullable: false),
                        Name = c.String(),
                        Abbreviation = c.String(maxLength: 5),
                    })
                .PrimaryKey(t => t.SubjectId, clustered: false)
                .Index(t => t.Abbreviation);
            
            CreateTable(
                "dbo.Terms",
                c => new
                    {
                        TermId = c.Guid(nullable: false),
                        TermCode = c.String(maxLength: 12),
                        StartDate = c.DateTime(nullable: false),
                        EndDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.TermId, clustered: false)
                .Index(t => t.TermCode);
            
            CreateTable(
                "dbo.Instructors",
                c => new
                    {
                        InstructorId = c.Guid(nullable: false),
                        Name = c.String(),
                        Email = c.String(maxLength: 254),
                    })
                .PrimaryKey(t => t.InstructorId, clustered: false)
                .Index(t => t.Email);
            
            CreateTable(
                "dbo.AspNetRoles",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Name = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id, clustered: false)
                .Index(t => t.Name, unique: true, name: "RoleNameIndex");
            
            CreateTable(
                "dbo.AspNetUserRoles",
                c => new
                    {
                        UserId = c.String(nullable: false, maxLength: 128),
                        RoleId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.UserId, t.RoleId })
                .ForeignKey("dbo.AspNetRoles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.RoleId);
            
            CreateTable(
                "dbo.AspNetUsers",
                c => new
                    {
                        Id = c.String(nullable: false, maxLength: 128),
                        Hometown = c.String(),
                        Email = c.String(maxLength: 256),
                        EmailConfirmed = c.Boolean(nullable: false),
                        PasswordHash = c.String(),
                        SecurityStamp = c.String(),
                        PhoneNumber = c.String(),
                        PhoneNumberConfirmed = c.Boolean(nullable: false),
                        TwoFactorEnabled = c.Boolean(nullable: false),
                        LockoutEndDateUtc = c.DateTime(),
                        LockoutEnabled = c.Boolean(nullable: false),
                        AccessFailedCount = c.Int(nullable: false),
                        UserName = c.String(nullable: false, maxLength: 256),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.UserName, unique: true, name: "UserNameIndex");
            
            CreateTable(
                "dbo.AspNetUserClaims",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 128),
                        ClaimType = c.String(),
                        ClaimValue = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.AspNetUserLogins",
                c => new
                    {
                        LoginProvider = c.String(nullable: false, maxLength: 128),
                        ProviderKey = c.String(nullable: false, maxLength: 128),
                        UserId = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => new { t.LoginProvider, t.ProviderKey, t.UserId })
                .ForeignKey("dbo.AspNetUsers", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.AspNetUserRoles", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserLogins", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserClaims", "UserId", "dbo.AspNetUsers");
            DropForeignKey("dbo.AspNetUserRoles", "RoleId", "dbo.AspNetRoles");
            DropForeignKey("dbo.Rooms", "Building_BuildingId", "dbo.Buildings");
            DropForeignKey("dbo.Sections", "Room_RoomId", "dbo.Rooms");
            DropForeignKey("dbo.Sections", "Instructor_InstructorId", "dbo.Instructors");
            DropForeignKey("dbo.Classes", "Term_TermId", "dbo.Terms");
            DropForeignKey("dbo.Sections", "Class_ClassId", "dbo.Classes");
            DropForeignKey("dbo.Classes", "Course_CourseId", "dbo.Courses");
            DropForeignKey("dbo.Courses", "Subject_SubjectId", "dbo.Subjects");
            DropForeignKey("dbo.Classes", "Campus_CampusId", "dbo.Campus");
            DropForeignKey("dbo.Buildings", "Campus_CampusId", "dbo.Campus");
            DropIndex("dbo.AspNetUserLogins", new[] { "UserId" });
            DropIndex("dbo.AspNetUserClaims", new[] { "UserId" });
            DropIndex("dbo.AspNetUsers", "UserNameIndex");
            DropIndex("dbo.AspNetUserRoles", new[] { "RoleId" });
            DropIndex("dbo.AspNetUserRoles", new[] { "UserId" });
            DropIndex("dbo.AspNetRoles", "RoleNameIndex");
            DropIndex("dbo.Instructors", new[] { "Email" });
            DropIndex("dbo.Terms", new[] { "TermCode" });
            DropIndex("dbo.Subjects", new[] { "Abbreviation" });
            DropIndex("dbo.Courses", new[] { "Subject_SubjectId" });
            DropIndex("dbo.Courses", new[] { "Number" });
            DropIndex("dbo.Classes", new[] { "Term_TermId" });
            DropIndex("dbo.Classes", new[] { "Course_CourseId" });
            DropIndex("dbo.Classes", new[] { "Campus_CampusId" });
            DropIndex("dbo.Sections", new[] { "Room_RoomId" });
            DropIndex("dbo.Sections", new[] { "Instructor_InstructorId" });
            DropIndex("dbo.Sections", new[] { "Class_ClassId" });
            DropIndex("dbo.Sections", new[] { "CRN" });
            DropIndex("dbo.Rooms", new[] { "Building_BuildingId" });
            DropIndex("dbo.Buildings", new[] { "Campus_CampusId" });
            DropIndex("dbo.Buildings", new[] { "ShortCode" });
            DropTable("dbo.AspNetUserLogins");
            DropTable("dbo.AspNetUserClaims");
            DropTable("dbo.AspNetUsers");
            DropTable("dbo.AspNetUserRoles");
            DropTable("dbo.AspNetRoles");
            DropTable("dbo.Instructors");
            DropTable("dbo.Terms");
            DropTable("dbo.Subjects");
            DropTable("dbo.Courses");
            DropTable("dbo.Classes");
            DropTable("dbo.Sections");
            DropTable("dbo.Rooms");
            DropTable("dbo.Campus");
            DropTable("dbo.Buildings");
        }
    }
}
