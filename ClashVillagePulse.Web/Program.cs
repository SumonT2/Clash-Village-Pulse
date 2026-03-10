using ClashVillagePulse.Application;
using ClashVillagePulse.Infrastructure;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Pages (Identity UI uses Razor Pages)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// Connection string
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? throw new InvalidOperationException("Missing DefaultConnection");

// Clean Architecture registrations
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connStr);

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<AppDbContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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

// Identity UI endpoints
app.MapRazorPages();

app.Run();