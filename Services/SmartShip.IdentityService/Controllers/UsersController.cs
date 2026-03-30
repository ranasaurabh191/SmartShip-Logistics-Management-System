using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShip.IdentityService.DTOs;
using SmartShip.IdentityService.Filters;
using SmartShip.IdentityService.Services;

namespace SmartShip.IdentityService.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "ADMIN")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] UserPagedRequest request) =>  Ok(await _userService.GetAllUsersPagedAsync(request));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        return user == null ? NotFound(new { message = "User Not Found" }) : Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        await _userService.UpdateUserAsync(id, request);
        return Ok(new { message = "Updated Successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteUserAsync(id);
        return Ok(new { message = "Deleted Successfully" }) ;
    }

    [HttpGet("email/{userId}")]
    [InternalApiKey]
    public async Task<IActionResult> GetEmail(int userId) => Ok(new { Id = userId, Email = await _userService.GetUserEmailAsync(userId) });

}
