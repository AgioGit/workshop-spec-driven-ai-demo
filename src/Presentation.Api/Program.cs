using Application.UseCases.GetEquipment;
using Core.Domain.EquipmentAggregate.Repositories;
using FastEndpoints;
using FastEndpoints.Swagger;
using Infrastructure.Data.Persistence;
using Infrastructure.Data.Persistence.Repositories;
using Infrastructure.Data.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BookingDbContext>(opt =>
    opt.UseInMemoryDatabase("WorkshopDemo"));

builder.Services.AddScoped<IEquipmentReadRepository, EquipmentReadRepository>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetEquipmentInteractor).Assembly));

builder.Services.AddAuthorization(opt =>
    opt.AddPolicy("AppAccess", p => p.RequireAssertion(_ => true)));

builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Equipment Booking Demo API";
        s.Version = "v1";
        s.Description = "Workshop demo — GET /get-equipment with optional filtering by location and status.";
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
    DataSeeder.Seed(db, dataDir);
}

app.UseAuthorization();
app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
