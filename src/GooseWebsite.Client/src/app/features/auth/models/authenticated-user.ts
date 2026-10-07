// Shares the session identity shape between auth requests and UI consumers.
export interface AuthenticatedUser {
  displayName: string;
  isAdmin: boolean;
}