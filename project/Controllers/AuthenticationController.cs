using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Formatting;
using System.Security.Claims;
using System.Text;
using DotnetCore.Config;
using DotnetCore.project.Contexts;
using DotnetCore.project.Exceptions;
using DotnetCore.project.Models;
using DotnetCore.project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;


namespace DotnetCore.Controllers
{
    [Route("authentication")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly UserContext _userContext;
        private readonly AppSettings _appSettings;

        public AuthenticationController(
            UserService userService, 
            UserContext userContext,
            IOptions<AppSettings> appSettings)
        {
            _userContext = userContext;
            _userService = userService;
            _appSettings = appSettings.Value;

        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login(Login user)
        {
            User _user = _userService.Authenticate(user.Username, user.Password);
            if(_user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] 
                {
                    new Claim(ClaimTypes.Name, _user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public ActionResult<User> Register(User user)
        {
            try {
                _userService.Create(user);
                return Ok();
            }
            catch(AppException ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}