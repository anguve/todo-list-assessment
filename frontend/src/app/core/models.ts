export interface CurrentUser {
  id: string;
  email: string;
  displayName: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: CurrentUser;
}

export interface TodoItem {
  id: string;
  title: string;
  description: string;
  createdAt: string;
}
