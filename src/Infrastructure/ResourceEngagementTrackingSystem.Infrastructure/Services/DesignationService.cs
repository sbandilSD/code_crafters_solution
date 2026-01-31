using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ResourceEngagementTrackingSystem.Application.DTOs;
using ResourceEngagementTrackingSystem.Application.Interfaces;
using ResourceEngagementTrackingSystem.Infrastructure.Models;

namespace ResourceEngagementTrackingSystem.Infrastructure.Services
{
    public class DesignationService : IDesignationService
    {
        private readonly ApplicationDbContext _context;
        public DesignationService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<DesignationDto>> GetAllAsync()
        {
            return await _context.Designations.Select(d => new DesignationDto { Id = d.Id, Name = d.Name, Description = d.Description }).ToListAsync();
        }
        public async Task<DesignationDto> GetByIdAsync(int id)
        {
            var d = await _context.Designations.FindAsync(id);
            if (d == null) return null;
            return new DesignationDto { Id = d.Id, Name = d.Name, Description = d.Description };
        }
        public async Task<DesignationDto> CreateAsync(CreateDesignationDto dto)
        {
            var d = new Designation { Name = dto.Name, Description = dto.Description };
            _context.Designations.Add(d);
            await _context.SaveChangesAsync();
            return new DesignationDto { Id = d.Id, Name = d.Name, Description = d.Description };
        }
        public async Task<DesignationDto> UpdateAsync(int id, UpdateDesignationDto dto)
        {
            var d = await _context.Designations.FindAsync(id);
            if (d == null) return null;
            d.Name = dto.Name;
            d.Description = dto.Description;
            await _context.SaveChangesAsync();
            return new DesignationDto { Id = d.Id, Name = d.Name, Description = d.Description };
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var d = await _context.Designations.FindAsync(id);
            if (d == null) return false;
            _context.Designations.Remove(d);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}