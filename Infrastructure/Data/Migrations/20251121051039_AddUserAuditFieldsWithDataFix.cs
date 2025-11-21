using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAuditFieldsWithDataFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add columns as nullable first
            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Users",
                type: "int",
                nullable: true); // Make it nullable initially

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Users",
                type: "datetime2",
                nullable: true); // Make it nullable initially

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedDate",
                table: "Users",
                type: "datetime2",
                nullable: true);

            // Step 2: Update existing data to have valid CreatedBy values
            migrationBuilder.Sql(@"
            -- Find the first admin user or create a default
            DECLARE @AdminId INT;
            SELECT TOP 1 @AdminId = Id FROM Users WHERE IsActive = 1 ORDER BY Id;
            
            -- If no users exist, we'll handle this case
            IF @AdminId IS NULL
            BEGIN
                -- Insert a default admin user if no users exist
                INSERT INTO Users (FirstName, LastName, Email, PanNumber, PasswordHash, IsActive, CreatedDate)
                VALUES ('System', 'Admin', 'admin@system.com', 'ADMIN1234X', '', 1, GETUTCDATE());
                SET @AdminId = SCOPE_IDENTITY();
            END
            
            -- Update all existing users to have valid CreatedBy values
            UPDATE Users SET CreatedBy = @AdminId, CreatedDate = ISNULL(CreatedAt, GETUTCDATE()) WHERE CreatedBy IS NULL;
            
            -- Update the admin user to be created by themselves
            UPDATE Users SET CreatedBy = Id WHERE Id = @AdminId AND CreatedBy != Id;
        ");

            // Step 3: Now make CreatedBy required
            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1); // Default to user 1 (admin)

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedDate",
                table: "Users",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // Step 4: Create the foreign key constraint
            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Step 5: Fix Investment precision (from your previous migration)
            migrationBuilder.AlterColumn<decimal>(
                name: "NavRate",
                table: "Investments",
                type: "decimal(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "InvestAmount",
                table: "Investments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NumberOfUnits",
                table: "Investments",
                type: "decimal(18,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedBy",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UpdatedDate",
                table: "Users");

            // Revert Investment precision changes
            migrationBuilder.AlterColumn<decimal>(
                name: "NavRate",
                table: "Investments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "NumberOfUnits",
                table: "Investments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,4)");
        }
    }
}
