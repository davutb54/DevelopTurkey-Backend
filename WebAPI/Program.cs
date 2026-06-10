using Business.Concrete;
using Business.Concrete.Actions;
using Business.Abstract;
using Business.Consumers;
using Business.Workflow.Messages;
using MassTransit;
using WebAPI.Hubs;
using WebAPI.SignalR;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Security.Encryption;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using System.Globalization;
using System.Net; // IPAddress için
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// WebAPI: request parsing should be culture-invariant (e.g., multipart/form-data doubles from JS use '.' decimal)
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IUserDal, EfUserDal>();

builder.Services.AddScoped<ITopicService, TopicManager>();
builder.Services.AddScoped<ITopicDal, EfTopicDal>();

builder.Services.AddScoped<IProblemService, ProblemManager>();
builder.Services.AddScoped<IProblemDal, EfProblemDal>();

builder.Services.AddScoped<IProblemUpvoteService, ProblemUpvoteManager>();
builder.Services.AddScoped<IProblemUpvoteDal, EfProblemUpvoteDal>();

builder.Services.AddScoped<IProblemViewService, ProblemViewManager>();
builder.Services.AddScoped<IProblemViewDal, EfProblemViewDal>();

builder.Services.AddScoped<ISolutionService, SolutionManager>();
builder.Services.AddScoped<ISolutionDal, EfSolutionDal>();

builder.Services.AddScoped<ICommentService, CommentManager>();
builder.Services.AddScoped<ICommentDal, EfCommentDal>();

builder.Services.AddScoped<ILogDal, EfLogDal>();
builder.Services.AddScoped<ITokenHelper, JwtHelper>();

builder.Services.AddScoped<ISolutionVoteService, SolutionVoteManager>();
builder.Services.AddScoped<ISolutionVoteDal, EfSolutionVoteDal>();

builder.Services.AddScoped<IEmailVerificationService, EmailVerificationManager>();
builder.Services.AddScoped<IEmailVerificationDal, EfEmailVerificationDal>();

builder.Services.AddScoped<IEmailHelper, SmtpEmailHelper>();
builder.Services.AddScoped<ILogService, LogManager>();

builder.Services.Configure<Core.CrossCuttingConcerns.Logging.ExceptionFileLoggingOptions>(builder.Configuration.GetSection("ExceptionFileLogging"));
builder.Services.AddSingleton<Core.CrossCuttingConcerns.Logging.IExceptionFileLogger, WebAPI.Services.ExceptionFileLogger>();

builder.Services.AddScoped<IReportDal, EfReportDal>();
builder.Services.AddScoped<IReportService, ReportManager>();

builder.Services.AddScoped<IInstitutionDal, EfInstitutionDal>();
builder.Services.AddScoped<IInstitutionService, InstitutionManager>();

builder.Services.AddScoped<IAdminService, AdminManager>();
builder.Services.AddScoped<IMetricsService, MetricsManager>();
builder.Services.AddSingleton<Core.CrossCuttingConcerns.Monitoring.ISystemMonitor, Core.CrossCuttingConcerns.Monitoring.SystemMonitorManager>();

builder.Services.AddScoped<Core.Utilities.Context.IClientContext, WebAPI.Context.WebClientContext>();
builder.Services.AddScoped<Core.Utilities.Context.ITenantProvider, WebAPI.Context.WebTenantProvider>();
builder.Services.AddScoped<IProblemTopicDal, EfProblemTopicDal>();

builder.Services.AddScoped<IFeedbackDal, EfFeedbackDal>();
builder.Services.AddScoped<IFeedbackService, FeedbackManager>();

builder.Services.AddScoped<IAnnouncementDal, EfAnnouncementDal>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementManager>();

builder.Services.AddScoped<IUserTitleDal, EfUserTitleDal>();
builder.Services.AddScoped<IUserTitleService, UserTitleManager>();

builder.Services.AddScoped<IOfficialResponseDal, EfOfficialResponseDal>();
builder.Services.AddScoped<IOfficialResponseService, OfficialResponseManager>();

builder.Services.AddScoped<ISystemSettingsDal, EfSystemSettingsDal>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsManager>();

