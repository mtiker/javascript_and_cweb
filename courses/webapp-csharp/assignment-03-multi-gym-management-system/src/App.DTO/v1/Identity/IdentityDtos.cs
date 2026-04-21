namespace App.DTO.v1.Identity;

public class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
}

public class LoginRequest
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public class RefreshTokenRequest
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}

public class JwtResponse
{
    public string Jwt { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public int ExpiresInSeconds { get; set; }
    public Guid? ActiveGymId { get; set; }
    public string? ActiveGymCode { get; set; }
    public string? ActiveRole { get; set; }
    public string[] SystemRoles { get; set; } = [];
}

public class SwitchGymRequest
{
    public string GymCode { get; set; } = default!;
}

public class SwitchRoleRequest
{
    public string RoleName { get; set; } = default!;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = default!;
}

public class ForgotPasswordResponse
{
    public string Message { get; set; } = default!;
    public string? ResetToken { get; set; }
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = default!;
    public string ResetToken { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
