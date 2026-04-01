using Alignd.Application.Interfaces;
using Alignd.Application.Voting.CastVote;
using Alignd.Domain.Entities;
using Alignd.Domain.Enums;
using Alignd.Domain.ValueObjects;
using Alignd.SharedKernel;
using Moq;

namespace Alignd.Tests.Application;

/// <summary>
/// Tests for <see cref="CastVoteHandler"/>.
/// Uses Moq for all dependency mocks.
/// </summary>
[TestFixture]
public sealed class CastVoteHandlerTests
{
    private Mock<IVotingRoundRepository> _roundsMock       = null!;
    private Mock<IParticipantRepository> _participantsMock = null!;
    private Mock<IRoomRepository>        _roomsMock        = null!;
    private Mock<IRoomNotifier>          _notifierMock     = null!;
    private CastVoteHandler              _handler          = null!;

    [SetUp]
    public void SetUp()
    {
        _roundsMock       = new Mock<IVotingRoundRepository>();
        _participantsMock = new Mock<IParticipantRepository>();
        _roomsMock        = new Mock<IRoomRepository>();
        _notifierMock     = new Mock<IRoomNotifier>();
        _handler          = new CastVoteHandler(
            _roundsMock.Object, _participantsMock.Object,
            _roomsMock.Object, _notifierMock.Object);
    }

    private Room CreateFibonacciRoom()
    {
        return Room.Create(RoomCode.From("SWIFT-TIGER-42"), VoteType.Fibonacci);
    }

    private Room CreateShirtSizeRoom()
    {
        return Room.Create(RoomCode.From("BRAVE-EAGLE-77"), VoteType.ShirtSize);
    }

    [Test]
    public async Task HandleAsync_WhenRoomDoesNotExist_ReturnsNotFound()
    {
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync((Room?)null);

        var result = await _handler.HandleAsync(
            new CastVoteCommand("MISSING", Guid.NewGuid(), "5"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenParticipantDoesNotExist_ReturnsNotFound()
    {
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(CreateFibonacciRoom());
        _participantsMock.Setup(p => p.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Participant?)null);

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", Guid.NewGuid(), "5"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.NotFound));
    }

    [Test]
    public async Task HandleAsync_WhenParticipantIsAWatcher_ReturnsForbidden()
    {
        var room    = CreateFibonacciRoom();
        var watcher = Participant.Create(room.Id, "Observer", ParticipantRole.Watcher);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(watcher.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(watcher);

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", watcher.Id, "5"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Forbidden),
            "Watchers are not permitted to cast votes");
    }

    [Test]
    public async Task HandleAsync_WhenNoActiveRoundExists_ReturnsConflict()
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync((VotingRound?)null);

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, "5"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "Casting a vote when no round is active must be rejected");
    }

    [Test]
    public async Task HandleAsync_WhenParticipantAlreadyVoted_ReturnsConflict()
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();
        round.AddVote(Vote.Cast(round.Id, voter.Id, "3")); // already voted

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, "5"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Conflict),
            "A participant cannot vote more than once per round");
    }

    [Test]
    public async Task HandleAsync_WhenVoteValueIsInvalidForFibonacciRoom_ReturnsUnprocessable()
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, "XL"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "'XL' is a shirt-size value and is not valid in a Fibonacci room");
    }

    [Test]
    public async Task HandleAsync_WhenVoteValueIsInvalidForShirtSizeRoom_ReturnsUnprocessable()
    {
        var room  = CreateShirtSizeRoom();
        var voter = Participant.Create(room.Id, "Bob", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        var result = await _handler.HandleAsync(
            new CastVoteCommand("BRAVE-EAGLE-77", voter.Id, "13"), CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(ResultCode.Unprocessable),
            "'13' is a Fibonacci value and is not valid in a ShirtSize room");
    }

    [Test]
    public async Task HandleAsync_WhenVoteIsValid_AddsVoteToRound_AndSavesChanges()
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, "5"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess,     Is.True);
            Assert.That(round.Votes,          Has.Count.EqualTo(1));
            Assert.That(round.Votes[0].Value, Is.EqualTo("5"));
        });
        // SaveChangesAsync may be called twice when auto-reveal is triggered (once for the vote,
        // once to persist the ended round), so we only assert it was called at least once.
        _roundsMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Test]
    public async Task HandleAsync_WhenAllVotersHaveVoted_AutomaticallyEndsTheRound()
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        // Only one voter in the room — casting one vote means everyone has voted
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, "8"), CancellationToken.None);

        Assert.That(round.Status, Is.EqualTo(RoundStatus.Ended),
            "When every voter has cast their vote the round must be auto-revealed");
    }

    [Test]
    public async Task HandleAsync_WhenAutoReveal_NotifiesRoomThatRoundEnded()
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, "3"), CancellationToken.None);

        _notifierMock.Verify(n =>
            n.NotifyRoundEnded(It.IsAny<string>(), It.IsAny<RoundEndedPayload>()),
            Times.Once, "NotifyRoundEnded must be called when auto-reveal triggers");
    }

    [Test]
    public async Task HandleAsync_WhenNotAllVotersHaveVoted_DoesNotEndTheRound()
    {
        var room   = CreateFibonacciRoom();
        var voter1 = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var voter2 = Participant.Create(room.Id, "Bob",   ParticipantRole.Voter);
        var round  = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter1.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter1);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        // Two voters in the room, only one has voted
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter1, voter2 });

        await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter1.Id, "5"), CancellationToken.None);

        Assert.That(round.Status, Is.EqualTo(RoundStatus.Active),
            "The round must remain active while at least one voter has not yet voted");
    }

    [TestCase("1")]
    [TestCase("2")]
    [TestCase("3")]
    [TestCase("5")]
    [TestCase("8")]
    [TestCase("13")]
    [TestCase("21")]
    [TestCase("?")]
    [TestCase("☕")]
    public async Task HandleAsync_AcceptsAllValidFibonacciValues(string value)
    {
        var room  = CreateFibonacciRoom();
        var voter = Participant.Create(room.Id, "Alice", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        var result = await _handler.HandleAsync(
            new CastVoteCommand("SWIFT-TIGER-42", voter.Id, value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True,
            $"'{value}' must be a valid vote in a Fibonacci room");
    }

    [TestCase("XS")]
    [TestCase("S")]
    [TestCase("M")]
    [TestCase("L")]
    [TestCase("XL")]
    [TestCase("XXL")]
    [TestCase("?")]
    [TestCase("☕")]
    public async Task HandleAsync_AcceptsAllValidShirtSizeValues(string value)
    {
        var room  = CreateShirtSizeRoom();
        var voter = Participant.Create(room.Id, "Bob", ParticipantRole.Voter);
        var round = VotingRound.CreateForTask(room.Id, Guid.NewGuid());
        round.Start();

        _roomsMock.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(room);
        _participantsMock.Setup(p => p.GetByIdAsync(voter.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(voter);
        _roundsMock.Setup(r => r.GetActiveByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(round);
        _participantsMock.Setup(p => p.GetConnectedByRoomAsync(room.Id, It.IsAny<CancellationToken>()))
                         .ReturnsAsync(new List<Participant> { voter });

        var result = await _handler.HandleAsync(
            new CastVoteCommand("BRAVE-EAGLE-77", voter.Id, value), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True,
            $"'{value}' must be a valid vote in a ShirtSize room");
    }
}
