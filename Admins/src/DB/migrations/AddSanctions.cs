using FluentMigrator;

namespace Admins.Database.Migrations;

[Migration(1764087672)]
public class AddSanctions : Migration
{
    public override void Up()
    {
        Create.Table("Sanctions")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("SteamId64").AsInt64().NotNullable()
            .WithColumn("PlayerName").AsString().NotNullable()
            .WithColumn("PlayerIp").AsString().NotNullable()
            .WithColumn("SanctionType").AsInt32().NotNullable()
            .WithColumn("ExpiresAt").AsInt64().NotNullable()
            .WithColumn("Length").AsInt64().NotNullable()
            .WithColumn("Reason").AsString().NotNullable()
            .WithColumn("AdminSteamId64").AsInt64().NotNullable()
            .WithColumn("AdminName").AsString().NotNullable()
            .WithColumn("Server").AsString().NotNullable()
            .WithColumn("Global").AsBoolean().NotNullable();

        Create.Table("Bans")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("SteamId64").AsInt64().NotNullable()
            .WithColumn("PlayerName").AsString().NotNullable()
            .WithColumn("PlayerIp").AsString().NotNullable()
            .WithColumn("BanType").AsInt32().NotNullable()
            .WithColumn("ExpiresAt").AsInt64().NotNullable()
            .WithColumn("Length").AsInt64().NotNullable()
            .WithColumn("Reason").AsString().NotNullable()
            .WithColumn("AdminSteamId64").AsInt64().NotNullable()
            .WithColumn("AdminName").AsString().NotNullable()
            .WithColumn("Server").AsString().NotNullable()
            .WithColumn("GlobalBan").AsBoolean().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("Sanctions");
        Delete.Table("Bans");
    }
}