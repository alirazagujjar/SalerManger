using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SellerManging.Data;
using SellerManging.Hubs;
using SellerManging.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("SellerManger"));
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))); //Replace with your connection string

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.Name = "AuthCookie";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<ChatHub>("/chatHub");
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    DbSeeder.SeedAdmin(dbContext);
}
app.Run();