import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-terms-of-use',
  templateUrl: './terms-of-use.component.html',
  styleUrls: ['./terms-of-use.component.scss'],
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, RouterLink],
})
export class TermsOfUseComponent {
  lastUpdated = 'April 1, 2026';
}
