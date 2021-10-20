using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MVCAddressBook.Data;
using MVCAddressBook.Models;
using MVCAddressBook.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAddressBook.Services
{
    // Responsible for making sure there is at least one user and some data in the database
    public static class DataService
    {
        public static async Task ManageDataAsync(IHost host)
        {
            using var svcScope = host.Services.CreateScope();
            var svcProvider = svcScope.ServiceProvider;
            // Database Access
            var dbContextSvc = svcProvider.GetRequiredService<ApplicationDbContext>();
            // User Manager to create a default user
            var userManagerSvc = svcProvider.GetRequiredService<UserManager<AppUser>>();
            // Category Service to add new contacts to categories
            var categorySvc = svcProvider.GetRequiredService<ICategoryService>();
            // For published applications - this runs update-database on production code
            await dbContextSvc.Database.MigrateAsync();

            // Custom Address Book seeding methods
            await SeedDefaultUserAsync(userManagerSvc);
            await SeedDefaultContacts(dbContextSvc);
            await SeedDefaultCategoriesAsync(dbContextSvc);
            await DefaultCategoryAssign(categorySvc, dbContextSvc);

        }

        private async static Task SeedDefaultUserAsync(UserManager<AppUser> userManagerSvc)
        {
            var defaultUser = new AppUser
            {
                UserName = "tonystark@mailinator.com",
                Email = "tonystark@mailinator.com",
                FirstName = "Tony",
                LastName = "Stark",
                EmailConfirmed = true,
            };
            try
            {
                var user = await userManagerSvc.FindByNameAsync(defaultUser.Email);
                if (user is null)
                {
                    await userManagerSvc.CreateAsync(defaultUser, "Abc&123!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("**** ERROR ****");
                Console.WriteLine("Error seeding default user");
                Console.WriteLine(ex.Message);
                Console.WriteLine("**** END OF ERROR ****");
            }
        }
        private async static Task SeedDefaultContacts(ApplicationDbContext dbContextSvc)
        {
            var userId = dbContextSvc.Users.FirstOrDefault(u => u.Email == "tonystark@mailinator.com").Id;
            var defaultContact = new Contact
            {
                UserId = userId,
                Created = DateTime.Now,
                FirstName = "Henry",
                LastName = "McCoy",
                Address1 = "1407 Graymalkin Ln.",
                City = "Salem Center",
                Phone = "555-111-3232",
                State = Enums.States.NY,
                Email = "hankmccoy@mailinator.com"
            };
            try
            {
                var contact = await dbContextSvc.Contacts.AnyAsync(c => c.Email == "hankmccoy@mailinator.com" && c.UserId == userId);
                if (!contact)
                {
                    await dbContextSvc.AddAsync(defaultContact);
                    await dbContextSvc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("**** ERROR ****");
                Console.WriteLine("Error seeding default user");
                Console.WriteLine(ex.Message);
                Console.WriteLine("**** END OF ERROR ****");
            }
        }
        private async static Task SeedDefaultCategoriesAsync(ApplicationDbContext dbContextSvc)
        {
            var userId = dbContextSvc.Users.FirstOrDefault(u => u.Email == "tonystark@mailinator.com").Id;
            var defaultCategory = new Category
            {
                UserId = userId,
                Name = "X-Men"
            };
            try
            {
                var category = await dbContextSvc.Categories.AnyAsync(c => c.Name == "X-Men" && c.UserId == userId);
                if (!category)
                {
                    await dbContextSvc.AddAsync(defaultCategory);
                    await dbContextSvc.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("**** ERROR ****");
                Console.WriteLine("Error seeding default user");
                Console.WriteLine(ex.Message);
                Console.WriteLine("**** END OF ERROR ****");
            }
        }
        private async static Task DefaultCategoryAssign(ICategoryService categorySvc, ApplicationDbContext dbContextSvc)
        {
            var user = dbContextSvc.Users
                .Include(c => c.Categories)
                .Include(c => c.Contacts)
                .FirstOrDefault(u => u.Email == "tonystark@mailinator.com");

            foreach (var contact in user.Contacts)
            {
                foreach (var category in user.Categories)
                {
                    await categorySvc.AddContactToCategoryAsync(category.Id, contact.Id);
                }
            }

        }

    }
}
