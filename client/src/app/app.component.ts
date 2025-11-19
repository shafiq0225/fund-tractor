import { Component, inject, OnInit, OnDestroy, ApplicationRef } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { AuthService } from './core/services/auth.service';
import { filter, first } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit, OnDestroy {
  protected title = 'FundTracker';
  private appRef = inject(ApplicationRef);
  private router = inject(Router);
  private authService = inject(AuthService);
  
  private splashMinTime = 1000;
  private splashStartTime = Date.now();
  private routerSub!: Subscription;

  ngOnInit() {
    this.splashStartTime = Date.now();

    // FIX: Use a more reliable way to hide splash
    this.hideSplashWithMinTime();

    // Additional: Listen to router events to detect if app is really working
    this.routerSub = this.router.events
      .pipe(filter(event => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        console.log('✅ Router navigation completed:', event.url);
        
        // If we're still showing splash after navigation, force hide it
        setTimeout(() => {
          this.forceHideSplash();
        }, 500);
      });
  }

  ngOnDestroy() {
    if (this.routerSub) {
      this.routerSub.unsubscribe();
    }
  }

  private hideSplashWithMinTime() {
    const elapsed = Date.now() - this.splashStartTime;
    const remaining = Math.max(this.splashMinTime - elapsed, 0);

    setTimeout(() => {
      this.hideSplash();
    }, remaining);
  }

  private hideSplash() {
    const splash = document.getElementById('splash');
    if (splash) {
      console.log('🎬 Hiding splash screen');
      splash.style.opacity = '0';
      setTimeout(() => {
        splash.remove();
        console.log('✅ Splash screen removed');
      }, 300);
    } else {
      console.log('ℹ️ Splash element not found');
    }
  }

  // Emergency method to force hide splash
  private forceHideSplash() {
    const splash = document.getElementById('splash');
    if (splash) {
      console.log('🆘 Force hiding splash screen');
      splash.style.display = 'none';
      splash.remove();
    }
  }
}