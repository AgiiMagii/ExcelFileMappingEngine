using System;
using System.Collections.Generic;
using System.Text;

namespace FileMappingEngine.Lib.Models
{
    public class ModelUser
    {
        public long Id { get; set; }
        public required string Email { get; set; }
        public string? DisplayName { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
