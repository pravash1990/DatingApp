using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API.Controllers
{
   
    public class Accountcontroller : BaseApi
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;
        public Accountcontroller( DataContext context ,ITokenService tokenService)
        {
            _tokenService = tokenService;
            _context= context;
          
        }       

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            
            if(await UserExist(registerDto.Username)) return BadRequest("User Name already Taken");
            using var hmac = new HMACSHA512();
            var user = new AppUser()
            {
                Username = registerDto.Username.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key            
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return new UserDto
            {
                Username = user.Username,
                Token = _tokenService.CreateToken(user)
            }; 
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            if(!await UserExist(loginDto.Username)) return BadRequest("Invalid Username"); 

            var user = await _context.Users.SingleOrDefaultAsync(X=> X.Username == loginDto.Username);

            if(user==null) return Unauthorized("Invalid User");

            using var hmac = new HMACSHA512(user.PasswordSalt);
            var computeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for(int i = 0;i<computeHash.Length;i++)
            {
                if(computeHash[i] != user.PasswordHash[i] ) 
                 return Unauthorized("Invalid Password");
            }
            return new UserDto
            {
                Username = user.Username,
                Token = _tokenService.CreateToken(user)
            }; 
        }

        private async Task<bool> UserExist(string Username)
        {
              return await  _context.Users.AnyAsync(x => x.Username == Username.ToLower());
            
        }
    }
}