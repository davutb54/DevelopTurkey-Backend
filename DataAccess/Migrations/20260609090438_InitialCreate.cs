using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AboutPageSections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutPageSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetGroup = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Capabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    GroupKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PageScope = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapabilityAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CapabilityCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapabilityAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapabilityTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapabilityTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    SolutionId = table.Column<int>(type: "int", nullable: false),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportCategory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DynamicRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlowJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FlowJsonSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AvailablePlaceholders = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    VerificationCode = table.Column<int>(type: "int", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    IsExpired = table.Column<bool>(type: "bit", nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSystemLevel = table.Column<bool>(type: "bit", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstitutionFeatureValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    FeatureDefinitionId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstitutionFeatureValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Institutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Domain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomFieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomHierarchyLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomHierarchyJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Institutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LegalAgreements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMajorVersion = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedByAdminId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalAgreements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Port = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceLink = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficialResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    AuthorUserId = table.Column<int>(type: "int", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficialResponses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemFollowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemFollowers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityCode = table.Column<int>(type: "int", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    IsReported = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    CustomHierarchyId = table.Column<int>(type: "int", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedByUserId = table.Column<int>(type: "int", nullable: true),
                    CloseReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsHidden = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemTopics",
                columns: table => new
                {
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemTopics", x => new { x.ProblemId, x.TopicId });
                });

            migrationBuilder.CreateTable(
                name: "ProblemUpvotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemUpvotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReporterUserId = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedSolutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SolutionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedSolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Solutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ProblemId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    IsReported = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SendDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpertApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    ImageUrls = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SolutionVotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    SolutionId = table.Column<int>(type: "int", nullable: false),
                    IsUpvote = table.Column<bool>(type: "bit", nullable: false),
                    VoteDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionVotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemKillSwitches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Mode = table.Column<byte>(type: "tinyint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActivatedByUserId = table.Column<int>(type: "int", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeactivatedByUserId = table.Column<int>(type: "int", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemKillSwitches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsMaintenanceMode = table.Column<bool>(type: "bit", nullable: false),
                    DisableNewRegistrations = table.Column<bool>(type: "bit", nullable: false),
                    MaintenanceMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactFullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialTwitter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialInstagram = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SocialLinkedIn = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicFollowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicFollowers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAgreementAcceptances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AgreementId = table.Column<int>(type: "int", nullable: false),
                    AgreementVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAgreementAcceptances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAppliedTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppliedBy = table.Column<int>(type: "int", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAppliedTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordHash = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PasswordSalt = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    AuthType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityCode = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    EmailNotificationPermission = table.Column<bool>(type: "bit", nullable: false),
                    MentionNotificationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsProfilePublic = table.Column<bool>(type: "bit", nullable: false),
                    ShowSolutions = table.Column<bool>(type: "bit", nullable: false),
                    ShowProblems = table.Column<bool>(type: "bit", nullable: false),
                    IsReported = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsBanned = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    RegisterDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastUsernameChangeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockoutCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    CustomHierarchyId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    AssignedByUserId = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTitles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserWarnings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    IssuedByAdminId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWarnings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParametersSchemaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDeadLetters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NodeRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActionRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorDetail = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRequeued = table.Column<bool>(type: "bit", nullable: false),
                    RequeuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequeuedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDeadLetters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TriggerEvent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    CurrentVersionId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowFields",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FieldPath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowFields", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    RuleName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalNodeCount = table.Column<int>(type: "int", nullable: false),
                    ExecutedNodeCount = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefinitionId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    TriggerEvent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    IsDryRun = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTriggers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetEntity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTriggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChangeNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateVersions_CapabilityTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "CapabilityTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConversationParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastReadMessageId = table.Column<int>(type: "int", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversationParticipants_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<int>(type: "int", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "Conversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCapabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CapabilityId = table.Column<int>(type: "int", nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    ScopeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedBy = table.Column<int>(type: "int", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCapabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCapabilities_Capabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Capabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCapabilities_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserCapabilities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DefinitionId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    FlowJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowVersions_WorkflowDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NodeRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeRuns_WorkflowRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "WorkflowRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RuleContextSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleContextSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuleContextSnapshots_WorkflowRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "WorkflowRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TemplateItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateVersionId = table.Column<int>(type: "int", nullable: false),
                    CapabilityId = table.Column<int>(type: "int", nullable: false),
                    ScopeJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateItems_Capabilities_CapabilityId",
                        column: x => x.CapabilityId,
                        principalTable: "Capabilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TemplateItems_TemplateVersions_TemplateVersionId",
                        column: x => x.TemplateVersionId,
                        principalTable: "TemplateVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActionRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActionRuns_NodeRuns_NodeRunId",
                        column: x => x.NodeRunId,
                        principalTable: "NodeRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionRun_ActionCode",
                table: "ActionRuns",
                column: "ActionCode");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRun_NodeRunId",
                table: "ActionRuns",
                column: "NodeRunId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionRun_Status",
                table: "ActionRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Capability_Code",
                table: "Capabilities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityAuditLog_Action",
                table: "CapabilityAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityAuditLog_ActorUserId",
                table: "CapabilityAuditLogs",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityAuditLog_CapabilityCode",
                table: "CapabilityAuditLogs",
                column: "CapabilityCode");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityAuditLog_CreatedAt",
                table: "CapabilityAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityAuditLog_TargetUserId",
                table: "CapabilityAuditLogs",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "UX_CapabilityTemplate_Name",
                table: "CapabilityTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_CapabilityTemplate_Slug",
                table: "CapabilityTemplates",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_ConversationId_UserId",
                table: "ConversationParticipants",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationParticipants_UserId",
                table: "ConversationParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_InstitutionId",
                table: "Conversations",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicRules_InstitutionId",
                table: "DynamicRules",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_InstitutionFeatureValues_InstitutionId",
                table: "InstitutionFeatureValues",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_Subdomain",
                table: "Institutions",
                column: "Subdomain",
                unique: true,
                filter: "[Subdomain] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId",
                table: "Messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeRun_RunId",
                table: "NodeRuns",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeRun_RunId_NodeId",
                table: "NodeRuns",
                columns: new[] { "RunId", "NodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_NodeRun_Status",
                table: "NodeRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_InstitutionId",
                table: "Problems",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemViews_ProblemId",
                table: "ProblemViews",
                column: "ProblemId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemViews_ProblemId_UserId_ViewedAt",
                table: "ProblemViews",
                columns: new[] { "ProblemId", "UserId", "ViewedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_RuleContextSnapshot_RunId",
                table: "RuleContextSnapshots",
                column: "RunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_InstitutionId",
                table: "Solutions",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemKillSwitch_Mode",
                table: "SystemKillSwitches",
                column: "Mode");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItem_TemplateVersionId",
                table: "TemplateItems",
                column: "TemplateVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateItems_CapabilityId",
                table: "TemplateItems",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "UX_TemplateVersion_TemplateId_Version",
                table: "TemplateVersions",
                columns: new[] { "TemplateId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_InstitutionId",
                table: "Topics",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAppliedTemplates_UserId_TemplateId",
                table: "UserAppliedTemplates",
                columns: new[] { "UserId", "TemplateId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCapabilities_CapabilityId",
                table: "UserCapabilities",
                column: "CapabilityId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCapability_InstitutionId",
                table: "UserCapabilities",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCapability_UserId_CapabilityId",
                table: "UserCapabilities",
                columns: new[] { "UserId", "CapabilityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserCapability_UserId_Status",
                table: "UserCapabilities",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_InstitutionId",
                table: "Users",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDeadLetter_CreatedAt",
                table: "WorkflowDeadLetters",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDeadLetter_RunId",
                table: "WorkflowDeadLetters",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_IsActive",
                table: "WorkflowDefinitions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinition_Trigger_Institution",
                table: "WorkflowDefinitions",
                columns: new[] { "TriggerEvent", "InstitutionId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowDefinitions_InstitutionId",
                table: "WorkflowDefinitions",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowLogs_InstitutionId",
                table: "WorkflowLogs",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRun_DefinitionId",
                table: "WorkflowRuns",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRun_InstitutionId",
                table: "WorkflowRuns",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRun_StartedAt",
                table: "WorkflowRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRun_Status",
                table: "WorkflowRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRuns_InstitutionId",
                table: "WorkflowRuns",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowRun_Idempotency",
                table: "WorkflowRuns",
                columns: new[] { "EventId", "DefinitionId", "VersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowVersion_Status",
                table: "WorkflowVersions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowVersion_Definition_Version",
                table: "WorkflowVersions",
                columns: new[] { "DefinitionId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutPageSections");

            migrationBuilder.DropTable(
                name: "ActionRuns");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "CapabilityAuditLogs");

            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "ConversationParticipants");

            migrationBuilder.DropTable(
                name: "DynamicRules");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "EmailVerifications");

            migrationBuilder.DropTable(
                name: "FeatureDefinitions");

            migrationBuilder.DropTable(
                name: "FeatureGroups");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "InstitutionFeatureValues");

            migrationBuilder.DropTable(
                name: "LegalAgreements");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "Messages");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "OfficialResponses");

            migrationBuilder.DropTable(
                name: "ProblemFollowers");

            migrationBuilder.DropTable(
                name: "Problems");

            migrationBuilder.DropTable(
                name: "ProblemTopics");

            migrationBuilder.DropTable(
                name: "ProblemUpvotes");

            migrationBuilder.DropTable(
                name: "ProblemViews");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropTable(
                name: "RuleContextSnapshots");

            migrationBuilder.DropTable(
                name: "SavedSolutions");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "Solutions");

            migrationBuilder.DropTable(
                name: "SolutionVotes");

            migrationBuilder.DropTable(
                name: "SystemKillSwitches");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TemplateItems");

            migrationBuilder.DropTable(
                name: "TopicFollowers");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "UserAgreementAcceptances");

            migrationBuilder.DropTable(
                name: "UserAppliedTemplates");

            migrationBuilder.DropTable(
                name: "UserCapabilities");

            migrationBuilder.DropTable(
                name: "UserTitles");

            migrationBuilder.DropTable(
                name: "UserWarnings");

            migrationBuilder.DropTable(
                name: "WorkflowActions");

            migrationBuilder.DropTable(
                name: "WorkflowDeadLetters");

            migrationBuilder.DropTable(
                name: "WorkflowFields");

            migrationBuilder.DropTable(
                name: "WorkflowLogs");

            migrationBuilder.DropTable(
                name: "WorkflowTriggers");

            migrationBuilder.DropTable(
                name: "WorkflowVersions");

            migrationBuilder.DropTable(
                name: "NodeRuns");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "TemplateVersions");

            migrationBuilder.DropTable(
                name: "Capabilities");

            migrationBuilder.DropTable(
                name: "Institutions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions");

            migrationBuilder.DropTable(
                name: "WorkflowRuns");

            migrationBuilder.DropTable(
                name: "CapabilityTemplates");
        }
    }
}
