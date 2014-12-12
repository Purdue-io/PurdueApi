namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BitwiseDOW : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Meetings", "DaysOfWeek", c => c.Byte(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Meetings", "DaysOfWeek");
        }
    }
}
