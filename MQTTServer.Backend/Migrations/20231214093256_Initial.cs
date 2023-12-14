using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MQTTServer.Backend.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MqttUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    Password = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MqttUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublishTopics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PublishTopics_MqttUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "MqttUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscribeTopics",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Topic = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscribeTopics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscribeTopics_MqttUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "MqttUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PublishTopics_Topic",
                table: "PublishTopics",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_PublishTopics_UserId",
                table: "PublishTopics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscribeTopics_Topic",
                table: "SubscribeTopics",
                column: "Topic");

            migrationBuilder.CreateIndex(
                name: "IX_SubscribeTopics_UserId",
                table: "SubscribeTopics",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PublishTopics");

            migrationBuilder.DropTable(
                name: "SubscribeTopics");

            migrationBuilder.DropTable(
                name: "MqttUsers");
        }
    }
}
