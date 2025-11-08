using Core.Data;
using Core.Hubs;
using Core.Models;
using Core.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 3;
    options.Password.RequiredUniqueChars = 0;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

}).AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DepartmentApprover", policy =>
    policy.RequireRole(RoleNames.DepartmentManager,RoleNames.ContractCreator,RoleNames.DepartmentDeputy)
    .RequireAssertion(context =>
    {
        var httpContext = (context.Resource as HttpContext);
        var contractId = httpContext.Request.RouteValues["id"]?.ToString();
        if (string.IsNullOrEmpty(contractId))
            return false;

        var dbContext = httpContext.RequestServices.GetRequiredService<AppDbContext>();

        var contract = dbContext.Contracts
                          .Include(c => c.CreatedBy)
                          .FirstOrDefault(c => c.Id == int.Parse(contractId));

        if (contract == null || contract.CreatedBy == null)
            return false;

        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var currentUser = userManager.GetUserAsync(httpContext.User).Result;

        return currentUser != null &&
               currentUser.DepartmentId == contract.CreatedBy.DepartmentId &&
               currentUser.IsDepartmentManager;
    }));
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "??? ?? ???????? ????? ?????? ????");
    }
}

app.UseHttpsRedirection();
app.UseRouting();

//app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Home}/{action=Index}/{id?}");
//    endpoints.MapHub<NotificationHub>("/notificationHub");
//});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

//app.MapGet("/Account/Login", (HttpContext context) =>
//{
//    context.Response.Redirect("/Account/Login");
//    return Task.CompletedTask;
//});

app.Run();
