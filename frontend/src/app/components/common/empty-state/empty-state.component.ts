import { Component, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatButton } from '@angular/material/button';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [MatIcon, MatButton],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.css'
})
export class EmptyStateComponent {
  // Inputs
  icon = input<string>('info');
  title = input.required<string>();
  description = input<string>();
  actionLabel = input<string>();

  // Output
  action = output<void>();

  onActionClick() {
    this.action.emit();
  }
}
