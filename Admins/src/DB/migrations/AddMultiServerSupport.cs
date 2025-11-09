using FluentMigrator;

namespace Admins.Database.Migrations;

[Migration(1762730148)]
public class AddMultiServerSupport : Migration
{
    public override void Up()
    {
        Alter.Table("Admins")
            .AddColumn("Servers").AsFixedLengthString(16384).NotNullable();

        Alter.Table("Groups")
            .AddColumn("Servers").AsFixedLengthString(16384).NotNullable();

        Create.Table("Servers")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("IP").AsString(45).NotNullable()
            .WithColumn("Port").AsInt32().NotNullable()
            .WithColumn("GUID").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Servers");

        Delete.Column("Servers").FromTable("Groups");

        Delete.Column("Servers").FromTable("Admins");
    }
}