import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-user-not-found',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './user-not-found.component.html',
  styleUrls: ['./user-not-found.component.scss']
})
export class UserNotFoundComponent implements OnInit {
  panNumber: string = '';
  email: string = '';

  constructor(
    private router: Router,
    private route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.panNumber = params['panNumber'] || '';
      this.email = params['email'] || '';
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  contactAdmin(): void {
    const adminEmail = 'admin@fundtrackr.com';
    const subject = 'User Account Creation Request';
    const body = this.panNumber 
      ? `Hello Administrator,\n\nPlease create my account in the FundTrackr system.\n\nMy details:\n- PAN Number: ${this.panNumber}\n- Email: ${this.email || 'Not provided'}\n- Name: [Please provide your full name]\n\nThank you.`
      : 'Hello Administrator,\n\nPlease create my account in the FundTrackr system.\n\nThank you.';
    
    window.location.href = `mailto:${adminEmail}?subject=${encodeURIComponent(subject)}&body=${encodeURIComponent(body)}`;
  }

  goToSignup(): void {
    this.router.navigate(['/signup'], {
      queryParams: { 
        panNumber: this.panNumber,
        email: this.email 
      }
    });
  }
}