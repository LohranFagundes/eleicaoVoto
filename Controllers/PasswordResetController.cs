using Microsoft.AspNetCore.Mvc;
using VoteHomWebApp.Models;
using VoteHomWebApp.Services;
using System.Diagnostics;

namespace VoteHomWebApp.Controllers
{
    public class PasswordResetController : Controller
    {
        private readonly IElectionService _electionService;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(IElectionService electionService, ILogger<PasswordResetController> logger)
        {
            _electionService = electionService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult RequestReset()
        {
            return View("Request");
        }

        [HttpPost]
        public async Task<IActionResult> RequestReset(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Email é obrigatório.");
                return View();
            }

            try
            {
                _logger.LogInformation("Requesting password reset for email: {Email}", email);
                
                var success = await _electionService.RequestPasswordResetAsync(email);
                
                if (success)
                {
                    _logger.LogInformation("Password reset request successful for email: {Email}", email);
                    ViewBag.SuccessMessage = "Se o email existir no sistema, você receberá um link para redefinir sua senha.";
                    ViewBag.Email = email;
                    return View("RequestSuccess");
                }
                else
                {
                    _logger.LogWarning("Password reset request failed for email: {Email}", email);
                    // For security, don't reveal if email exists or not
                    ViewBag.SuccessMessage = "Se o email existir no sistema, você receberá um link para redefinir sua senha.";
                    ViewBag.Email = email;
                    return View("RequestSuccess");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for email: {Email}", email);
                ModelState.AddModelError("", "Erro interno. Tente novamente mais tarde.");
                return View();
            }
        }

        [HttpGet]
        [Route("PasswordReset/Reset")]
        [Route("reset-password")]
        public IActionResult Reset(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Password reset accessed without token");
                ViewBag.ErrorMessage = "Token de redefinição não fornecido.";
                return View("Error", new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
            }

            _logger.LogInformation("Displaying password reset form for token from API email");
            
            // For tokens from API email, show the form directly
            // Token validation will happen during submission to the API
            var model = new PasswordResetModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [Route("PasswordReset/Reset")]
        [Route("reset-password")]
        public async Task<IActionResult> Reset(PasswordResetModel model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for password reset");
                return View(model);
            }

            try
            {
                _logger.LogInformation("Processing password reset with API token");
                
                // Use the new API endpoint with token, newPassword and confirmPassword
                var success = await _electionService.ResetPasswordWithTokenAsync(
                    model.Token, 
                    model.NewPassword, 
                    model.ConfirmPassword);
                
                if (success)
                {
                    _logger.LogInformation("Password reset successful using API token");
                    ViewBag.SuccessMessage = "Senha redefinida com sucesso!";
                    return View("ResetSuccess");
                }
                else
                {
                    _logger.LogWarning("Password reset failed with API token");
                    ModelState.AddModelError("", "Token inválido, expirado ou erro ao redefinir senha. Verifique se o token do email ainda é válido.");
                    return View("TokenExpired");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset with API token");
                ModelState.AddModelError("", "Erro interno ao redefinir senha. Tente novamente mais tarde.");
                return View(model);
            }
        }
    }
}