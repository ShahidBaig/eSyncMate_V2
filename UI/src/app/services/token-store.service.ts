import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenStoreService {
  private _token: string = '';

  set(token: string): void { this._token = token; }
  get(): string { return this._token; }
  clear(): void { this._token = ''; }
  has(): boolean { return !!this._token; }
}
