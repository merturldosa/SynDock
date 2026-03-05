using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "SP_Users",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Birthday",
                table: "SP_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "SP_Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "SP_Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "SP_Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpiry",
                table: "SP_Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorBackupCodes",
                table: "SP_Users",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "SP_Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TwoFactorSecret",
                table: "SP_Users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MesOrderId",
                table: "SP_Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MesOrderNo",
                table: "SP_Orders",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClickCount",
                table: "SP_EmailCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConversionCount",
                table: "SP_EmailCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsAbTest",
                table: "SP_EmailCampaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OpenCount",
                table: "SP_EmailCampaigns",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Revenue",
                table: "SP_EmailCampaigns",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "SP_Addresses",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Address2",
                table: "SP_Addresses",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address1",
                table: "SP_Addresses",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.CreateTable(
                name: "SP_AutoReorderRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReorderThreshold = table.Column<int>(type: "integer", nullable: false),
                    ReorderQuantity = table.Column<int>(type: "integer", nullable: false),
                    MaxStockLevel = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AutoForwardToMes = table.Column<bool>(type: "boolean", nullable: false),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MinIntervalHours = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_AutoReorderRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_AutoReorderRules_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_CampaignVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    VariantName = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubjectLine = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TrafficPercent = table.Column<int>(type: "integer", nullable: false),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    OpenCount = table.Column<int>(type: "integer", nullable: false),
                    ClickCount = table.Column<int>(type: "integer", nullable: false),
                    ConversionCount = table.Column<int>(type: "integer", nullable: false),
                    Revenue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsWinner = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_CampaignVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_CampaignVariants_SP_EmailCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "SP_EmailCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_CommissionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SettlementCycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SettlementDayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    MinSettlementAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    BankName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankHolder = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_CommissionSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_CommissionSettings_SP_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SP_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_CommissionSettings_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_CommissionSettings_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_ProductionPlanSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    AverageDailySales = table.Column<double>(type: "double precision", nullable: false),
                    EstimatedDaysUntilStockout = table.Column<int>(type: "integer", nullable: false),
                    SuggestedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Urgency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AiReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TrendAnalysis = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SeasonalityFactor = table.Column<double>(type: "double precision", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "double precision", nullable: true),
                    MesOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_ProductionPlanSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_ProductionPlanSuggestions_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    MesOrderId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MesOrderNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ForwardedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedByUser = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PurchaseOrders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SP_PushSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    P256dh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Auth = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PushSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PushSubscriptions_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PushSubscriptions_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Settlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    TotalOrderAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TotalCommission = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TotalSettlementAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankAccount = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Settlements_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_SocialPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    Platform = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Caption = table.Column<string>(type: "character varying(2200)", maxLength: 2200, nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PostUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalPostId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_SocialPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_SocialPosts_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SP_CampaignMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    VariantId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LinkUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_CampaignMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_CampaignMetrics_SP_CampaignVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "SP_CampaignVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_CampaignMetrics_SP_EmailCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "SP_EmailCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PurchaseOrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MesProductCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    ReorderThreshold = table.Column<int>(type: "integer", nullable: false),
                    OrderedQuantity = table.Column<int>(type: "integer", nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PurchaseOrderItems_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_PurchaseOrderItems_SP_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "SP_PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Commissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    OrderAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    SettlementAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SettlementId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Commissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Commissions_SP_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SP_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Commissions_SP_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "SP_Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_Commissions_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SP_AutoReorderRules_IsEnabled",
                table: "SP_AutoReorderRules",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_SP_AutoReorderRules_ProductId",
                table: "SP_AutoReorderRules",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_AutoReorderRules_TenantId_ProductId",
                table: "SP_AutoReorderRules",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_CampaignMetrics_CampaignId_EventType",
                table: "SP_CampaignMetrics",
                columns: new[] { "CampaignId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_CampaignMetrics_UserId",
                table: "SP_CampaignMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CampaignMetrics_VariantId_EventType",
                table: "SP_CampaignMetrics",
                columns: new[] { "VariantId", "EventType" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_CampaignVariants_CampaignId_VariantName",
                table: "SP_CampaignVariants",
                columns: new[] { "CampaignId", "VariantName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Commissions_OrderId",
                table: "SP_Commissions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Commissions_SettlementId",
                table: "SP_Commissions",
                column: "SettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Commissions_TenantId_Status",
                table: "SP_Commissions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_CommissionSettings_CategoryId",
                table: "SP_CommissionSettings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CommissionSettings_ProductId",
                table: "SP_CommissionSettings",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CommissionSettings_TenantId_ProductId_CategoryId",
                table: "SP_CommissionSettings",
                columns: new[] { "TenantId", "ProductId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductionPlanSuggestions_ProductId_Status",
                table: "SP_ProductionPlanSuggestions",
                columns: new[] { "ProductId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductionPlanSuggestions_TenantId_Status",
                table: "SP_ProductionPlanSuggestions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductionPlanSuggestions_Urgency",
                table: "SP_ProductionPlanSuggestions",
                column: "Urgency");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PurchaseOrderItems_ProductId",
                table: "SP_PurchaseOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PurchaseOrderItems_PurchaseOrderId",
                table: "SP_PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PurchaseOrders_CreatedAt",
                table: "SP_PurchaseOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PurchaseOrders_Status",
                table: "SP_PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PurchaseOrders_TenantId_OrderNumber",
                table: "SP_PurchaseOrders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_PurchaseOrders_TriggerType",
                table: "SP_PurchaseOrders",
                column: "TriggerType");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PushSubscriptions_TenantId",
                table: "SP_PushSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PushSubscriptions_UserId",
                table: "SP_PushSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Settlements_TenantId_PeriodStart_PeriodEnd",
                table: "SP_Settlements",
                columns: new[] { "TenantId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_Settlements_TenantId_Status",
                table: "SP_Settlements",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_SocialPosts_ProductId",
                table: "SP_SocialPosts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_SocialPosts_Status",
                table: "SP_SocialPosts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SP_SocialPosts_TenantId_Platform",
                table: "SP_SocialPosts",
                columns: new[] { "TenantId", "Platform" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SP_AutoReorderRules");

            migrationBuilder.DropTable(
                name: "SP_CampaignMetrics");

            migrationBuilder.DropTable(
                name: "SP_Commissions");

            migrationBuilder.DropTable(
                name: "SP_CommissionSettings");

            migrationBuilder.DropTable(
                name: "SP_ProductionPlanSuggestions");

            migrationBuilder.DropTable(
                name: "SP_PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "SP_PushSubscriptions");

            migrationBuilder.DropTable(
                name: "SP_SocialPosts");

            migrationBuilder.DropTable(
                name: "SP_CampaignVariants");

            migrationBuilder.DropTable(
                name: "SP_Settlements");

            migrationBuilder.DropTable(
                name: "SP_PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "Birthday",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpiry",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorBackupCodes",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "TwoFactorSecret",
                table: "SP_Users");

            migrationBuilder.DropColumn(
                name: "MesOrderId",
                table: "SP_Orders");

            migrationBuilder.DropColumn(
                name: "MesOrderNo",
                table: "SP_Orders");

            migrationBuilder.DropColumn(
                name: "ClickCount",
                table: "SP_EmailCampaigns");

            migrationBuilder.DropColumn(
                name: "ConversionCount",
                table: "SP_EmailCampaigns");

            migrationBuilder.DropColumn(
                name: "IsAbTest",
                table: "SP_EmailCampaigns");

            migrationBuilder.DropColumn(
                name: "OpenCount",
                table: "SP_EmailCampaigns");

            migrationBuilder.DropColumn(
                name: "Revenue",
                table: "SP_EmailCampaigns");

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "SP_Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "SP_Addresses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<string>(
                name: "Address2",
                table: "SP_Addresses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Address1",
                table: "SP_Addresses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
