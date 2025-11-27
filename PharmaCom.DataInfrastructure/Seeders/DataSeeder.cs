using Microsoft.AspNetCore.Identity;
using PharmaCom.DataInfrastructure.Data;
using PharmaCom.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PharmaCom.DataInfrastructure.Seeders
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(
            ApplicationDBContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // ✅ Seed Roles First
            await SeedRolesAsync(roleManager);

            // ✅ Seed Default Users
            await SeedDefaultUsersAsync(userManager);

            // Seed Categories if none exist
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Pain Relief" },
                    new Category { Name = "Antibiotics" },
                    new Category { Name = "Vitamins & Supplements" },
                    new Category { Name = "Digestive Health" },
                    new Category { Name = "Skin Care" }
                };

                context.Categories.AddRange(categories);
                context.SaveChanges();

                // Retrieve category IDs for product seeding
                var categoryIds = context.Categories.Select(c => c.Id).ToList();
            }

            // Seed Products if none exist (200 products, 40 per category)
            if (!context.Products.Any())
            {
                var random = new Random(42); // Seeded for reproducibility
                var categoryIds = context.Categories.Select(c => c.Id).ToList();
                var brands = new[] { "Pfizer", "Johnson & Johnson", "GSK", "Novartis", "Sanofi", "Merck", "AbbVie", "Bayer" };
                var forms = new[] { "Tablet", "Capsule", "Syrup", "Injection", "Cream", "Gel", "Powder" };
                var productNames = new List<string>
                {
                    // Pain Relief (40 slots, but we'll generate)
                    "Acetaminophen 500mg", "Ibuprofen 200mg", "Aspirin 325mg", "Naproxen 250mg", "Codeine 30mg",
                    "Tramadol 50mg", "Oxycodone 5mg", "Morphine 10mg", "Fentanyl Patch", "Lidocaine Cream",
                    "Menthol Gel", "Capsaicin Cream", "Diclofenac Gel", "Ketorolac 10mg", "Celecoxib 200mg"
                };
                productNames.AddRange(Enumerable.Repeat("Generic Analgesic", 25)); // Filler for category 1

                var productNames2 = new List<string>
                {
                    // Antibiotics
                    "Amoxicillin 500mg", "Azithromycin 250mg", "Ciprofloxacin 500mg", "Doxycycline 100mg", "Cephalexin 500mg",
                    "Clindamycin 300mg", "Metronidazole 500mg", "Levofloxacin 500mg", "Erythromycin 250mg", "Penicillin V 500mg",
                    "Augmentin 875mg", "Bactrim DS", "Zithromax Pack", "Flagyl ER", "Tetracycline 500mg"
                };
                productNames2.AddRange(Enumerable.Repeat("Generic Antibiotic", 25)); // Filler for category 2

                var productNames3 = new List<string>
                {
                    // Vitamins & Supplements
                    "Vitamin D3 2000IU", "Multivitamin Daily", "Omega-3 Fish Oil", "Calcium 600mg", "Vitamin C 1000mg",
                    "B-Complex", "Iron 65mg", "Magnesium 400mg", "Probiotic 10B CFU", "Zinc 50mg",
                    "CoQ10 100mg", "Turmeric Curcumin", "Melatonin 5mg", "Ginkgo Biloba 120mg", "Elderberry Syrup"
                };
                productNames3.AddRange(Enumerable.Repeat("Generic Supplement", 25)); // Filler for category 3

                var productNames4 = new List<string>
                {
                    // Digestive Health
                    "Omeprazole 20mg", "Lansoprazole 30mg", "Ranitidine 150mg", "Famotidine 20mg", "Pepto-Bismol",
                    "Lactulose Syrup", "Bisacodyl 5mg", "Docusate Sodium", "Metamucil Powder", "Imodium 2mg",
                    "Phazyme Gas Relief", "Tums Antacid", "Maalox Liquid", "Mylanta", "Reglan 10mg"
                };
                productNames4.AddRange(Enumerable.Repeat("Generic Digestive Aid", 25)); // Filler for category 4

                var productNames5 = new List<string>
                {
                    // Skin Care
                    "Hydrocortisone 1% Cream", "Clotrimazole Cream", "Benzoyl Peroxide Gel", "Salicylic Acid Lotion", "Calamine Lotion",
                    "Aloe Vera Gel", "Tea Tree Oil", "Neosporin Ointment", "Eucerin Cream", "Cetaphil Lotion",
                    "Differin Gel", "Tretinoin 0.05%", "Minoxidil 5%", "Acne Wash", "Sunscreen SPF 50"
                };
                productNames5.AddRange(Enumerable.Repeat("Generic Skin Product", 25)); // Filler for category 5

                var allProductNames = new List<string>();
                allProductNames.AddRange(productNames);
                allProductNames.AddRange(productNames2);
                allProductNames.AddRange(productNames3);
                allProductNames.AddRange(productNames4);
                allProductNames.AddRange(productNames5);

                var products = new List<Product>();
                for (int i = 0; i < 200; i++)
                {
                    int catIndex = i / 40; // 40 per category
                    int catId = categoryIds[catIndex];
                    string baseName = allProductNames[i % allProductNames.Count]; // Cycle through names
                    string brand = brands[random.Next(brands.Length)];
                    bool isRx = random.Next(2) == 1; // 50% chance
                    string form = forms[random.Next(forms.Length)];
                    decimal price = (decimal)(random.Next(1000, 10000) / 100.0); // 10.00 to 100.00
                    string description = $"High-quality {baseName} from {brand}. {form} form for effective relief.";
                    string? gtin = random.Next(10) == 0 ? $"0123456789{random.Next(10000, 99999)}" : null; // 10% have GTIN
                    string? imageUrl = random.Next(1, 7).ToString();
                    imageUrl = $"/images/product_0{imageUrl}.png";

                    products.Add(new Product
                    {
                        Name = baseName,
                        Brand = brand,
                        GTIN = gtin,
                        Description = description,
                        Price = price,
                        IsRxRequired = isRx,
                        Form = form,
                        ImageURLString = imageUrl,
                        CategoryId = catId
                    });
                }

                context.Products.AddRange(products);
                context.SaveChanges();
            }
        }

        // ✅ NEW: Seed Roles
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            string[] roleNames = { "Admin", "Pharmacist", "Customer" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                    Console.WriteLine($"✓ Created role: {roleName}");
                }
            }
        }

        // ✅ NEW: Seed Default Users
        private static async Task SeedDefaultUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Seed Default Admin
            var adminEmail = "admin@pharmacom.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    Console.WriteLine($"✓ Created default admin user: {adminEmail}");
                }
                else
                {
                    Console.WriteLine("✗ Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"  - {error.Description}");
                    }
                }
            }

            // Seed Default Pharmacist
            var pharmacistEmail = "pharmacist@pharmacom.com";
            var pharmacistUser = await userManager.FindByEmailAsync(pharmacistEmail);

            if (pharmacistUser == null)
            {
                var pharmacist = new ApplicationUser
                {
                    UserName = "pharmacist",
                    Email = pharmacistEmail,
                    FirstName = "John",
                    LastName = "Pharmacist",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false
                };

                var result = await userManager.CreateAsync(pharmacist, "Pharmacist@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(pharmacist, "Pharmacist");
                    Console.WriteLine($"✓ Created default pharmacist user: {pharmacistEmail}");
                }
            }

            // Seed Default Customer
            var customerEmail = "customer@pharmacom.com";
            var customerUser = await userManager.FindByEmailAsync(customerEmail);

            if (customerUser == null)
            {
                var customer = new ApplicationUser
                {
                    UserName = "customer",
                    Email = customerEmail,
                    FirstName = "Jane",
                    LastName = "Customer",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                    TwoFactorEnabled = false,
                    LockoutEnabled = false
                };

                var result = await userManager.CreateAsync(customer, "Customer@123");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(customer, "Customer");
                    Console.WriteLine($"✓ Created default customer user: {customerEmail}");
                }
            }
        }
    }
}
