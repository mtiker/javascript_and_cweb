using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.DAL.EF.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTreatmentPlanFinanceWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PlanItemId",
                table: "Treatments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAtUtc",
                table: "TreatmentPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoverageAmount",
                table: "CostEstimates",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PatientEstimatedAmount",
                table: "CostEstimates",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PatientInsurancePolicyId",
                table: "CostEstimates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TreatmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CoverageAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PatientAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_PlanItems_PlanItemId",
                        column: x => x.PlanItemId,
                        principalTable: "PlanItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InvoiceLines_Treatments_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "Treatments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PatientInsurancePolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsurancePlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyNumber = table.Column<string>(type: "text", nullable: false),
                    MemberNumber = table.Column<string>(type: "text", nullable: true),
                    GroupNumber = table.Column<string>(type: "text", nullable: true),
                    CoverageStart = table.Column<DateOnly>(type: "date", nullable: false),
                    CoverageEnd = table.Column<DateOnly>(type: "date", nullable: true),
                    AnnualMaximum = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Deductible = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientInsurancePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientInsurancePolicies_InsurancePlans_InsurancePlanId",
                        column: x => x.InsurancePlanId,
                        principalTable: "InsurancePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PatientInsurancePolicies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPlanInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPlanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentPlanInstallments_PaymentPlans_PaymentPlanId",
                        column: x => x.PaymentPlanId,
                        principalTable: "PaymentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    });

            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CostEstimates"
                ALTER COLUMN "Status" TYPE integer
                USING CASE lower(coalesce("Status", 'draft'))
                    WHEN 'draft' THEN 0
                    WHEN 'prepared' THEN 1
                    WHEN 'sent' THEN 2
                    WHEN 'approved' THEN 3
                    WHEN 'archived' THEN 4
                    ELSE 0
                END;
                """);

            migrationBuilder.Sql("""
                UPDATE "TreatmentPlans"
                SET "SubmittedAtUtc" = coalesce("ApprovedAtUtc", "CreatedAtUtc")
                WHERE "SubmittedAtUtc" IS NULL
                  AND ("Status" <> 0 OR "ApprovedAtUtc" IS NOT NULL);
                """);

            migrationBuilder.Sql("""
                UPDATE "CostEstimates"
                SET "PatientEstimatedAmount" = "TotalEstimatedAmount",
                    "CoverageAmount" = 0
                WHERE "PatientEstimatedAmount" = 0
                  AND "CoverageAmount" = 0;
                """);

            migrationBuilder.Sql("""
                INSERT INTO "InvoiceLines"
                (
                    "Id", "InvoiceId", "TreatmentId", "PlanItemId", "Description", "Quantity", "UnitPrice",
                    "LineTotal", "CoverageAmount", "PatientAmount", "CompanyId", "CreatedAtUtc",
                    "ModifiedAtUtc", "CreatedByUserId", "ModifiedByUserId", "IsDeleted", "DeletedAtUtc"
                )
                SELECT
                    gen_random_uuid(),
                    invoice."Id",
                    NULL,
                    NULL,
                    'Legacy migrated invoice',
                    1,
                    invoice."TotalAmount",
                    invoice."TotalAmount",
                    GREATEST(invoice."TotalAmount" - invoice."BalanceAmount", 0),
                    invoice."BalanceAmount",
                    invoice."CompanyId",
                    CURRENT_TIMESTAMP,
                    NULL,
                    NULL,
                    NULL,
                    FALSE,
                    NULL
                FROM "Invoices" AS invoice
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "InvoiceLines" AS line
                    WHERE line."InvoiceId" = invoice."Id"
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "PaymentPlanInstallments"
                (
                    "Id", "PaymentPlanId", "DueDateUtc", "Amount", "Status", "PaidAtUtc", "CompanyId",
                    "CreatedAtUtc", "ModifiedAtUtc", "CreatedByUserId", "ModifiedByUserId", "IsDeleted", "DeletedAtUtc"
                )
                SELECT
                    gen_random_uuid(),
                    plan."Id",
                    plan."StartsAtUtc" + ((series.installment_index - 1) * INTERVAL '30 day'),
                    plan."InstallmentAmount",
                    0,
                    NULL,
                    plan."CompanyId",
                    CURRENT_TIMESTAMP,
                    NULL,
                    NULL,
                    NULL,
                    FALSE,
                    NULL
                FROM "PaymentPlans" AS plan
                JOIN generate_series(1, plan."InstallmentCount") AS series(installment_index) ON TRUE
                WHERE plan."InstallmentCount" > 0
                  AND plan."InstallmentAmount" > 0;
                """);

            migrationBuilder.DropColumn(
                name: "InstallmentAmount",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                table: "PaymentPlans");

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_CompanyId_PlanItemId",
                table: "Treatments",
                columns: new[] { "CompanyId", "PlanItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_Treatments_PlanItemId",
                table: "Treatments",
                column: "PlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CostEstimates_PatientInsurancePolicyId",
                table: "CostEstimates",
                column: "PatientInsurancePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_CompanyId_InvoiceId",
                table: "InvoiceLines",
                columns: new[] { "CompanyId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_CompanyId_PlanItemId",
                table: "InvoiceLines",
                columns: new[] { "CompanyId", "PlanItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_CompanyId_TreatmentId",
                table: "InvoiceLines",
                columns: new[] { "CompanyId", "TreatmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_InvoiceId",
                table: "InvoiceLines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_PlanItemId",
                table: "InvoiceLines",
                column: "PlanItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLines_TreatmentId",
                table: "InvoiceLines",
                column: "TreatmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurancePolicies_CompanyId_PatientId_PolicyNumber",
                table: "PatientInsurancePolicies",
                columns: new[] { "CompanyId", "PatientId", "PolicyNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurancePolicies_InsurancePlanId",
                table: "PatientInsurancePolicies",
                column: "InsurancePlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInsurancePolicies_PatientId",
                table: "PatientInsurancePolicies",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_CompanyId_PaymentPlanId_DueDateUtc",
                table: "PaymentPlanInstallments",
                columns: new[] { "CompanyId", "PaymentPlanId", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlanInstallments_PaymentPlanId",
                table: "PaymentPlanInstallments",
                column: "PaymentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CompanyId_InvoiceId_PaidAtUtc",
                table: "Payments",
                columns: new[] { "CompanyId", "InvoiceId", "PaidAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments",
                column: "InvoiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_CostEstimates_PatientInsurancePolicies_PatientInsurancePoli~",
                table: "CostEstimates",
                column: "PatientInsurancePolicyId",
                principalTable: "PatientInsurancePolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Treatments_PlanItems_PlanItemId",
                table: "Treatments",
                column: "PlanItemId",
                principalTable: "PlanItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CostEstimates_PatientInsurancePolicies_PatientInsurancePoli~",
                table: "CostEstimates");

            migrationBuilder.DropForeignKey(
                name: "FK_Treatments_PlanItems_PlanItemId",
                table: "Treatments");

            migrationBuilder.DropTable(
                name: "InvoiceLines");

            migrationBuilder.DropTable(
                name: "PatientInsurancePolicies");

            migrationBuilder.DropTable(
                name: "PaymentPlanInstallments");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Treatments_CompanyId_PlanItemId",
                table: "Treatments");

            migrationBuilder.DropIndex(
                name: "IX_Treatments_PlanItemId",
                table: "Treatments");

            migrationBuilder.DropIndex(
                name: "IX_CostEstimates_PatientInsurancePolicyId",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "PlanItemId",
                table: "Treatments");

            migrationBuilder.DropColumn(
                name: "SubmittedAtUtc",
                table: "TreatmentPlans");

            migrationBuilder.DropColumn(
                name: "CoverageAmount",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "PatientEstimatedAmount",
                table: "CostEstimates");

            migrationBuilder.DropColumn(
                name: "PatientInsurancePolicyId",
                table: "CostEstimates");

            migrationBuilder.AddColumn<decimal>(
                name: "InstallmentAmount",
                table: "PaymentPlans",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                table: "PaymentPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "CostEstimates",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
