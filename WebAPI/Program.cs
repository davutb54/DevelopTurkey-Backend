using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Helpers.Email;
using Core.Utilities.Security.Encryption;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSingleton<IUserService, UserManager>();
builder.Services.AddSingleton<IUserDal, EfUserDal>();

builder.Services.AddSingleton<ITopicService, TopicManager>();
builder.Services.AddSingleton<ITopicDal, EfTopicDal>();

builder.Services.AddSingleton<IProblemService, ProblemManager>();
builder.Services.AddSingleton<IProblemDal, EfProblemDal>();

builder.Services.AddSingleton<ISolutionService, SolutionManager>();
builder.Services.AddSingleton<ISolutionDal, EfSolutionDal>();

builder.Services.AddSingleton<ICommentService, CommentManager>();
builder.Services.AddSingleton<ICommentDal, EfCommentDal>();

builder.Services.AddSingleton<ILogDal, EfLogDal>();
builder.Services.AddSingleton<ITokenHelper, JwtHelper>();

builder.Services.AddSingleton<ISolutionVoteService, SolutionVoteManager>();
builder.Services.AddSingleton<ISolutionVoteDal, EfSolutionVoteDal>();

builder.Services.AddSingleton<IEmailVerificationService, EmailVerificationManager>();
builder.Services.AddSingleton<IEmailVerificationDal, EfEmailVerificationDal>();

builder.Services.AddSingleton<IEmailHelper, SmtpEmailHelper>();
builder.Services.AddSingleton<ILogService, LogManager>();

builder.Services.AddSingleton<IReportDal, EfReportDal>();
builder.Services.AddSingleton<IReportService, ReportManager>();

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
    });

builder.Services.AddControllers();
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
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 50,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(c => c.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { success = false, message = "Sunucuda beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin." });
    }));
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowOrigin");
app.UseAuthentication();
app.UseAuthorization(); 

app.MapControllers();
app.UseRateLimiter();

app.Run();