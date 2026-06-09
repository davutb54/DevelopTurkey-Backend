using Core.Entities.Concrete;
using Core.Utilities.Context;
using Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DataAccess.Concrete.EntityFramework;

public class DevelopTurkeyContext : DbContext
{
    // Instance fields captured by EF Core global query filter closures.
    // Populated from TenantScopeContext at construction time so each
    // context instance carries its own immutable tenant snapshot.
    private readonly bool _isGlobalAdmin;
    private readonly int? _institutionId;

    // Used by EfEntityRepositoryBase via `new TContext()` — reads from ambient TenantScopeContext.
    public DevelopTurkeyContext()
    {
        _isGlobalAdmin = TenantScopeContext.ShouldBypassFilter;
        _institutionId = TenantScopeContext.InstitutionId;
    }

    // Used by tests with InMemory database — still reads from TenantScopeContext so tests
    // can control the effective tenant by calling TenantScopeContext.Set() before the constructor.
    public DevelopTurkeyContext(DbContextOptions<DevelopTurkeyContext> options) : base(options)
    {
        _isGlobalAdmin = TenantScopeContext.ShouldBypassFilter;
        _institutionId = TenantScopeContext.InstitutionId;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{env}.json")
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connectionString);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProblemTopic>()
            .HasKey(pt => new { pt.ProblemId, pt.TopicId });

