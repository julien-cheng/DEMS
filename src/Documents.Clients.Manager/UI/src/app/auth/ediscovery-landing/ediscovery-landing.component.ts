import { Component, OnInit } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService, LoadingService, AppConfigService } from '../../shared/index';

@Component({
  selector: 'app-ediscovery-landing',
  templateUrl: './ediscovery-landing.component.html',
  styleUrls: ['./ediscovery-landing.component.scss']
})
export class EdiscoveryLandingComponent implements OnInit {
  public authenticateUserForm: FormGroup;
  public authenticateInvalid = false;
  public isSaving = false;
  private token: string;
  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router,
    private formBuilder: FormBuilder,
    private loadingService: LoadingService,
    public appConfigService: AppConfigService
  ) {
    this.route.params.subscribe(params => {
      this.token = params.token;
    });
  }

  ngOnInit() {
    this.authenticateUserForm = this.formBuilder.group({
      email: '',
      password: ''
    });
    this.loadingService.setLoading(false);
  }

  authenticateRecipient(formValues) {
    this.isSaving = true;
    formValues.token = this.token;
    this.authService.ediscoveryAuthenticateUser(formValues).subscribe(
      resp => {
        if (resp.response.isAuthenticated) {
          this.appConfigService.setAPIConfiguration();
          this.router.navigate([
            '/manager/',
            resp.response.folderIdentifier.organizationKey,
            resp.response.folderIdentifier.folderKey,
            'eDiscovery'
          ]);
        } else {
          this.authenticateInvalid = true;
          this.authenticateUserForm.reset();
        }
      },
      error => {
        console.error(error);
      },
      () => {
        this.isSaving = false;
      }
    );
  }

  cancel() {
    // Reset form
    this.authenticateUserForm.reset();
    this.authenticateInvalid = false;
  }
}
