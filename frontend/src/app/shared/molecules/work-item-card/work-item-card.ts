import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { VoteType } from '../../../rooms/rooms.service';

@Component({
  selector: 'app-work-item-card',
  templateUrl: './work-item-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkItemCard {
  readonly title = input<string>('');
  readonly currentVote = input<string | null>(null);
  readonly voteType = input.required<VoteType>();
}
