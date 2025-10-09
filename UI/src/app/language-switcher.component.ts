import { Component } from '@angular/core';
import { LanguageService } from './services/language.service';

@Component({
  selector: 'app-language-switcher',
  template: `
    <select (change)="changeLanguage($event.target.value)" [value]="languageService.currentLanguage">
      <option value="en">English</option>
      <option value="es">Spanish</option>
    </select>
  `
})
export class LanguageSwitcherComponent {
  constructor(public languageService: LanguageService) {}

  changeLanguage(language: string): void {
    this.languageService.setLanguage(language);
  }
}
