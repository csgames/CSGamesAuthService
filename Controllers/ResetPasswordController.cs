﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PolyHxDotNetServices.Mail;
using PolyHxDotNetServices.Mail.Inputs;
using STS.Inputs;
using STS.Interface;
using STS.Models;

namespace STS.Controllers
{
    [Route("resetPassword")]
    public class ResetPasswordController : Controller
    {
        private readonly IRepository _db;
        private readonly IMailService _mailService;

        public ResetPasswordController(IRepository db, IMailService mailService)
        {
            _db = db;
            _mailService = mailService;
        }

        [HttpPost]
        public Task<IActionResult> Create(AskResetPasswordInput input)
        {
            return Task.Run<IActionResult>(async () =>
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest();
                }

                var user = _db.Single<User>(c => c.Username == input.Username);

                if (user == null)
                {
                    return new StatusCodeResult((int) HttpStatusCode.BadRequest);
                }

                var resetPassword = _db.Single<ResetPassword>(c => c.UserId == user.Id && !c.Used);

                if (resetPassword == null)
                {
                    resetPassword = new ResetPassword
                    {
                        UserId = user.Id,
                        Uuid = Guid.NewGuid().ToString()
                    };

                    _db.Add(resetPassword);
                }
                else
                {
                    resetPassword.Uuid = Guid.NewGuid().ToString();
                    _db.Update<ResetPassword>(resetPassword.Id, new Dictionary<string, object>
                    {
                        {"Uuid", resetPassword.Uuid}
                    });
                }

                var mailInput = new SendMailInput
                {
                    From = "PolyHx <support@polyhx.io>",
                    To = new[] {user.Username},
                    Subject = "Réinitialisation du Mot de Passe | Password Reset",
                    Template = "password_reset",
                    Html = "<html></html>",
                    Text = "Text",
                    Variables = new Dictionary<string, string>
                    {
                        {"name", $"{user.FirstName}"},
                        {"url", $"{Environment.GetEnvironmentVariable("RESET_PASSWORD_URL")}/{resetPassword.Uuid}"}
                    }
                };
                var res = await _mailService.SendEmail(mailInput);

                return res ? (IActionResult) Ok() : BadRequest();
            });
        }
        
        [HttpPost("{uuid}")]
        public Task<IActionResult> GetByUuid(string uuid, ResetPasswordInput input)
        {
            return Task.Run<IActionResult>(() =>
            {
                var resetPassword = _db.Where<ResetPassword>(r => r.Uuid == uuid).First();

                if (resetPassword == null)
                    return BadRequest();

                if (resetPassword.Used)
                    return BadRequest();

                _db.Update<ResetPassword>(resetPassword.Id, new Dictionary<string, object>
                {
                    { "Used", true }
                });

                var hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(input.Password);
                _db.Update<User>(resetPassword.UserId, new Dictionary<string, object>
                {
                    { "Password", hashedNewPassword }
                });

                return Ok();
            });
        }
    }
}