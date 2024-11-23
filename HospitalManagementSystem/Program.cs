using HospitalManagementSystem;
using HospitalManagementSystem.Repository;
using HospitalManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddTransient<IUserService, UserService>();
//builder.Services.AddScoped<RedirectMiddleware>();

var connectionString = builder.Configuration.GetConnectionString("localdb")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddDbContext<IdentityContext>(options =>
{
    options.UseSqlServer(connectionString);
});

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddRoles<IdentityRole>() // Rolleri eklemek için gerekli
    .AddEntityFrameworkStores<IdentityContext>();

var app = builder.Build();


// Seed roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        await SeedRoles(roleManager); // Rolleri seed etme iþlemi
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding roles: {ex.Message}");
    }
}


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Kullanýcý kimlik doðrulama iþlemleri
app.UseAuthorization();  // Kullanýcý yetkilendirme iþlemleri

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
app.MapControllers();

app.Run();


// SeedRoles metodu
static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
{
    if (!await roleManager.RoleExistsAsync("Patient"))
        await roleManager.CreateAsync(new IdentityRole("Patient"));

    if (!await roleManager.RoleExistsAsync("Doctor"))
        await roleManager.CreateAsync(new IdentityRole("Doctor"));

    if (!await roleManager.RoleExistsAsync("Manager"))
        await roleManager.CreateAsync(new IdentityRole("Manager"));
}