builder.Services.AddScoped<INotificationDal, EfNotificationDal>();
builder.Services.AddScoped<INotificationService, NotificationManager>();

builder.Services.AddScoped<IUserWarningDal, EfUserWarningDal>();
builder.Services.AddScoped<IUserWarningService, UserWarningManager>();

builder.Services.AddScoped<IProblemFollowDal, EfProblemFollowDal>();
builder.Services.AddScoped<IProblemFollowService, ProblemFollowManager>();

builder.Services.AddScoped<ITopicFollowDal, EfTopicFollowDal>();
builder.Services.AddScoped<ITopicFollowService, TopicFollowManager>();

builder.Services.AddScoped<ISavedSolutionDal, EfSavedSolutionDal>();
builder.Services.AddScoped<ISavedSolutionService, SavedSolutionManager>();

builder.Services.AddScoped<ILegalAgreementDal, EfLegalAgreementDal>();
builder.Services.AddScoped<IUserAgreementAcceptanceDal, EfUserAgreementAcceptanceDal>();
builder.Services.AddScoped<ILegalAgreementService, LegalAgreementManager>();

builder.Services.AddScoped<IAboutPageSectionService, AboutPageSectionManager>();
builder.Services.AddScoped<IAboutPageSectionDal, EfAboutPageSectionDal>();

builder.Services.AddScoped<IFeatureGroupDal, EfFeatureGroupDal>();
builder.Services.AddScoped<IFeatureGroupService, FeatureGroupManager>();

builder.Services.AddScoped<IFeatureDefinitionDal, EfFeatureDefinitionDal>();
builder.Services.AddScoped<IFeatureDefinitionService, FeatureDefinitionManager>();

builder.Services.AddScoped<IDynamicRuleDal, EfDynamicRuleDal>();
builder.Services.AddScoped<IDynamicRuleService, DynamicRuleManager>();

builder.Services.AddScoped<IWorkflowTriggerDal, EfWorkflowTriggerDal>();
builder.Services.AddScoped<IWorkflowTriggerService, WorkflowTriggerManager>();
builder.Services.AddScoped<IWorkflowFieldDal, EfWorkflowFieldDal>();
builder.Services.AddScoped<IWorkflowFieldService, WorkflowFieldManager>();
builder.Services.AddScoped<IWorkflowActionDal, EfWorkflowActionDal>();
builder.Services.AddScoped<IWorkflowActionService, WorkflowActionManager>();

// Action Registry — her handler IWorkflowActionHandler olarak kayıtlı;
// WorkflowActionDispatcher bunları IEnumerable<IWorkflowActionHandler> ile alır
// ve ActionCode'a göre dictionary'e dönüştürür.
builder.Services.AddScoped<IWorkflowActionHandler, SendEmailActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, SendNotificationActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, SendBulkNotificationActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, BanUserActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, UnbanUserActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, WarnUserActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, ResolveProblemActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, HighlightProblemActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, DeleteProblemActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, ReportProblemActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, ApproveSolutionActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, RejectSolutionActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, HighlightSolutionActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, DeleteSolutionActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, DeleteCommentActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, LogEventActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, WebhookActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, GrantCapabilityActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, ApplyCapabilityTemplateActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, AssignProblemToInstitutionActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, ChangeProblemStatusActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, CreateAnnouncementActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, TriggerWorkflowActionHandler>();
builder.Services.AddScoped<IWorkflowActionHandler, SendChatMessageActionHandler>();

builder.Services.AddScoped<IWorkflowActionDispatcher, WorkflowActionDispatcher>();
builder.Services.AddScoped<IWorkflowInterpreterService, WorkflowInterpreterManager>();

builder.Services.AddScoped<IWorkflowLogDal, EfWorkflowLogDal>();
builder.Services.AddScoped<IWorkflowLogService, WorkflowLogManager>();

// Workflow Event Bus — yeni handler eklemek için IWorkflowEventHandler olarak kaydet
builder.Services.AddScoped<IWorkflowEventHandler, WorkflowEventHandler>();
builder.Services.AddScoped<IRuleContextEnricher, RuleContextEnricher>();
builder.Services.AddScoped<IWorkflowEventBus, WorkflowEventBus>();

