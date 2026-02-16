using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Mappers;
using Corely.IAM.Users.Models;

namespace Corely.IAM.UnitTests.Users.Mappers;

public class UserMapperTests
{
    [Fact]
    public void ToUser_ShouldMapAllProperties()
    {
        // Arrange
        var request = new CreateUserRequest(Username: "testuser", Email: "test@example.com");

        // Act
        var result = request.ToUser();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
    }

    [Fact]
    public void ToUser_ShouldSetDefaultValues()
    {
        // Arrange
        var request = new CreateUserRequest(Username: "testuser", Email: "test@example.com");

        // Act
        var result = request.ToUser();

        // Assert
        Assert.Equal(Guid.Empty, result.Id);
        Assert.Null(result.LockedUtc);
        Assert.Equal(0, result.TotalSuccessfulLogins);
        Assert.Null(result.LastSuccessfulLoginUtc);
        Assert.Equal(0, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(0, result.TotalFailedLogins);
        Assert.Null(result.LastFailedLoginUtc);
    }

    [Theory]
    [InlineData("user1", "user1@test.com")]
    [InlineData("admin", "admin@example.com")]
    [InlineData("", "")]
    public void ToUser_ShouldMapVariousInputs(string username, string email)
    {
        // Arrange
        var request = new CreateUserRequest(Username: username, Email: email);

        // Act
        var result = request.ToUser();

        // Assert
        Assert.Equal(username, result.Username);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public void ToEntity_ShouldMapAllProperties()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@example.com",
            LockedUtc = DateTime.UtcNow.AddMinutes(-5),
            TotalSuccessfulLogins = 10,
            LastSuccessfulLoginUtc = DateTime.UtcNow.AddDays(-1),
            FailedLoginsSinceLastSuccess = 2,
            TotalFailedLogins = 5,
            LastFailedLoginUtc = DateTime.UtcNow.AddHours(-2),
            CreatedUtc = DateTime.UtcNow.AddMonths(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = user.ToEntity();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(user.LockedUtc, result.LockedUtc);
        Assert.Equal(10, result.TotalSuccessfulLogins);
        Assert.Equal(user.LastSuccessfulLoginUtc, result.LastSuccessfulLoginUtc);
        Assert.Equal(2, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(5, result.TotalFailedLogins);
        Assert.Equal(user.LastFailedLoginUtc, result.LastFailedLoginUtc);
        Assert.Equal(user.CreatedUtc, result.CreatedUtc);
        Assert.Equal(user.ModifiedUtc, result.ModifiedUtc);
        Assert.Null(result.BasicAuth);
        Assert.Null(result.Accounts);
        Assert.Null(result.Groups);
        Assert.Null(result.Roles);
    }

    [Fact]
    public void ToModel_ShouldMapAllProperties()
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = Guid.CreateVersion7(),
            Username = "testuser",
            Email = "test@example.com",
            LockedUtc = DateTime.UtcNow.AddMinutes(-5),
            TotalSuccessfulLogins = 10,
            LastSuccessfulLoginUtc = DateTime.UtcNow.AddDays(-1),
            FailedLoginsSinceLastSuccess = 2,
            TotalFailedLogins = 5,
            LastFailedLoginUtc = DateTime.UtcNow.AddHours(-2),
            CreatedUtc = DateTime.UtcNow.AddMonths(-1),
            ModifiedUtc = DateTime.UtcNow,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(entity.LockedUtc, result.LockedUtc);
        Assert.Equal(10, result.TotalSuccessfulLogins);
        Assert.Equal(entity.LastSuccessfulLoginUtc, result.LastSuccessfulLoginUtc);
        Assert.Equal(2, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(5, result.TotalFailedLogins);
        Assert.Equal(entity.LastFailedLoginUtc, result.LastFailedLoginUtc);
        Assert.Equal(entity.CreatedUtc, result.CreatedUtc);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }
}
