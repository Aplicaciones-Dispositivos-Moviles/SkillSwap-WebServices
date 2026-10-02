using Microsoft.EntityFrameworkCore;
using SkillSwap.Platform.Iam.Application.Internal.CommandServices;
using SkillSwap.Platform.Iam.Domain.Model;
using SkillSwap.Platform.Iam.Domain.Model.Commands;
using SkillSwap.Platform.Iam.Domain.Model.ValueObjects;
using SkillSwap.Platform.Iam.Domain.Services;
using SkillSwap.Platform.Shared.Application.Model;
using SkillSwap.Platform.Shared.Resources.Errors;
using SkillSwap.Platform.Tests.Support;
using SkillSwap.Platform.Iam.Domain.Model.Events;

namespace SkillSwap.Platform.Tests.Iam.Application;

public class UserCommandServiceTests
{
    private readonly FakeUserRepository _repository = new();
    private readonly UserCommandService _service;
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDomainEventPublisher _events = new();

    public UserCommandServiceTests()
    {
        _service = new UserCommandService(
            _repository,
            new FakePasswordHasher(),
            new EmailDomainValidator(),
            new FakeTokenGenerator(),
            _unitOfWork,
            _events,
            new FakeLocalizer<ErrorMessage>());
    }

    private static SignUpCommand SignUp(string username = "Ana", string email = "ana@upc.edu.pe",
        string password = "password123")
    {
        return new SignUpCommand(username, email, password, UserRole.Student);
    }

