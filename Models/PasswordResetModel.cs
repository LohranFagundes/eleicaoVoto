using System.ComponentModel.DataAnnotations;

namespace VoteHomWebApp.Models
{
    public class PasswordResetModel
    {
        [Required(ErrorMessage = "Token é obrigatório")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nova senha é obrigatória")]
        [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nova Senha")]
        [Compare("NewPassword", ErrorMessage = "A confirmação de senha não confere")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? UserEmail { get; set; }
        public string? UserCpf { get; set; }
    }

    public class PasswordResetTokenValidation
    {
        public bool IsValid { get; set; }
        public string? UserEmail { get; set; }
        public string? UserCpf { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}