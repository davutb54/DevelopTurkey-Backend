using Business.Abstract;
using Business.Concrete;
using Core.Utilities.Security.Encryption;
using Core.Utilities.Security.JWT;
using DataAccess.Abstract;
using DataAccess.Concrete.EntityFramework;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();

        policy.WithOrigins("https://www.lucid-sinoussi.89-252-187-226.plesk.page")
              .AllowAnyMethod()
              .AllowAnyHeader();

        policy.WithOrigins("https://davutb54.github.io/developturkey/")
              .AllowAnyMethod()
              .AllowAnyHeader();

        policy.WithOrigins("https://www.turkiyeyigelistirmeplatformu.com")
              .AllowAnyMethod()
              .AllowAnyHeader();

        policy.WithOrigins("https://www.turkiyeyigelistirmeplatformu.com.tr")
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
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowOrigin");
app.UseAuthentication();
app.UseAuthorization(); 

app.MapControllers();

app.Run();