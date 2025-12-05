import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { UserDataService } from '../../services/userdata.service';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
  templateUrl: './access-denied.component.html',
  styleUrl: './access-denied.component.css',
})
export class AccessDeniedComponent {
  private _userDataService = inject(UserDataService);
  private _router = inject(Router);

  ngOnInit() {
    setTimeout(() => {
      this.geriDon();
    }, 3000);
  }

  geriDon() {
    if (this._userDataService.isAuthenticated()) {
      this._router.navigate([this._userDataService.redirectUrl()]);
    } else {
      this._router.navigate(['/']);
    }
  }
}
