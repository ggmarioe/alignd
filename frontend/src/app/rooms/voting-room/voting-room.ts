import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Participant } from '../../shared/models';
import { VoteType } from '../rooms.service';
import { ParticipantsPanel } from '../../shared/organisms/participants-panel/participants-panel';
import { VotingPanel } from '../../shared/organisms/voting-panel/voting-panel';
import { ActionButton } from '../../shared/atoms/action-button/action-button';

@Component({
  selector: 'app-voting-room',
  imports: [VotingPanel, ParticipantsPanel, ActionButton],
  templateUrl: './voting-room.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VotingRoom {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly roomCode = this.route.snapshot.paramMap.get('code') ?? '';

  // State to be driven by SignalR — stubs until hub integration
  readonly workItemTitle = signal('');
  readonly voteType = signal<VoteType>(0);
  readonly currentVote = signal<string | null>(null);
  readonly participants = signal<Participant[]>([]);
  readonly isAdminPresent = signal(true);
  readonly isCurrentUserAdmin = signal(false);
  readonly currentUserId = signal('');

  /** Derived: true once the server pushes actual vote values (non-'?', non-'-') to any participant.
   *  This means every connected client flips automatically when SignalR broadcasts revealed data. */
  readonly isVotesRevealed = computed(() =>
    this.participants().some(p => p.voteValue !== '-' && p.voteValue !== '?')
  );

  onVoteSelected(value: string): void {
    this.currentVote.set(value);
    // TODO: emit CastVote via SignalR hub
  }

  onRevealVotes(): void {
    // TODO: emit EndRound via SignalR hub — server will broadcast actual vote values to all clients
  }

  onChangeAdmin(): void {
    // TODO: emit ClaimAdmin via SignalR hub
  }

  onTakeAdmin(): void {
    // TODO: emit ClaimAdmin via SignalR hub
  }

  onRefreshChannel(): void {
    // TODO: reconnect SignalR hub
  }

  onLogout(): void {
    this.router.navigate(['/']);
  }
}
