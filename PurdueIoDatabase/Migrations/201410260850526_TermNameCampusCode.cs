namespace PurdueIoDb.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class TermNameCampusCode : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Campus", "Code", c => c.String(maxLength: 12));
            AddColumn("dbo.Terms", "Name", c => c.String());
            CreateIndex("dbo.Campus", "Code");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Campus", new[] { "Code" });
            DropColumn("dbo.Terms", "Name");
            DropColumn("dbo.Campus", "Code");
        }
    }
}
