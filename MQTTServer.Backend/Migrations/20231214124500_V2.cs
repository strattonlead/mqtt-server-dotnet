using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MQTTServer.Backend.Migrations
{
    public partial class V2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomProperties",
                table: "MqttUsers",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomProperties",
                table: "MqttUsers");
        }
    }
}
