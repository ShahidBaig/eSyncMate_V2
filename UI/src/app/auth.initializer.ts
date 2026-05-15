import { firstValueFrom } from 'rxjs';
import { ApiService } from './services/api.service';

export function authInitializerFactory(api: ApiService): () => Promise<void> {
  return async () => {
    if (api.isLoggedIn()) return;
    try {
      const res: any = await firstValueFrom(api.refreshToken());
      if (res?.token && res.token !== 'Invalid') {
        api.saveToken(res.token);
        if (res.menus) api.saveUserMenus(res.menus);
      }
    } catch {
      // No valid cookie — auth guards will redirect to login
    }
  };
}
