import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'danger';

@Component({
  selector: 'app-action-button',
  templateUrl: './action-button.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActionButton {
  readonly variant = input<ButtonVariant>('primary');
  readonly disabled = input(false);
  readonly type = input<'button' | 'submit'>('button');

  readonly cssClass = computed(() => {
    const base = 'inline-flex items-center justify-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';
    const variants: Record<ButtonVariant, string> = {
      primary:   'bg-indigo-600 text-white hover:bg-indigo-700 focus-visible:ring-indigo-500',
      secondary: 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50 focus-visible:ring-gray-400',
      ghost:     'text-gray-600 hover:bg-gray-100 focus-visible:ring-gray-400',
      danger:    'bg-red-600 text-white hover:bg-red-700 focus-visible:ring-red-500',
    };
    return `${base} ${variants[this.variant()]}`;
  });
}
