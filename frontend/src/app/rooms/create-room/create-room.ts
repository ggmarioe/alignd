import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DOCUMENT } from '@angular/common';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { CreateRoomResponse, RoomsService, VoteType } from '../rooms.service';

@Component({
  selector: 'app-create-room',
  imports: [ReactiveFormsModule],
  templateUrl: './create-room.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateRoom {
  private readonly rooms = inject(RoomsService);
  private readonly document = inject(DOCUMENT);

  readonly form = new FormGroup({
    voteType: new FormControl<VoteType>(0, { nonNullable: true }),
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<CreateRoomResponse | null>(null);
  readonly copied = signal<'admin' | 'player' | null>(null);

  readonly adminLink = computed(() => {
    const r = this.result();
    if (!r) return '';
    return `${this.document.location.origin}/room/${r.roomCode}?token=${r.adminToken}`;
  });

  readonly playerLink = computed(() => {
    const r = this.result();
    if (!r) return '';
    return `${this.document.location.origin}/room/${r.roomCode}`;
  });

  selectVoteType(type: VoteType): void {
    this.form.controls.voteType.setValue(type);
  }

  submit(): void {
    if (this.loading()) return;
    this.loading.set(true);
    this.error.set(null);

    this.rooms.createRoom(this.form.controls.voteType.value).subscribe({
      next: (res) => {
        this.result.set(res);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Failed to create room. Please try again.');
        this.loading.set(false);
      },
    });
  }

  copy(field: 'admin' | 'player'): void {
    const text = field === 'admin' ? this.adminLink() : this.playerLink();
    this.document.defaultView?.navigator.clipboard.writeText(text);
    this.copied.set(field);
    setTimeout(() => this.copied.set(null), 2000);
  }

  reset(): void {
    this.result.set(null);
    this.error.set(null);
  }
}
