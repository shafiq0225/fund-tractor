import { Component, EventEmitter, Input, Output, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIcon } from '@angular/material/icon';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { Scheme } from '../../../../shared/models/Amfi/Scheme';

@Component({
  selector: 'app-scheme-list',
  standalone: true,
  imports: [CommonModule, MatIcon, MatProgressSpinner],
  templateUrl: './scheme-list.component.html',
  styleUrls: ['./scheme-list.component.scss']
})
export class SchemeListComponent implements OnChanges {
  @Input() schemes: Scheme[] = [];

  // Scheme-level toggle
  @Output() toggle = new EventEmitter<Scheme>();

  // Fund-level action button
  @Output() fundUpdate = new EventEmitter<{ fundId: string; isApproved: boolean }>();

  fundSchemesMap: Record<string, Scheme[]> = {};

  ngOnChanges(changes: SimpleChanges) {
    if (changes['schemes']) {
      this.mapSchemesByFund();
    }
  }

  private mapSchemesByFund() {
    this.fundSchemesMap = {};
    if (!this.schemes) return;
    this.schemes.forEach(s => {
      if (!this.fundSchemesMap[s.fundCode]) {
        this.fundSchemesMap[s.fundCode] = [];
      }
      this.fundSchemesMap[s.fundCode].push(s);
    });
  }

  getFunds(): string[] {
    return Object.keys(this.fundSchemesMap);
  }

  getSchemesByFund(fundId: string): Scheme[] {
    return this.fundSchemesMap[fundId] || [];
  }

  isFundActive(fundId: string): boolean {
    const fundSchemes = this.getSchemesByFund(fundId);
    return fundSchemes.every(s => s.isApproved);
  }

  isFundUpdating(fundId: string): boolean {
    const fundSchemes = this.getSchemesByFund(fundId);
    return fundSchemes.some(s => s.isUpdating);
  }

  // Emit scheme-level toggle event
  onToggle(scheme: Scheme) {
    this.toggle.emit(scheme);
  }

  // Emit fund-level toggle event
  onFundUpdateClick(fundId: string) {
    const newStatus = !this.isFundActive(fundId);
    this.fundUpdate.emit({ fundId, isApproved: newStatus });
  }
}
