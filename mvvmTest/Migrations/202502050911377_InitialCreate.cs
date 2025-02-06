namespace CoATool.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.CoAs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, unicode: false),
                        Description = c.String(unicode: false),
                        CompleteTime = c.DateTime(nullable: false, precision: 0),
                        Culture = c.Int(nullable: false),
                        IsCollect = c.Boolean(nullable: false),
                        Rate = c.Single(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.CoAs");
        }
    }
}
