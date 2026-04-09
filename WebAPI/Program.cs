using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Security.Encryption;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IUserDal, EfUserDal>();

builder.Services.AddScoped<ITopicService, TopicManager>();
builder.Services.AddScoped<ITopicDal, EfTopicDal>();

builder.Services.AddScoped<IProblemService, ProblemManager>();
builder.Services.AddScoped<IProblemDal, EfProblemDal>();

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

builder.Services.AddScoped<IReportDal, EfReportDal>();
builder.Services.AddScoped<IReportService, ReportManager>();

builder.Services.AddScoped<IInstitutionDal, EfInstitutionDal>();
builder.Services.AddScoped<IInstitutionService, InstitutionManager>();

builder.Services.AddScoped<IAdminService, AdminManager>();
builder.Services.AddSingleton<Core.CrossCuttingConcerns.Monitoring.ISystemMonitor, Core.CrossCuttingConcerns.Monitoring.SystemMonitorManager>();

builder.Services.AddScoped<Core.Utilities.Context.IClientContext, WebAPI.Context.WebClientContext>();
builder.Services.AddScoped<IProblemTopicDal, EfProblemTopicDal>();

builder.Services.AddScoped<IFeedbackDal, EfFeedbackDal>();
builder.Services.AddScoped<IFeedbackService, FeedbackManager>();

builder.Services.AddHttpClient<ICaptchaService, CaptchaManager>();

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//app.Use(async (context, next) =>
//{
//    if (context.Request.Path.Value.StartsWith("/api"))
//    {
//        var expectedToken = builder.Configuration["ApiSettings:SiteToken"];
//        var hasHeader = context.Request.Headers.TryGetValue("X-Site-Token", out var token);

//        if (!hasHeader || token != expectedToken)
//        {
//            context.Response.StatusCode = 403;
//            context.Response.ContentType = "application/json";
//            await context.Response.WriteAsync("{\"message\": \"Erisim reddedildi. Bu API'ye sadece site uzerinden erisim saglanabilir.\"}");
//            return;
//        }
//    }
//    await next();
//});

app.UseCors("AllowOrigin");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();