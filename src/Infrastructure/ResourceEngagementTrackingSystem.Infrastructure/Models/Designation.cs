using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ResourceEngagementTrackingSystem.Infrastructure.Models
{
    public class Designation : BaseEntity
    {
        [Key]
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string Name { get; set; }
        [MaxLength(500)]
        public string? Description { get; set; }
        public ICollection<Employee> Employees { get; set; }
    }
}