using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsistentePersonal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            // Spec 002, RNF-5: emails are compared ignoring letter case.
            // EF Core cannot model an index on lower("Email"), so this is written by hand.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Users_LowerEmail\" ON \"Users\" (lower(\"Email\"));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Users_LowerEmail\";");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