// Roslyn C# Script Motoru - Yalnızca SuperAdmin yetkisiyle çalışır
builder.Services.AddScoped<IRuleExecutionService, RuleExecutionManager>();

builder.Services.AddScoped<IInstitutionFeatureValueDal, EfInstitutionFeatureValueDal>();
builder.Services.AddScoped<IInstitutionFeatureService, InstitutionFeatureManager>();
builder.Services.AddScoped<IMentionService, MentionManager>();
builder.Services.AddScoped<ILiveNotificationService, SignalRLiveNotificationManager>();

builder.Services.AddScoped<IEmailTemplateDal, EfEmailTemplateDal>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateManager>();

builder.Services.AddScoped<DevelopTurkeyContext, DevelopTurkeyContext>();

// Capability sistemi
builder.Services.AddScoped<ICapabilityDal, EfCapabilityDal>();
builder.Services.AddScoped<IUserCapabilityDal, EfUserCapabilityDal>();
builder.Services.AddScoped<ICapabilityTemplateDal, EfCapabilityTemplateDal>();
builder.Services.AddScoped<ITemplateVersionDal, EfTemplateVersionDal>();
builder.Services.AddScoped<ITemplateItemDal, EfTemplateItemDal>();
builder.Services.AddScoped<ICapabilityAuditLogDal, EfCapabilityAuditLogDal>();
builder.Services.AddScoped<IUserAppliedTemplateDal, EfUserAppliedTemplateDal>();

builder.Services.AddScoped<ICapabilityService, CapabilityManager>();
builder.Services.AddScoped<ICapabilityAuditService, CapabilityAuditManager>();
builder.Services.AddScoped<IUserCapabilityService, UserCapabilityManager>();
builder.Services.AddScoped<ICapabilityTemplateService, CapabilityTemplateManager>();

// Snapshot singleton — app boyunca tek instance, tüm scope'lardan paylaşılır
builder.Services.AddSingleton<Core.Utilities.Authorization.ICapabilitySnapshot, Business.Concrete.CapabilitySnapshotService>();
builder.Services.AddSingleton<Core.Utilities.Authorization.ICapabilityResolver, Business.Concrete.CapabilityResolver>();
builder.Services.AddScoped<Core.Utilities.Authorization.ICapabilityPolicy, Business.Concrete.CapabilityPolicy>();

// Snapshot startup loader
builder.Services.AddHostedService<WebAPI.HostedServices.CapabilitySnapshotInitializer>();

// Kill Switch
builder.Services.AddScoped<ISystemKillSwitchDal, EfSystemKillSwitchDal>();
builder.Services.AddScoped<IKillSwitchService, KillSwitchManager>();

// Sqids ID obfuscation (singleton — deterministic, no state)
builder.Services.AddSingleton<Core.Utilities.Hashing.IHashidsService, Business.Concrete.HashidsService>();

// Security event log
builder.Services.AddScoped<ISecurityEventDal, EfSecurityEventDal>();
builder.Services.AddScoped<ISecurityEventService, SecurityEventManager>();

// Epic G — Medya Yaşam Döngüsü
builder.Services.AddScoped<IMediaAssetDal, EfMediaAssetDal>();
builder.Services.AddScoped<IMediaAssetService, MediaAssetManager>();
builder.Services.AddHostedService<WebAPI.HostedServices.MediaCleanupHostedService>();

// Epic E — Sohbet Sistemi
builder.Services.AddScoped<IConversationDal, EfConversationDal>();
builder.Services.AddScoped<IConversationParticipantDal, EfConversationParticipantDal>();
builder.Services.AddScoped<IMessageDal, EfMessageDal>();
builder.Services.AddScoped<IConversationService, ConversationManager>();
builder.Services.AddScoped<IMessageService, MessageManager>();

