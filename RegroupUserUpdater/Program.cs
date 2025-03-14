using Microsoft.EntityFrameworkCore;
using RegroupUserUpdater.Data;
using RegroupUserUpdater.Endpoints;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=addresses.db"));

// Register services
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddSingleton<IRegroupApiService, RegroupApiService>();
builder.Services.AddSingleton<ICsvService, CsvService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map endpoints
app.MapAddressEndpoints();
app.MapCsvEndpoints();

app.Run();