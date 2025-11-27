using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PharmaCom.DataInfrastructure.Data;
using PharmaCom.DataInfrastructure.Implementation;
using PharmaCom.DataInfrastructure.Seeders;
using PharmaCom.Domain.Models;
using PharmaCom.Domain.Repositories;
using PharmaCom.Service.Implementation;
using PharmaCom.Service.Interfaces;
using Stripe;

namespace PharmaCom.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDBContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("cs")));
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDBContext>()
            .AddDefaultTokenProviders();
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IProductService, Service.Implementation.ProductService>();
            builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Configuration.GetSection("Email");
            var stripeSettings = builder.Configuration.GetSection("Stripe");
            StripeConfiguration.ApiKey = stripeSettings["SecretKey"];

            var app = builder.Build();

            // Configure the HTTP request pipeline. 
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");

                app.UseHsts();
            }

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                // Run seeding asynchronously
                Task.Run(async () =>
                {
                    try
                    {
                        var context = services.GetRequiredService<ApplicationDBContext>();
                        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

                        await context.Database.MigrateAsync();

                        await DataSeeder.SeedAsync(context, userManager, roleManager);

                        Console.WriteLine("✓ Database seeding completed successfully");
                    }
                    catch (Exception ex)
                    {
                        var logger = services.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "An error occurred while seeding the database");
                        Console.WriteLine($"✗ Error seeding database: {ex.Message}");
                    }
                }).Wait();
            }


            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
