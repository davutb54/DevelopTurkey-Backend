var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowOrigin", policy =>
	{
		policy.WithOrigins("http://localhost:3000").AllowAnyMethod().AllowAnyHeader();
		policy.WithOrigins("https://www.lucid-sinoussi.89-252-187-226.plesk.page").AllowAnyMethod().AllowAnyHeader();
		policy.WithOrigins("https://davutb54.github.io/developturkey/").AllowAnyMethod().AllowAnyHeader();
		policy.WithOrigins("https://www.turkiyeyigelistirmeplatformu.com").AllowAnyMethod().AllowAnyHeader();
		policy.WithOrigins("https://www.turkiyeyigelistirmeplatformu.com.tr").AllowAnyMethod().AllowAnyHeader();
	});
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

app.UseAuthorization();

app.MapControllers();

app.UseCors("AllowOrigin");

app.Run();
