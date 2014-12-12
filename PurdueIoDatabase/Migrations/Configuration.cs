namespace PurdueIoDb.Migrations
{
	using PurdueIoDb;
	using PurdueIoDb.Catalog;
	using System;
	using System.Collections.Generic;
	using System.Data.Entity;
	using System.Data.Entity.Migrations;
	using System.Linq;

    public sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
        }
    }
}
