using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using BookStore_API.Contracts;
using BookStore_API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BookStore_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILoggerService _logger;
        private readonly IConfiguration _config;

        public UsersController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, 
            ILoggerService loggerService, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = loggerService;
            _config = configuration;
        }

        /// <summary>
        /// User login endpoint
        /// </summary>
        /// <param name="userDTO"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] UserDTO userDTO)
        {
            var userName = userDTO.UserName;
            var password = userDTO.Password;
            var result = await _signInManager.PasswordSignInAsync(userName, password, false, false);

            if(result.Succeeded)
            {
                var user = await _userManager.FindByNameAsync(userName);
                var tokenString = await GenerateJSONWebToken(user);
                return Ok(new { token = tokenString });
            }
            return Unauthorized(userDTO); 
        }
        private async Task<string> GenerateJSONWebToken(IdentityUser user)
        {
            Debug.WriteLine("config key: " + _config["Jwt:Key"]);
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(r => new Claim(ClaimsIdentity.DefaultNameClaimType, r)));
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Issuer"],
                claims,
                null,
                expires: DateTime.Now.AddHours(5),
                signingCredentials: credentials
                );

            Debug.WriteLine("Jwt issuer: " + _config["Jwt:Issuer"]);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
