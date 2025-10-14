using Microsoft.EntityFrameworkCore;
using Partnerly.Events;
using Partnerly.Events.BaseEvents;
using Partnerly.Hubs;
using Partnerly.Infrastructure.Interfaces;
using Partnerly.Infrastructure.Repositories;
using Partnerly.Infrastructure.Services;
using Partnerly.Infrastructure.Services.HostedServices;
using Partnerly.Infrastructure.Services.MiddlewareServices;
using Partnerly.Models;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddControllersWithViews();
//builder.Services.AddSignalR();

//builder.Services.AddScoped<IPermissionService, PermissionService>();
//builder.Services.AddScoped<IUserService, UserService>();
//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<ILogService, LogService>();
//builder.Services.AddScoped<ILogRepository, LogRepository>();
//builder.Services.AddScoped<IRoleService, RoleService>();
//builder.Services.AddScoped<IRoleRepository, RoleRepository>();
//builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
//builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
//builder.Services.AddScoped<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
//builder.Services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
//builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
//builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
//builder.Services.AddScoped<INotificationService, NotificationService>();
//builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

//builder.Services.AddTransient<IEmailSender, EmailSender>();
//builder.Services.AddHostedService<OnlineStatusService>();

//builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DbContext")));

//builder.Services.AddAuthentication("Cookies")
//    .AddCookie("Cookies", options =>
//    {
//        options.LoginPath = "/Account/Login"; // куда пойдет неавторизованный
//        options.AccessDeniedPath = "/Account/AccessDenied";
//    });

//builder.Services.AddSingleton<IEventBus, EventBus>();

//var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
//    eventBus.Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>();
//}

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseMiddleware<UpdateLastActivityMiddleware>();
//app.UseMiddleware<MaintenanceMiddleware>();
//app.UseMiddleware<RoleChangeMiddleware>();

//app.UseRouting();
//app.UseAuthentication();
//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapHub<NotificationHub>("/notificationHub");

//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    DbInitializer.Initialize(context);
//}

//app.Run();

var builder = WebApplication.CreateBuilder(args);

// -------------------- SERVICES --------------------
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Scoped services
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();
builder.Services.AddScoped<IEmailConfirmationTokenService, EmailConfirmationTokenService>();
builder.Services.AddScoped<IEmailConfirmationTokenRepository, EmailConfirmationTokenRepository>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Transient services
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<UserRegisteredEventHandler>();

// Hosted & middleware-related
builder.Services.AddHostedService<OnlineStatusService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbContext")));

// Authentication
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// EventBus (Singleton)
builder.Services.AddSingleton<IEventBus, EventBus>();

var app = builder.Build();

// -------------------- INITIALIZATION --------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // 1. Инициализация базы
    var context = services.GetRequiredService<AppDbContext>();
    DbInitializer.Initialize(context);

    // 2. Подписка на события
    var eventBus = services.GetRequiredService<IEventBus>();
    eventBus.Subscribe<UserRegisteredEvent, UserRegisteredEventHandler>();
}

// -------------------- MIDDLEWARE --------------------

// Use middlewares до маршрутизации
app.UseMiddleware<UpdateLastActivityMiddleware>();
app.UseMiddleware<MaintenanceMiddleware>();
app.UseMiddleware<RoleChangeMiddleware>();

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

// -------------------- ROUTES & HUBS --------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<NotificationHub>("/notificationHub");

app.Run();