// Faz 2 — Durable Pipeline DAL'ları
builder.Services.AddScoped<IWorkflowDefinitionDal, EfWorkflowDefinitionDal>();
builder.Services.AddScoped<IWorkflowVersionDal, EfWorkflowVersionDal>();
builder.Services.AddScoped<IWorkflowRunDal, EfWorkflowRunDal>();
builder.Services.AddScoped<INodeRunDal, EfNodeRunDal>();
builder.Services.AddScoped<IActionRunDal, EfActionRunDal>();
builder.Services.AddScoped<IRuleContextSnapshotDal, EfRuleContextSnapshotDal>();
builder.Services.AddScoped<IWorkflowDeadLetterDal, EfWorkflowDeadLetterDal>();

// Faz 2 — Durable Pipeline Servisleri
builder.Services.AddScoped<IWorkflowRunService, WorkflowRunManager>();
builder.Services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestrator>();

// Faz 2 — MassTransit message bus
var busTransport = builder.Configuration["MessageBus:Transport"] ?? "InMemory";
builder.Services.AddMassTransit(x =>
{
    // Consumer'ları kaydet
    x.AddConsumer<NodeRunConsumer>();
    x.AddConsumer<NodeRunFaultConsumer>();
    x.AddConsumer<ActionRunConsumer>();
    x.AddConsumer<DeadLetterConsumer>();

    if (busTransport.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingRabbitMq((ctx, cfg) =>
        {
            var rmq = builder.Configuration.GetSection("MessageBus:RabbitMQ");
            cfg.Host(rmq["Host"] ?? "localhost", ushort.Parse(rmq["Port"] ?? "5672"),
                rmq["VirtualHost"] ?? "/", h =>
                {
                    h.Username(rmq["Username"] ?? "guest");
                    h.Password(rmq["Password"] ?? "guest");
                });

            // Queue tanımları
            cfg.ReceiveEndpoint("workflow.noderun", ep =>
            {
                ep.ConfigureConsumer<NodeRunConsumer>(ctx);
                ep.UseMessageRetry(r => r.Exponential(5,
                    TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(2)));
            });

            cfg.ReceiveEndpoint("workflow.actionrun", ep =>
            {
                ep.ConfigureConsumer<ActionRunConsumer>(ctx);
                ep.UseMessageRetry(r => r.Exponential(3,
                    TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(500)));
            });

            cfg.ReceiveEndpoint("workflow.deadletter", ep =>
            {
                ep.ConfigureConsumer<DeadLetterConsumer>(ctx);
            });

            cfg.ConfigureEndpoints(ctx);
        });
    }
    else
    {
        // Development: InMemory transport (RabbitMQ gerekmez)
        x.UsingInMemory((ctx, cfg) =>
        {
            cfg.ConfigureEndpoints(ctx);
        });
    }
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ICaptchaService, CaptchaManager>();
// Workflow webhook action'ı için: HttpClient default timeout'u (100s) workflow
// bağlamında çok uzun. 10s ile sınırla — hung webhook tüm kuralı bloke etmesin.
builder.Services.AddHttpClient<IWebhookClient, WebAPI.Services.WebhookClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<WebAPI.Services.IGeoLocationService, WebAPI.Services.GeoLocationService>();

var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidAudience = tokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.ContainsKey("token"))
                {
                    context.Token = context.Request.Cookies["token"];
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
});

builder.Services.AddValidatorsFromAssemblyContaining<Business.ValidationRules.FluentValidation.ProblemValidator>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = (context, _) =>
    {
        var secSvc = context.HttpContext.RequestServices.GetService<ISecurityEventService>();
        if (secSvc != null)
        {
            var ip  = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.HttpContext.Request.Path.Value;
            var userIdStr = context.HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int? userId = int.TryParse(userIdStr, out var uid) ? uid : null;
            secSvc.LogEvent("rate_limited", "medium", ip, path, null, userId);
        }
        context.HttpContext.Response.StatusCode = 429;
        return ValueTask.CompletedTask;
    };

    // Ortak PartitionKey oluşturucu (Aynı modeme bağlı cihazları ayırmak için IP + User-Agent)
    string GetPartitionKey(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var userId = context.User.Identity?.IsAuthenticated == true
            ? context.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            : "guest";

        // Benzersiz cihaz kimliği: IP + UserAgent + Kullanıcı Kimliği
        return $"{ip}_{userAgent}_{userId}";
    }

    // 1. GLOBAL LIMIT (Genel Tarama için, Cihaz Başına 300 İstek / Dakika)
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetPartitionKey(httpContext),
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            }));

    // 2. AUTH LIMIT (Sadece Kayıt/Giriş için, Cihaz Başına 10 İstek / Dakika)
    // Brute Force ve DDoS engellemek için daha katı bir kural.
    options.AddPolicy("AuthLimit", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetPartitionKey(httpContext),
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});


