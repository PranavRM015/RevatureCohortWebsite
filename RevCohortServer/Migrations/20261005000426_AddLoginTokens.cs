using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RevCohortServer.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginTokens",
                columns: table => new
                {
                    Token = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DiscordId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Expiration = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginTokens", x => x.Token);
                    table.ForeignKey(
                        name: "FK_LoginTokens_Users_DiscordId",
                        column: x => x.DiscordId,
                        principalTable: "Users",
                        principalColumn: "DiscordId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginTokens_DiscordId",
                table: "LoginTokens",
                column: "DiscordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginTokens");
        }
    }
}
