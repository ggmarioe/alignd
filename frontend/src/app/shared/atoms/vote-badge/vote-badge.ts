import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type VoteBadgeState = 'idle' | 'voted' | 'revealed';

@Component({
  selector: 'app-vote-badge',
  templateUrl: './vote-badge.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VoteBadge {
  readonly value = input<string>('-');
  readonly state = input<VoteBadgeState>('idle');

  readonly cssClass = computed(() => {
    const base = 'inline-flex items-center justify-center min-w-[2rem] px-2 py-0.5 rounded-full border text-sm font-medium';
    switch (this.state()) {
      case 'voted':    return `${base} bg-indigo-100 text-indigo-700 border-indigo-300`;
      case 'revealed': return `${base} bg-emerald-100 text-emerald-700 border-emerald-300`;
      default:         return `${base} bg-gray-100 text-gray-400 border-gray-200`;
    }
  });
}