var app = builder.Build();

// === CLOUDFLARE GERÇEK IP ÇÖZÜMÜ ===
// Cloudflare, kullanıcının gerçek IP'sini CF-Connecting-IP header'ına yazar.
// Bu middleware onu okuyarak RemoteIpAddress'i günceller.
// KnownNetworks listesi sayesinde yalnızca Cloudflare'den gelen header'lar kabul edilir.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    // Cloudflare IPv4 aralıkları (https://www.cloudflare.com/ips/)
    KnownNetworks =
    {
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("173.245.48.0"),  20),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("103.21.244.0"),  22),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("103.22.200.0"),  22),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("103.31.4.0"),    22),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("141.101.64.0"),  18),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("108.162.192.0"), 18),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("190.93.240.0"),  20),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("188.114.96.0"),  20),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("197.234.240.0"), 22),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("198.41.128.0"),  17),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("162.158.0.0"),   15),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("104.16.0.0"),    13),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("104.24.0.0"),    14),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.64.0.0"),    13),
        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("131.0.72.0"),    22),
    }
});

// CF-Connecting-IP → RemoteIpAddress override
// X-Forwarded-For bazen birden fazla IP içerdiğinden, Cloudflare'in
// özel header'ı daha güvenilirdir ve tek bir IP döndürür.
app.Use(async (context, next) =>
{
    if (context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp)
        && IPAddress.TryParse(cfIp, out var realIp))
    {
        context.Connection.RemoteIpAddress = realIp;
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self'; connect-src 'self'; frame-ancestors 'none';");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        await next();
    });
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler(c => c.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { success = false, message = "Sunucuda beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
    }));

    app.UseHsts();
}

app.UseMiddleware<WebAPI.Middlewares.ExceptionMiddleware>();
app.UseMiddleware<WebAPI.Middlewares.SystemMonitorMiddleware>();
//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.Value.StartsWith("/api") &&
            !context.Request.Path.Value.StartsWith("/api/hubs/"))
        {
            var expectedToken = builder.Configuration["ApiSettings:SiteToken"];
            var hasHeader = context.Request.Headers.TryGetValue("X-Site-Token", out var token);

            if (!hasHeader || token != expectedToken)
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"message\": \"Erisim reddedildi. Bu API'ye sadece site uzerinden erisim saglanabilir.\"}");
                return;
            }
        }
        await next();
    });
}


app.UseCors("AllowOrigin");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<WebAPI.Middlewares.MaintenanceMiddleware>();
app.UseAuthorization();
app.UseMiddleware<WebAPI.Middlewares.TenantResolutionMiddleware>(); // auth sonrası: claim'ler hazır
app.UseMiddleware<WebAPI.Middlewares.IpWhitelistMiddleware>();
app.UseMiddleware<WebAPI.Middlewares.EnumerationDetectionMiddleware>();

app.MapControllers();
app.MapHub<NotificationHub>("/api/hubs/notification");
app.MapHub<WebAPI.Hubs.ChatHub>("/api/hubs/chat");

// Seeders
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DataAccess.Concrete.EntityFramework.DevelopTurkeyContext>();
    var config  = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    WebAPI.Seeders.InstitutionSeeder.Seed(context);
    WebAPI.Seeders.FeatureSeeder.Seed(context);
    WebAPI.Seeders.EmailTemplateSeeder.Seed(context);
    WebAPI.Seeders.WorkflowReferenceSeeder.Seed(context);
    WebAPI.Seeders.CapabilitySeeder.Seed(context);
    WebAPI.Seeders.CapabilityTemplateSeeder.Seed(context);
    WebAPI.Seeders.BootstrapAdminSeeder.Seed(context, config);
    WebAPI.Seeders.SystemUserSeeder.Seed(context, config);
}

app.Run();