using DapperForge.Configuration;
using DapperForge.Extensions;
using SampleApi.Endpoints;
using SampleApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDapperForge(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Database=SchoolDb;Trusted_Connection=true;TrustServerCertificate=true;";
    options.Provider = DatabaseProvider.SqlServer;
    options.EnableDiagnostics = true;
    options.SlowQueryThreshold = TimeSpan.FromSeconds(2);

    // Register entities for SP validation
    options.RegisterEntity<Student>();

    // Optional: customize convention
    // options.SetConvention(c =>
    // {
    //     c.SelectPrefix = "sel";
    //     c.UpsertPrefix = "up";
    //     c.DeletePrefix = "del";
    //     c.Schema = "dbo";
    // });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapStudentEndpoints();
app.Run();
