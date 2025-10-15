using Microsoft.EntityFrameworkCore;
using PharmaCom.DataInfrastructure.Data;
using PharmaCom.DataInfrastructure.Implementation;
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
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            var stripeSettings = builder.Configuration.GetSection("Stripe");
            StripeConfiguration.ApiKey = stripeSettings["SecretKey"];

            var app = builder.Build();

            // Configure the HTTP request pipeline. 
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
               
                app.UseHsts();
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
