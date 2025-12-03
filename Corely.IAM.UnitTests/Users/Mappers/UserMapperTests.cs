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
        Assert.Equal(0, result.Id);
        Assert.False(result.Disabled);
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
            Id = 42,
            Username = "testuser",
            Email = "test@example.com",
            Disabled = true,
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
        Assert.Equal(42, result.Id);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.True(result.Disabled);
        Assert.Equal(10, result.TotalSuccessfulLogins);
        Assert.Equal(user.LastSuccessfulLoginUtc, result.LastSuccessfulLoginUtc);
        Assert.Equal(2, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(5, result.TotalFailedLogins);
        Assert.Equal(user.LastFailedLoginUtc, result.LastFailedLoginUtc);
        Assert.Equal(user.CreatedUtc, result.CreatedUtc);
        Assert.Equal(user.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToEntity_ShouldNotMapNavigationProperties()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "test",
            Email = "test@test.com",
        };

        // Act
        var result = user.ToEntity();

        // Assert
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
            Id = 42,
            Username = "testuser",
            Email = "test@example.com",
            Disabled = true,
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
        Assert.Equal(42, result.Id);
        Assert.Equal("testuser", result.Username);
        Assert.Equal("test@example.com", result.Email);
        Assert.True(result.Disabled);
        Assert.Equal(10, result.TotalSuccessfulLogins);
        Assert.Equal(entity.LastSuccessfulLoginUtc, result.LastSuccessfulLoginUtc);
        Assert.Equal(2, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(5, result.TotalFailedLogins);
        Assert.Equal(entity.LastFailedLoginUtc, result.LastFailedLoginUtc);
        Assert.Equal(entity.CreatedUtc, result.CreatedUtc);
        Assert.Equal(entity.ModifiedUtc, result.ModifiedUtc);
    }

    [Fact]
    public void ToModel_ToEntity_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var originalUser = new User
        {
            Id = 99,
            Username = "roundtripuser",
            Email = "roundtrip@test.com",
            Disabled = false,
            TotalSuccessfulLogins = 15,
            LastSuccessfulLoginUtc = DateTime.UtcNow.AddDays(-5),
            FailedLoginsSinceLastSuccess = 1,
            TotalFailedLogins = 3,
            LastFailedLoginUtc = DateTime.UtcNow.AddHours(-10),
            CreatedUtc = DateTime.UtcNow.AddMonths(-3),
            ModifiedUtc = DateTime.UtcNow.AddDays(-1),
        };

        // Act
        var entity = originalUser.ToEntity();
        var resultUser = entity.ToModel();

        // Assert
        Assert.Equal(originalUser.Id, resultUser.Id);
        Assert.Equal(originalUser.Username, resultUser.Username);
        Assert.Equal(originalUser.Email, resultUser.Email);
        Assert.Equal(originalUser.Disabled, resultUser.Disabled);
        Assert.Equal(originalUser.TotalSuccessfulLogins, resultUser.TotalSuccessfulLogins);
        Assert.Equal(originalUser.LastSuccessfulLoginUtc, resultUser.LastSuccessfulLoginUtc);
        Assert.Equal(
            originalUser.FailedLoginsSinceLastSuccess,
            resultUser.FailedLoginsSinceLastSuccess
        );
        Assert.Equal(originalUser.TotalFailedLogins, resultUser.TotalFailedLogins);
        Assert.Equal(originalUser.LastFailedLoginUtc, resultUser.LastFailedLoginUtc);
        Assert.Equal(originalUser.CreatedUtc, resultUser.CreatedUtc);
        Assert.Equal(originalUser.ModifiedUtc, resultUser.ModifiedUtc);
    }

    [Theory]
    [InlineData(1, "user1", "user1@test.com", false, 5, 1, 2)]
    [InlineData(2, "user2", "user2@test.com", true, 0, 10, 15)]
    [InlineData(0, "", "", false, 0, 0, 0)]
    public void ToEntity_ShouldMapVariousInputs(
        int id,
        string username,
        string email,
        bool disabled,
        int totalSuccessful,
        int failedSinceLast,
        int totalFailed
    )
    {
        // Arrange
        var user = new User
        {
            Id = id,
            Username = username,
            Email = email,
            Disabled = disabled,
            TotalSuccessfulLogins = totalSuccessful,
            FailedLoginsSinceLastSuccess = failedSinceLast,
            TotalFailedLogins = totalFailed,
        };

        // Act
        var result = user.ToEntity();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(username, result.Username);
        Assert.Equal(email, result.Email);
        Assert.Equal(disabled, result.Disabled);
        Assert.Equal(totalSuccessful, result.TotalSuccessfulLogins);
        Assert.Equal(failedSinceLast, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(totalFailed, result.TotalFailedLogins);
    }

    [Theory]
    [InlineData(1, "user1", "user1@test.com", false, 5, 1, 2)]
    [InlineData(2, "user2", "user2@test.com", true, 0, 10, 15)]
    [InlineData(0, "", "", false, 0, 0, 0)]
    public void ToModel_ShouldMapVariousInputs(
        int id,
        string username,
        string email,
        bool disabled,
        int totalSuccessful,
        int failedSinceLast,
        int totalFailed
    )
    {
        // Arrange
        var entity = new UserEntity
        {
            Id = id,
            Username = username,
            Email = email,
            Disabled = disabled,
            TotalSuccessfulLogins = totalSuccessful,
            FailedLoginsSinceLastSuccess = failedSinceLast,
            TotalFailedLogins = totalFailed,
        };

        // Act
        var result = entity.ToModel();

        // Assert
        Assert.Equal(id, result.Id);
        Assert.Equal(username, result.Username);
        Assert.Equal(email, result.Email);
        Assert.Equal(disabled, result.Disabled);
        Assert.Equal(totalSuccessful, result.TotalSuccessfulLogins);
        Assert.Equal(failedSinceLast, result.FailedLoginsSinceLastSuccess);
        Assert.Equal(totalFailed, result.TotalFailedLogins);
    }
}
