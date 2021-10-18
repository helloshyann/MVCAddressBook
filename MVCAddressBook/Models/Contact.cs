using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MVCAddressBook.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace MVCAddressBook.Models
{
    [Authorize]
    public class Contact
    {
        // Primary Key
        public int Id { get; set; }

        //Foreign Key
        public string UserId { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [Display(Name = "Address")]
        public string Address1 { get; set; }

        [Display(Name = "Additional Address")]
        public string Address2 { get; set; }

        public string City { get; set; }

        public States State { get; set; }

        [DataType(DataType.PostalCode)]
        [Display(Name = "Zip Code")]
        public int ZipCode { get; set; }

        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [DataType(DataType.PhoneNumber)]
        public string Phone { get; set; }

        public DateTime Created { get; set; }

        [NotMapped]
        [DataType(DataType.Upload)]
        [Display(Name = "Contact Image")]
        public IFormFile ImageFile { get; set; }

        public byte[] ImageData { get; set; }
        public string ImageType { get; set; }
        [NotMapped]
        public string FullName { get { return $"{FirstName} {LastName}"; } }

        public virtual AppUser User { get; set; }

        public virtual ICollection<Category> Categories { get; set; } = new HashSet<Category>();

    }
}
