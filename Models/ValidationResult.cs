namespace VoteHomWebApp.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public ValidationErrorType ErrorType { get; set; } = ValidationErrorType.Unknown;
        
        public static ValidationResult Success() => new ValidationResult { IsValid = true };
        
        public static ValidationResult Failure(string errorMessage, ValidationErrorType errorType = ValidationErrorType.Unknown)
            => new ValidationResult { IsValid = false, ErrorMessage = errorMessage, ErrorType = errorType };
    }
    
    public class UserValidationResult : ValidationResult
    {
        public string Token { get; set; } = string.Empty;
        public string VoterName { get; set; } = string.Empty;
        
        public static UserValidationResult Success(string token, string voterName)
            => new UserValidationResult { IsValid = true, Token = token, VoterName = voterName };
        
        public static new UserValidationResult Failure(string errorMessage, ValidationErrorType errorType = ValidationErrorType.Unknown)
            => new UserValidationResult { IsValid = false, ErrorMessage = errorMessage, ErrorType = errorType };
    }
    
    public enum ValidationErrorType
    {
        Unknown,
        InvalidCredentials,
        UserNotFound,
        WrongPassword,
        ElectionExpired,
        ElectionNotStarted,
        ElectionUnavailable,
        UserInactive,
        AlreadyVoted,
        ServerError,
        NetworkError
    }
}