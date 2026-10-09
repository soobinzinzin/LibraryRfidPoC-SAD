using Microsoft.EntityFrameworkCore;
using LibraryRfidPoC.Data;
using LibraryRfidPoC.Models;
using LibraryRfidPoC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.Configure<LibraryRulesOptions>(builder.Configuration.GetSection("LibraryRules"));
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LibraryDb")));
builder.Services.AddScoped<BorrowService>();
builder.Services.AddScoped<ReturnService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Kiosk}/{action=Borrow}/{id?}");

app.Run();

