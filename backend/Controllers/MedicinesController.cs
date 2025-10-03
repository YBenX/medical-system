using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicalSystem.Data;
using MedicalSystem.Models;

namespace MedicalSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicinesController : ControllerBase
{
    private readonly MedicalDbContext _context;

    public MedicinesController(MedicalDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 获取所有药品
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Medicine>>> GetAllMedicines(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] bool activeOnly = true)
    {
        var query = _context.Medicines.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(m => m.IsActive);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(m => m.Category == category);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m =>
                m.Name.Contains(search) ||
                m.Description.Contains(search));
        }

        var medicines = await query
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();

        return medicines;
    }

    /// <summary>
    /// 根据ID获取药品
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Medicine>> GetMedicine(int id)
    {
        var medicine = await _context.Medicines.FindAsync(id);

        if (medicine == null)
            return NotFound($"药品ID {id} 不存在");

        return medicine;
    }

    /// <summary>
    /// 创建药品
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Medicine>> CreateMedicine([FromBody] Medicine medicine)
    {
        medicine.CreatedAt = DateTime.Now;
        medicine.UpdatedAt = DateTime.Now;

        _context.Medicines.Add(medicine);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMedicine), new { id = medicine.Id }, medicine);
    }

    /// <summary>
    /// 更新药品
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMedicine(int id, [FromBody] Medicine medicine)
    {
        if (id != medicine.Id)
            return BadRequest("ID不匹配");

        var existing = await _context.Medicines.FindAsync(id);
        if (existing == null)
            return NotFound($"药品ID {id} 不存在");

        existing.Name = medicine.Name;
        existing.Specification = medicine.Specification;
        existing.Unit = medicine.Unit;
        existing.Price = medicine.Price;
        existing.Stock = medicine.Stock;
        existing.Category = medicine.Category;
        existing.Description = medicine.Description;
        existing.IsActive = medicine.IsActive;
        existing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 删除药品（软删除）
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteMedicine(int id)
    {
        var medicine = await _context.Medicines.FindAsync(id);
        if (medicine == null)
            return NotFound($"药品ID {id} 不存在");

        medicine.IsActive = false;
        medicine.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// 更新库存
    /// </summary>
    [HttpPatch("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
    {
        var medicine = await _context.Medicines.FindAsync(id);
        if (medicine == null)
            return NotFound($"药品ID {id} 不存在");

        medicine.Stock += dto.Quantity; // 正数为入库，负数为出库
        medicine.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new { NewStock = medicine.Stock });
    }

    /// <summary>
    /// 获取药品分类列表
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<List<string>>> GetCategories()
    {
        var categories = await _context.Medicines
            .Where(m => m.IsActive)
            .Select(m => m.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return categories;
    }
}

/// <summary>
/// 更新库存DTO
/// </summary>
public class UpdateStockDto
{
    public int Quantity { get; set; } // 变动数量（正数入库，负数出库）
}
