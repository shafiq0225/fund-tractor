import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule, DecimalPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatRadioModule } from '@angular/material/radio';
import { AuthService, User } from '../../../core/services/auth.service';
import { InvestmentService } from '../../../core/services/investment.service';
import { BreadcrumbComponent } from "../../../shared/components/breadcrumb/breadcrumb.component";

// Define interface for form fields
interface FormField {
  label: string;
  control: AbstractControl | null;
}

// Define interface for investor
interface Investor {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
}

// Define interface for fund scheme with NAV rate
interface FundScheme {
  code: string;
  name: string;
  currentNav: number;
}

@Component({
  selector: 'app-create-investment',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    BreadcrumbComponent
  ],
  providers: [DecimalPipe],
  templateUrl: './create-investment.component.html',
  styleUrls: ['./create-investment.component.scss']
})
export class CreateInvestmentComponent implements OnInit {
  investmentForm: FormGroup;
  isSubmitting = false;
  calculatedUnits = 0;
  currentUser: User | null = null;
  isLoadingInvestors = false;
  selectedFile: File | null = null;
  imagePreview: string | ArrayBuffer | null = null;

  @ViewChild('fileInput') fileInput!: ElementRef;

  // Form fields for validation status with proper typing
  formFields: FormField[] = [
    { label: 'Investor Selection', control: null },
    { label: 'Scheme Selection', control: null },
    { label: 'NAV Rate', control: null },
    { label: 'Investment Amount', control: null },
    { label: 'Purchase Date', control: null },
    { label: 'Investment Mode', control: null },
    { label: 'Document Upload', control: null }
  ];

  // Mock data for investors - replace with API call
  investors: Investor[] = [
    { id: 1, firstName: 'Rajesh', lastName: 'Kumar', email: 'rajesh.kumar@email.com' },
    { id: 2, firstName: 'Priya', lastName: 'Sharma', email: 'priya.sharma@email.com' },
    { id: 3, firstName: 'Amit', lastName: 'Verma', email: 'amit.verma@email.com' },
    { id: 4, firstName: 'Sneha', lastName: 'Patel', email: 'sneha.patel@email.com' },
    { id: 5, firstName: 'Vikram', lastName: 'Singh', email: 'vikram.singh@email.com' }
  ];

  // Mock data for fund schemes with NAV rates
  fundSchemes: FundScheme[] = [
    { code: 'SBI001', name: 'SBI Equity Hybrid Fund - Direct Plan Growth', currentNav: 125.4567 },
    { code: 'HDFC002', name: 'HDFC Balanced Advantage Fund - Direct Growth', currentNav: 89.1234 },
    { code: 'ICICI003', name: 'ICICI Prudential Bluechip Fund - Direct Growth', currentNav: 156.7890 },
    { code: 'AXIS004', name: 'Axis Long Term Equity Fund - Direct Plan Growth', currentNav: 67.8912 },
    { code: 'KOTAK005', name: 'Kotak Emerging Equity Fund - Direct Growth', currentNav: 234.5678 }
  ];

  modeOfInvestmentOptions = [
    { value: 'online', label: 'Online', icon: 'public', description: 'Digital investment through online platforms' },
    { value: 'offline', label: 'Offline', icon: 'store', description: 'Traditional investment through branches' }
  ];

  constructor(
    private fb: FormBuilder,
    private investmentService: InvestmentService,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar,
    private decimalPipe: DecimalPipe
  ) {
    this.investmentForm = this.createForm();
  }