        // ── Tenant isolation global query filters ─────────────────────────────
        // All entities with InstitutionId get a filter that restricts rows to the
        // current tenant. Global admins and background/seeder contexts bypass this
        // via _isGlobalAdmin = true (read from TenantScopeContext at construction).
        modelBuilder.Entity<Problem>()
            .HasQueryFilter(p => _isGlobalAdmin || p.InstitutionId == _institutionId);
        modelBuilder.Entity<Solution>()
            .HasQueryFilter(s => _isGlobalAdmin || s.InstitutionId == _institutionId);
        modelBuilder.Entity<Topic>()
            .HasQueryFilter(t => _isGlobalAdmin || t.InstitutionId == _institutionId);
        modelBuilder.Entity<DynamicRule>()
            .HasQueryFilter(r => _isGlobalAdmin || r.InstitutionId == _institutionId);
        modelBuilder.Entity<WorkflowDefinition>()
            .HasQueryFilter(w => _isGlobalAdmin || w.InstitutionId == _institutionId);
        modelBuilder.Entity<WorkflowRun>()
            .HasQueryFilter(r => _isGlobalAdmin || r.InstitutionId == _institutionId);
        modelBuilder.Entity<WorkflowLog>()
            .HasQueryFilter(l => _isGlobalAdmin || l.InstitutionId == _institutionId);
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => _isGlobalAdmin || u.InstitutionId == _institutionId);
        // Sohbet: global konuşmalar (InstitutionId == null) herkes için görünür;
        // institution-scoped konuşmalar sadece ilgili kuruma aittir.
        modelBuilder.Entity<Conversation>()
            .HasQueryFilter(c => _isGlobalAdmin || c.InstitutionId == null || c.InstitutionId == _institutionId);
        // ─────────────────────────────────────────────────────────────────────

        // ── Tenant InstitutionId indexes (50-tenant ölçeği için kritik) ──────
        modelBuilder.Entity<Problem>()
            .HasIndex(p => p.InstitutionId).HasDatabaseName("IX_Problems_InstitutionId");
        modelBuilder.Entity<Solution>()
            .HasIndex(s => s.InstitutionId).HasDatabaseName("IX_Solutions_InstitutionId");
        modelBuilder.Entity<Topic>()
            .HasIndex(t => t.InstitutionId).HasDatabaseName("IX_Topics_InstitutionId");
        modelBuilder.Entity<DynamicRule>()
            .HasIndex(r => r.InstitutionId).HasDatabaseName("IX_DynamicRules_InstitutionId");
        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(w => w.InstitutionId).HasDatabaseName("IX_WorkflowDefinitions_InstitutionId");
        modelBuilder.Entity<WorkflowRun>()
            .HasIndex(r => r.InstitutionId).HasDatabaseName("IX_WorkflowRuns_InstitutionId");
        modelBuilder.Entity<WorkflowLog>()
            .HasIndex(l => l.InstitutionId).HasDatabaseName("IX_WorkflowLogs_InstitutionId");
        modelBuilder.Entity<User>()
            .HasIndex(u => u.InstitutionId).HasDatabaseName("IX_Users_InstitutionId");
        modelBuilder.Entity<InstitutionFeatureValue>()
            .HasIndex(v => v.InstitutionId).HasDatabaseName("IX_InstitutionFeatureValues_InstitutionId");
        // Sohbet index'leri
        modelBuilder.Entity<Conversation>()
            .HasIndex(c => c.InstitutionId).HasDatabaseName("IX_Conversations_InstitutionId");
        modelBuilder.Entity<ConversationParticipant>()
            .HasIndex(p => new { p.ConversationId, p.UserId })
            .HasDatabaseName("IX_ConversationParticipants_ConversationId_UserId")
            .IsUnique();
        modelBuilder.Entity<ConversationParticipant>()
            .HasIndex(p => p.UserId).HasDatabaseName("IX_ConversationParticipants_UserId");
        modelBuilder.Entity<Message>()
            .HasIndex(m => m.ConversationId).HasDatabaseName("IX_Messages_ConversationId");
        // ─────────────────────────────────────────────────────────────────────

        // ProblemView indexes
        modelBuilder.Entity<ProblemView>()
            .HasIndex(v => v.ProblemId).HasDatabaseName("IX_ProblemViews_ProblemId");
        modelBuilder.Entity<ProblemView>()
            .HasIndex(v => new { v.ProblemId, v.UserId, v.ViewedAt }).HasDatabaseName("IX_ProblemViews_ProblemId_UserId_ViewedAt");

        // Subdomain lookup — unique nullable (null = subdomain yok)
        modelBuilder.Entity<Institution>()
            .HasIndex(i => i.Subdomain)
            .HasDatabaseName("IX_Institutions_Subdomain")
            .IsUnique()
            .HasFilter("[Subdomain] IS NOT NULL");
        // ─────────────────────────────────────────────────────────────────────

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Topic> Topics { get; set; }
    public DbSet<Solution> Solutions { get; set; }
    public DbSet<Problem> Problems { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Log> Logs { get; set; }
    public DbSet<EmailVerification> EmailVerifications { get; set; }
    public DbSet<SolutionVote> SolutionVotes { get; set; }
    public DbSet<Report> Reports { get; set; }
    public DbSet<Institution> Institutions { get; set; }
    public DbSet<ProblemTopic> ProblemTopics { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<SystemSettings> SystemSettings { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<UserWarning> UserWarnings { get; set; }
    public DbSet<ProblemFollow> ProblemFollowers { get; set; }
    public DbSet<TopicFollow> TopicFollowers { get; set; }
    public DbSet<SavedSolution> SavedSolutions { get; set; }
    public DbSet<LegalAgreement> LegalAgreements { get; set; }
    public DbSet<UserAgreementAcceptance> UserAgreementAcceptances { get; set; }
    public DbSet<ProblemUpvote> ProblemUpvotes { get; set; }
    public DbSet<AboutPageSection> AboutPageSections { get; set; }
    public DbSet<FeatureGroup> FeatureGroups { get; set; }
    public DbSet<FeatureDefinition> FeatureDefinitions { get; set; }
    public DbSet<Capability> Capabilities { get; set; }
    public DbSet<UserCapability> UserCapabilities { get; set; }
    public DbSet<CapabilityTemplate> CapabilityTemplates { get; set; }
    public DbSet<TemplateVersion> TemplateVersions { get; set; }
    public DbSet<TemplateItem> TemplateItems { get; set; }
    public DbSet<CapabilityAuditLog> CapabilityAuditLogs { get; set; }
    public DbSet<UserAppliedTemplate> UserAppliedTemplates { get; set; }
    public DbSet<InstitutionFeatureValue> InstitutionFeatureValues { get; set; }
    public DbSet<DynamicRule> DynamicRules { get; set; }
    public DbSet<WorkflowTrigger> WorkflowTriggers { get; set; }
    public DbSet<WorkflowField> WorkflowFields { get; set; }
    public DbSet<WorkflowAction> WorkflowActions { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; }
    public DbSet<WorkflowLog> WorkflowLogs { get; set; }

    // Faz 2 — Durable Pipeline
    public DbSet<WorkflowDefinition> WorkflowDefinitions { get; set; }
    public DbSet<WorkflowVersion> WorkflowVersions { get; set; }
    public DbSet<WorkflowRun> WorkflowRuns { get; set; }
    public DbSet<NodeRun> NodeRuns { get; set; }
    public DbSet<ActionRun> ActionRuns { get; set; }
    public DbSet<RuleContextSnapshot> RuleContextSnapshots { get; set; }
    public DbSet<WorkflowDeadLetter> WorkflowDeadLetters { get; set; }

    // Kill Switch
    public DbSet<SystemKillSwitch> SystemKillSwitches { get; set; }

    // Duyurular
    public DbSet<Announcement> Announcements { get; set; }

    // Güvenlik olayları
    public DbSet<SecurityEvent> SecurityEvents { get; set; }

    // Epic D — Unvan/Rozet + Resmi Yanıt
    public DbSet<UserTitle> UserTitles { get; set; }
    public DbSet<OfficialResponse> OfficialResponses { get; set; }

    // Epic E — Çok Katmanlı Sohbet Sistemi
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }

    // Epic G — Medya Yaşam Döngüsü
    public DbSet<MediaAsset> MediaAssets { get; set; }

    // Problem görüntüleme takibi
    public DbSet<ProblemView> ProblemViews { get; set; }
}