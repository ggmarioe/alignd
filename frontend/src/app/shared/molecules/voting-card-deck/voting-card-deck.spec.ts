import { render, screen } from '@testing-library/angular';
import { userEvent } from '@testing-library/user-event';
import { VotingCardDeck } from './voting-card-deck';

const EXPECTED_FIBONACCI_CARDS = ['0', '1', '2', '3', '5', '8', '13', '21', '?', '☕'];
const REMOVED_CARDS = ['34', '55', '89'];

describe('VotingCardDeck — Fibonacci', () => {
  it('renders all 10 Fibonacci cards', async () => {
    await render(VotingCardDeck, { inputs: { voteType: 0, currentVote: null } });

    for (const value of EXPECTED_FIBONACCI_CARDS) {
      expect(screen.getByRole('button', { name: `Vote ${value}` })).toBeInTheDocument();
    }
  });

  it('does not render cards with value greater than 21', async () => {
    await render(VotingCardDeck, { inputs: { voteType: 0, currentVote: null } });

    for (const value of REMOVED_CARDS) {
      expect(screen.queryByRole('button', { name: `Vote ${value}` })).not.toBeInTheDocument();
    }
  });

  it('emits the selected value when a card is clicked', async () => {
    const user = userEvent.setup();
    const voteSelected = vi.fn();

    await render(VotingCardDeck, {
      inputs: { voteType: 0, currentVote: null },
      on: { voteSelected },
    });

    await user.click(screen.getByRole('button', { name: 'Vote 8' }));

    expect(voteSelected).toHaveBeenCalledOnce();
    expect(voteSelected).toHaveBeenCalledWith('8');
  });

  it('marks the current vote card as selected', async () => {
    await render(VotingCardDeck, { inputs: { voteType: 0, currentVote: '13' } });

    const selected = screen.getByRole('button', { name: 'Vote 13' });
    expect(selected).toHaveAttribute('aria-pressed', 'true');
  });
});

describe('VotingCardDeck — T-shirt sizes', () => {
  it('renders T-shirt size cards when voteType is 1', async () => {
    await render(VotingCardDeck, { inputs: { voteType: 1, currentVote: null } });

    for (const value of ['XS', 'S', 'M', 'L', 'XL', 'XXL']) {
      expect(screen.getByRole('button', { name: `Vote ${value}` })).toBeInTheDocument();
    }
  });

  it('does not render Fibonacci cards when voteType is 1', async () => {
    await render(VotingCardDeck, { inputs: { voteType: 1, currentVote: null } });

    expect(screen.queryByRole('button', { name: 'Vote 8' })).not.toBeInTheDocument();
  });
});
