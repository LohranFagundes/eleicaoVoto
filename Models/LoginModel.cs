using System.ComponentModel.DataAnnotations;

namespace VoteHomWebApp.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "CPF é obrigatório")]
        public string CPF { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Password { get; set; } = string.Empty;
    }
}