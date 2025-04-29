public class CreateUserDto
{
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserProfileDto UserProfile { get; set; }
}

public class UpdateUserDto : CreateUserDto { }

public class UserProfileDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
}

public class UserDto : CreateUserDto
{
    public long Id { get; set; }
}
