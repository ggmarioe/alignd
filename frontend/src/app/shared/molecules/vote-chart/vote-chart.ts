import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Participant } from '../../models';

interface BarData {
  value: string;
  count: number;
  isTop: boolean;
}

const FIBONACCI_ORDER = ['0', '1', '2', '3', '5', '8', '13', '21', '?', '☕'];

@Component({
  selector: 'app-vote-chart',
  templateUrl: './vote-chart.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoteChart {
  readonly participants = input.required<Participant[]>();

  readonly chartData = computed((): BarData[] => {
    const counts = new Map<string, number>();
    for (const p of this.participants()) {
      if (p.voteValue !== '-' && p.voteValue !== '?') {
        counts.set(p.voteValue, (counts.get(p.voteValue) ?? 0) + 1);
      }
    }
    if (counts.size === 0) return [];

    const max = Math.max(...counts.values());
    return Array.from(counts.entries())
      .map(([value, count]) => ({ value, count, isTop: count === max }))
      .sort((a, b) => {
        const ai = FIBONACCI_ORDER.indexOf(a.value);
        const bi = FIBONACCI_ORDER.indexOf(b.value);
        return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi);
      });
  });

  readonly maxCount = computed(() =>
    Math.max(...this.chartData().map(d => d.count), 1)
  );
}
