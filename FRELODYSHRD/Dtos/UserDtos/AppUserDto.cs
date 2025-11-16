using FRELODYAPP.Dtos.SubDtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.UserDtos
{
    public class AppUserDto : BaseEntityDto
    {
        public string? UserId { get; set; }
        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }

        public string? Email { get; set; }
        public string? FullName => FirstName + " " + LastName;
        public string? Initials => 
            (string.IsNullOrWhiteSpace(FirstName) ? "" : FirstName[0].ToString()) + 
            (string.IsNullOrWhiteSpace(LastName) ? "" : LastName[0].ToString());
        public string? UserName { get; set; }
        public string? Address { get; set; }
        public string? Aboutme { get; set; }

        public string? Contacts { get; set; }
        public string? ProfilePicUrl { get; set; }
        public string? CoverPhotoUrl { get; set; }
    }
}
