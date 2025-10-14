import { ApplicationRef, Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { first } from 'rxjs';

@Component({
  selector: 'app-root',
  imports: [ RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  protected title = 'FundTracker';
  private appRef = inject(ApplicationRef);
  private splashMinTime = 1000; // minimum 1 second
  private splashStartTime = Date.now();

  ngOnInit() {
    // Splash start time
    this.splashStartTime = Date.now();

    // Wait until app is stable
    this.appRef.isStable
      .pipe(first(stable => stable))
      .subscribe(() => this.hideSplashWithMinTime());
  }

  private hideSplashWithMinTime() {
    const elapsed = Date.now() - this.splashStartTime;
    const remaining = Math.max(this.splashMinTime - elapsed, 0);

    setTimeout(() => this.hideSplash(), remaining);
  }

  private hideSplash() {
    const splash = document.getElementById('splash');
    if (splash) {
      splash.style.opacity = '0';
      setTimeout(() => splash.remove(), 300); // match CSS transition
    }
  }

}
