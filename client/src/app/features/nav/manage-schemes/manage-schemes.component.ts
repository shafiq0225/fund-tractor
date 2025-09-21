import { Component, OnInit } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { ActivatedRoute, Router } from '@angular/router';
import { SchemeListComponent } from "./scheme-list/scheme-list.component";
import { MatTooltip } from '@angular/material/tooltip';
import { Scheme } from '../../../shared/models/Amfi/Scheme';
import { AmfiService } from '../../../core/services/amfi.service';
import { CommonModule } from '@angular/common';
import { MatProgressBarModule } from '@angular/material/progress-bar';

@Component({
  selector: 'app-manage-schemes',
  imports: [CommonModule, MatIcon, SchemeListComponent, MatTooltip, MatProgressBarModule],
  templateUrl: './manage-schemes.component.html',
  styleUrl: './manage-schemes.component.scss'
})
export class ManageSchemesComponent implements OnInit {
  constructor(private router: Router, private route: ActivatedRoute, private amfiService: AmfiService) { }
  schemes: Scheme[] = [];
  loading = true;
  errorMessage: string | null = null;


  ngOnInit(): void {
    this.fetchSchemes();
  }

  goToDashboard() {
    this.router.navigate(['../'], { relativeTo: this.route });
  }

  fetchSchemes() {
    this.amfiService.getSchemes().subscribe({
      next: (res) => {
        this.schemes = res.data;
        console.log(this.schemes);
        
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Failed to load schemes.';
        this.loading = false;
      }
    });
  }



  // this.schemes = [
  //   {
  //           "id": 1,
  //           "fundCode": "CanaraRobeco_MF",
  //           "schemeCode": "150504",
  //           "schemeName": "Canara Robeco Banking and PSU Debt Fund- Regular Plan- IDCW (Payout/ Reinvestment)",
  //           "isApproved": false,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 2,
  //           "fundCode": "DSP_MF",
  //           "schemeCode": "124175",
  //           "schemeName": "DSP Banking & PSU Debt Fund - Direct Plan - Growth",
  //           "isApproved": true,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 3,
  //           "fundCode": "DSP_MF",
  //           "schemeCode": "124172",
  //           "schemeName": "DSP Banking & PSU Debt Fund - Regular Plan - Growth",
  //           "isApproved": false,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 4,
  //           "fundCode": "FranklinTempleton_MF",
  //           "schemeCode": "129008",
  //           "schemeName": "Franklin India Banking & PSU Debt Fund - Direct - Growth",
  //           "isApproved": true,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 5,
  //           "fundCode": "FranklinTempleton_MF",
  //           "schemeCode": "129006",
  //           "schemeName": "Franklin India Banking & PSU Debt Fund - Growth",
  //           "isApproved": true,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 6,
  //           "fundCode": "FranklinTempleton_MF",
  //           "schemeCode": "129009",
  //           "schemeName": "Franklin India Banking and PSU Debt Fund - Direct - IDCW",
  //           "isApproved": true,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 7,
  //           "fundCode": "FranklinTempleton_MF",
  //           "schemeCode": "129007",
  //           "schemeName": "Franklin India Banking and PSU Debt Fund - IDCW",
  //           "isApproved": true,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 8,
  //           "fundCode": "NipponIndia_MF",
  //           "schemeCode": "118650",
  //           "schemeName": "Nippon India Multi Cap Fund - Direct Plan Growth Plan - Growth Option",
  //           "isApproved": true,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:28:17.14",
  //           "lastUpdatedDate": "2025-09-19T09:28:17.14"
  //       },
  //       {
  //           "id": 9,
  //           "fundCode": "TestFund",
  //           "schemeCode": "Test123",
  //           "schemeName": "Test xxx",
  //           "isApproved": false,
  //           "approvedName": "Shafiq",
  //           "createdAt": "2025-09-19T09:34:02.5468492",
  //           "lastUpdatedDate": "2025-09-19T09:47:10.8654769"
  //       },
  // ];

  toggleStatus(scheme: any) {
    scheme.status = !scheme.status;
  }
  openAddSchemeModal() {

  }

  onSchemeToggle(scheme: any) {
    console.log('Toggled:', scheme);
    // here you could call API to persist
  }

}
