using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VehicleRegistry.Infrastructure.Persistence.Migrations;

/// <summary>
/// Link a service record to the Identity user who created it.
/// Existing rows referencing deleted users are preserved with a null creator.
/// </summary>
public partial class AddServiceRecordCreatorReference : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Data written before the FK existed may reference an unknown user.
        migrationBuilder.Sql(@"UPDATE r SET CreatedById = NULL
            FROM ServiceRecords AS r
            LEFT JOIN AspNetUsers AS u ON u.Id = r.CreatedById
            WHERE r.CreatedById IS NOT NULL AND u.Id IS NULL;");

        migrationBuilder.AlterColumn<string>(
            name: "CreatedById", table: "ServiceRecords", type: "nvarchar(450)",
            nullable: true, oldClrType: typeof(string), oldType: "nvarchar(max)",
            oldNullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ServiceRecords_CreatedById", table: "ServiceRecords",
            column: "CreatedById");

        migrationBuilder.AddForeignKey(
            name: "FK_ServiceRecords_AspNetUsers_CreatedById",
            table: "ServiceRecords", column: "CreatedById",
            principalTable: "AspNetUsers", principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ServiceRecords_AspNetUsers_CreatedById", table: "ServiceRecords");
        migrationBuilder.DropIndex(
            name: "IX_ServiceRecords_CreatedById", table: "ServiceRecords");
        migrationBuilder.AlterColumn<string>(
            name: "CreatedById", table: "ServiceRecords", type: "nvarchar(max)",
            nullable: true, oldClrType: typeof(string), oldType: "nvarchar(450)",
            oldNullable: true);
    }
}
