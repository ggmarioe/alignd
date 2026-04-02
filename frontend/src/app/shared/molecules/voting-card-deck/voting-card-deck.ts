import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { VotingCard } from '../../atoms/voting-card/voting-card';
import { VoteType } from '../../../rooms/rooms.service';

const FIBONACCI_CARDS = ['0', '1', '2', '3', '5', '8', '13', '21', '?', '☕'];
const TSHIRT_CARDS = ['XS', 'S', 'M', 'L', 'XL', 'XXL', '?', '☕'];

@Component({
  selector: 'app-voting-card-deck',
  imports: [VotingCard],
  templateUrl: './voting-card-deck.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VotingCardDeck {
  readonly voteType = input.required<VoteType>();
  readonly currentVote = input<string | null>(null);

  readonly voteSelected = output<string>();

  readonly cards = computed(() =>
    this.voteType() === 0 ? FIBONACCI_CARDS : TSHIRT_CARDS
  );
}
