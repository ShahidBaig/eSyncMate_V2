import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Injectable({
  providedIn: 'root'
})
export class LanguageService {
  private defaultLanguage = 'en';

  constructor(private translate: TranslateService) {
    this.setInitialLanguage();
  }

  private setInitialLanguage(): void {
    const language = localStorage.getItem('language') || this.defaultLanguage;
    this.translate.setDefaultLang(language);
    this.translate.use(language);
  }

  setLanguage(language: string): void {
    this.translate.use(language);
    localStorage.setItem('language', language);
  }

  getTranslation(key: string): string {
    return this.translate.instant(key);
  }

  get currentLanguage(): string {
    return this.translate.currentLang;
  }
}
