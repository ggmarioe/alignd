using Alignd.Application.Interfaces;
using Alignd.Application.Rooms.CreateRoom;
using Alignd.Domain.Enums;
using Alignd.SharedKernel;
using NSubstitute;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="CreateRoomHandler"/>.
/// Uses NSubstitute to mock <see cref="IRoomRepository"/> and <see cref="IAdminTokenService"/>.
/// </summary>
[TestFixture]
public sealed class CreateRoomHandlerTests
{
    private IRoomRepository    _rooms        = null!;
    private IAdminTokenService _tokenService = null!;
    private CreateRoomHandler  _handler      = null!;

    [SetUp]
    public void SetUp()
    {
        _rooms        = Substitute.For<IRoomRepository>();
        _tokenService = Substitute.For<IAdminTokenService>();
        _handler      = new CreateRoomHandler(_rooms, _tokenService);
    }

    [Test]
    public async Task HandleAsync_WhenCodeIsUniqueOnFirstAttempt_ReturnsCreatedResult()
    {
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>()).Returns("admin-token-123");

        var result = await _handler.HandleAsync(
            new CreateRoomCommand(VoteType.Fibonacci), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,  Is.True);
            Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Created));
        });
    }

    [Test]
    public async Task HandleAsync_WhenCodeIsUniqueOnFirstAttempt_ReturnsAdminToken()
    {
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>()).Returns("generated-admin-token");

        var result = await _handler.HandleAsync(
            new CreateRoomCommand(VoteType.Fibonacci), CancellationToken.None);

        Assert.That(result.Value!.AdminToken, Is.EqualTo("generated-admin-token"));
    }

    [Test]
    public async Task HandleAsync_WhenCodeIsUniqueOnFirstAttempt_ReturnsNonEmptyRoomCode()
    {
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>()).Returns("token");

        var result = await _handler.HandleAsync(
            new CreateRoomCommand(VoteType.ShirtSize), CancellationToken.None);

        Assert.That(result.Value!.RoomCode, Is.Not.Null.And.Not.Empty,
            "The response must include a non-empty room code");
    }

    [Test]
    public async Task HandleAsync_WhenFirstCodeAlreadyExists_RetriesAndGeneratesANewCode()
    {
        // First call returns true (code taken), second returns false (code available)
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(true, false);
        _tokenService.Generate(Arg.Any<Guid>()).Returns("token");

        var result = await _handler.HandleAsync(
            new CreateRoomCommand(VoteType.Fibonacci), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True,
            "The handler must keep retrying code generation until a unique code is found");
        await _rooms.Received(2).ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_PersistsTheNewRoom_AndSavesChanges()
    {
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>()).Returns("token");

        await _handler.HandleAsync(
            new CreateRoomCommand(VoteType.Fibonacci), CancellationToken.None);

        await _rooms.Received(1).AddAsync(Arg.Any<Alignd.Domain.Entities.Room>(), Arg.Any<CancellationToken>());
        await _rooms.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_GeneratesAdminToken_UsingTheNewRoomsId()
    {
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);
        _tokenService.Generate(Arg.Any<Guid>()).Returns("token");

        await _handler.HandleAsync(
            new CreateRoomCommand(VoteType.Fibonacci), CancellationToken.None);

        _tokenService.Received(1).Generate(Arg.Is<Guid>(id => id != Guid.Empty));
    }

    // ─── RoomExistsAsync ─────────────────────────────────────────────────────

    [Test]
    public async Task RoomExistsAsync_WhenCodeExistsInRepository_ReturnsOkWithExistsTrue()
    {
        _rooms.ExistsByCodeAsync("SWIFT-TIGER-42", Arg.Any<CancellationToken>())
              .Returns(true);

        var result = await _handler.RoomExistsAsync("SWIFT-TIGER-42", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,       Is.True);
            Assert.That(result.Value!.Exists,   Is.True);
        });
    }

    [Test]
    public async Task RoomExistsAsync_WhenCodeDoesNotExist_ReturnsNotFound()
    {
        _rooms.ExistsByCodeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns(false);

        var result = await _handler.RoomExistsAsync("MISSING-CODE-99", CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task RoomExistsAsync_WhenCodeIsWhitespace_ReturnsUnprocessable()
    {
        var result = await _handler.RoomExistsAsync("   ", CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "A blank code must be rejected with an Unprocessable error");
    }

    [Test]
    public async Task RoomExistsAsync_NormalisesCodeToUpperCase_BeforeQuerying()
    {
        _rooms.ExistsByCodeAsync("SWIFT-TIGER-42", Arg.Any<CancellationToken>())
              .Returns(true);

        var result = await _handler.RoomExistsAsync("swift-tiger-42", CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True,
            "The code must be upper-cased before the repository is queried");
    }
}
