using Microsoft.AspNetCore.Authentication.Cookies;
using VoteHomWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Vote/Login";
        options.AccessDeniedPath = "/Vote/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

// Add HTTP client and services
builder.Services.AddHttpClient<IElectionService, ElectionService>();

// Add session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// Force developer exception page for debugging
app.UseDeveloperExceptionPage();

if (!app.Environment.IsDevelopment())
{
    // app.UseExceptionHandler("/Home/Error"); // Temporarily disabled for debugging
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Vote}/{action=Index}/{id?}");

app.Run();
