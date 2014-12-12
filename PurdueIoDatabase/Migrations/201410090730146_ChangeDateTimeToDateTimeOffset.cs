namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeDateTimeToDateTimeOffset : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Meetings", "StartDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Meetings", "EndDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Sections", "StartDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Sections", "EndDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Terms", "StartDate", c => c.DateTimeOffset(nullable: false, precision: 7));
            AlterColumn("dbo.Terms", "EndDate", c => c.DateTimeOffset(nullable: false, precision: 7));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Terms", "EndDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Terms", "StartDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Sections", "EndDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Sections", "StartDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Meetings", "EndDate", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Meetings", "StartDate", c => c.DateTime(nullable: false));
        }
    }
}
