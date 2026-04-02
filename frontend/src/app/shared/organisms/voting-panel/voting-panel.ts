import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { VoteType } from '../../../rooms/rooms.service';
import { WorkItemCard } from '../../molecules/work-item-card/work-item-card';
import { VotingCardDeck } from '../../molecules/voting-card-deck/voting-card-deck';

@Component({
  selector: 'app-voting-panel',
  imports: [WorkItemCard, VotingCardDeck],
  templateUrl: './voting-panel.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VotingPanel {
  readonly workItemTitle = input<string>('');
  readonly voteType = input.required<VoteType>();
  readonly currentVote = input<string | null>(null);

  readonly voteSelected = output<string>();
}
