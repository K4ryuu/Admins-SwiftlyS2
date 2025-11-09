using FluentMigrator;

namespace Admins.Database.Migrations;

[Migration(1762471031)]
public class InitTable : Migration
{
    public override void Up()
    {
        Create.Table("Admins")
            .WithColumn("Id").AsInt64().PrimaryKey().NotNullable()
            .WithColumn("SteamId64").AsInt64().Unique().NotNullable()
            .WithColumn("Username").AsString().Unique().NotNullable()
            .WithColumn("Permissions").AsFixedLengthString(16384).NotNullable()
            .WithColumn("Groups").AsFixedLengthString(16384).NotNullable()
            .WithColumn("Immunity").AsInt32().NotNullable();

        Create.Table("Groups")
            .WithColumn("Id").AsInt64().PrimaryKey().NotNullable()
            .WithColumn("Name").AsString().Unique().NotNullable()
            .WithColumn("Permissions").AsFixedLengthString(16384).NotNullable()
            .WithColumn("Immunity").AsInt32().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Admins");
        Delete.Table("Groups");
    }
}