using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TheAccountant.Interfaces;
using TheAccountant.Services;
using TheAccountant.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllersWithViews(); // Needed for [Area] controllers

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// External Auth via Google (OpenID Connect)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "Google";
})
.AddCookie()
.AddOpenIdConnect("Google", options =>
{
    var config = builder.Configuration;

    options.ClientId = config["Authentication:Google:ClientId"];
    options.ClientSecret = config["Authentication:Google:ClientSecret"];
    options.Authority = "https://accounts.google.com";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.CallbackPath = "/signin-google";

    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    options.SaveTokens = true;
    options.CallbackPath = "/signin-google";

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        ValidateIssuer = true
    };
});

builder.Services.AddScoped<ITransactionImportService, TransactionImportService>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// ✅ VERY IMPORTANT for Areas & Razor Pages
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();     // Enables attribute-routed [Area] controllers
    endpoints.MapRazorPages();      // Enables Identity UI and RP endpoints
    endpoints.MapDefaultControllerRoute(); // Optional if you want /Home/Index
});

app.Run();
