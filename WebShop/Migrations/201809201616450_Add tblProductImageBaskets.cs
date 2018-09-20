namespace WebShop.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddtblProductImageBaskets : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.tblProductImageBaskets",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(maxLength: 150),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.tblProductImageBaskets");
        }
    }
}
