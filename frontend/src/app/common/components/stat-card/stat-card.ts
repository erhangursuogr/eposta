import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  templateUrl: './stat-card.html',
  styleUrl: './stat-card.css'
})
export class StatCardComponent {
  // Inputs using new signal-based input API
  title = input.required<string>();
  value = input.required<number | string>();
  icon = input.required<string>();
  color = input<string>('primary'); // primary, accent, warn, success, info
  subtitle = input<string>('');
  trend = input<number | null>(null); // positive or negative percentage
  loading = input<boolean>(false);
  clickable = input<boolean>(false); // Tıklanabilir mi?

  // Output event for click
  cardClick = output<void>();

  onCardClick(): void {
    if (this.clickable() && !this.loading()) {
      this.cardClick.emit();
    }
  }
}
