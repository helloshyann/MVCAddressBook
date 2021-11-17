using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVCAddressBook.Data;
using MVCAddressBook.Models;
using MVCAddressBook.Models.ViewModels;
using MVCAddressBook.Services;
using MVCAddressBook.Services.Interfaces;

namespace MVCAddressBook.Controllers
{
    [Authorize] // CLASS LEVEL AUTHORIZATION: All actions in this controller will require authorization
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IImageService _imageService;
        private readonly int _minSize = 1024; // 1 kB of data
        private readonly int _maxSize = 2097152; // 2 MB of data
        private readonly SearchService _searchService;
        private readonly ICategoryService _categoryService;

        public ContactsController(ApplicationDbContext context, UserManager<AppUser> userManager, IImageService imageService, SearchService searchService, ICategoryService categoryService)
        {
            _context = context;
            _userManager = userManager;
            _imageService = imageService;
            _searchService = searchService;
            _categoryService = categoryService;
        }

        // GET: Contacts

        public async Task<IActionResult> Index()
        {
            // Get the ID of the currently logged in user and return only the contacts created by the user
            var userId = _userManager.GetUserId(User);
            var model = new ContactIndexViewModel();
            model.Contacts = await _context.Contacts.Include(c => c.Categories).Where(c => c.UserId == userId).ToListAsync();
            model.CategoryFilter = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            return View(model);
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contact == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var model = new ContactCreateViewModel();
            model.Contact = contact;
            model.CategoryList = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");

            return View(model);
        }

        // GET: Contacts/Create
        public IActionResult Create()
        {
            var userId = _userManager.GetUserId(User);
            var model = new ContactCreateViewModel();
            model.CategoryList = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            return View(model);
        }

        // POST: Contacts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Birthday,Address1,Address2,City,State,ZipCode,Email,Phone,ImageFile")] Contact contact, List<int> categoryList)
        {
            if (ModelState.IsValid)
            {
                contact.UserId = _userManager.GetUserId(User);
                contact.Created = DateTime.Now;

                if(contact.ImageFile is not null)
                {
                    var fileSize = _imageService.Size(contact.ImageFile);
                    if (fileSize >= _minSize && fileSize <= _maxSize)
                    {
                        contact.ImageData = await _imageService.EncodeImageAsync(contact.ImageFile);
                        contact.ImageType = _imageService.ContentType(contact.ImageFile);
                    }                    
                }

                _context.Add(contact);
                await _context.SaveChangesAsync();

                foreach(var categoryId in categoryList)
                {
                    await _categoryService.AddContactToCategoryAsync(categoryId, contact.Id);
                }


                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            var model = new ContactCreateViewModel();
            model.Contact = contact;
            model.CategoryList = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            return View(model);
        }

        // GET: Contacts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts.Include(c => c.Categories).FirstOrDefaultAsync(c => c.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            var model = new ContactCreateViewModel();
            model.Contact = contact;
            model.CategoryList = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            return View(model);
        }

        // POST: Contacts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserId,FirstName,LastName,Birthday,Address1,Address2,City,State,ZipCode,Email,Phone,Created,ImageData,ImageType,ImageFile")] Contact contact, List<int> categoryList)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {

                    if (contact.ImageFile is not null)
                    {
                        var fileSize = _imageService.Size(contact.ImageFile);
                        if (fileSize >= _minSize && fileSize <= _maxSize)
                        {
                            contact.ImageData = await _imageService.EncodeImageAsync(contact.ImageFile);
                            contact.ImageType = _imageService.ContentType(contact.ImageFile);
                        }
                    }

                    _context.Update(contact);
                    await _context.SaveChangesAsync();

                    contact = _context.Contacts.Include(contact => contact.Categories).FirstOrDefault(c => c.Id == contact.Id);

                    foreach(var category in contact.Categories)
                    {
                        await _categoryService.RemoveContactToCategoryAsync(category.Id, contact.Id);
                    }

                    foreach (var categoryId in categoryList)
                    {
                        await _categoryService.AddContactToCategoryAsync(categoryId, contact.Id);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            var model = new ContactCreateViewModel();
            model.Contact = contact;
            model.CategoryList = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            return View(model);
        }

        // GET: Contacts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }

        #region Filter/Search Post methods

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FilterContacts(int categoryId)
        {
            var model = new ContactIndexViewModel();
            var userId = _userManager.GetUserId(User);

            var category = await _context.Categories.Include(c => c.Contacts)
                .ThenInclude(c => c.Categories)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            model.Contacts = category.Contacts;
            model.CategoryFilter = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            return View(nameof(Index), model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SearchContacts(string searchString)
        {
            var model = new ContactIndexViewModel();
            var userId = _userManager.GetUserId(User);

            model.CategoryFilter = new SelectList(_context.Categories.Where(c => c.UserId == userId), "Id", "Name");
            model.Contacts = _searchService.SearchContacts(searchString, userId);

            return View(nameof(Index), model);
        }

        #endregion

    }
}
