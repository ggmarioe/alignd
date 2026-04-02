import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Participant } from '../../models';
import { VoteBadge, VoteBadgeState } from '../../atoms/vote-badge/vote-badge';
import { ActionButton } from '../../atoms/action-button/action-button';

@Component({
  selector: 'app-participant-row',
  imports: [VoteBadge, ActionButton],
  templateUrl: './participant-row.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ParticipantRow {
  readonly participant = input.required<Participant>();
  readonly isCurrentUserAdmin = input(false);
  readonly isSelf = input(false);
  readonly canReveal = input(false);
  readonly votesRevealed = input(false);

  readonly revealVotes = output<void>();
  readonly changeAdmin = output<void>();

  readonly badgeState = computed<VoteBadgeState>(() => {
    const v = this.participant().voteValue;
    if (v === '-') return 'idle';
    if (this.votesRevealed()) return 'revealed';
    // Own vote shown before reveal: actual value but 'voted' style, not 'revealed'
    if (v === '?' || this.isSelf()) return 'voted';
    return 'revealed';
  });

  readonly showAdminControls = computed(
    () => this.participant().isAdmin && this.isCurrentUserAdmin()
  );
}