  ngOnInit(): void {
    this.loadCurrentUser();
    this.loadInvestors();
    this.setupFormFields();
    this.setDefaultDate();
  }
  // Drag and drop handlers
  isDragOver = false;

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.onFileSelected({ target: { files: [files[0]] } });
    }
  }
  private formatNumber(value: number | null | undefined, format: string = '1.4-4'): string {
    if (value === null || value === undefined) return '0.0000';
    return this.decimalPipe.transform(value, format) || '0.0000';
  }

  // Add this method to your component class
  getSelectedSchemeCode(): string {
    const schemeCode = this.investmentForm.get('schemeCode')?.value;
    if (schemeCode) {
      const selectedScheme = this.fundSchemes.find(scheme => scheme.code === schemeCode);
      return selectedScheme ? selectedScheme.code : 'Not selected';
    }
    return 'Not selected';
  }
  // Your existing formatNavRate method should work, but here it is for reference:
  formatNavRate(navRate: number): string {
    if (!navRate) return '0.0000';
    return this.decimalPipe.transform(navRate, '1.4-4') || '0.0000';
  }
  // Summary items for sidebar
  get summaryItems(): any[] {
    return [
      {
        label: 'Investor',
        value: this.getSelectedInvestorName(),
        icon: 'person',
        valueClass: 'text-sm'
      },
      {
        label: 'Mode',
        value: this.modeOfInvestment?.value ? this.modeOfInvestment.value.charAt(0).toUpperCase() + this.modeOfInvestment.value.slice(1) : 'Not selected',
        icon: 'swap_horiz',
        valueClass: 'text-sm capitalize'
      },
      {
        label: 'Amount',
        value: `₹${this.investmentForm.get('investAmount')?.value || '0.00'}`,
        icon: 'payments',
        valueClass: 'text-lg'
      },
      {
        label: 'NAV Rate',
        value: this.formatNumber(this.investmentForm.get('navRate')?.value),
        icon: 'show_chart',
        valueClass: 'text-sm'
      },
      {
        label: 'Units',
        value: this.formatNumber(this.calculatedUnits), // Clean and readable
        icon: 'pie_chart',
        valueClass: 'text-sm'
      },
      {
        label: 'Document',
        value: this.selectedFile ? 'Uploaded' : 'Required',
        icon: 'description',
        valueClass: this.selectedFile ? 'text-green-300 text-sm' : 'text-red-300 text-sm'
      }
    ];
  }

  private loadCurrentUser(): void {
    this.currentUser = this.authService.getCurrentUser();
    console.log('Current User:', this.currentUser);
  }

  private loadInvestors(): void {
    this.isLoadingInvestors = true;

    // Simulate API call to load investors
    setTimeout(() => {
      this.isLoadingInvestors = false;
    }, 1000);
  }

  private setupFormFields(): void {
    // Initialize form field controls with proper typing
    this.formFields = [
      { label: 'Investor Selection', control: this.investmentForm.get('investorId') },
      { label: 'Scheme Selection', control: this.investmentForm.get('schemeCode') },
      { label: 'NAV Rate', control: this.investmentForm.get('navRate') },
      { label: 'Investment Amount', control: this.investmentForm.get('investAmount') },
      { label: 'Purchase Date', control: this.investmentForm.get('dateOfPurchase') },
      { label: 'Investment Mode', control: this.investmentForm.get('modeOfInvestment') },
      { label: 'Document Upload', control: this.investmentForm.get('imageFile') }
    ];
  }

  private setDefaultDate(): void {
    // Set default date to today
    const today = new Date();
    this.investmentForm.patchValue({
      dateOfPurchase: today
    });
  }

  private createForm(): FormGroup {
    return this.fb.group({
      investorId: ['', [Validators.required]],
      schemeCode: ['', [Validators.required]],
      schemeName: ['', [Validators.required]],
      navRate: [{ value: '', disabled: false }, [Validators.required, Validators.min(0.0001)]],
      dateOfPurchase: ['', [Validators.required]],
      investAmount: ['', [Validators.required, Validators.min(0.01)]],
      modeOfInvestment: ['online', [Validators.required]], // Default to online
      remarks: [''],
      imageFile: [null, [Validators.required]] // Made mandatory
    });
  }

  onInvestorChange(event: any): void {
    // Investor selection logic if needed
  }

  onSchemeChange(event: any): void {
    const selectedSchemeCode = event.value;
    const selectedScheme = this.fundSchemes.find(scheme => scheme.code === selectedSchemeCode);

    if (selectedScheme) {
      this.investmentForm.patchValue({
        schemeName: selectedScheme.name,
        navRate: selectedScheme.currentNav
      });

      // Recalculate units if investment amount exists
      this.calculateUnits();
    }
  }

  calculateUnits(): void {
    const navRate = this.investmentForm.get('navRate')?.value;
    const investAmount = this.investmentForm.get('investAmount')?.value;

    if (navRate && investAmount && navRate > 0 && investAmount > 0) {
      this.calculatedUnits = investAmount / navRate;
    } else {
      this.calculatedUnits = 0;
    }
  }

  triggerFileInput(): void {
    if (this.fileInput && this.fileInput.nativeElement) {
      this.fileInput.nativeElement.click();
    }
  }

  onFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      // Check file type
      const validTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'application/pdf'];
      if (!validTypes.includes(file.type)) {
        this.snackBar.open('Please select a valid image or PDF file', 'Close', {
          duration: 3000,
          panelClass: ['error-snackbar']
        });
        return;
      }

      // Check file size (5MB max)
      if (file.size > 5 * 1024 * 1024) {
        this.snackBar.open('File size should be less than 5MB', 'Close', {
          duration: 3000,
          panelClass: ['error-snackbar']
        });
        return;
      }

      this.selectedFile = file;

      // Create preview for images
      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => {
          this.imagePreview = reader.result;
        };
        reader.readAsDataURL(file);
      } else {
        this.imagePreview = null;
      }

      this.investmentForm.patchValue({
        imageFile: file
      });
    }
  }

  removeImage(): void {
    this.selectedFile = null;
    this.imagePreview = null;
    this.investmentForm.patchValue({
      imageFile: null
    });
    // Reset file input
    if (this.fileInput && this.fileInput.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
  }

  clearForm(): void {
    this.investmentForm.reset();
    this.calculatedUnits = 0;
    this.selectedFile = null;
    this.imagePreview = null;
    this.setDefaultDate();
    // Set default mode to online
    this.investmentForm.patchValue({
      modeOfInvestment: 'online'
    });
    this.snackBar.open('Form cleared', 'Close', {
      duration: 2000,
      panelClass: ['info-snackbar']
    });
  }

  onCancel(): void {
    this.router.navigate(['/portfolio']);
  }

  onSubmit(): void {
    if (this.investmentForm.valid && !this.isSubmitting) {
      this.isSubmitting = true;

      const formData = new FormData();

      // Append form data
      Object.keys(this.investmentForm.controls).forEach(key => {
        const control = this.investmentForm.get(key);
        if (control && key !== 'imageFile') {
          formData.append(key, control.value);
        }
      });

      // Append file if selected
      if (this.selectedFile) {
        formData.append('imageFile', this.selectedFile);
      }

      console.log('Submitting investment:', formData);

      // Simulate API call - replace with actual service call
      setTimeout(() => {
        this.isSubmitting = false;
        this.snackBar.open('Investment created successfully!', 'Close', {
          duration: 5000,
          panelClass: ['success-snackbar']
        });
        this.router.navigate(['/portfolio']);
      }, 2000);
    } else {
      // Mark all fields as touched to show validation errors
      Object.keys(this.investmentForm.controls).forEach(key => {
        this.investmentForm.get(key)?.markAsTouched();
      });

      this.snackBar.open('Please fill all required fields correctly', 'Close', {
        duration: 5000,
        panelClass: ['error-snackbar']
      });
    }
  }

  // Safe method to check if control is valid
  isControlValid(control: AbstractControl | null): boolean {
    return control ? control.valid : false;
  }

  // Get display name for investor
  getInvestorDisplayName(investor: Investor): string {
    return `${investor.firstName} ${investor.lastName}`;
  }

  // Get selected investor name
  getSelectedInvestorName(): string {
    const investorId = this.investmentForm.get('investorId')?.value;
    const investor = this.investors.find(inv => inv.id === investorId);
    return investor ? `${investor.firstName} ${investor.lastName}` : 'Not selected';
  }

  // Format currency for display
  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    }).format(amount);
  }

  // Format NAV rate for display
  // formatNavRate(navRate: number): string {
  //   return new Intl.NumberFormat('en-IN', {
  //     minimumFractionDigits: 4,
  //     maximumFractionDigits: 4
  //   }).format(navRate);
  // }

  // Getters for form controls for easy template access
  get investorId() { return this.investmentForm.get('investorId'); }
  get schemeCode() { return this.investmentForm.get('schemeCode'); }
  get schemeName() { return this.investmentForm.get('schemeName'); }
  get navRate() { return this.investmentForm.get('navRate'); }
  get dateOfPurchase() { return this.investmentForm.get('dateOfPurchase'); }
  get investAmount() { return this.investmentForm.get('investAmount'); }
  get modeOfInvestment() { return this.investmentForm.get('modeOfInvestment'); }
  get remarks() { return this.investmentForm.get('remarks'); }
  get imageFile() { return this.investmentForm.get('imageFile'); }

  // Helper method to check if form is partially filled
  isFormDirty(): boolean {
    return this.investmentForm.dirty;
  }
  
  getFormCompletion(): number {
  const requiredFields = [
    'investorId',
    'schemeCode', 
    'navRate',
    'investAmount',
    'dateOfPurchase',
    'modeOfInvestment',
    'imageFile'
  ];

  let filledFields = 0;

  requiredFields.forEach(field => {
    const control = this.investmentForm.get(field);
    if (control && control.value && control.valid) {
      filledFields++;
    }
  });

  const percentage = Math.round((filledFields / requiredFields.length) * 100);
  return Math.min(percentage, 100);
}

  // Add these methods to your component

  // Add this method to handle quick amount selection
  setQuickAmount(amount: number): void {
    this.investmentForm.patchValue({
      investAmount: amount
    });
    this.calculateUnits();
  }
  // Clear calculation
  clearCalculation(): void {
    this.investmentForm.patchValue({
      investAmount: null
    });
    this.calculatedUnits = 0;
  }

  // Get allocation percentage for visualization
  getAllocationPercentage(): number {
    if (!this.calculatedUnits) return 0;
    // This can be enhanced based on your business logic
    return Math.min(this.calculatedUnits * 100, 100);
  }

  // Real-time calculation on input
  onAmountInput(): void {
    // Calculate units as user types (optional - can be performance intensive)
    setTimeout(() => {
      this.calculateUnits();
    }, 300);
  }
}