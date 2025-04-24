using Microsoft.EntityFrameworkCore;
using RegroupUserUpdater.Data;
using RegroupUserUpdater.Endpoints;
using RegroupUserUpdater.Interfaces;
using RegroupUserUpdater.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SftpSettings>(
    builder.Configuration.GetSection("sftp"));

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=addresses.db"));

// Register services
builder.Services.AddScoped<IAddressService, AddressService>();
builder.Services.AddScoped<IRegroupApiService, RegroupApiService>();
builder.Services.AddScoped<ICsvService, CsvService>();

builder.Services.AddHostedService<AddressDbConsumer>();
builder.Services.AddHostedService<LocationUpdaterConsumer>();
builder.Services.AddHostedService<NotificationsConsumer>();

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