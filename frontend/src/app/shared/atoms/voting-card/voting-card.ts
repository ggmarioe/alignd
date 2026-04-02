import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-voting-card',
  templateUrl: './voting-card.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VotingCard {
  readonly label = input.required<string>();
  readonly selected = input(false);

  readonly select = output<string>();

  readonly cssClass = computed(() => {
    const base = 'flex items-center justify-center w-14 h-20 rounded-lg border-2 text-lg font-bold transition-all duration-150 cursor-pointer select-none focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:ring-indigo-500';
    return this.selected()
      ? `${base} border-indigo-500 bg-indigo-50 text-indigo-700 shadow-md scale-105`
      : `${base} border-gray-300 bg-white text-gray-700 hover:border-indigo-300 hover:bg-indigo-50 hover:scale-105`;
  });

  onSelect(): void {
    this.select.emit(this.label());
  }
}
