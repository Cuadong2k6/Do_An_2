using System;
using System.ComponentModel.DataAnnotations;

namespace Model
{
    internal class AuthenticateModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

    }
}
