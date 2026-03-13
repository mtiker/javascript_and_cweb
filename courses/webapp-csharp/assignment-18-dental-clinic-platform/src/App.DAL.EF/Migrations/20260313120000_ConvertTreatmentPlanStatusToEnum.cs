using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations;

public partial class ConvertTreatmentPlanStatusToEnum : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "TreatmentPlans"
            ALTER COLUMN "Status" TYPE integer
            USING CASE "Status"
                WHEN 'Draft' THEN 0
                WHEN 'Pending' THEN 1
                WHEN 'Accepted' THEN 2
                WHEN 'PartiallyAccepted' THEN 3
                WHEN 'Deferred' THEN 4
                ELSE 0
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "TreatmentPlans"
            ALTER COLUMN "Status" TYPE text
            USING CASE "Status"
                WHEN 0 THEN 'Draft'
                WHEN 1 THEN 'Pending'
                WHEN 2 THEN 'Accepted'
                WHEN 3 THEN 'PartiallyAccepted'
                WHEN 4 THEN 'Deferred'
                ELSE 'Draft'
            END;
            """);
    }
}
