import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Participant } from '../../models';
import { ParticipantRow } from '../../molecules/participant-row/participant-row';
import { ActionButton } from '../../atoms/action-button/action-button';
import { VoteChart } from '../../molecules/vote-chart/vote-chart';

@Component({
  selector: 'app-participants-panel',
  imports: [ParticipantRow, ActionButton, VoteChart],
  templateUrl: './participants-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParticipantsPanel {
  readonly participants = input.required<Participant[]>();
  readonly isAdminPresent = input(true);
  readonly isCurrentUserAdmin = input(false);
  readonly currentUserId = input('');
  readonly currentVote = input<string | null>(null);
  readonly votesRevealed = input(false);

  readonly takeAdmin = output<void>();
  readonly revealVotes = output<void>();
  readonly changeAdmin = output<void>();

  /** Participants with the current user's actual vote value substituted in. */
  readonly displayedParticipants = computed(() =>
    this.participants().map(p =>
      p.id === this.currentUserId() && this.currentVote() !== null
        ? { ...p, voteValue: this.currentVote()! }
        : p
    )
  );

  /** True once ≥ floor(n/2)+1 participants have a vote registered. */
  readonly canReveal = computed(() => {
    const all = this.displayedParticipants();
    if (all.length === 0) return false;
    const voted = all.filter(p => p.voteValue !== '-').length;
    return voted >= Math.floor(all.length / 2) + 1;
  });
}
