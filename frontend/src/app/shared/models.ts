export interface Participant {
  id: string;
  name: string;
  /** '-' = not voted, '?' = voted but hidden, any other string = revealed value */
  voteValue: string;
  isAdmin: boolean;
}
