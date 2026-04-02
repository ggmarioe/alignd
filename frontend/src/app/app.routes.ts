import { Routes } from '@angular/router';
import { CreateRoom } from './rooms/create-room/create-room';

export const routes: Routes = [
  { path: '', component: CreateRoom },
  { path: 'room/:code', loadComponent: () => import('./rooms/voting-room/voting-room').then(m => m.VotingRoom) },
];
