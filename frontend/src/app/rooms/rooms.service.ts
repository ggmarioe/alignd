import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface CreateRoomResponse {
  roomCode: string;
  adminToken: string;
}

// Matches backend VoteType enum: Fibonacci = 0, ShirtSize = 1
export type VoteType = 0 | 1;

@Injectable({ providedIn: 'root' })
export class RoomsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  createRoom(voteType: VoteType): Observable<CreateRoomResponse> {
    return this.http
      .post<{ data: CreateRoomResponse }>(`${this.baseUrl}/rooms`, { voteType })
      .pipe(map(res => res.data));
  }
}
