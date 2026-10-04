using EventTixHub.Web.Data;
using EventTixHub.Web.Models;
using EventTixHub.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, Role>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<PromoCodeService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<VenueService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    var roleManager = sp.GetRequiredService<RoleManager<Role>>();
    // Demo data includes known credentials: development only. SeedAsync also seeds roles.
    if (app.Environment.IsDevelopment())
        await DbSeeder.SeedAsync(db, sp.GetRequiredService<UserManager<User>>(), roleManager,
            sp.GetRequiredService<TimeProvider>());
    else
        await DbSeeder.SeedRolesAsync(roleManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
