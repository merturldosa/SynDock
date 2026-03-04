using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Shop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMesSyncHistoryAndForecastAccuracy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SP_EmailCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Target = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentCount = table.Column<int>(type: "integer", nullable: false),
                    FailCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_EmailCampaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SP_LiturgicalSeasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    SeasonName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LiturgicalColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_LiturgicalSeasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SP_MesSyncHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false),
                    ElapsedMs = table.Column<long>(type: "bigint", nullable: false),
                    ErrorDetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    ConflictDetailsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_MesSyncHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SP_Saints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    KoreanName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LatinName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EnglishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FeastDay = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Patronage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Saints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SP_Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomDomain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subdomain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SP_Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SourceCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Categories_SP_Categories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SP_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_Categories_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DiscountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    MinOrderAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    MaxDiscountAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxUsageCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentUsageCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Coupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Coupons_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Hashtags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Hashtags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Hashtags_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Invoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BillingPeriod = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    PlanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Invoices_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Pages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Pages_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_TenantPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PlanType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BillingStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_TenantPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_TenantPlans_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_TenantUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductCount = table.Column<int>(type: "integer", nullable: false),
                    UserCount = table.Column<int>(type: "integer", nullable: false),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    MonthlyOrderCount = table.Column<int>(type: "integer", nullable: false),
                    CurrentPeriod = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_TenantUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_TenantUsages_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomFieldsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Users_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    SalePrice = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    PriceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Specification = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    SourceId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    SourceSubCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    IsNew = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    CustomFieldsJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Products_SP_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SP_Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SP_Products_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Address1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Addresses_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Addresses_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Carts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Carts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Carts_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Carts_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Collections_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Collections_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Follows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    FollowerId = table.Column<int>(type: "integer", nullable: false),
                    FollowingId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Follows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Follows_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Follows_SP_Users_FollowerId",
                        column: x => x.FollowerId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Follows_SP_Users_FollowingId",
                        column: x => x.FollowingId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferenceId = table.Column<int>(type: "integer", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Notifications_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Notifications_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_RefreshTokens_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_UserPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_UserPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_UserPoints_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_UserPoints_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_ForecastAccuracies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    ForecastDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PredictedQuantity = table.Column<double>(type: "double precision", nullable: false),
                    ActualQuantity = table.Column<double>(type: "double precision", nullable: true),
                    AbsoluteError = table.Column<double>(type: "double precision", nullable: true),
                    PercentageError = table.Column<double>(type: "double precision", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_ForecastAccuracies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_ForecastAccuracies_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    PostType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: true),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    ReactionCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Posts_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_Posts_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Posts_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_ProductDetailSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImageAltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SectionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_ProductDetailSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_ProductDetailSections_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_ProductDetailSections_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_ProductImages_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,0)", nullable: true),
                    Stock = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_ProductVariants_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_QnAs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsAnswered = table.Column<bool>(type: "boolean", nullable: false),
                    IsSecret = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_QnAs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_QnAs_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_QnAs_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_QnAs_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Reviews_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Reviews_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Reviews_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_Wishlists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Wishlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Wishlists_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Wishlists_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Wishlists_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    ShippingFee = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    PointsUsed = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    CouponId = table.Column<int>(type: "integer", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ShippingAddressId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Orders_SP_Addresses_ShippingAddressId",
                        column: x => x.ShippingAddressId,
                        principalTable: "SP_Addresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_Orders_SP_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "SP_Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_Orders_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Orders_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_CollectionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CollectionId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_CollectionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_CollectionItems_SP_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "SP_Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_CollectionItems_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_PostComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ParentId = table.Column<int>(type: "integer", nullable: true),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PostComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PostComments_SP_PostComments_ParentId",
                        column: x => x.ParentId,
                        principalTable: "SP_PostComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PostComments_SP_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "SP_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PostComments_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PostComments_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_PostHashtags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    HashtagId = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PostHashtags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PostHashtags_SP_Hashtags_HashtagId",
                        column: x => x.HashtagId,
                        principalTable: "SP_Hashtags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PostHashtags_SP_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "SP_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_PostImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PostImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PostImages_SP_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "SP_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_PostReactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ReactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PostReactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PostReactions_SP_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "SP_Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PostReactions_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PostReactions_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    CartId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    VariantId = table.Column<int>(type: "integer", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_CartItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_CartItems_SP_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "SP_Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_CartItems_SP_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "SP_ProductVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SP_CartItems_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_QnAReplies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    QnAId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_QnAReplies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_QnAReplies_SP_QnAs_QnAId",
                        column: x => x.QnAId,
                        principalTable: "SP_QnAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_QnAReplies_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_QnAReplies_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_OrderHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingCarrier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_OrderHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_OrderHistories_SP_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SP_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_OrderHistories_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    VariantId = table.Column<int>(type: "integer", nullable: true),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_OrderItems_SP_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SP_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_OrderItems_SP_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "SP_ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_OrderItems_SP_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "SP_Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SP_Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    OrderId = table.Column<int>(type: "integer", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TransactionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FailReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_Payments_SP_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SP_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_Payments_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_PointHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,0)", nullable: false),
                    TransactionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_PointHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_PointHistories_SP_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SP_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_PointHistories_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_PointHistories_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SP_UserCoupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    CouponId = table.Column<int>(type: "integer", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsedOrderId = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SP_UserCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SP_UserCoupons_SP_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "SP_Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_UserCoupons_SP_Orders_UsedOrderId",
                        column: x => x.UsedOrderId,
                        principalTable: "SP_Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SP_UserCoupons_SP_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "SP_Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SP_UserCoupons_SP_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "SP_Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SP_Addresses_TenantId",
                table: "SP_Addresses",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Addresses_UserId",
                table: "SP_Addresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CartItems_CartId",
                table: "SP_CartItems",
                column: "CartId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CartItems_ProductId",
                table: "SP_CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CartItems_VariantId",
                table: "SP_CartItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Carts_TenantId_UserId",
                table: "SP_Carts",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Carts_UserId",
                table: "SP_Carts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Categories_IsActive",
                table: "SP_Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Categories_ParentId",
                table: "SP_Categories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Categories_SortOrder",
                table: "SP_Categories",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Categories_TenantId_Slug",
                table: "SP_Categories",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CollectionItems_CollectionId",
                table: "SP_CollectionItems",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_CollectionItems_CollectionId_ProductId",
                table: "SP_CollectionItems",
                columns: new[] { "CollectionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_CollectionItems_ProductId",
                table: "SP_CollectionItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Collections_IsPublic",
                table: "SP_Collections",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Collections_TenantId",
                table: "SP_Collections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Collections_UserId",
                table: "SP_Collections",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Coupons_IsActive",
                table: "SP_Coupons",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Coupons_StartDate_EndDate",
                table: "SP_Coupons",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_Coupons_TenantId_Code",
                table: "SP_Coupons",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_EmailCampaigns_ScheduledAt",
                table: "SP_EmailCampaigns",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_SP_EmailCampaigns_Status",
                table: "SP_EmailCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SP_EmailCampaigns_TenantId",
                table: "SP_EmailCampaigns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Follows_FollowerId",
                table: "SP_Follows",
                column: "FollowerId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Follows_FollowingId",
                table: "SP_Follows",
                column: "FollowingId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Follows_TenantId_FollowerId_FollowingId",
                table: "SP_Follows",
                columns: new[] { "TenantId", "FollowerId", "FollowingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_ForecastAccuracies_ForecastDate",
                table: "SP_ForecastAccuracies",
                column: "ForecastDate");

            migrationBuilder.CreateIndex(
                name: "IX_SP_ForecastAccuracies_ProductId",
                table: "SP_ForecastAccuracies",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_ForecastAccuracies_TenantId_ProductId_TargetDate",
                table: "SP_ForecastAccuracies",
                columns: new[] { "TenantId", "ProductId", "TargetDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_Hashtags_PostCount",
                table: "SP_Hashtags",
                column: "PostCount");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Hashtags_TenantId_Tag",
                table: "SP_Hashtags",
                columns: new[] { "TenantId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Invoices_InvoiceNumber",
                table: "SP_Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Invoices_Status",
                table: "SP_Invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Invoices_TenantId",
                table: "SP_Invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Invoices_TenantId_BillingPeriod",
                table: "SP_Invoices",
                columns: new[] { "TenantId", "BillingPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_LiturgicalSeasons_StartDate_EndDate",
                table: "SP_LiturgicalSeasons",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_LiturgicalSeasons_Year_SeasonName",
                table: "SP_LiturgicalSeasons",
                columns: new[] { "Year", "SeasonName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_MesSyncHistories_StartedAt",
                table: "SP_MesSyncHistories",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SP_MesSyncHistories_Status",
                table: "SP_MesSyncHistories",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Notifications_CreatedAt",
                table: "SP_Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Notifications_IsRead",
                table: "SP_Notifications",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Notifications_TenantId",
                table: "SP_Notifications",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Notifications_Type",
                table: "SP_Notifications",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Notifications_UserId",
                table: "SP_Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_OrderHistories_OrderId",
                table: "SP_OrderHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_OrderHistories_TenantId",
                table: "SP_OrderHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_OrderItems_OrderId",
                table: "SP_OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_OrderItems_ProductId",
                table: "SP_OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_OrderItems_VariantId",
                table: "SP_OrderItems",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Orders_CouponId",
                table: "SP_Orders",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Orders_ShippingAddressId",
                table: "SP_Orders",
                column: "ShippingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Orders_Status",
                table: "SP_Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Orders_TenantId_OrderNumber",
                table: "SP_Orders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Orders_UserId",
                table: "SP_Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Pages_IsPublished",
                table: "SP_Pages",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Pages_TenantId_Slug",
                table: "SP_Pages",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Payments_OrderId",
                table: "SP_Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Payments_TenantId",
                table: "SP_Payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Payments_TransactionId",
                table: "SP_Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PointHistories_OrderId",
                table: "SP_PointHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PointHistories_TenantId",
                table: "SP_PointHistories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PointHistories_TransactionType",
                table: "SP_PointHistories",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PointHistories_UserId",
                table: "SP_PointHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostComments_ParentId",
                table: "SP_PostComments",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostComments_PostId",
                table: "SP_PostComments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostComments_TenantId",
                table: "SP_PostComments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostComments_UserId",
                table: "SP_PostComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostHashtags_HashtagId",
                table: "SP_PostHashtags",
                column: "HashtagId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostHashtags_PostId_HashtagId",
                table: "SP_PostHashtags",
                columns: new[] { "PostId", "HashtagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostImages_PostId_SortOrder",
                table: "SP_PostImages",
                columns: new[] { "PostId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostReactions_PostId_UserId_ReactionType",
                table: "SP_PostReactions",
                columns: new[] { "PostId", "UserId", "ReactionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostReactions_TenantId",
                table: "SP_PostReactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_PostReactions_UserId",
                table: "SP_PostReactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Posts_CreatedAt",
                table: "SP_Posts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Posts_PostType",
                table: "SP_Posts",
                column: "PostType");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Posts_ProductId",
                table: "SP_Posts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Posts_TenantId",
                table: "SP_Posts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Posts_UserId",
                table: "SP_Posts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductDetailSections_ProductId_SortOrder",
                table: "SP_ProductDetailSections",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductDetailSections_TenantId",
                table: "SP_ProductDetailSections",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductImages_ProductId_SortOrder",
                table: "SP_ProductImages",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_Products_CategoryId",
                table: "SP_Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Products_IsActive",
                table: "SP_Products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Products_IsFeatured",
                table: "SP_Products",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Products_SortOrder",
                table: "SP_Products",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Products_SourceId",
                table: "SP_Products",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Products_TenantId_Slug",
                table: "SP_Products",
                columns: new[] { "TenantId", "Slug" },
                unique: true,
                filter: "\"Slug\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SP_ProductVariants_ProductId_SortOrder",
                table: "SP_ProductVariants",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_QnAReplies_QnAId",
                table: "SP_QnAReplies",
                column: "QnAId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_QnAReplies_TenantId",
                table: "SP_QnAReplies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_QnAReplies_UserId",
                table: "SP_QnAReplies",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_QnAs_ProductId",
                table: "SP_QnAs",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_QnAs_TenantId",
                table: "SP_QnAs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_QnAs_UserId",
                table: "SP_QnAs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_RefreshTokens_Token",
                table: "SP_RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_RefreshTokens_UserId_IsRevoked",
                table: "SP_RefreshTokens",
                columns: new[] { "UserId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_SP_Reviews_ProductId",
                table: "SP_Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Reviews_TenantId",
                table: "SP_Reviews",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Reviews_UserId",
                table: "SP_Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Saints_FeastDay",
                table: "SP_Saints",
                column: "FeastDay");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Saints_IsActive",
                table: "SP_Saints",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Saints_KoreanName",
                table: "SP_Saints",
                column: "KoreanName");

            migrationBuilder.CreateIndex(
                name: "IX_SP_TenantPlans_BillingStatus",
                table: "SP_TenantPlans",
                column: "BillingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_SP_TenantPlans_TenantId",
                table: "SP_TenantPlans",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Tenants_CustomDomain",
                table: "SP_Tenants",
                column: "CustomDomain",
                unique: true,
                filter: "\"CustomDomain\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Tenants_IsActive",
                table: "SP_Tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Tenants_Slug",
                table: "SP_Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Tenants_Subdomain",
                table: "SP_Tenants",
                column: "Subdomain",
                unique: true,
                filter: "\"Subdomain\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SP_TenantUsages_TenantId",
                table: "SP_TenantUsages",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserCoupons_CouponId",
                table: "SP_UserCoupons",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserCoupons_IsUsed",
                table: "SP_UserCoupons",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserCoupons_TenantId_UserId_CouponId",
                table: "SP_UserCoupons",
                columns: new[] { "TenantId", "UserId", "CouponId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserCoupons_UsedOrderId",
                table: "SP_UserCoupons",
                column: "UsedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserCoupons_UserId",
                table: "SP_UserCoupons",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserPoints_TenantId_UserId",
                table: "SP_UserPoints",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_UserPoints_UserId",
                table: "SP_UserPoints",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Users_IsActive",
                table: "SP_Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Users_Role",
                table: "SP_Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Users_TenantId_Email",
                table: "SP_Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Users_TenantId_Username",
                table: "SP_Users",
                columns: new[] { "TenantId", "Username" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Wishlists_ProductId",
                table: "SP_Wishlists",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SP_Wishlists_TenantId_UserId_ProductId",
                table: "SP_Wishlists",
                columns: new[] { "TenantId", "UserId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SP_Wishlists_UserId",
                table: "SP_Wishlists",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SP_CartItems");

            migrationBuilder.DropTable(
                name: "SP_CollectionItems");

            migrationBuilder.DropTable(
                name: "SP_EmailCampaigns");

            migrationBuilder.DropTable(
                name: "SP_Follows");

            migrationBuilder.DropTable(
                name: "SP_ForecastAccuracies");

            migrationBuilder.DropTable(
                name: "SP_Invoices");

            migrationBuilder.DropTable(
                name: "SP_LiturgicalSeasons");

            migrationBuilder.DropTable(
                name: "SP_MesSyncHistories");

            migrationBuilder.DropTable(
                name: "SP_Notifications");

            migrationBuilder.DropTable(
                name: "SP_OrderHistories");

            migrationBuilder.DropTable(
                name: "SP_OrderItems");

            migrationBuilder.DropTable(
                name: "SP_Pages");

            migrationBuilder.DropTable(
                name: "SP_Payments");

            migrationBuilder.DropTable(
                name: "SP_PointHistories");

            migrationBuilder.DropTable(
                name: "SP_PostComments");

            migrationBuilder.DropTable(
                name: "SP_PostHashtags");

            migrationBuilder.DropTable(
                name: "SP_PostImages");

            migrationBuilder.DropTable(
                name: "SP_PostReactions");

            migrationBuilder.DropTable(
                name: "SP_ProductDetailSections");

            migrationBuilder.DropTable(
                name: "SP_ProductImages");

            migrationBuilder.DropTable(
                name: "SP_QnAReplies");

            migrationBuilder.DropTable(
                name: "SP_RefreshTokens");

            migrationBuilder.DropTable(
                name: "SP_Reviews");

            migrationBuilder.DropTable(
                name: "SP_Saints");

            migrationBuilder.DropTable(
                name: "SP_TenantPlans");

            migrationBuilder.DropTable(
                name: "SP_TenantUsages");

            migrationBuilder.DropTable(
                name: "SP_UserCoupons");

            migrationBuilder.DropTable(
                name: "SP_UserPoints");

            migrationBuilder.DropTable(
                name: "SP_Wishlists");

            migrationBuilder.DropTable(
                name: "SP_Carts");

            migrationBuilder.DropTable(
                name: "SP_Collections");

            migrationBuilder.DropTable(
                name: "SP_ProductVariants");

            migrationBuilder.DropTable(
                name: "SP_Hashtags");

            migrationBuilder.DropTable(
                name: "SP_Posts");

            migrationBuilder.DropTable(
                name: "SP_QnAs");

            migrationBuilder.DropTable(
                name: "SP_Orders");

            migrationBuilder.DropTable(
                name: "SP_Products");

            migrationBuilder.DropTable(
                name: "SP_Addresses");

            migrationBuilder.DropTable(
                name: "SP_Coupons");

            migrationBuilder.DropTable(
                name: "SP_Categories");

            migrationBuilder.DropTable(
                name: "SP_Users");

            migrationBuilder.DropTable(
                name: "SP_Tenants");
        }
    }
}