    private static void AssertFailure<T>(Result<T> result, IamError expected)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected, Assert.IsType<IamError>(result.Error));
    }

    // ---------- Sign up ----------

    [Fact]
    public async Task SignUp_WithValidData_CreatesUserWithHashedPasswordAndNormalizedValues()
    {
        var result = await _service.Handle(SignUp("Ana", "Ana@UPC.edu.pe"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = Assert.Single(_repository.Users);
        Assert.Equal("ana", user.Username.Value);
        Assert.Equal("ana@upc.edu.pe", user.Email.Value);
        Assert.Equal("hashed:password123", user.PasswordHash.Value);
        Assert.Equal(UserRole.Student, user.Role);
        Assert.False(user.IsVerified);
        Assert.Equal(1, _unitOfWork.CompleteCalls);
    }
    
    [Fact]
    public async Task SignUp_WithValidData_PublishesTheUserRegisteredEvent()
    {
        var result = await _service.Handle(SignUp(), CancellationToken.None);

        var published = Assert.IsType<UserRegistered>(Assert.Single(_events.Published));
        Assert.Equal(result.Value!.Id, published.UserId);
        Assert.True(published.UserId > 0);
        Assert.Equal(UserRole.Student, published.Role);
    }

    [Fact]
    public async Task SignUp_WhenTheUsernameIsTaken_PublishesNothing()
    {
        await _service.Handle(SignUp(), CancellationToken.None);
        _events.Published.Clear();

        var result = await _service.Handle(SignUp(email: "other@upc.edu.pe"), CancellationToken.None);

        AssertFailure(result, IamError.UsernameAlreadyTaken);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task SignUp_WithNonInstitutionalEmail_PublishesNothing()
    {
        var result = await _service.Handle(SignUp(email: "ana@gmail.com"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_events.Published);
    }

    [Fact]
    public async Task SignUp_WhenPersistenceFails_PublishesNothing()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        var result = await _service.Handle(SignUp(), CancellationToken.None);

        AssertFailure(result, IamError.DatabaseError);
        Assert.Empty(_events.Published);
    }

    [Theory]
    [InlineData("ana@gmail.com")]
    [InlineData("ana@upc.edu")]
    [InlineData("ana@edu.pe")]
    [InlineData("")]
    public async Task SignUp_WithNonInstitutionalEmail_Fails(string email)
    {
        var result = await _service.Handle(SignUp(email: email), CancellationToken.None);

        AssertFailure(result, IamError.InvalidInstitutionalEmail);
        Assert.Empty(_repository.Users);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("with space")]
    public async Task SignUp_WithInvalidUsername_Fails(string username)
    {
        var result = await _service.Handle(SignUp(username), CancellationToken.None);

        AssertFailure(result, IamError.InvalidUsername);
        Assert.Empty(_repository.Users);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task SignUp_WithShortPassword_Fails(string password)
    {
        var result = await _service.Handle(SignUp(password: password), CancellationToken.None);

        AssertFailure(result, IamError.WeakPassword);
        Assert.Empty(_repository.Users);
    }

    [Fact]
    public async Task SignUp_WithPasswordOver72Bytes_Fails()
    {
        var result = await _service.Handle(SignUp(password: new string('a', 73)), CancellationToken.None);

        AssertFailure(result, IamError.WeakPassword);
    }

    [Fact]
    public async Task SignUp_WithMultiByteCharactersExceeding72Bytes_Fails()
    {
        // 37 characters, 74 bytes in UTF-8
        var result = await _service.Handle(SignUp(password: new string('ñ', 37)), CancellationToken.None);

        AssertFailure(result, IamError.WeakPassword);
    }

    [Fact]
    public async Task SignUp_WithPasswordOfExactly72Bytes_Succeeds()
    {
        var result = await _service.Handle(SignUp(password: new string('a', 72)), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task SignUp_WithTakenUsername_Fails()
    {
        await _service.Handle(SignUp("Ana", "ana@upc.edu.pe"), CancellationToken.None);

        var result = await _service.Handle(SignUp("ANA", "other@upc.edu.pe"), CancellationToken.None);

        AssertFailure(result, IamError.UsernameAlreadyTaken);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task SignUp_WithTakenEmail_Fails()
    {
        await _service.Handle(SignUp("ana", "ana@upc.edu.pe"), CancellationToken.None);

        var result = await _service.Handle(SignUp("other", "ANA@upc.edu.pe"), CancellationToken.None);

        AssertFailure(result, IamError.EmailAlreadyTaken);
        Assert.Single(_repository.Users);
    }

    [Fact]
    public async Task SignUp_WhenSavingFailsWithDbUpdateException_ReturnsDatabaseError()
    {
        _unitOfWork.ExceptionToThrow = new DbUpdateException("failure");

        var result = await _service.Handle(SignUp(), CancellationToken.None);

        AssertFailure(result, IamError.DatabaseError);
    }

    [Fact]
    public async Task SignUp_WhenSavingIsCancelled_ReturnsOperationCancelled()
    {
        _unitOfWork.ExceptionToThrow = new OperationCanceledException();

        var result = await _service.Handle(SignUp(), CancellationToken.None);

        AssertFailure(result, IamError.OperationCancelled);
    }

    [Fact]
    public async Task SignUp_WhenSavingFailsUnexpectedly_ReturnsInternalServerError()
    {
        _unitOfWork.ExceptionToThrow = new InvalidOperationException("boom");

        var result = await _service.Handle(SignUp(), CancellationToken.None);

        AssertFailure(result, IamError.InternalServerError);
    }

    // ---------- Sign in ----------

    [Fact]
    public async Task SignIn_WithCorrectCredentials_ReturnsUserAndToken()
    {
        await _service.Handle(SignUp("Ana"), CancellationToken.None);

        var result = await _service.Handle(new SignInCommand("ANA", "password123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ana", result.Value.User.Username.Value);
        Assert.Equal("token-for-1", result.Value.Token);
    }

    [Fact]
    public async Task SignIn_WithWrongPassword_ReturnsInvalidCredentials()
    {
        await _service.Handle(SignUp("ana"), CancellationToken.None);

        var result = await _service.Handle(new SignInCommand("ana", "wrong-password"), CancellationToken.None);

        AssertFailure(result, IamError.InvalidCredentials);
    }

    [Fact]
    public async Task SignIn_WithUnknownUser_ReturnsSameErrorAsWrongPassword()
    {
        var result = await _service.Handle(new SignInCommand("nobody", "password123"), CancellationToken.None);

        AssertFailure(result, IamError.InvalidCredentials);
    }

    [Fact]
    public async Task SignIn_WithMalformedUsername_ReturnsInvalidCredentials()
    {
        var result = await _service.Handle(new SignInCommand("", "password123"), CancellationToken.None);

        AssertFailure(result, IamError.InvalidCredentials);
    }

    // ---------- Update bio ----------

    [Fact]
    public async Task UpdateBio_ByOwner_UpdatesAndSaves()
    {
        var created = await _service.Handle(SignUp(), CancellationToken.None);
        var userId = created.Value!.Id;

        var result = await _service.Handle(new UpdateUserBioCommand(userId, "  Hello world  ", userId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello world", result.Value!.Bio);
        Assert.Equal(2, _unitOfWork.CompleteCalls);
    }

    [Fact]
    public async Task UpdateBio_ByAnotherUser_ReturnsNotProfileOwner()
    {
        var created = await _service.Handle(SignUp(), CancellationToken.None);
        var userId = created.Value!.Id;

        var result = await _service.Handle(new UpdateUserBioCommand(userId, "Hello", userId + 1),
            CancellationToken.None);

        AssertFailure(result, IamError.NotProfileOwner);
        Assert.Equal(string.Empty, _repository.Users[0].Bio);
    }

    [Fact]
    public async Task UpdateBio_ForUnknownUser_ReturnsUserNotFound()
    {
        var result = await _service.Handle(new UpdateUserBioCommand(99, "Hello", 99), CancellationToken.None);

        AssertFailure(result, IamError.UserNotFound);
    }

    [Fact]
    public async Task UpdateBio_ExceedingMaxLength_ReturnsBioTooLong()
    {
        var created = await _service.Handle(SignUp(), CancellationToken.None);
        var userId = created.Value!.Id;

        var result = await _service.Handle(new UpdateUserBioCommand(userId, new string('a', 1001), userId),
            CancellationToken.None);

        AssertFailure(result, IamError.BioTooLong);
    }
}