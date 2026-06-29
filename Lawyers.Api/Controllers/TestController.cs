using Lawyers.Application.Interfaces;
using Lawyers.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Lawyers.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public TestController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    
    [HttpPost("create-consultation")]
    public async Task<IActionResult> CreateTestConsultation()
    {
        // 1. Use GetByIdAsync(1) to return a single ClientProfile? instead of a list
        var existingClient = await _unitOfWork.ClientProfiles.GetByIdAsync(1);

        if (existingClient == null)
        {
            return BadRequest("Test failed: Client with ID 1 does not exist in the database!");
        }

        var testConsultation = new Consultation
        {
            ClientId = existingClient.Id, 
            CreatedAt = DateTime.UtcNow,
        };

        await _unitOfWork.Consultations.AddAsync(testConsultation);
        await _unitOfWork.SaveChangesAsync(); 

        return Ok(testConsultation);
    }
}