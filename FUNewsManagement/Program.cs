using Microsoft.AspNetCore.Authentication.Cookies;
using FUNewsManagement.Services;

namespace FUNewsManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DotNetEnv.Env.Load();
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();
            
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddTransient<FUNewsManagement.Services.AuthHeaderHandler>();
            
            builder.Services.AddHttpClient("BackendApi", client =>
            {
                client.BaseAddress = new Uri("http://localhost:5012/"); // Update with actual API port
            }).AddHttpMessageHandler<FUNewsManagement.Services.AuthHeaderHandler>();
            
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("BackendApi"));

            // Dependency Injection for API Clients
            builder.Services.AddScoped<FUNewsManagement.Services.IAccountService, FUNewsManagement.Services.AccountApiClient>();
            builder.Services.AddScoped<FUNewsManagement.Services.ICategoryService, FUNewsManagement.Services.CategoryApiClient>();
            builder.Services.AddScoped<FUNewsManagement.Services.INewsService, FUNewsManagement.Services.NewsApiClient>();
            builder.Services.AddScoped<FUNewsManagement.Services.ITagService, FUNewsManagement.Services.TagApiClient>();
            builder.Services.AddScoped<FUNewsManagement.Services.ICloudinaryService, FUNewsManagement.Services.CloudinaryService>();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "FUNews.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;

            });

            builder.Services.AddAuthorization();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
